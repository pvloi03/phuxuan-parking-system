using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using PhuXuanParkingSystem.Services.Anpr;
using PhuXuanParkingSystem.Services.Logging;
using PhuXuanParkingSystem.Services.Notification;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Parking
{
    /// <summary>
    /// Triển khai dịch vụ điều phối nghiệp vụ đỗ xe Clean Architecture
    /// Tách toàn bộ nghiệp vụ truy vấn DB, khóa chéo liên làn, xử lý phiên ra/vào khỏi WinForms UI
    /// </summary>
    public class ParkingLaneService : IParkingLaneService
    {
        private readonly IRepository<ParkingSession> _sessionRepo;
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IRepository<Person> _personRepo;
        private readonly IRepository<Department> _departmentRepo;
        private readonly IRepository<Company> _companyRepo;
        private readonly IRepository<Contractor> _contractorRepo;

        // ── Cơ chế Chống quét chéo liên làn (Cross-Lane Lockout) & Chống lặp cùng biển số ──
        private readonly object _lockDebounce = new();
        private string _lastInProcessedPlate = string.Empty;
        private DateTime _lastInProcessedTime = DateTime.MinValue;
        private string _lastOutProcessedPlate = string.Empty;
        private DateTime _lastOutProcessedTime = DateTime.MinValue;

        private const int CROSS_LANE_LOCKOUT_SECONDS = 5;    // Khóa chéo biển số giữa 2 làn: 5 giây
        private const int SAME_LANE_PLATE_DEBOUNCE_SECONDS = 3;// Chống chụp lặp cùng 1 biển số trên 1 làn: 3 giây

        public ParkingLaneService(
            IRepository<ParkingSession> sessionRepo,
            IRepository<Vehicle> vehicleRepo,
            IRepository<Person> personRepo,
            IRepository<Department> departmentRepo,
            IRepository<Company> companyRepo,
            IRepository<Contractor> contractorRepo)
        {
            _sessionRepo = sessionRepo ?? throw new ArgumentNullException(nameof(sessionRepo));
            _vehicleRepo = vehicleRepo ?? throw new ArgumentNullException(nameof(vehicleRepo));
            _personRepo = personRepo ?? throw new ArgumentNullException(nameof(personRepo));
            _departmentRepo = departmentRepo ?? throw new ArgumentNullException(nameof(departmentRepo));
            _companyRepo = companyRepo ?? throw new ArgumentNullException(nameof(companyRepo));
            _contractorRepo = contractorRepo ?? throw new ArgumentNullException(nameof(contractorRepo));
        }

        public async Task<LaneProcessResult> ProcessInLaneAsync(
            PlateRecognitionResult? anprResult,
            string triggerSource,
            string filePlate,
            string fileOverview)
        {
            if (anprResult == null || !anprResult.IsSuccess)
            {
                // Biển số không nhận dạng được -> Vẫn cho phép vào như xe lạ
                string unknownPlate = "UNKNOWN_" + DateTime.Now.ToString("HHmmss");
                var unknownSession = ParkingSession.CheckIn(
                    "Làn Vào Số 1",
                    unknownPlate,
                    fileOverview,
                    filePlate,
                    null,
                    VehicleType.Car,
                    $"Nguồn: {triggerSource}, Không nhận dạng được biển số lúc vào");

                try
                {
                    await _sessionRepo.AddAsync(unknownSession);
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, "Lỗi lưu ParkingSession xe lạ vào MongoDB");
                }

                AppNotificationService.NotifyWarning(
                    NotificationCategory.LaneIn,
                    "Nhận diện biển số",
                    "Chụp ảnh thành công và cho phép vào. Không nhận dạng được biển số xe.",
                    triggerSource);

                return new LaneProcessResult
                {
                    DisplayPlate = "Không nhận dạng được",
                    OwnerName = "Xe lạ",
                    DepartmentName = "Khách vãng lai",
                    VehicleType = VehicleType.Car,
                    StatusText = "Cho phép vào - Ghi nhận hình ảnh (Không đọc được biển)",
                    StatusColor = Color.FromArgb(200, 120, 30),
                    Session = unknownSession
                };
            }

            string cleanPlate = anprResult.CleanPlate;

            // 1. Kiểm tra Khóa Chéo Liên Làn & Chống Lặp
            lock (_lockDebounce)
            {
                if (!string.IsNullOrEmpty(_lastOutProcessedPlate) &&
                    _lastOutProcessedPlate == cleanPlate &&
                    (DateTime.Now - _lastOutProcessedTime).TotalSeconds < CROSS_LANE_LOCKOUT_SECONDS)
                {
                    double secondsSinceOut = (DateTime.Now - _lastOutProcessedTime).TotalSeconds;
                    AppLogger.Warning($"[CHỐNG QUÉT CHÉO] Bỏ qua Làn Vào cho '{anprResult.FormattedPlate}' vì vừa qua Làn Ra cách đây {secondsSinceOut:F1}s.");
                    return LaneProcessResult.Ignored($"Xe vừa qua Làn Ra {secondsSinceOut:F0}s trước", anprResult.FormattedPlate);
                }

                if (!string.IsNullOrEmpty(_lastInProcessedPlate) &&
                    _lastInProcessedPlate == cleanPlate &&
                    (DateTime.Now - _lastInProcessedTime).TotalSeconds < SAME_LANE_PLATE_DEBOUNCE_SECONDS)
                {
                    AppLogger.Debug($"[CHỐNG CHỤP LẶP] Bỏ qua Làn Vào cho '{anprResult.FormattedPlate}' vì vừa xử lý cách đây {(DateTime.Now - _lastInProcessedTime).TotalSeconds:F1}s.");
                    return LaneProcessResult.Ignored("", anprResult.FormattedPlate);
                }

                _lastInProcessedPlate = cleanPlate;
                _lastInProcessedTime = DateTime.Now;
            }

            // 2. Tra cứu thông tin chủ xe & đơn vị
            var (vehicle, person, department, company, contractor) = await LookupVehicleOwnerAsync(cleanPlate);

            bool isRegistered = person != null || vehicle != null;
            string ownerName = person?.FullName ?? "Xe lạ";
            string deptName = department?.Name ?? company?.Name ?? contractor?.Name ?? (isRegistered ? "Đơn vị nội bộ" : "Khách vãng lai");
            VehicleType vType = vehicle?.Type ?? VehicleType.Car;

            // 3. Kiểm tra xem xe này có đang ở trong bãi (Active Session) không
            ParkingSession? existingActiveSession = null;
            try
            {
                existingActiveSession = await _sessionRepo.FindOneAsync(s => s.PlateNumber == cleanPlate && s.Status == ParkingSessionStatus.Active && !s.IsDeleted);
            }
            catch (Exception ex)
            {
                AppLogger.Warning($"Lỗi kiểm tra phiên Active xe vào: {ex.Message}");
            }

            if (existingActiveSession != null)
            {
                // Xe đã có trong bãi (vào lại lần 2 liên tiếp mà chưa ra) -> Cập nhật phiên hiện tại
                existingActiveSession.InOverviewImagePath = fileOverview;
                existingActiveSession.InPlateImagePath = filePlate;
                existingActiveSession.InLaneName = "Làn Vào Số 1";
                existingActiveSession.Note = string.IsNullOrWhiteSpace(existingActiveSession.Note)
                    ? $"Xe vào lại lúc {DateTime.Now:dd/MM/yyyy HH:mm:ss}"
                    : $"{existingActiveSession.Note}; Xe vào lại lúc {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                existingActiveSession.UpdatedAt = DateTime.Now;

                try
                {
                    await _sessionRepo.UpdateAsync(existingActiveSession);
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, "Lỗi cập nhật phiên xe vào lặp lại");
                }

                AppNotificationService.NotifyWarning(
                    NotificationCategory.LaneIn,
                    "Xe vào lại liên tiếp",
                    $"Biển số {anprResult.FormattedPlate} đang có trong bãi (Vào lúc {existingActiveSession.InTime:HH:mm:ss}). Đã cập nhật ảnh và ghi nhận lượt vào mới.",
                    anprResult.FormattedPlate);

                return new LaneProcessResult
                {
                    DisplayPlate = anprResult.FormattedPlate,
                    OwnerName = ownerName,
                    DepartmentName = deptName,
                    VehicleType = vType,
                    IsRegistered = isRegistered,
                    StatusText = $"Cho phép vào (Cảnh báo: Xe đang trong bãi từ {existingActiveSession.InTime:HH:mm:ss})",
                    StatusColor = Color.FromArgb(220, 110, 0),
                    Session = existingActiveSession
                };
            }

            // Tạo mới phiên xe vào
            var newSession = ParkingSession.CheckIn(
                "Làn Vào Số 1",
                cleanPlate,
                fileOverview,
                filePlate,
                isRegistered ? ownerName : null,
                vType,
                $"Nguồn: {triggerSource}, Time: {anprResult.DurationMs}ms{(isRegistered ? " [Nội bộ]" : " [Xe lạ]")}");

            try
            {
                await _sessionRepo.AddAsync(newSession);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Lỗi lưu ParkingSession vào MongoDB");
            }

            AppNotificationService.NotifySuccess(
                NotificationCategory.LaneIn,
                "Nhận diện xe vào",
                $"Biển số: {anprResult.FormattedPlate} - {ownerName} ({(isRegistered ? deptName : "Xe lạ")}) (Độ tin cậy: {anprResult.Confidence:P0}, {anprResult.DurationMs}ms)",
                anprResult.FormattedPlate);

            return new LaneProcessResult
            {
                DisplayPlate = anprResult.FormattedPlate,
                OwnerName = ownerName,
                DepartmentName = deptName,
                VehicleType = vType,
                IsRegistered = isRegistered,
                StatusText = isRegistered ? "Cho phép vào - Đã đăng ký" : "Cho phép vào - Khách vãng lai",
                StatusColor = isRegistered ? Color.FromArgb(40, 140, 70) : Color.FromArgb(0, 120, 215),
                Session = newSession
            };
        }

        public async Task<LaneProcessResult> ProcessOutLaneAsync(
            PlateRecognitionResult? anprResult,
            string triggerSource,
            string filePlate,
            string fileOverview)
        {
            if (anprResult == null || !anprResult.IsSuccess)
            {
                string unknownPlate = "UNKNOWN_OUT_" + DateTime.Now.ToString("HHmmss");
                var unmatchedSession = ParkingSession.CreateUnmatchedOut(
                    "Làn Ra Số 1",
                    unknownPlate,
                    fileOverview,
                    filePlate,
                    null,
                    VehicleType.Car,
                    $"Nguồn: {triggerSource}, Không nhận dạng được biển số lúc ra");

                try
                {
                    await _sessionRepo.AddAsync(unmatchedSession);
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, "Lỗi lưu Unmatched ParkingSession không biển số vào MongoDB");
                }

                AppNotificationService.NotifyWarning(
                    NotificationCategory.LaneOut,
                    "Xe ra",
                    "Chụp ảnh thành công và cho phép ra. Không nhận dạng được biển số rõ ràng.",
                    triggerSource);

                return new LaneProcessResult
                {
                    DisplayPlate = "Không nhận dạng được",
                    OwnerName = "Xe lạ",
                    DepartmentName = "Khách vãng lai",
                    VehicleType = VehicleType.Car,
                    StatusText = "Cho phép ra - Ghi nhận hình ảnh (Không đọc được biển - Unmatched Out)",
                    StatusColor = Color.FromArgb(200, 120, 30),
                    Session = unmatchedSession
                };
            }

            string cleanPlate = anprResult.CleanPlate;

            // 1. Kiểm tra Khóa Chéo Liên Làn & Chống Lặp
            lock (_lockDebounce)
            {
                if (!string.IsNullOrEmpty(_lastInProcessedPlate) &&
                    _lastInProcessedPlate == cleanPlate &&
                    (DateTime.Now - _lastInProcessedTime).TotalSeconds < CROSS_LANE_LOCKOUT_SECONDS)
                {
                    double secondsSinceIn = (DateTime.Now - _lastInProcessedTime).TotalSeconds;
                    AppLogger.Warning($"[CHỐNG QUÉT CHÉO] Bỏ qua Làn Ra cho '{anprResult.FormattedPlate}' vì vừa qua Làn Vào cách đây {secondsSinceIn:F1}s.");
                    return LaneProcessResult.Ignored($"Xe vừa qua Làn Vào {secondsSinceIn:F0}s trước", anprResult.FormattedPlate);
                }

                if (!string.IsNullOrEmpty(_lastOutProcessedPlate) &&
                    _lastOutProcessedPlate == cleanPlate &&
                    (DateTime.Now - _lastOutProcessedTime).TotalSeconds < SAME_LANE_PLATE_DEBOUNCE_SECONDS)
                {
                    AppLogger.Debug($"[CHỐNG CHỤP LẶP] Bỏ qua Làn Ra cho '{anprResult.FormattedPlate}' vì vừa xử lý cách đây {(DateTime.Now - _lastOutProcessedTime).TotalSeconds:F1}s.");
                    return LaneProcessResult.Ignored("", anprResult.FormattedPlate);
                }

                _lastOutProcessedPlate = cleanPlate;
                _lastOutProcessedTime = DateTime.Now;
            }

            // 2. Tra cứu thông tin chủ xe
            var (vehicle, person, department, company, contractor) = await LookupVehicleOwnerAsync(cleanPlate);

            ParkingSession? activeSession = null;
            try
            {
                activeSession = await _sessionRepo.FindOneAsync(s => s.PlateNumber == cleanPlate && s.Status == ParkingSessionStatus.Active && !s.IsDeleted);
            }
            catch (Exception ex)
            {
                AppLogger.Warning($"Lỗi tìm kiếm Active Session lúc xe ra: {ex.Message}");
            }

            bool isRegistered = person != null || vehicle != null;
            string ownerName = person?.FullName ?? activeSession?.PersonName ?? (isRegistered ? "Cán bộ / Nhân viên" : "Xe lạ");
            string deptName = department?.Name ?? company?.Name ?? contractor?.Name ?? (isRegistered ? "Đơn vị nội bộ" : "Khách vãng lai");
            VehicleType vType = activeSession?.VehicleType ?? vehicle?.Type ?? VehicleType.Car;

            if (activeSession != null)
            {
                // Khớp đúng lượt vào -> CheckOut hoàn thành
                activeSession.CheckOut("Làn Ra Số 1", fileOverview, filePlate, $"Nguồn: {triggerSource}, Conf: {anprResult.Confidence:P0}, Time: {anprResult.DurationMs}ms");
                try
                {
                    await _sessionRepo.UpdateAsync(activeSession);
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, "Lỗi cập nhật Checkout ParkingSession");
                }

                string durationText = activeSession.Duration.HasValue
                    ? $"{activeSession.Duration.Value.Hours}h {activeSession.Duration.Value.Minutes}m"
                    : "";

                AppNotificationService.NotifySuccess(
                    NotificationCategory.LaneOut,
                    "Nhận diện xe ra",
                    $"Biển số: {anprResult.FormattedPlate} - {ownerName} (Khớp lượt vào {activeSession.InTime:HH:mm:ss}, đỗ {durationText})",
                    anprResult.FormattedPlate);

                return new LaneProcessResult
                {
                    DisplayPlate = anprResult.FormattedPlate,
                    OwnerName = ownerName,
                    DepartmentName = deptName,
                    VehicleType = vType,
                    IsRegistered = isRegistered,
                    DurationText = durationText,
                    StatusText = $"Cho phép ra ({durationText}) - {(isRegistered ? "Nội bộ" : "Xe lạ")}",
                    StatusColor = Color.FromArgb(40, 140, 70),
                    Session = activeSession
                };
            }
            else
            {
                // Xe ra không có lượt vào trước đó -> Tạo UnmatchedOut Session
                var unmatchedSession = ParkingSession.CreateUnmatchedOut(
                    "Làn Ra Số 1",
                    cleanPlate,
                    fileOverview,
                    filePlate,
                    isRegistered ? ownerName : null,
                    vType,
                    $"Nguồn: {triggerSource}, Ghi nhận xe ra không có lượt vào");

                try
                {
                    await _sessionRepo.AddAsync(unmatchedSession);
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, "Lỗi lưu Unmatched ParkingSession vào MongoDB");
                }

                AppNotificationService.NotifyWarning(
                    NotificationCategory.LaneOut,
                    "Xe ra không có lượt vào (Unmatched Out)",
                    $"Biển số {anprResult.FormattedPlate} - {ownerName} (Không tìm thấy lượt vào trước đó - Đã ghi nhận bản ghi Unmatched Out)",
                    anprResult.FormattedPlate);

                return new LaneProcessResult
                {
                    DisplayPlate = anprResult.FormattedPlate,
                    OwnerName = ownerName,
                    DepartmentName = deptName,
                    VehicleType = vType,
                    IsRegistered = isRegistered,
                    StatusText = "Cho phép ra - Không có dữ liệu vào (Unmatched Out)",
                    StatusColor = Color.FromArgb(210, 80, 20),
                    Session = unmatchedSession
                };
            }
        }

        private async Task<(Vehicle? vehicle, Person? person, Department? dept, Company? company, Contractor? contractor)> LookupVehicleOwnerAsync(string cleanPlate)
        {
            Vehicle? vehicle = null;
            Person? person = null;
            Department? dept = null;
            Company? company = null;
            Contractor? contractor = null;

            try
            {
                vehicle = await _vehicleRepo.FindOneAsync(v => v.PlateNumber == cleanPlate && !v.IsDeleted);
                if (vehicle != null && !string.IsNullOrEmpty(vehicle.OwnerPersonId))
                {
                    person = await _personRepo.GetByIdAsync(vehicle.OwnerPersonId!);
                    if (person != null)
                    {
                        if (!string.IsNullOrEmpty(person.DepartmentId))
                        {
                            dept = await _departmentRepo.GetByIdAsync(person.DepartmentId!);
                        }
                        if (!string.IsNullOrEmpty(person.CompanyId))
                        {
                            company = await _companyRepo.GetByIdAsync(person.CompanyId!);
                        }
                        if (!string.IsNullOrEmpty(person.ContractorId))
                        {
                            contractor = await _contractorRepo.GetByIdAsync(person.ContractorId!);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warning($"Lỗi tra cứu chủ xe/đơn vị từ CSDL: {ex.Message}");
            }

            return (vehicle, person, dept, company, contractor);
        }
    }
}
