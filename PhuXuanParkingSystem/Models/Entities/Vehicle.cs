using PhuXuanParkingSystem.Models.Common;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Models.ValueObjects;
using MongoDB.Bson.Serialization.Attributes;

namespace PhuXuanParkingSystem.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho phương tiện giao thông đăng ký trong hệ thống
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Vehicle : BaseEntity
    {
        public string PlateNumber { get; set; } = string.Empty;
        public VehicleType Type { get; set; } = VehicleType.Car;
        public string? OwnerPersonId { get; set; }
        public string? OwnerName { get; set; }
        public string? Brand { get; set; }
        public string? Color { get; set; }
        public bool IsActive { get; set; } = true;

        public Vehicle() { }

        public Vehicle(string plateNumber, VehicleType type = VehicleType.Car, string? ownerPersonId = null, string? ownerName = null)
        {
            PlateNumber = ValueObjects.PlateNumber.Clean(plateNumber);
            Type = type;
            OwnerPersonId = ownerPersonId;
            OwnerName = ownerName;
        }
    }
}
