using MongoDB.Driver;
using HPParkingSystem.Models.Data;
using HPParkingSystem.Models.Entities;
using HPParkingSystem.Models.Enums;
using HPParkingSystem.Models.ValueObjects;
using HPParkingSystem.Repositories;
using HPParkingSystem.Services.Anpr;
using HPParkingSystem.Services.Devices.Camera;
using HPParkingSystem.Services.Devices.Health;
using HPParkingSystem.Services.Logging;
using HPParkingSystem.Services.Offline;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace HPParkingSystem.Services.Parking
{
    public class ParkingLaneService : IParkingLaneService
    {
        private readonly IRepository<ParkingSession> _sessionRepo;
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IRepository<Person> _personRepo;
        private readonly IRepository<Department> _departmentRepo;
        private readonly IRepository<Device>? _deviceRepo;
        private readonly IRepository<Lane>? _laneRepo;
        private readonly IPlateRecognitionService _anprService;
        private readonly IDeviceHealthMonitorService? _healthService;
        private readonly IServerHealthTracker _healthTracker;

        // Bộ nhớ đệm chống chụp chéo 2 làn cạnh nhau: Plate -> (Thời điểm xử lý, Loại làn "IN" | "OUT")
        private readonly ConcurrentDictionary<string, (DateTime ProcessTime, string LaneType)> _lastProcessedPlates = new();
        private const int CROSS_LANE_INTERVAL_SECONDS = 15;

        public ParkingLaneService(
            IRepository<ParkingSession> sessionRepo,
            IRepository<Vehicle> vehicleRepo,
            IRepository<Person> personRepo,
            IRepository<Department> departmentRepo,
            IPlateRecognitionService anprService,
            IDeviceHealthMonitorService? healthService = null,
            IRepository<Device>? deviceRepo = null,
            IRepository<Lane>? laneRepo = null,
            IServerHealthTracker? healthTracker = null)
        {
            _sessionRepo = sessionRepo ?? throw new ArgumentNullException(nameof(sessionRepo));
            _vehicleRepo = vehicleRepo ?? throw new ArgumentNullException(nameof(vehicleRepo));
            _personRepo = personRepo ?? throw new ArgumentNullException(nameof(personRepo));
            _departmentRepo = departmentRepo ?? throw new ArgumentNullException(nameof(departmentRepo));
            _anprService = anprService ?? throw new ArgumentNullException(nameof(anprService));
            _healthService = healthService;
            _deviceRepo = deviceRepo;
            _laneRepo = laneRepo;
            _healthTracker = healthTracker ?? ServerHealthTracker.Instance;
        }

        public async Task<LaneProcessResult> ProcessInLaneAsync(
            string inLaneName,
            ICameraService? plateCam,
            ICameraService? overviewCam,
            string? plateDeviceId = null,
            string? overviewDeviceId = null,
            string triggerSource = "RADAR",
            string captureDir = "")
        {
            var result = new LaneProcessResult
            {
                ProcessedTime = DateTime.Now
            };

            // 0. Xác định tên làn chính xác từ bảng Lanes nếu chưa có
            if (string.IsNullOrWhiteSpace(inLaneName) && _laneRepo != null)
            {
                try
                {
                    var lane = await _laneRepo.FindOneAsync(l => l.Direction == LaneDirection.In && !l.IsDeleted && l.IsActive);
                    if (lane != null && !string.IsNullOrWhiteSpace(lane.Name))
                    {
                        inLaneName = lane.Name;
                    }
                }
                catch { }
            }
            if (string.IsNullOrWhiteSpace(inLaneName)) inLaneName = "Làn Vào";

            // 1. Chuẩn bị thư mục lưu trữ ảnh (tự động fallback sang OfflineCaptures nếu server mất mạng)
            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var captureContext = PrepareCapturePaths(captureDir, timeStamp, triggerSource, "in");

            // 2. Chụp ảnh song song với cơ chế chống chịu lỗi (Graceful Degradation & Retry cục bộ)
            var captureResult = await CaptureSnapshotsAsync(
                plateCam, overviewCam, captureContext, plateDeviceId, overviewDeviceId);

            string filePlate = captureResult.PlateImagePath;
            string fileOverview = captureResult.OverviewImagePath;
            bool plateOk = captureResult.PlateCamSuccess;
            bool ovwOk = captureResult.OverviewCamSuccess;
            result.PlateCamSuccess = plateOk;
            result.OverviewCamSuccess = ovwOk;
            result.PlateImagePath = plateOk ? filePlate : null;
            result.OverviewImagePath = ovwOk ? fileOverview : null;

            // 3. Nhận diện biển số (ANPR)
            string detectedPlate = PlateNumber.Unrecognized;
            Bitmap? croppedBmp = null;
            if (plateOk)
            {
                try
                {
                    var anpr = await _anprService.RecognizeAsync(filePlate);
                    if (anpr != null && anpr.IsSuccess && !string.IsNullOrWhiteSpace(anpr.FormattedPlate))
                    {
                        detectedPlate = PlateNumber.Clean(anpr.FormattedPlate);
                        // Chỉ giữ Bitmap trong bộ nhớ để hiển thị UI, KHÔNG lưu file ảnh crop ra đĩa
                        croppedBmp = anpr.CroppedPlateImage;
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, $"[LÀN VÀO] Lỗi trong quá trình nhận diện ANPR: {ex.Message}", "ParkingLaneService");
                }
            }

            // Nếu nhận diện không thành công: Không lưu ảnh, không lưu DB, trả về thất bại
            if (PlateNumber.IsUnrecognized(detectedPlate) || string.IsNullOrWhiteSpace(detectedPlate))
            {
                AppLogger.Information($"[LÀN VÀO] Nhận diện biển số không thành công. Hủy lưu ảnh và không tạo phiên.", "ParkingLaneService");
                DeleteFileSafe(filePlate);
                DeleteFileSafe(fileOverview);
                croppedBmp?.Dispose();

                result.PlateNumber = PlateNumber.Unrecognized;
                result.CroppedPlateImage = null;
                result.PlateImagePath = null;
                result.OverviewImagePath = null;
                result.Success = false;
                result.ErrorMessage = "Nhận diện biển số không thành công.";
                return result;
            }

            result.PlateNumber = detectedPlate;
            result.CroppedPlateImage = croppedBmp;
            result.PlateCropImagePath = null; // Không lưu ảnh biển số từ nhận diện xuống đĩa

            // 4. Kiểm tra Chống chụp chéo 2 làn cạnh nhau (Cross-Lane Deduplication)
            if (!PlateNumber.IsUnrecognized(detectedPlate) && IsCrossLaneCollision(detectedPlate, inLaneName))
            {
                AppLogger.Warning($"[CROSS-LANE IN] Bỏ qua lượt vào cho biển số {detectedPlate} do vừa được xử lý tại làn khác cách đây < {CROSS_LANE_INTERVAL_SECONDS}s.", "ParkingLaneService");
                result.IsCrossLaneIgnored = true;
                result.Success = false;
                result.ErrorMessage = $"Bỏ qua góc nhìn chéo từ làn khác cho xe {detectedPlate}.";

                // Xóa file ảnh snapshot trên đĩa để không lưu file rác khi không tạo phiên
                DeleteFileSafe(filePlate);
                DeleteFileSafe(fileOverview);
                result.PlateImagePath = null;
                result.OverviewImagePath = null;
                return result;
            }

            // 5. Kiểm tra xe đang có phiên Active trong bãi (Anti-Passback / Chống xe vào trùng lặp)
            if (!PlateNumber.IsUnrecognized(detectedPlate))
            {
                var clean = PlateNumber.Clean(detectedPlate);
                ParkingSession? existingActive = null;

                if (_sessionRepo is IHybridParkingSessionRepository hybridRepo)
                {
                    existingActive = await hybridRepo.GetActiveSessionByPlateAsync(clean).ConfigureAwait(false);
                }
                else
                {
                    var filter = Builders<ParkingSession>.Filter.Eq(s => s.PlateNumber, clean) &
                                 Builders<ParkingSession>.Filter.Eq(s => s.Status, ParkingSessionStatus.Active) &
                                 Builders<ParkingSession>.Filter.Eq(s => s.IsDeleted, false);

                    var activeSessions = await _sessionRepo.FindAsync(filter, Builders<ParkingSession>.Sort.Descending(s => s.InTime));
                    existingActive = activeSessions?.FirstOrDefault();
                }

                if (existingActive != null)
                {
                    AppLogger.Warning($"[LÀN VÀO ANTI-PASSBACK] Chặn xe {clean} vào lại do đang có phiên Active trong bãi (ID: {existingActive.Id}, Vào lúc: {existingActive.InTime:dd/MM/yyyy HH:mm:ss} tại {existingActive.InLaneName}).", "ParkingLaneService");
                    result.IsAlreadyInLot = true;
                    result.Success = false;
                    result.Session = existingActive;
                    result.ErrorMessage = $"Xe {clean} đang ở trong bãi (Vào lúc {existingActive.InTime:dd/MM/yyyy HH:mm:ss} tại {existingActive.InLaneName}).";

                    // Đọc ảnh toàn cảnh vào bộ nhớ RAM để WinForms vẫn xem được rồi xóa file trên đĩa
                    if (ovwOk && File.Exists(fileOverview))
                    {
                        try { result.OverviewImageBytes = File.ReadAllBytes(fileOverview); } catch { }
                    }

                    // Xóa file ảnh snapshot trên đĩa vì không tạo phiên mới
                    DeleteFileSafe(filePlate);
                    DeleteFileSafe(fileOverview);
                    result.PlateImagePath = null;
                    result.OverviewImagePath = null;

                    // Ghi nhận bộ đệm chống chụp chéo
                    _lastProcessedPlates[clean] = (DateTime.Now, inLaneName);
                    return result;
                }
            }

            // 6. Tra cứu hồ sơ phương tiện & chủ xe đã đăng ký (PersonType rõ ràng)
            var (personName, deptName, compName, personId, vehicleType, personType, isRegistered, weight) =
                await LookupVehicleAndPersonAsync(detectedPlate);

            result.PersonName = personName;
            result.DepartmentName = deptName;
            result.CompanyName = compName;
            result.VehicleType = vehicleType;
            result.PersonType = personType;
            result.IsRegisteredVehicle = isRegistered;
            result.Weight = weight;

            // 7. Tạo phiên đỗ xe (ParkingSession) và lưu (Hybrid: MongoDB hoặc LiteDB < 2ms)
            string? note = null;
            if (!plateOk && !ovwOk) note = "Cả 2 camera mất kết nối hoặc lỗi lúc chụp";
            else if (!plateOk) note = "Camera biển số lỗi/mất kết nối lúc chụp";
            else if (!ovwOk) note = "Camera toàn cảnh lỗi/mất kết nối lúc chụp";

            // Xếp hàng tác vụ copy ảnh offline lên server nếu đang chạy chế độ ngoại tuyến
            QueueOfflineImageSync(captureContext, captureResult);

            var session = ParkingSession.CheckIn(
                inLaneName: inLaneName,
                plateNumber: detectedPlate,
                inOverviewImagePath: ovwOk ? fileOverview : ImageStoragePath.Empty,
                inPlateImagePath: plateOk ? filePlate : ImageStoragePath.Empty, // Lưu ảnh gốc từ camera
                personName: personName,
                vehicleType: vehicleType,
                note: note,
                personId: personId,
                companyName: compName,
                departmentName: deptName,
                personType: personType,
                weight: weight
            );

            if (_sessionRepo is IHybridParkingSessionRepository sessionHybridRepo)
            {
                await sessionHybridRepo.CheckInAsync(session).ConfigureAwait(false);
            }
            else
            {
                await _sessionRepo.AddAsync(session);
            }

            result.Session = session;
            result.Success = true;

            // 8. Ghi nhận bộ nhớ đệm chống chụp chéo
            if (!PlateNumber.IsUnrecognized(detectedPlate))
            {
                _lastProcessedPlates[detectedPlate] = (DateTime.Now, inLaneName);
            }

            AppLogger.Information($"[LÀN VÀO] Tạo phiên thành công. ID: {session.Id}, Làn: {inLaneName}, Biển số: {detectedPlate}, Chủ xe: {personName ?? "Khách lạ"}, Đối tượng: {personType}, Trạng thái Cam: Plate={plateOk}, Overview={ovwOk}", "ParkingLaneService");
            return result;
        }

        public async Task<LaneProcessResult> ProcessOutLaneAsync(
            string outLaneName,
            ICameraService? plateCam,
            ICameraService? overviewCam,
            string? plateDeviceId = null,
            string? overviewDeviceId = null,
            string triggerSource = "RADAR",
            string captureDir = "")
        {
            var result = new LaneProcessResult
            {
                ProcessedTime = DateTime.Now
            };

            // 0. Xác định tên làn chính xác từ bảng Lanes nếu chưa có
            if (string.IsNullOrWhiteSpace(outLaneName) && _laneRepo != null)
            {
                try
                {
                    var lane = await _laneRepo.FindOneAsync(l => l.Direction == LaneDirection.Out && !l.IsDeleted && l.IsActive);
                    if (lane != null && !string.IsNullOrWhiteSpace(lane.Name))
                    {
                        outLaneName = lane.Name;
                    }
                }
                catch { }
            }
            if (string.IsNullOrWhiteSpace(outLaneName)) outLaneName = "Làn Ra";

            // 1. Chuẩn bị thư mục lưu trữ ảnh (tự động fallback sang OfflineCaptures nếu server mất mạng)
            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var captureContext = PrepareCapturePaths(captureDir, timeStamp, triggerSource, "out");

            // 2. Chụp ảnh song song với cơ chế chống chịu lỗi (Graceful Degradation & Retry cục bộ)
            var captureResult = await CaptureSnapshotsAsync(
                plateCam, overviewCam, captureContext, plateDeviceId, overviewDeviceId);

            string filePlate = captureResult.PlateImagePath;
            string fileOverview = captureResult.OverviewImagePath;
            bool plateOk = captureResult.PlateCamSuccess;
            bool ovwOk = captureResult.OverviewCamSuccess;
            result.PlateCamSuccess = plateOk;
            result.OverviewCamSuccess = ovwOk;
            result.PlateImagePath = plateOk ? filePlate : null;
            result.OverviewImagePath = ovwOk ? fileOverview : null;

            // 3. Nhận diện biển số (ANPR)
            string detectedPlate = PlateNumber.Unrecognized;
            Bitmap? croppedBmp = null;
            if (plateOk)
            {
                try
                {
                    var anpr = await _anprService.RecognizeAsync(filePlate);
                    if (anpr != null && anpr.IsSuccess && !string.IsNullOrWhiteSpace(anpr.FormattedPlate))
                    {
                        detectedPlate = PlateNumber.Clean(anpr.FormattedPlate);
                        // Chỉ giữ Bitmap trong bộ nhớ để hiển thị UI, KHÔNG lưu file ảnh crop ra đĩa
                        croppedBmp = anpr.CroppedPlateImage;
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, $"[LÀN RA] Lỗi trong quá trình nhận diện ANPR: {ex.Message}", "ParkingLaneService");
                }
            }

            // Nếu nhận diện không thành công: Không lưu ảnh, không lưu DB, trả về thất bại
            if (PlateNumber.IsUnrecognized(detectedPlate) || string.IsNullOrWhiteSpace(detectedPlate))
            {
                AppLogger.Information($"[LÀN RA] Nhận diện biển số không thành công. Hủy lưu ảnh và không tạo phiên.", "ParkingLaneService");
                DeleteFileSafe(filePlate);
                DeleteFileSafe(fileOverview);
                croppedBmp?.Dispose();

                result.PlateNumber = PlateNumber.Unrecognized;
                result.CroppedPlateImage = null;
                result.PlateImagePath = null;
                result.OverviewImagePath = null;
                result.Success = false;
                result.ErrorMessage = "Nhận diện biển số không thành công.";
                return result;
            }

            result.PlateNumber = detectedPlate;
            result.CroppedPlateImage = croppedBmp;
            result.PlateCropImagePath = null; // Không lưu ảnh biển số từ nhận diện xuống đĩa

            // 4. Kiểm tra Chống chụp chéo 2 làn cạnh nhau (Cross-Lane Deduplication)
            if (!PlateNumber.IsUnrecognized(detectedPlate) && IsCrossLaneCollision(detectedPlate, outLaneName))
            {
                AppLogger.Warning($"[CROSS-LANE OUT] Bỏ qua lượt ra cho biển số {detectedPlate} do vừa được xử lý tại làn khác cách đây < {CROSS_LANE_INTERVAL_SECONDS}s.", "ParkingLaneService");
                result.IsCrossLaneIgnored = true;
                result.Success = false;
                result.ErrorMessage = $"Bỏ qua góc nhìn chéo từ làn khác cho xe {detectedPlate}.";

                // Xóa file ảnh tạm vì không tạo phiên
                DeleteFileSafe(filePlate);
                DeleteFileSafe(fileOverview);
                result.PlateImagePath = null;
                result.OverviewImagePath = null;
                return result;
            }

            // 5. Tra cứu hồ sơ phương tiện & chủ xe (PersonType rõ ràng)
            var (personName, deptName, compName, personId, vehicleType, personType, isRegistered, weight) =
                await LookupVehicleAndPersonAsync(detectedPlate);

            result.PersonName = personName;
            result.DepartmentName = deptName;
            result.CompanyName = compName;
            result.VehicleType = vehicleType;
            result.PersonType = personType;
            result.IsRegisteredVehicle = isRegistered;
            result.Weight = weight;

            // 6. Tìm kiếm phiên Active trong bãi khớp 100% biển số (Kiểm tra cả Mongo lẫn LiteDB)
            ParkingSession? activeSession = null;
            if (!PlateNumber.IsUnrecognized(detectedPlate))
            {
                var clean = PlateNumber.Clean(detectedPlate);
                if (_sessionRepo is IHybridParkingSessionRepository hybridRepo)
                {
                    activeSession = await hybridRepo.GetActiveSessionByPlateAsync(clean).ConfigureAwait(false);
                }
                else
                {
                    var filter = Builders<ParkingSession>.Filter.Eq(s => s.PlateNumber, clean) &
                                 Builders<ParkingSession>.Filter.Eq(s => s.Status, ParkingSessionStatus.Active) &
                                 Builders<ParkingSession>.Filter.Eq(s => s.IsDeleted, false);

                    var candidates = await _sessionRepo.FindAsync(filter, Builders<ParkingSession>.Sort.Descending(s => s.InTime));
                    activeSession = candidates.FirstOrDefault();
                }
            }

            string? note = null;
            if (!plateOk && !ovwOk) note = "Cả 2 camera mất kết nối hoặc lỗi lúc chụp";
            else if (!plateOk) note = "Camera biển số lỗi/mất kết nối lúc chụp";
            else if (!ovwOk) note = "Camera toàn cảnh lỗi/mất kết nối lúc chụp";

            // Xếp hàng tác vụ copy ảnh offline lên server nếu đang chạy chế độ ngoại tuyến
            QueueOfflineImageSync(captureContext, captureResult);

            if (activeSession != null)
            {
                // Hoàn tất phiên gửi xe (Check-out)
                activeSession.CheckOut(
                    outLaneName: outLaneName,
                    outOverviewImagePath: ovwOk ? fileOverview : ImageStoragePath.Empty,
                    outPlateImagePath: plateOk ? filePlate : ImageStoragePath.Empty, // Lưu ảnh gốc từ camera
                    note: note
                );

                if (_sessionRepo is IHybridParkingSessionRepository hybridRepo)
                {
                    await hybridRepo.CheckOutAsync(activeSession).ConfigureAwait(false);
                }
                else
                {
                    await _sessionRepo.UpdateAsync(activeSession);
                }

                result.Session = activeSession;
                result.Weight = activeSession.Weight ?? weight;
                result.Success = true;
                AppLogger.Information($"[LÀN RA] Hoàn tất Check-out cho xe {detectedPlate}. Làn: {outLaneName}, Session ID: {activeSession.Id}, Thời gian gửi: {activeSession.Duration?.TotalMinutes:F0} phút.", "ParkingLaneService");
            }
            else
            {
                // Không tìm thấy phiên vào khớp -> Tạo phiên xe ra bình thường (Completed)
                var directOutSession = ParkingSession.CreateDirectOut(
                    outLaneName: outLaneName,
                    plateNumber: detectedPlate,
                    outOverviewImagePath: ovwOk ? fileOverview : ImageStoragePath.Empty,
                    outPlateImagePath: plateOk ? filePlate : ImageStoragePath.Empty, // Lưu ảnh gốc từ camera
                    personName: personName,
                    vehicleType: vehicleType,
                    note: note,
                    personId: personId,
                    companyName: compName,
                    departmentName: deptName,
                    personType: personType,
                    weight: weight
                );

                if (_sessionRepo is IHybridParkingSessionRepository hybridRepo)
                {
                    await hybridRepo.CheckInAsync(directOutSession).ConfigureAwait(false);
                }
                else
                {
                    await _sessionRepo.AddAsync(directOutSession);
                }

                result.Session = directOutSession;
                result.Success = true;
                AppLogger.Information($"[LÀN RA] Ghi nhận phiên xe ra bình thường cho xe {detectedPlate}. Làn: {outLaneName}, Session ID: {directOutSession.Id}.", "ParkingLaneService");
            }

            // 7. Ghi nhận bộ nhớ đệm chống chụp chéo
            if (!PlateNumber.IsUnrecognized(detectedPlate))
            {
                _lastProcessedPlates[detectedPlate] = (DateTime.Now, outLaneName);
            }

            return result;
        }

        public void ClearCrossLaneCache()
        {
            _lastProcessedPlates.Clear();
        }

        #region Private Helper Methods

        private static void DeleteFileSafe(string filePath)
        {
            try
            {
                if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warning($"Không thể xóa file tạm {filePath}: {ex.Message}");
            }
        }

        private async Task<LaneCaptureResult> CaptureSnapshotsAsync(
            ICameraService? plateCam,
            ICameraService? overviewCam,
            CapturePathContext context,
            string? plateDeviceId = null,
            string? overviewDeviceId = null)
        {
            var taskPlate = Task.Run(async () =>
            {
                if (plateCam == null) return (false, context.PlateFilePath);
                try
                {
                    await plateCam.CaptureToFileAsync(context.PlateFilePath);
                    if (File.Exists(context.PlateFilePath)) return (true, context.PlateFilePath);
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, $"Lỗi chụp camera biển số ({context.PlateFilePath}): {ex.Message}", "ParkingLaneService");
                }

                // Nếu ghi lên Server Share thất bại, tự động retry lưu vào thư mục cục bộ OfflineCaptures
                if (!context.IsLocalFallback)
                {
                    var (fallbackOk, fallbackPath) = await TryLocalFallbackCaptureAsync(plateCam, "camera biển số", context.DateFolder, context.PlateFileName);
                    if (fallbackOk) return (true, fallbackPath);
                }

                return (false, context.PlateFilePath);
            });

            var taskOverview = Task.Run(async () =>
            {
                if (overviewCam == null) return (false, context.OverviewFilePath);
                try
                {
                    await overviewCam.CaptureToFileAsync(context.OverviewFilePath);
                    if (File.Exists(context.OverviewFilePath)) return (true, context.OverviewFilePath);
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, $"Lỗi chụp camera toàn cảnh ({context.OverviewFilePath}): {ex.Message}", "ParkingLaneService");
                }

                // Nếu ghi lên Server Share thất bại, tự động retry lưu vào thư mục cục bộ OfflineCaptures
                if (!context.IsLocalFallback)
                {
                    var (fallbackOk, fallbackPath) = await TryLocalFallbackCaptureAsync(overviewCam, "camera toàn cảnh", context.DateFolder, context.OverviewFileName);
                    if (fallbackOk) return (true, fallbackPath);
                }

                return (false, context.OverviewFilePath);
            });

            await Task.WhenAll(taskPlate, taskOverview);

            var (plateOk, actualPlatePath) = taskPlate.Result;
            var (ovwOk, actualOvwPath) = taskOverview.Result;

            // Đồng bộ chủ động với Health Monitor nếu có cấu hình
            if (_healthService != null && _deviceRepo != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(plateDeviceId))
                        {
                            var dev = await _deviceRepo.GetByIdAsync(plateDeviceId!);
                            if (dev != null)
                            {
                                var pingRes = plateOk
                                    ? DevicePingResult.Success(dev, 10, "Chụp ảnh biển số thành công")
                                    : DevicePingResult.Fail(dev, "Chụp ảnh biển số thất bại hoặc camera ngắt kết nối", 0);
                                await _healthService.SyncStatusToDbAsync(pingRes);
                            }
                        }

                        if (!string.IsNullOrEmpty(overviewDeviceId))
                        {
                            var dev = await _deviceRepo.GetByIdAsync(overviewDeviceId!);
                            if (dev != null)
                            {
                                var pingRes = ovwOk
                                    ? DevicePingResult.Success(dev, 10, "Chụp ảnh toàn cảnh thành công")
                                    : DevicePingResult.Fail(dev, "Chụp ảnh toàn cảnh thất bại hoặc camera ngắt kết nối", 0);
                                await _healthService.SyncStatusToDbAsync(pingRes);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Warning($"Lỗi đồng bộ trạng thái sức khỏe thiết bị sau khi chụp: {ex.Message}");
                    }
                });
            }

            return new LaneCaptureResult(plateOk, ovwOk, actualPlatePath, actualOvwPath);
        }

        private bool IsCrossLaneCollision(string cleanPlate, string currentLane)
        {
            if (_lastProcessedPlates.TryGetValue(cleanPlate, out var last))
            {
                var elapsed = (DateTime.Now - last.ProcessTime).TotalSeconds;
                if (elapsed < CROSS_LANE_INTERVAL_SECONDS && last.LaneType != currentLane)
                {
                    return true;
                }
            }
            return false;
        }

        private async Task<(string? personName, string? deptName, string? compName, string? personId, VehicleType vehicleType, PersonType personType, bool isRegistered, double? weight)>
            LookupVehicleAndPersonAsync(string plateNumber)
        {
            if (string.IsNullOrWhiteSpace(plateNumber) || PlateNumber.IsUnrecognized(plateNumber))
            {
                return (null, null, null, null, VehicleType.Car, PersonType.Visitor, false, null);
            }

            try
            {
                string clean = PlateNumber.Clean(plateNumber);
                var vehicle = await _vehicleRepo.FindOneAsync(v => v.PlateNumber == clean && !v.IsDeleted);
                if (vehicle != null)
                {
                    string? personName = null;
                    string? deptName = null;
                    string? compName = null;
                    string? personId = vehicle.OwnerPersonId;
                    PersonType personType = PersonType.Visitor;

                    if (!string.IsNullOrEmpty(vehicle.OwnerPersonId))
                    {
                        var person = await _personRepo.GetByIdAsync(vehicle.OwnerPersonId!);
                        if (person != null && !person.IsDeleted)
                        {
                            personName = person.FullName;
                            personType = person.Type;

                            if (!string.IsNullOrEmpty(person.DepartmentId))
                            {
                                var dept = await _departmentRepo.GetByIdAsync(person.DepartmentId!);
                                deptName = dept?.Name;
                            }
                        }
                    }

                    return (personName, deptName, compName, personId, vehicle.Type, personType, true, vehicle.Weight);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Lỗi tra cứu thông tin xe/chủ xe: {ex.Message}", "ParkingLaneService");
            }

            return (null, null, null, null, VehicleType.Car, PersonType.Visitor, false, null);
        }

        private CapturePathContext PrepareCapturePaths(
            string captureDir, string timeStamp, string triggerSource, string laneType)
        {
            string dateFolder = DateTime.Now.ToString("yyyy-MM-dd");
            string uniqueId = Guid.NewGuid().ToString("N").Substring(0, 4);
            string fileNamePlate = $"{timeStamp}_{uniqueId}_{triggerSource}_{laneType}_plate.jpg";
            string fileNameOvw = $"{timeStamp}_{uniqueId}_{triggerSource}_{laneType}_overview.jpg";

            string primaryBase = string.IsNullOrWhiteSpace(captureDir) ? "Captures" : captureDir;
            string primaryDir = Path.IsPathRooted(primaryBase) ? primaryBase : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, primaryBase);

            if (_healthTracker == null || _healthTracker.IsServerOnline)
            {
                try
                {
                    string primaryDateFolder = Path.Combine(primaryDir, dateFolder);
                    if (!Directory.Exists(primaryDateFolder)) Directory.CreateDirectory(primaryDateFolder);
                    _healthTracker?.MarkImageShareStatus(true);
                    return new CapturePathContext(
                        Path.Combine(primaryDateFolder, fileNamePlate),
                        Path.Combine(primaryDateFolder, fileNameOvw),
                        false,
                        dateFolder,
                        fileNamePlate,
                        fileNameOvw);
                }
                catch (Exception ex)
                {
                    // Lỗi I/O thư mục mạng máy chủ -> fallback
                    AppLogger.Error(ex, $"[LÀN XE] Lỗi I/O khi kết nối/tạo thư mục ảnh máy chủ ({primaryDir}): {ex.Message}", "ParkingLaneService");
                    _healthTracker?.MarkImageShareStatus(false, $"Không thể kết nối/tạo thư mục ảnh máy chủ: {ex.Message}");
                }
            }

            // Fallback lưu vào thư mục Offline cục bộ trên máy bốt
            string offlineDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OfflineCaptures", dateFolder);
            try
            {
                if (!Directory.Exists(offlineDir)) Directory.CreateDirectory(offlineDir);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"[LÀN XE] Không thể tạo thư mục lưu ảnh ngoại tuyến cục bộ ({offlineDir}): {ex.Message}", "ParkingLaneService");
            }

            return new CapturePathContext(
                Path.Combine(offlineDir, fileNamePlate),
                Path.Combine(offlineDir, fileNameOvw),
                true,
                dateFolder,
                fileNamePlate,
                fileNameOvw);
        }

        private async Task<(bool success, string fallbackPath)> TryLocalFallbackCaptureAsync(
            ICameraService camera,
            string camDescription,
            string dateFolder,
            string fileName)
        {
            try
            {
                string localDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OfflineCaptures", dateFolder);
                if (!Directory.Exists(localDir)) Directory.CreateDirectory(localDir);
                string localFilePath = Path.Combine(localDir, fileName);

                await camera.CaptureToFileAsync(localFilePath);
                if (File.Exists(localFilePath))
                {
                    _healthTracker?.MarkImageShareStatus(false, $"Lỗi ghi ảnh {camDescription} lên máy chủ, đã chuyển lưu cục bộ: {fileName}");
                    return (true, localFilePath);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Lỗi khi retry lưu ảnh {camDescription} cục bộ ({fileName}): {ex.Message}", "ParkingLaneService");
            }

            return (false, string.Empty);
        }

        private void QueueSingleImageSync(string localFilePath, string remoteRelativePath)
        {
            try
            {
                var col = LiteDbContext.Instance.GetImageSyncTasks();
                col.Insert(new OfflineImageSyncTask
                {
                    LocalFilePath = localFilePath,
                    RemoteRelativePath = remoteRelativePath,
                    CreatedAt = DateTime.Now,
                    IsSynced = false
                });
            }
            catch (Exception ex)
            {
                AppLogger.Warning($"Không thể xếp hàng đồng bộ ảnh offline: {ex.Message}");
            }
        }

        private void QueueOfflineImageSync(CapturePathContext context, LaneCaptureResult captureResult)
        {
            string offlineBase = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OfflineCaptures");

            void TryQueueSync(bool isCamOk, string actualPath, string fileName)
            {
                if (isCamOk && !string.IsNullOrEmpty(actualPath) && File.Exists(actualPath))
                {
                    if (context.IsLocalFallback || actualPath.StartsWith(offlineBase, StringComparison.OrdinalIgnoreCase))
                    {
                        QueueSingleImageSync(actualPath, Path.Combine(context.DateFolder, fileName));
                    }
                }
            }

            TryQueueSync(captureResult.PlateCamSuccess, captureResult.PlateImagePath, context.PlateFileName);
            TryQueueSync(captureResult.OverviewCamSuccess, captureResult.OverviewImagePath, context.OverviewFileName);
        }

        #endregion
    }
}
