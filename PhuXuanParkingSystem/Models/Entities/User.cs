using System;
using PhuXuanParkingSystem.Models.Common;
using PhuXuanParkingSystem.Models.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace PhuXuanParkingSystem.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho Tài khoản người dùng đăng nhập hệ thống (Web Admin / WinForms)
    /// </summary>
    [BsonIgnoreExtraElements]
    public class User : BaseEntity
    {
        // =========================================================================
        // --- CÁC TRƯỜNG LƯU TRỮ DATABASE (PERSISTED PROPERTIES) ---
        // =========================================================================
        public string Username { get; set; } = string.Empty;       // [LƯU DB] Tên đăng nhập
        public string PasswordHash { get; set; } = string.Empty;   // [LƯU DB] Mật khẩu đã băm (BCrypt / SHA256)
        public string FullName { get; set; } = string.Empty;       // [LƯU DB] Họ và tên hiển thị
        public string? Email { get; set; }                         // [LƯU DB] Email
        public string? PhoneNumber { get; set; }                   // [LƯU DB] Số điện thoại
        public UserRole Role { get; set; } = UserRole.Operator;    // [LƯU DB] Vai trò phân quyền (Admin, Operator, Security, Viewer)
        public bool IsActive { get; set; } = true;                 // [LƯU DB] Trạng thái hoạt động
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? LastLoginAt { get; set; }                 // [LƯU DB] Thời điểm đăng nhập lần cuối

        public User() { }

        public User(string username, string passwordHash, string fullName, UserRole role = UserRole.Operator)
        {
            Username = username;
            PasswordHash = passwordHash;
            FullName = fullName;
            Role = role;
        }
    }
}
