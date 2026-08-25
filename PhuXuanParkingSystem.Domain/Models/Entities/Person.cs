using PhuXuanParkingSystem.Models.Common;
using PhuXuanParkingSystem.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PhuXuanParkingSystem.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho thông tin người dùng / chủ phương tiện (Cán bộ, Nhân viên, Nhà thầu, Khách)
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Person : BaseEntity
    {
        public string Code { get; set; } = string.Empty;              // Mã nhân viên / Mã định danh
        public string FullName { get; set; } = string.Empty;          // Họ và tên
        [BsonRepresentation(BsonType.ObjectId)]
        public string? DepartmentId { get; set; }                     // ID Phòng ban / Bộ phận
        public string? PhoneNumber { get; set; }                      // Số điện thoại
        public string? Email { get; set; }                            // Email
        [BsonRepresentation(BsonType.String)]
        public PersonType Type { get; set; } = PersonType.Employee;   // Loại người dùng (Employee, Contractor, Visitor, VIP, Other)
        [BsonRepresentation(BsonType.ObjectId)]
        public string? CompanyId { get; set; }                        // Liên kết công ty / đơn vị thành viên
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ContractorId { get; set; }                     // Liên kết đơn vị nhà thầu nếu là nhân sự nhà thầu
        public bool IsActive { get; set; } = true;                    // Trạng thái hoạt động

        public Person() { }

        public Person(string code, string fullName, PersonType type = PersonType.Employee)
        {
            Code = code;
            FullName = fullName;
            Type = type;
        }
    }
}
