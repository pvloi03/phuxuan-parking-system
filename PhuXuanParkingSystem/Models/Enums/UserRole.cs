namespace PhuXuanParkingSystem.Models.Enums
{
    /// <summary>
    /// Vai trò của tài khoản người dùng trong hệ thống
    /// </summary>
    public enum UserRole
    {
        Admin = 1,           // Quản trị viên hệ thống
        Manager = 2,         // Quản lý bãi xe
        Operator = 3,        // Nhân viên vận hành / trực làn
        Security = 4,        // Bảo vệ trạm
        Viewer = 5           // Người xem báo cáo
    }
}
