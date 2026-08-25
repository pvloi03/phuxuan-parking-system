using PhuXuanParkingSystem.Models.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace PhuXuanParkingSystem.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho Công ty / Đơn vị thành viên
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Company : BaseEntity
    {
        // =========================================================================
        // --- CÁC TRƯỜNG LƯU TRỮ DATABASE (PERSISTED PROPERTIES) ---
        // =========================================================================
        public string Code { get; set; } = string.Empty;          // [LƯU DB] Mã công ty / đơn vị thành viên
        public string Name { get; set; } = string.Empty;          // [LƯU DB] Tên công ty đầy đủ
        public string? PhoneNumber { get; set; }                  // [LƯU DB] Số điện thoại liên hệ
        public string? Email { get; set; }                        // [LƯU DB] Email liên hệ
        public bool IsActive { get; set; } = true;                // [LƯU DB] Trạng thái hoạt động

        public Company() { }

        public Company(string code, string name, string? phoneNumber = null, string? email = null)
        {
            Code = code;
            Name = name;
            PhoneNumber = phoneNumber;
            Email = email;
        }
    }
}
