using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Services.Devices.Camera;
using System;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Parking
{
    /// <summary>
    /// Kết quả xử lý nghiệp vụ cho một lượt xe qua làn
    /// </summary>
    public class LaneProcessResult
    {
        public bool Success { get; set; }
        public ParkingSession? Session { get; set; }
        public string PlateNumber { get; set; } = string.Empty;
        public string? PlateImagePath { get; set; }
        public string? PlateCropImagePath { get; set; }
        public System.Drawing.Bitmap? CroppedPlateImage { get; set; }
        public string? OverviewImagePath { get; set; }
        public string? PersonName { get; set; }
        public string? DepartmentName { get; set; }
        public string? CompanyName { get; set; }
        public VehicleType VehicleType { get; set; } = VehicleType.Car;
        public PersonType PersonType { get; set; } = PersonType.Visitor;
        public byte[]? OverviewImageBytes { get; set; }
        public bool IsRegisteredVehicle { get; set; }
        public bool IsCrossLaneIgnored { get; set; }
        public bool IsAlreadyInLot { get; set; }
        public bool PlateCamSuccess { get; set; }
        public bool OverviewCamSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime ProcessedTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Giao diện dịch vụ nghiệp vụ điều phối làn xe (Chụp ảnh, ANPR, kiểm tra xe, lưu ParkingSession)
    /// </summary>
    public interface IParkingLaneService
    {
        /// <summary>
        /// Xử lý một lượt xe vào (Check-in)
        /// </summary>
        Task<LaneProcessResult> ProcessInLaneAsync(
            string inLaneName,
            ICameraService? plateCam,
            ICameraService? overviewCam,
            string? plateDeviceId = null,
            string? overviewDeviceId = null,
            string triggerSource = "RADAR",
            string captureDir = "");

        /// <summary>
        /// Xử lý một lượt xe ra (Check-out)
        /// </summary>
        Task<LaneProcessResult> ProcessOutLaneAsync(
            string outLaneName,
            ICameraService? plateCam,
            ICameraService? overviewCam,
            string? plateDeviceId = null,
            string? overviewDeviceId = null,
            string triggerSource = "RADAR",
            string captureDir = "");

        /// <summary>
        /// Xóa bộ nhớ đệm chống chụp chéo (Cross-lane cache) khi cần
        /// </summary>
        void ClearCrossLaneCache();
    }
}
