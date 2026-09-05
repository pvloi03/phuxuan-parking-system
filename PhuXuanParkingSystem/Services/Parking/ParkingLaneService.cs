using MongoDB.Driver;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Models.ValueObjects;
using PhuXuanParkingSystem.Repositories;
using PhuXuanParkingSystem.Services.Anpr;
using PhuXuanParkingSystem.Services.Devices.Camera;
using PhuXuanParkingSystem.Services.Devices.Health;
using PhuXuanParkingSystem.Services.Logging;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Parking
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
            IRepository<Lane>? laneRepo = null)
        {
            _sessionRepo = sessionRepo ?? throw new ArgumentNullException(nameof(sessionRepo));
            _vehicleRepo = vehicleRepo ?? throw new ArgumentNullException(nameof(vehicleRepo));
            _personRepo = personRepo ?? throw new ArgumentNullException(nameof(personRepo));
            _departmentRepo = departmentRepo ?? throw new ArgumentNullException(nameof(departmentRepo));
            _anprService = anprService ?? throw new ArgumentNullException(nameof(anprService));
            _healthService = healthService;
            _deviceRepo = deviceRepo;
            _laneRepo = laneRepo;
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

            // 1. Chuẩn bị thư mục lưu trữ ảnh
            string baseDir = string.IsNullOrWhiteSpace(captureDir)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Captures")
                : captureDir;
            string todayFolder = Path.Combine(baseDir, DateTime.Now.ToString("yyyy-MM-dd"));
            if (!Directory.Exists(todayFolder))
            {
                Directory.CreateDirectory(todayFolder);
            }

            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string filePlate = Path.Combine(todayFolder, $"{timeStamp}_{triggerSource}_in_plate.jpg");
            string fileOverview = Path.Combine(todayFolder, $"{timeStamp}_{triggerSource}_in_overview.jpg");

            // 2. Chụp ảnh song song với cơ chế chống chịu lỗi (Graceful Degradation)
            var (plateOk, ovwOk) = await CaptureSnapshotsAsync(plateCam, overviewCam, filePlate, fileOverview, plateDeviceId, overviewDeviceId);
            result.PlateCamSuccess = plateOk;
            result.OverviewCamSuccess = ovwOk;
            result.PlateImagePath = plateOk ? filePlate : null;
            result.OverviewImagePath = ovwOk ? fileOverview : null;

            // 3. Nhận diện biển số (ANPR)
            string detectedPlate = "Không đọc được";
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
            result.PlateNumber = detectedPlate;
            result.CroppedPlateImage = croppedBmp;
            result.PlateCropImagePath = null; // Không lưu ảnh biển số từ nhận diện xuống đĩa

            // 4. Kiểm tra Chống chụp chéo 2 làn cạnh nhau (Cross-Lane Deduplication)
            if (detectedPlate != "Không đọc được" && IsCrossLaneCollision(detectedPlate, "IN"))
            {
                AppLogger.Warning($"[CROSS-LANE IN] Bỏ qua lượt vào cho biển số {detectedPlate} do vừa được xử lý tại Làn Ra cách đây < {CROSS_LANE_INTERVAL_SECONDS}s.", "ParkingLaneService");
                result.IsCrossLaneIgnored = true;
                result.Success = false;
                result.ErrorMessage = $"Bỏ qua góc nhìn chéo từ Làn Ra cho xe {detectedPlate}.";

                // Xóa file ảnh snapshot trên đĩa để không lưu file rác khi không tạo phiên
                DeleteFileSafe(filePlate);
                DeleteFileSafe(fileOverview);
                result.PlateImagePath = null;
                result.OverviewImagePath = null;
                return result;
            }

            // 5. Kiểm tra xe đang có phiên Active trong bãi (Anti-Passback / Chống xe vào trùng lặp)
            if (detectedPlate != "Không đọc được")
            {
                var clean = PlateNumber.Clean(detectedPlate);
                var filter = Builders<ParkingSession>.Filter.Eq(s => s.PlateNumber, clean) &
                             Builders<ParkingSession>.Filter.Eq(s => s.Status, ParkingSessionStatus.Active) &
                             Builders<ParkingSession>.Filter.Eq(s => s.IsDeleted, false);

                var activeSessions = await _sessionRepo.FindAsync(filter, Builders<ParkingSession>.Sort.Descending(s => s.InTime));
                var existingActive = activeSessions?.FirstOrDefault();

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
                    _lastProcessedPlates[clean] = (DateTime.Now, "IN");
                    return result;
                }
            }

            // 6. Tra cứu hồ sơ phương tiện & chủ xe đã đăng ký (PersonType rõ ràng)
            var (personName, deptName, compName, personId, vehicleType, personType, isRegistered) =
                await LookupVehicleAndPersonAsync(detectedPlate);

            result.PersonName = personName;
            result.DepartmentName = deptName;
            result.CompanyName = compName;
            result.VehicleType = vehicleType;
            result.PersonType = personType;
            result.IsRegisteredVehicle = isRegistered;

            // 7. Tạo phiên đỗ xe (ParkingSession) và lưu vào MongoDB
            string? note = null;
            if (!plateOk && !ovwOk) note = "Cả 2 camera mất kết nối hoặc lỗi lúc chụp";
            else if (!plateOk) note = "Camera biển số lỗi/mất kết nối lúc chụp";
            else if (!ovwOk) note = "Camera toàn cảnh lỗi/mất kết nối lúc chụp";

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
                personType: personType
            );

            await _sessionRepo.AddAsync(session);
            result.Session = session;
            result.Success = true;

            // 8. Ghi nhận bộ nhớ đệm chống chụp chéo
            if (detectedPlate != "Không đọc được")
            {
                _lastProcessedPlates[detectedPlate] = (DateTime.Now, "IN");
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

            // 1. Chuẩn bị thư mục lưu trữ ảnh
            string baseDir = string.IsNullOrWhiteSpace(captureDir)
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Captures")
                : captureDir;
            string todayFolder = Path.Combine(baseDir, DateTime.Now.ToString("yyyy-MM-dd"));
            if (!Directory.Exists(todayFolder))
            {
                Directory.CreateDirectory(todayFolder);
            }

            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string filePlate = Path.Combine(todayFolder, $"{timeStamp}_{triggerSource}_out_plate.jpg");
            string fileOverview = Path.Combine(todayFolder, $"{timeStamp}_{triggerSource}_out_overview.jpg");

            // 2. Chụp ảnh song song với cơ chế chống chịu lỗi
            var (plateOk, ovwOk) = await CaptureSnapshotsAsync(plateCam, overviewCam, filePlate, fileOverview, plateDeviceId, overviewDeviceId);
            result.PlateCamSuccess = plateOk;
            result.OverviewCamSuccess = ovwOk;
            result.PlateImagePath = plateOk ? filePlate : null;
            result.OverviewImagePath = ovwOk ? fileOverview : null;

            // 3. Nhận diện biển số (ANPR)
            string detectedPlate = "Không đọc được";
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
            result.PlateNumber = detectedPlate;
            result.CroppedPlateImage = croppedBmp;
            result.PlateCropImagePath = null; // Không lưu ảnh biển số từ nhận diện xuống đĩa

            // 4. Kiểm tra Chống chụp chéo 2 làn cạnh nhau (Cross-Lane Deduplication)
            if (detectedPlate != "Không đọc được" && IsCrossLaneCollision(detectedPlate, "OUT"))
            {
                AppLogger.Warning($"[CROSS-LANE OUT] Bỏ qua lượt ra cho biển số {detectedPlate} do vừa được Check-in tại Làn Vào cách đây < {CROSS_LANE_INTERVAL_SECONDS}s.", "ParkingLaneService");
                result.IsCrossLaneIgnored = true;
                result.Success = false;
                result.ErrorMessage = $"Bỏ qua góc nhìn chéo từ Làn Vào cho xe {detectedPlate}.";

                // Xóa file ảnh tạm vì không tạo phiên
                DeleteFileSafe(filePlate);
                DeleteFileSafe(fileOverview);
                result.PlateImagePath = null;
                result.OverviewImagePath = null;
                return result;
            }

            // 5. Tra cứu hồ sơ phương tiện & chủ xe (PersonType rõ ràng)
            var (personName, deptName, compName, personId, vehicleType, personType, isRegistered) =
                await LookupVehicleAndPersonAsync(detectedPlate);

            result.PersonName = personName;
            result.DepartmentName = deptName;
            result.CompanyName = compName;
            result.VehicleType = vehicleType;
            result.PersonType = personType;
            result.IsRegisteredVehicle = isRegistered;

            // 6. Tìm kiếm phiên Active trong bãi khớp 100% biển số
            ParkingSession? activeSession = null;
            if (detectedPlate != "Không đọc được")
            {
                var clean = PlateNumber.Clean(detectedPlate);
                var filter = Builders<ParkingSession>.Filter.Eq(s => s.PlateNumber, clean) &
                             Builders<ParkingSession>.Filter.Eq(s => s.Status, ParkingSessionStatus.Active) &
                             Builders<ParkingSession>.Filter.Eq(s => s.IsDeleted, false);

                var candidates = await _sessionRepo.FindAsync(filter, Builders<ParkingSession>.Sort.Descending(s => s.InTime));
                activeSession = candidates.FirstOrDefault();
            }

            string? note = null;
            if (!plateOk && !ovwOk) note = "Cả 2 camera mất kết nối hoặc lỗi lúc chụp";
            else if (!plateOk) note = "Camera biển số lỗi/mất kết nối lúc chụp";
            else if (!ovwOk) note = "Camera toàn cảnh lỗi/mất kết nối lúc chụp";

            if (activeSession != null)
            {
                // Hoàn tất phiên gửi xe (Check-out)
                activeSession.CheckOut(
                    outLaneName: outLaneName,
                    outOverviewImagePath: ovwOk ? fileOverview : ImageStoragePath.Empty,
                    outPlateImagePath: plateOk ? filePlate : ImageStoragePath.Empty, // Lưu ảnh gốc từ camera
                    note: note
                );

                await _sessionRepo.UpdateAsync(activeSession);
                result.Session = activeSession;
                result.Success = true;
                AppLogger.Information($"[LÀN RA] Hoàn tất Check-out cho xe {detectedPlate}. Làn: {outLaneName}, Session ID: {activeSession.Id}, Thời gian gửi: {activeSession.Duration?.TotalMinutes:F0} phút.", "ParkingLaneService");
            }
            else
            {
                // Không tìm thấy phiên vào khớp -> Tạo phiên UnmatchedOut
                var unmatchedSession = ParkingSession.CreateUnmatchedOut(
                    outLaneName: outLaneName,
                    plateNumber: detectedPlate,
                    outOverviewImagePath: ovwOk ? fileOverview : ImageStoragePath.Empty,
                    outPlateImagePath: plateOk ? filePlate : ImageStoragePath.Empty, // Lưu ảnh gốc từ camera
                    personName: personName,
                    vehicleType: vehicleType,
                    note: string.IsNullOrWhiteSpace(note) ? "Xe ra không có lượt vào khớp" : $"{note}; Xe ra không có lượt vào khớp",
                    personId: personId,
                    companyName: compName,
                    departmentName: deptName,
                    personType: personType
                );

                await _sessionRepo.AddAsync(unmatchedSession);
                result.Session = unmatchedSession;
                result.Success = true;
                AppLogger.Warning($"[LÀN RA] Tạo phiên UNMATCHED-OUT cho xe {detectedPlate}. Làn: {outLaneName}, Session ID: {unmatchedSession.Id}.", "ParkingLaneService");
            }

            // 7. Ghi nhận bộ nhớ đệm chống chụp chéo
            if (detectedPlate != "Không đọc được")
            {
                _lastProcessedPlates[detectedPlate] = (DateTime.Now, "OUT");
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

        private async Task<(bool plateOk, bool ovwOk)> CaptureSnapshotsAsync(
            ICameraService? plateCam,
            ICameraService? overviewCam,
            string filePlate,
            string fileOverview,
            string? plateDeviceId,
            string? overviewDeviceId)
        {
            var taskPlate = Task.Run(async () =>
            {
                if (plateCam == null) return false;
                try
                {
                    await plateCam.CaptureToFileAsync(filePlate);
                    return File.Exists(filePlate);
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, $"Lỗi chụp camera biển số: {ex.Message}", "ParkingLaneService");
                    return false;
                }
            });

            var taskOverview = Task.Run(async () =>
            {
                if (overviewCam == null) return false;
                try
                {
                    await overviewCam.CaptureToFileAsync(fileOverview);
                    return File.Exists(fileOverview);
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, $"Lỗi chụp camera toàn cảnh: {ex.Message}", "ParkingLaneService");
                    return false;
                }
            });

            await Task.WhenAll(taskPlate, taskOverview);

            bool plateOk = taskPlate.Result;
            bool ovwOk = taskOverview.Result;

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

            return (plateOk, ovwOk);
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

        private async Task<(string? personName, string? deptName, string? compName, string? personId, VehicleType vehicleType, PersonType personType, bool isRegistered)>
            LookupVehicleAndPersonAsync(string plateNumber)
        {
            if (string.IsNullOrWhiteSpace(plateNumber) || plateNumber == "Không đọc được")
            {
                return (null, null, null, null, VehicleType.Car, PersonType.Visitor, false);
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

                    return (personName, deptName, compName, personId, vehicle.Type, personType, true);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Lỗi tra cứu thông tin xe/chủ xe: {ex.Message}", "ParkingLaneService");
            }

            return (null, null, null, null, VehicleType.Car, PersonType.Visitor, false);
        }

        #endregion
    }
}
