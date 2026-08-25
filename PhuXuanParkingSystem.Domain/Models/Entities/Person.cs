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
        // =========================================================================
        // --- CÁC TRƯỜNG LƯU TRỮ DATABASE (PERSISTED PROPERTIES) ---
        // =========================================================================
        public string Code { get; set; } = string.Empty;              // [LƯU DB] Mã nhân viên / Mã định danh
        public string FullName { get; set; } = string.Empty;          // [LƯU DB] Họ và tên
        [BsonRepresentation(BsonType.ObjectId)]
        public string? DepartmentId { get; set; }                     // [LƯU DB] ID Phòng ban / Bộ phận
        public string? PhoneNumber { get; set; }                      // [LƯU DB] Số điện thoại
        public string? Email { get; set; }                            // [LƯU DB] Email
        [BsonRepresentation(BsonType.String)]
        public PersonType Type { get; set; } = PersonType.Employee;   // [LƯU DB] Loại người dùng (Employee, Contractor, Visitor, VIP, Other)
        [BsonRepresentation(BsonType.ObjectId)]
        public string? CompanyId { get; set; }                        // [LƯU DB] Liên kết công ty / đơn vị thành viên
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ContractorId { get; set; }                     // [LƯU DB] Liên kết đơn vị nhà thầu nếu là nhân sự nhà thầu
        public bool IsActive { get; set; } = true;                    // [LƯU DB] Trạng thái hoạt động

        public Person() { }

        public Person(string code, string fullName, PersonType type = PersonType.Employee)
        {
            Code = code;
            FullName = fullName;
            Type = type;
        }
    }
}
