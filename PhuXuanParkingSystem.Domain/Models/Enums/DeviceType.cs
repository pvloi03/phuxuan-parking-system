namespace PhuXuanParkingSystem.Models.Enums
{
    /// <summary>
    /// Loại thiết bị phần cứng trong hệ thống bãi đỗ xe HPParking.
    /// Chỉ có 2 loại: Camera IP giám sát/nhận diện biển số
    /// và Controller điều khiển Barrier qua tín hiệu Radar/cảm biến xe.
    /// </summary>
    public enum DeviceType
    {
        Camera = 1,      // Camera IP (Hikvision, NST, ONVIF...) — quan sát & nhận diện biển số
        Controller = 2   // Bộ điều khiển Barrier — nhận tín hiệu Radar → mở/đóng cổng
    }
}
