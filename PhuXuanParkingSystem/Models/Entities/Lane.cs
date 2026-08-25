using PhuXuanParkingSystem.Models.Common;
using PhuXuanParkingSystem.Models.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace PhuXuanParkingSystem.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho một làn kiểm soát (Làn Vào / Làn Ra)
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Lane : BaseEntity
    {
        public string Code { get; set; } = string.Empty;              // Mã làn (L01, L02...)
        public string Name { get; set; } = string.Empty;              // Tên làn (Làn Vào, Làn Ra...)
        public LaneDirection Direction { get; set; } = LaneDirection.In; // Chiều làn (In / Out)
        public string? Description { get; set; }                      // Mô tả
        public bool IsActive { get; set; } = true;

        // Cấu hình thiết bị gán với làn
        public string? OverviewCameraDeviceId { get; set; }           // ID Camera toàn cảnh
        public string? PlateCameraDeviceId { get; set; }              // ID Camera biển số
        public string? ControllerDeviceId { get; set; }               // ID Controller C3-200
        public int TriggerAuxPort { get; set; } = 1;                  // Cổng AUX Radar (1 = Vào, 2 = Ra)

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
