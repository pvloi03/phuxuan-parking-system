using PhuXuanParkingSystem.Models.Common;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Models.ValueObjects;
using MongoDB.Bson.Serialization.Attributes;

namespace PhuXuanParkingSystem.Models.Entities
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
        public PlateNumber PlateNumber { get; set; } = PlateNumber.Create(string.Empty); // [LƯU DB] Biển số xe
        public VehicleType Type { get; set; } = VehicleType.Car;                         // [LƯU DB] Loại xe (Ô tô, Xe máy...)
        public string? OwnerPersonId { get; set; }                                       // [LƯU DB] Khóa ngoại liên kết chủ xe (Person)
        public bool IsActive { get; set; } = true;                                       // [LƯU DB] Trạng thái hoạt động

        public Vehicle() { }

        public Vehicle(PlateNumber plateNumber, VehicleType type = VehicleType.Car, string? ownerPersonId = null)
        {
            PlateNumber = plateNumber ?? PlateNumber.Create(string.Empty);
            Type = type;
            OwnerPersonId = ownerPersonId;
        }
    }
}
