namespace PhuXuanParkingSystem.Models.Enums
{
    /// <summary>
    /// Trạng thái kết nối của thiết bị phần cứng
    /// </summary>
    public enum DeviceStatus
    {
        Connected = 1,       // Đang kết nối bình thường
        Disconnected = 2,    // Mất kết nối
        Error = 3,           // Báo lỗi hoạt động
        Maintenance = 4      // Đang bảo trì
    }
}
