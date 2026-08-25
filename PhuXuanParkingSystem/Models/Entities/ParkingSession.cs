using PhuXuanParkingSystem.Models.Common;
using PhuXuanParkingSystem.Models.Enums;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace PhuXuanParkingSystem.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho một Phiên gửi xe / Lượt vào-ra trọn vẹn
    /// Lưu trữ đầy đủ thông tin thời gian, biển số, hình ảnh và phân luồng làn
    /// </summary>
    [BsonIgnoreExtraElements]
    public class ParkingSession : BaseEntity
    {
        // --- THÔNG TIN PHƯƠNG TIỆN ---
        public string PlateNumber { get; set; } = string.Empty;
        public VehicleType VehicleType { get; set; } = VehicleType.Car;
        public ParkingSessionStatus Status { get; set; } = ParkingSessionStatus.Active;

        // --- THÔNG TIN CHỦ XE & ĐƠN VỊ ---
        public string? PersonId { get; set; }
        public string? PersonName { get; set; }
        public string? DepartmentName { get; set; }
        public string? PhoneNumber { get; set; }

        // --- THÔNG TIN LƯỢT VÀO (CHECK-IN) ---
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? InTime { get; set; }
        public string? InLaneId { get; set; }
        public string? InOverviewImagePath { get; set; }
        public string? InPlateImagePath { get; set; }

        // --- THÔNG TIN LƯỢT RA (CHECK-OUT) ---
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? OutTime { get; set; }
        public string? OutLaneId { get; set; }
        public string? OutOverviewImagePath { get; set; }
        public string? OutPlateImagePath { get; set; }

        // --- GHI CHÚ PHIÊN ---
        public string? Note { get; set; }

        // --- COMPUTED PROPERTIES (KHÔNG LƯU DB) ---
        [BsonIgnore]
        public bool IsUnknown => string.IsNullOrWhiteSpace(PersonName);

        [BsonIgnore]
        public TimeSpan? Duration => (InTime.HasValue && OutTime.HasValue && OutTime >= InTime)
            ? OutTime.Value - InTime.Value
            : null;

        public ParkingSession() { }

        /// <summary>
        /// Khởi tạo phiên xe vào (Check-in)
        /// </summary>
        public static ParkingSession CheckIn(
            string inLaneId,
            string plateNumber,
            string inOverviewImagePath,
            string inPlateImagePath,
            string? personId = null,
            string? personName = null,
            string? departmentName = null,
            VehicleType vehicleType = VehicleType.Car,
            string? note = null)
        {
            return new ParkingSession
            {
                InLaneId = inLaneId,
                PlateNumber = ValueObjects.PlateNumber.Clean(plateNumber),
                InOverviewImagePath = inOverviewImagePath,
                InPlateImagePath = inPlateImagePath,
                PersonId = personId,
                PersonName = personName,
                DepartmentName = departmentName,
                VehicleType = vehicleType,
                Note = note,
                InTime = DateTime.Now,
                Status = ParkingSessionStatus.Active,
                CreatedAt = DateTime.Now
            };
        }

        /// <summary>
        /// Hoàn thành phiên khi xe ra (Check-out)
        /// </summary>
        public void CheckOut(
            string outLaneId,
            string outOverviewImagePath,
            string outPlateImagePath,
            string? note = null)
        {
            OutLaneId = outLaneId;
            OutOverviewImagePath = outOverviewImagePath;
            OutPlateImagePath = outPlateImagePath;
            if (!string.IsNullOrWhiteSpace(note))
            {
                Note = string.IsNullOrWhiteSpace(Note) ? note : $"{Note}; {note}";
            }
            OutTime = DateTime.Now;
            Status = ParkingSessionStatus.Completed;
            UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// Tạo phiên khi xe ra mà không tìm thấy bản ghi xe vào tương ứng (Unmatched Out)
        /// </summary>
        public static ParkingSession CreateUnmatchedOut(
            string outLaneId,
            string plateNumber,
            string outOverviewImagePath,
            string outPlateImagePath,
            string? personName = null,
            VehicleType vehicleType = VehicleType.Car,
            string? note = null)
        {
            return new ParkingSession
            {
                OutLaneId = outLaneId,
                PlateNumber = ValueObjects.PlateNumber.Clean(plateNumber),
                OutOverviewImagePath = outOverviewImagePath,
                OutPlateImagePath = outPlateImagePath,
                PersonName = personName,
                VehicleType = vehicleType,
                Note = note,
                OutTime = DateTime.Now,
                Status = ParkingSessionStatus.UnmatchedOut,
                CreatedAt = DateTime.Now
            };
        }
    }
}
