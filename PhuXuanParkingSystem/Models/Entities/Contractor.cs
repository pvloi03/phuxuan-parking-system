using PhuXuanParkingSystem.Models.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace PhuXuanParkingSystem.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho Nhà thầu / Đơn vị đối tác làm việc tại bãi xe
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Contractor : BaseEntity
    {
        public string Code { get; set; } = string.Empty;          // Mã nhà thầu
        public string Name { get; set; } = string.Empty;          // Tên nhà thầu
        public string? ContactPerson { get; set; }                // Người liên hệ chính
        public string? PhoneNumber { get; set; }                  // Số điện thoại
        public string? Email { get; set; }                        // Email
        public bool IsActive { get; set; } = true;

        public Contractor() { }

        public Contractor(string code, string name, string? contactPerson = null, string? phoneNumber = null)
        {
            Code = code;
            Name = name;
            ContactPerson = contactPerson;
            PhoneNumber = phoneNumber;
        }
    }
}
