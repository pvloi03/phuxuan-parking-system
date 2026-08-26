using PhuXuanParkingSystem.Models.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace PhuXuanParkingSystem.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho đơn vị Nhà thầu / Đối tác (Tối giản)
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Contractor : BaseEntity
    {
        // =========================================================================
        // --- CÁC TRƯỜNG LƯU TRỮ DATABASE (PERSISTED PROPERTIES) ---
        // =========================================================================
        public string Code { get; set; } = string.Empty;          // [LƯU DB] Mã nhà thầu
        public string Name { get; set; } = string.Empty;          // [LƯU DB] Tên nhà thầu / đơn vị thi công
        public string? ContactPerson { get; set; }                // [LƯU DB] Người đại diện liên hệ
        public string? PhoneNumber { get; set; }                  // [LƯU DB] Số điện thoại liên hệ
        public string? Email { get; set; }                        // [LƯU DB] Email liên hệ
        public string? Note { get; set; }                         // [LƯU DB] Ghi chú
        public bool IsActive { get; set; } = true;                // [LƯU DB] Trạng thái hoạt động

        public Contractor() { }

        public Contractor(string code, string name, string? phoneNumber = null)
        {
            Code = code;
            Name = name;
            PhoneNumber = phoneNumber;
        }
    }
}
