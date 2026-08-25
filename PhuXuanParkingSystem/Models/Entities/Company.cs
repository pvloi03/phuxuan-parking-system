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
        public string Code { get; set; } = string.Empty;          // Mã công ty (HP, TT...)
        public string Name { get; set; } = string.Empty;          // Tên đầy đủ
        public string? TaxCode { get; set; }                      // Mã số thuế
        public string? Address { get; set; }                      // Địa chỉ
        public string? PhoneNumber { get; set; }                  // Số điện thoại
        public string? Email { get; set; }                        // Email
        public bool IsActive { get; set; } = true;

        public Company() { }

        public Company(string code, string name, string? taxCode = null, string? address = null)
        {
            Code = code;
            Name = name;
            TaxCode = taxCode;
            Address = address;
        }
    }
}
