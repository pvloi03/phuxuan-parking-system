using HPParkingSystem.Models.Common;
using HPParkingSystem.Models.Enums;
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
        private string _plateNumber = string.Empty;
        public string PlateNumber
        {
            get => _plateNumber;
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (!ValueObjects.PlateNumber.IsValid(value))
                        throw new System.ArgumentException("Biển số xe không hợp lệ (Domain Invariant #3).", nameof(value));
                    _plateNumber = ValueObjects.PlateNumber.Clean(value);
                }
                else
                {
                    _plateNumber = string.Empty;
                }
            }
        }

        [BsonRepresentation(BsonType.String)]
        public VehicleType Type { get; set; } = VehicleType.Car;                         // [LƯU DB] Loại xe (Ô tô, Xe máy...)
        [BsonRepresentation(BsonType.ObjectId)]
        public string? OwnerPersonId { get; set; }                                       // [LƯU DB] Khóa ngoại liên kết chủ xe (Person)
        public bool IsActive { get; set; } = true;                                       // [LƯU DB] Trạng thái hoạt động
        private double? _weight;
        public double? Weight
        {
            get => _weight;
            set
            {
                if (value.HasValue && value.Value < 0)
                    throw new System.ArgumentOutOfRangeException(nameof(value), "Khối lượng xe không thể nhỏ hơn 0.");
                _weight = value;
            }
        }

        public Vehicle() { }

        public Vehicle(string plateNumber, VehicleType type = VehicleType.Car, string? ownerPersonId = null, double? weight = null)
        {
            PlateNumber = plateNumber;
            Type = type;
            OwnerPersonId = ownerPersonId;
            Weight = weight;
        }

        /// <summary>
        /// Domain Factory Method khởi tạo Vehicle tuân thủ các quy tắc bất biến miền (Domain Invariants)
        /// </summary>
        public static Vehicle Create(
            string plateNumber,
            VehicleType type = VehicleType.Car,
            string? ownerPersonId = null,
            double? weight = null,
            bool isActive = true)
        {
            var vehicle = new Vehicle(plateNumber, type, ownerPersonId, weight)
            {
                IsActive = isActive
            };
            return vehicle;
        }
    }
}
