namespace PhuXuanParkingSystem.Models.Enums
{
    /// <summary>
    /// Loại thiết bị phần cứng trong hệ thống
    /// </summary>
    public enum DeviceType
    {
        OverviewCamera = 1,  // Camera toàn cảnh (Hikvision)
        PlateCamera = 2,     // Camera nhận diện biển số (NST)
        Controller = 3,      // Bộ điều khiển Controller (ZKTeco C3-200)
        RadarSensor = 4,     // Cảm biến Radar phát hiện xe
        Barrier = 5,         // Cổng chắn Barrier
        Other = 99           // Thiết bị khác
    }
}
