using System;
using PhuXuanParkingSystem.Models.Common;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Models.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PhuXuanParkingSystem.Models.Entities
{
    /// <summary>
    /// Aggregate Root đại diện cho một Phiên gửi xe / Lượt vào-ra trọn vẹn (Tối giản)
    /// </summary>
    [BsonIgnoreExtraElements]
    public class ParkingSession : BaseEntity
    {
        // =========================================================================
        // --- 1. CÁC TRƯỜNG LƯU TRỮ DATABASE (PERSISTED PROPERTIES) ---
        // =========================================================================

        // --- THÔNG TIN PHƯƠNG TIỆN ---
        public string PlateNumber { get; set; } = string.Empty;                        // [LƯU DB] Biển số xe (chuỗi chuẩn hóa)
        [BsonRepresentation(BsonType.String)]
        public VehicleType VehicleType { get; set; } = VehicleType.Car;                 // [LƯU DB] Loại xe (Ô tô, Xe máy...)
        [BsonRepresentation(BsonType.String)]
        public ParkingSessionStatus Status { get; set; } = ParkingSessionStatus.Active;// [LƯU DB] Trạng thái (Active, Completed, UnmatchedOut)

        // --- ĐỊNH DANH ĐỐI TƯỢNG (TỐI GIẢN) ---
        public string? PersonName { get; set; }                                        // [LƯU DB] Tên chủ xe / người lái (null nếu là người lạ / xe lạ)

        // --- THÔNG TIN LƯỢT VÀO (CHECK-IN) ---
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? InTime { get; set; }                                          // [LƯU DB] Thời gian xe vào
        public string? InLaneName { get; set; }                                        // [LƯU DB] Tên làn vào
        public ImageStoragePath InOverviewImagePath { get; set; } = ImageStoragePath.Empty; // [LƯU DB] Đường dẫn UNC ảnh toàn cảnh lúc vào
        public ImageStoragePath InPlateImagePath { get; set; } = ImageStoragePath.Empty;    // [LƯU DB] Đường dẫn UNC ảnh biển số lúc vào

        // --- THÔNG TIN LƯỢT RA (CHECK-OUT) ---
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? OutTime { get; set; }                                         // [LƯU DB] Thời gian xe ra
        public string? OutLaneName { get; set; }                                       // [LƯU DB] Tên làn ra
        public ImageStoragePath OutOverviewImagePath { get; set; } = ImageStoragePath.Empty;// [LƯU DB] Đường dẫn UNC ảnh toàn cảnh lúc ra
        public ImageStoragePath OutPlateImagePath { get; set; } = ImageStoragePath.Empty;   // [LƯU DB] Đường dẫn UNC ảnh biển số lúc ra

        // --- GHI CHÚ PHIÊN ---
        public string? Note { get; set; }                                              // [LƯU DB] Ghi chú chung của phiên

        // =========================================================================
        // --- 2. CÁC THUỘC TÍNH KHÔNG LƯU DATABASE (COMPUTED GETTERS TRONG RAM) ---
        // =========================================================================
        
        /// <summary>
        /// [KHÔNG LƯU DB] Phân biệt xe lạ / người lạ (khi không có tên chủ xe)
        /// </summary>
        [BsonIgnore]
        public bool IsUnknown => string.IsNullOrWhiteSpace(PersonName);

        /// <summary>
        /// [KHÔNG LƯU DB] Thời gian lưu bãi (tự động tính từ InTime đến OutTime)
        /// </summary>
        [BsonIgnore]
        public TimeSpan? Duration => (InTime.HasValue && OutTime.HasValue && OutTime >= InTime)
            ? OutTime.Value - InTime.Value
            : null;

        public ParkingSession() { }

        /// <summary>
        /// Khởi tạo phiên xe vào (Check-in)
        /// </summary>
        public static ParkingSession CheckIn(
            string inLaneName,
            string plateNumber,
            ImageStoragePath inOverviewImagePath,
            ImageStoragePath inPlateImagePath,
            string? personName = null,
            VehicleType vehicleType = VehicleType.Car,
            string? note = null)
        {
            return new ParkingSession
            {
                InLaneName = inLaneName,
                PlateNumber = PhuXuanParkingSystem.Models.ValueObjects.PlateNumber.Clean(plateNumber),
                InOverviewImagePath = inOverviewImagePath ?? ImageStoragePath.Empty,
                InPlateImagePath = inPlateImagePath ?? ImageStoragePath.Empty,
                PersonName = personName,
                VehicleType = vehicleType,
                Note = note,
                InTime = DateTime.Now,
                Status = ParkingSessionStatus.Active
            };
        }

        /// <summary>
        /// Hoàn thành phiên khi xe ra (Check-out)
        /// </summary>
        public void CheckOut(
            string outLaneName,
            ImageStoragePath outOverviewImagePath,
            ImageStoragePath outPlateImagePath,
            string? note = null)
        {
            OutLaneName = outLaneName;
            OutOverviewImagePath = outOverviewImagePath ?? ImageStoragePath.Empty;
            OutPlateImagePath = outPlateImagePath ?? ImageStoragePath.Empty;
            if (!string.IsNullOrWhiteSpace(note))
            {
                Note = string.IsNullOrWhiteSpace(Note) ? note : $"{Note}; {note}";
            }
            OutTime = DateTime.Now;
            Status = ParkingSessionStatus.Completed;
            UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// Tạo phiên khi xe ra mà không có bản ghi xe vào tương ứng (Unmatched Out)
        /// </summary>
        public static ParkingSession CreateUnmatchedOut(
            string outLaneName,
            string plateNumber,
            ImageStoragePath outOverviewImagePath,
            ImageStoragePath outPlateImagePath,
            string? personName = null,
            VehicleType vehicleType = VehicleType.Car,
            string? note = null)
        {
            return new ParkingSession
            {
                OutLaneName = outLaneName,
                PlateNumber = PhuXuanParkingSystem.Models.ValueObjects.PlateNumber.Clean(plateNumber),
                OutOverviewImagePath = outOverviewImagePath ?? ImageStoragePath.Empty,
                OutPlateImagePath = outPlateImagePath ?? ImageStoragePath.Empty,
                PersonName = personName,
                VehicleType = vehicleType,
                Note = note,
                OutTime = DateTime.Now,
                Status = ParkingSessionStatus.UnmatchedOut
            };
        }
    }
}
