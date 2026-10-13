namespace HPParkingSystem.Models.Enums
{
    /// <summary>
    /// Vai trò của tài khoản người dùng trong hệ thống (Quản trị viên, Quản lý, Người xem)
    /// </summary>
    public enum UserRole
    {
        Admin = 1,           // Quản trị viên hệ thống
        Manager = 2,         // Quản lý bãi xe
        Viewer = 3,          // Người xem báo cáo
    }
}
