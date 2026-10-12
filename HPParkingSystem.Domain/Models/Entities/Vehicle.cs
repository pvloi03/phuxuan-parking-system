using HPParkingSystem.Models.Common;
using HPParkingSystem.Models.Enums;
using HPParkingSystem.Models.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace HPParkingSystem.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho phương tiện giao thông đăng ký trong hệ thống (Tối giản)
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Vehicle : BaseEntity
    {
        // =========================================================================
        // --- CÁC TRƯỜNG LƯU TRỮ DATABASE (PERSISTED PROPERTIES) ---
        // =========================================================================
        public string PlateNumber { get; set; } = string.Empty;                          // [LƯU DB] Biển số xe (chuỗi chuẩn hóa)
        [BsonRepresentation(BsonType.String)]
        public VehicleType Type { get; set; } = VehicleType.Car;                         // [LƯU DB] Loại xe (Ô tô, Xe máy...)
        [BsonRepresentation(BsonType.ObjectId)]
        public string? OwnerPersonId { get; set; }                                       // [LƯU DB] Khóa ngoại liên kết chủ xe (Person)
        public bool IsActive { get; set; } = true;                                       // [LƯU DB] Trạng thái hoạt động
        public double? Weight { get; set; }                                              // [LƯU DB] Khối lượng phương tiện (đơn vị: tấn)

        public Vehicle() { }

        public Vehicle(string plateNumber, VehicleType type = VehicleType.Car, string? ownerPersonId = null, double? weight = null)
        {
            if (weight.HasValue && weight.Value < 0)
                throw new System.ArgumentOutOfRangeException(nameof(weight), "Khối lượng xe không thể nhỏ hơn 0.");

            PlateNumber = ValueObjects.PlateNumber.Clean(plateNumber);
            Type = type;
            OwnerPersonId = ownerPersonId;
            Weight = weight;
        }
    }
}
