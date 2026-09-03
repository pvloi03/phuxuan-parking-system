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
            IRepository<Device>? deviceRepo = null)
        {
            _sessionRepo = sessionRepo ?? throw new ArgumentNullException(nameof(sessionRepo));
            _vehicleRepo = vehicleRepo ?? throw new ArgumentNullException(nameof(vehicleRepo));
            _personRepo = personRepo ?? throw new ArgumentNullException(nameof(personRepo));
            _departmentRepo = departmentRepo ?? throw new ArgumentNullException(nameof(departmentRepo));
            _anprService = anprService ?? throw new ArgumentNullException(nameof(anprService));
            _healthService = healthService;
            _deviceRepo = deviceRepo;
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
            string fileOverview = Path.Combine(todayFolder, $"{timeStamp}_{triggerSource}_in_panoramic.jpg");

            // 2. Chụp ảnh song song với cơ chế chống chịu lỗi (Graceful Degradation)
            var (plateOk, ovwOk) = await CaptureSnapshotsAsync(plateCam, overviewCam, filePlate, fileOverview, plateDeviceId, overviewDeviceId);
            result.PlateCamSuccess = plateOk;
            result.OverviewCamSuccess = ovwOk;
            result.PlateImagePath = plateOk ? filePlate : null;
            result.OverviewImagePath = ovwOk ? fileOverview : null;

            // 3. Nhận diện biển số (ANPR)
            string detectedPlate = "Không đọc được";
            if (plateOk)
            {
                try
                {
                    var anpr = await _anprService.RecognizeAsync(filePlate);
                    if (anpr != null && anpr.IsSuccess && !string.IsNullOrWhiteSpace(anpr.FormattedPlate))
                    {
                        detectedPlate = PlateNumber.Clean(anpr.FormattedPlate);
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, $"[LÀN VÀO] Lỗi trong quá trình nhận diện ANPR: {ex.Message}", "ParkingLaneService");
                }
            }
            result.PlateNumber = detectedPlate;

            // 4. Kiểm tra Chống chụp chéo 2 làn cạnh nhau (Cross-Lane Deduplication)
            if (detectedPlate != "Không đọc được" && IsCrossLaneCollision(detectedPlate, "IN"))
            {
                AppLogger.Warning($"[CROSS-LANE IN] Bỏ qua lượt vào cho biển số {detectedPlate} do vừa được xử lý tại Làn Ra cách đây < {CROSS_LANE_INTERVAL_SECONDS}s.", "ParkingLaneService");
                result.IsCrossLaneIgnored = true;
                result.Success = false;
                result.ErrorMessage = $"Bỏ qua góc nhìn chéo từ Làn Ra cho xe {detectedPlate}.";
                return result;
            }

            // 5. Tra cứu hồ sơ phương tiện & chủ xe đã đăng ký
            var (personName, deptName, compName, personId, vehicleType, personType, isRegistered) =
                await LookupVehicleAndPersonAsync(detectedPlate);

            result.PersonName = personName;
            result.DepartmentName = deptName;
            result.CompanyName = compName;
            result.VehicleType = vehicleType;
            result.IsRegisteredVehicle = isRegistered;

            // 6. Tạo phiên đỗ xe (ParkingSession) và lưu vào MongoDB
            string? note = null;
            if (!plateOk && !ovwOk) note = "Cả 2 camera mất kết nối hoặc lỗi lúc chụp";
            else if (!plateOk) note = "Camera biển số lỗi/mất kết nối lúc chụp";
            else if (!ovwOk) note = "Camera toàn cảnh lỗi/mất kết nối lúc chụp";

            var session = ParkingSession.CheckIn(
                inLaneName: inLaneName,
                plateNumber: detectedPlate,
                inOverviewImagePath: ovwOk ? fileOverview : ImageStoragePath.Empty,
                inPlateImagePath: plateOk ? filePlate : ImageStoragePath.Empty,
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

            // 7. Ghi nhận bộ nhớ đệm chống chụp chéo
            if (detectedPlate != "Không đọc được")
            {
                _lastProcessedPlates[detectedPlate] = (DateTime.Now, "IN");
            }

            AppLogger.Information($"[LÀN VÀO] Tạo phiên thành công. ID: {session.Id}, Biển số: {detectedPlate}, Chủ xe: {personName ?? "Khách lạ"}, Trạng thái Cam: Plate={plateOk}, Overview={ovwOk}", "ParkingLaneService");
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
            string fileOverview = Path.Combine(todayFolder, $"{timeStamp}_{triggerSource}_out_panoramic.jpg");

            // 2. Chụp ảnh song song với cơ chế chống chịu lỗi
            var (plateOk, ovwOk) = await CaptureSnapshotsAsync(plateCam, overviewCam, filePlate, fileOverview, plateDeviceId, overviewDeviceId);
            result.PlateCamSuccess = plateOk;
            result.OverviewCamSuccess = ovwOk;
            result.PlateImagePath = plateOk ? filePlate : null;
            result.OverviewImagePath = ovwOk ? fileOverview : null;

            // 3. Nhận diện biển số (ANPR)
            string detectedPlate = "Không đọc được";
            if (plateOk)
            {
                try
                {
                    var anpr = await _anprService.RecognizeAsync(filePlate);
                    if (anpr != null && anpr.IsSuccess && !string.IsNullOrWhiteSpace(anpr.FormattedPlate))
                    {
                        detectedPlate = PlateNumber.Clean(anpr.FormattedPlate);
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, $"[LÀN RA] Lỗi trong quá trình nhận diện ANPR: {ex.Message}", "ParkingLaneService");
                }
            }
            result.PlateNumber = detectedPlate;

            // 4. Kiểm tra Chống chụp chéo 2 làn cạnh nhau (Cross-Lane Deduplication)
            if (detectedPlate != "Không đọc được" && IsCrossLaneCollision(detectedPlate, "OUT"))
            {
                AppLogger.Warning($"[CROSS-LANE OUT] Bỏ qua lượt ra cho biển số {detectedPlate} do vừa được Check-in tại Làn Vào cách đây < {CROSS_LANE_INTERVAL_SECONDS}s.", "ParkingLaneService");
                result.IsCrossLaneIgnored = true;
                result.Success = false;
                result.ErrorMessage = $"Bỏ qua góc nhìn chéo từ Làn Vào cho xe {detectedPlate}.";
                return result;
            }

            // 5. Tra cứu hồ sơ phương tiện & chủ xe
            var (personName, deptName, compName, personId, vehicleType, personType, isRegistered) =
                await LookupVehicleAndPersonAsync(detectedPlate);

            result.PersonName = personName;
            result.DepartmentName = deptName;
            result.CompanyName = compName;
            result.VehicleType = vehicleType;
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
                    outPlateImagePath: plateOk ? filePlate : ImageStoragePath.Empty,
                    note: note
                );

                await _sessionRepo.UpdateAsync(activeSession);
                result.Session = activeSession;
                result.Success = true;
                AppLogger.Information($"[LÀN RA] Hoàn tất Check-out cho xe {detectedPlate}. Session ID: {activeSession.Id}, Thời gian gửi: {activeSession.Duration?.TotalMinutes:F0} phút.", "ParkingLaneService");
            }
            else
            {
                // Không tìm thấy phiên vào khớp -> Tạo phiên UnmatchedOut
                var unmatchedSession = ParkingSession.CreateUnmatchedOut(
                    outLaneName: outLaneName,
                    plateNumber: detectedPlate,
                    outOverviewImagePath: ovwOk ? fileOverview : ImageStoragePath.Empty,
                    outPlateImagePath: plateOk ? filePlate : ImageStoragePath.Empty,
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
                AppLogger.Warning($"[LÀN RA] Tạo phiên UNMATCHED-OUT cho xe {detectedPlate}. Session ID: {unmatchedSession.Id}.", "ParkingLaneService");
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

        private async Task<(string? personName, string? deptName, string? compName, string? personId, VehicleType vehicleType, PersonType? personType, bool isRegistered)>
            LookupVehicleAndPersonAsync(string plateNumber)
        {
            if (string.IsNullOrWhiteSpace(plateNumber) || plateNumber == "Không đọc được")
            {
                return (null, null, null, null, VehicleType.Car, null, false);
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
                    PersonType? personType = null;

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

            return (null, null, null, null, VehicleType.Car, null, false);
        }

        #endregion
    }
}
