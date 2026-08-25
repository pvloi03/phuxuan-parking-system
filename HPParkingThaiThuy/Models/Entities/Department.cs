using HPParkingThaiThuy.Models.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace HPParkingThaiThuy.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho Phòng ban / Bộ phận trong hệ thống
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Department : BaseEntity
    {
        public string Code { get; set; } = string.Empty;          // Mã phòng ban (PB-KT, PB-HC...)
        public string Name { get; set; } = string.Empty;          // Tên phòng ban đầy đủ
        public string? CompanyId { get; set; }                    // ID Công ty trực thuộc
        public string? PhoneNumber { get; set; }                  // Số điện thoại liên hệ
        public string? Email { get; set; }                        // Email phòng ban
        public bool IsActive { get; set; } = true;                // Trạng thái hoạt động

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
