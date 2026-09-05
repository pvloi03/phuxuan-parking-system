namespace PhuXuanParkingSystem.Models.Enums
{
    /// <summary>
    /// Loại hành động trong nhật ký kiểm toán (Audit Log)
    /// </summary>
    public enum AuditActionType
    {
        Login = 1,              // Đăng nhập hệ thống
        Logout = 2,             // Đăng xuất hệ thống
        Create = 3,             // Thêm mới dữ liệu
        Update = 4,             // Cập nhật dữ liệu
        Delete = 5,             // Xóa dữ liệu (xóa mềm hoặc xóa cứng)
        ChangePassword = 6,     // Đổi mật khẩu
        ChangeRole = 7,         // Thay đổi vai trò phân quyền
        LicenseUpdate = 8,      // Cập nhật bản quyền
        Export = 9,             // Xuất dữ liệu / báo cáo
        ManualOverride = 10,    // Can thiệp thủ công / mở barie khẩn cấp
        PermanentDelete = 11,   // Xóa vĩnh viễn dữ liệu khỏi CSDL
        Restore = 12            // Khôi phục dữ liệu từ thùng rác
    }
}
