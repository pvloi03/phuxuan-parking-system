namespace PhuXuanParkingSystem.Models.Enums
{
    public enum DeviceType
    {
        OverviewCamera = 1,  // Camera toàn cảnh (Hikvision)
        PlateCamera = 2,     // Camera nhận diện biển số (NST)
        Controller = 3,      // Bộ điều khiển Controller (ZKTeco C3-200)
        RadarSensor = 4,     // Cảm biến Radar phát hiện xe
        Barrier = 5,         // Cổng chắn Barrier
        Other = 99           // Thiết bị khác
    }

    public enum DeviceStatus
    {
        Connected = 1,       // Đang kết nối bình thường
        Disconnected = 2,    // Mất kết nối
        Error = 3,           // Báo lỗi hoạt động
        Maintenance = 4      // Đang bảo trì
    }

    public enum UserRole
    {
        Admin = 1,           // Quản trị viên hệ thống
        Manager = 2,         // Quản lý bãi xe
        Operator = 3,        // Nhân viên vận hành / trực làn
        Security = 4,        // Bảo vệ trạm
        Viewer = 5           // Người xem báo cáo
    }
}
