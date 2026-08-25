using PhuXuanParkingSystem.Models.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace PhuXuanParkingSystem.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho Phòng ban / Bộ phận trong hệ thống
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Department : BaseEntity
    {
        // =========================================================================
        // --- CÁC TRƯỜNG LƯU TRỮ DATABASE (PERSISTED PROPERTIES) ---
        // =========================================================================
        public string Code { get; set; } = string.Empty;          // [LƯU DB] Mã phòng ban (PB-KT, PB-HC...)
        public string Name { get; set; } = string.Empty;          // [LƯU DB] Tên phòng ban đầy đủ
        public string? CompanyId { get; set; }                    // [LƯU DB] ID Công ty trực thuộc
        public string? PhoneNumber { get; set; }                  // [LƯU DB] Số điện thoại liên hệ
        public string? Email { get; set; }                        // [LƯU DB] Email phòng ban
        public bool IsActive { get; set; } = true;                // [LƯU DB] Trạng thái hoạt động

        public Department() { }

        public Department(string code, string name, string? companyId = null, string? phoneNumber = null, string? email = null)
        {
            Code = code;
            Name = name;
            CompanyId = companyId;
            PhoneNumber = phoneNumber;
            Email = email;
        }
    }
}
