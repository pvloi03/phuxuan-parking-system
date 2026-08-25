using PhuXuanParkingSystem.Models.Common;
using PhuXuanParkingSystem.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PhuXuanParkingSystem.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho một làn kiểm soát (Làn Vào / Làn Ra)
    /// Chịu trách nhiệm cấu hình phân bổ thiết bị cho từng làn
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Lane : BaseEntity
    {
        // =========================================================================
        // --- 1. CÁC TRƯỜNG LƯU TRỮ DATABASE (PERSISTED PROPERTIES) ---
        // =========================================================================
        public string Code { get; set; } = string.Empty;              // [LƯU DB] Mã làn (L01, L02...)
        public string Name { get; set; } = string.Empty;              // [LƯU DB] Tên làn (Làn Vào, Làn Ra...)
        [BsonRepresentation(BsonType.String)]
        public LaneDirection Direction { get; set; } = LaneDirection.In; // [LƯU DB] Chiều làn (In / Out)
        public string? Description { get; set; }                      // [LƯU DB] Mô tả làn
        public bool IsActive { get; set; } = true;                    // [LƯU DB] Trạng thái hoạt động

        // --- CẤU HÌNH THIẾT BỊ GẮN VỚI LÀN (THEO ID THAM CHIẾU BẢNG DEVICES) ---
        [BsonRepresentation(BsonType.ObjectId)]
        public string? OverviewCameraDeviceId { get; set; }           // [LƯU DB] ID Camera chụp ảnh toàn cảnh (Hikvision / ONVIF)
        [BsonRepresentation(BsonType.ObjectId)]
        public string? PlateCameraDeviceId { get; set; }              // [LƯU DB] ID Camera chụp ảnh nhận diện biển số (NST / ONVIF)

        // --- CẤU HÌNH BỘ ĐIỀU KHIỂN & CẢM BIẾN RADAR (DÙNG CHUNG CONTROLLER) ---
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ControllerDeviceId { get; set; }               // [LƯU DB] ID Bộ điều khiển ZKTeco C3-200 (dùng chung cho cả 2 làn)
        public int TriggerAuxPort { get; set; } = 1;                  // [LƯU DB] Cổng tín hiệu Aux In nhận tín hiệu Radar (1 = Làn Vào, 2 = Làn Ra)

        // =========================================================================
        // --- 2. CÁC ĐỐI TƯỢNG KHÔNG LƯU DATABASE (NAVIGATION RUNTIME OBJECTS) ---
        // =========================================================================
        [BsonIgnore]
        public Device? OverviewCamera { get; set; }                   // [KHÔNG LƯU DB] Đối tượng Camera toàn cảnh nạp lúc runtime
        [BsonIgnore]
        public Device? PlateCamera { get; set; }                      // [KHÔNG LƯU DB] Đối tượng Camera biển số nạp lúc runtime
        [BsonIgnore]
        public Device? Controller { get; set; }                       // [KHÔNG LƯU DB] Đối tượng Controller C3-200 nạp lúc runtime

        public Lane() { }

        public Lane(string code, string name, LaneDirection direction, int triggerAuxPort = 1)
        {
            Code = code;
            Name = name;
            Direction = direction;
            TriggerAuxPort = triggerAuxPort;
        }
    }
}
