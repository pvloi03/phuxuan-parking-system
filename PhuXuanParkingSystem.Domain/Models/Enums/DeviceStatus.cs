namespace PhuXuanParkingSystem.Models.Enums
{
    /// <summary>
    /// Trạng thái kết nối của thiết bị phần cứng
    /// </summary>
    public enum DeviceStatus
    {
        Connected = 1,       // Đang kết nối bình thường (SDK Login OK / Ping OK)
        Disconnected = 2,    // Mất kết nối
        Error = 3,           // Báo lỗi hoạt động
        Maintenance = 4,     // Đang bảo trì
        Connecting = 5,      // Đang kết nối
        Streaming = 6        // Đang phát luồng video / Nhận log realtime
    }
}

