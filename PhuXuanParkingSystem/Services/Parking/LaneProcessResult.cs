using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using System.Drawing;

namespace PhuXuanParkingSystem.Services.Parking
{
    /// <summary>
    /// Kết quả xử lý nghiệp vụ khi xe qua làn (Vào hoặc Ra)
    /// </summary>
    public class LaneProcessResult
    {
        public string DisplayPlate { get; set; } = string.Empty;
        public string OwnerName { get; set; } = "Xe lạ";
        public string DepartmentName { get; set; } = "Khách vãng lai";
        public VehicleType VehicleType { get; set; } = VehicleType.Car;
        public string StatusText { get; set; } = string.Empty;
        public Color StatusColor { get; set; } = Color.FromArgb(0, 120, 215);
        public bool IsRegistered { get; set; }
        public bool IsIgnored { get; set; }
        public string? IgnoreReason { get; set; }
        public string? DurationText { get; set; }
        public ParkingSession? Session { get; set; }

        public static LaneProcessResult Ignored(string reason, string displayPlate = "")
        {
            return new LaneProcessResult
            {
                IsIgnored = true,
                IgnoreReason = reason,
                DisplayPlate = displayPlate,
                StatusText = reason,
                StatusColor = Color.FromArgb(220, 100, 20)
            };
        }
    }
}
