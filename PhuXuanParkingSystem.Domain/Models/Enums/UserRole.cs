namespace PhuXuanParkingSystem.Models.Enums
{
    /// <summary>
    /// Vai trò của tài khoản người dùng trong hệ thống (Quản trị viên, Quản lý, Người xem)
    /// </summary>
    public enum UserRole
    {
        Admin = 1,           // Quản trị viên hệ thống
        Manager = 2,         // Quản lý bãi xe
        Viewer = 3,          // Người xem báo cáo

        [System.Obsolete("Đã tinh gọn vai trò - chỉ dùng để tương thích dữ liệu cũ")]
        Operator = 4,
        [System.Obsolete("Đã tinh gọn vai trò - chỉ dùng để tương thích dữ liệu cũ")]
        Security = 5
    }
}
