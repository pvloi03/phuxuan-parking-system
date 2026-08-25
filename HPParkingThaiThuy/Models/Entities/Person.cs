using HPParkingThaiThuy.Models.Common;
using HPParkingThaiThuy.Models.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace HPParkingThaiThuy.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho thông tin người dùng / chủ phương tiện (Cán bộ, Nhân viên, Nhà thầu, Khách)
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Person : BaseEntity
    {
        public string Code { get; set; } = string.Empty;              // Mã nhân viên / Mã định danh
        public string FullName { get; set; } = string.Empty;          // Họ và tên
        public string? DepartmentId { get; set; }                     // ID Phòng ban / Bộ phận
        public string? DepartmentName { get; set; }                   // Tên Phòng ban
        public string? PhoneNumber { get; set; }                      // Số điện thoại
        public string? Email { get; set; }                            // Email
        public PersonType Type { get; set; } = PersonType.Employee;   // Loại người dùng
        public string? CompanyId { get; set; }                        // Liên kết công ty thành viên
        public string? ContractorId { get; set; }                     // Liên kết đơn vị nhà thầu
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
