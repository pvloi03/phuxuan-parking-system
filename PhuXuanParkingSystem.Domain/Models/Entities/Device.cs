using System;
using PhuXuanParkingSystem.Models.Common;
using PhuXuanParkingSystem.Models.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace PhuXuanParkingSystem.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho một thiết bị phần cứng trong hệ thống
    /// (Camera Hikvision, Camera NST, Camera ONVIF, Bộ điều khiển ZKTeco C3-200)
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Device : BaseEntity
    {
        // =========================================================================
        // --- CÁC TRƯỜNG LƯU TRỮ DATABASE (PERSISTED PROPERTIES) ---
        // =========================================================================
        public string Code { get; set; } = string.Empty;                      // [LƯU DB] Mã thiết bị (CAM-01, C3-01...)
        public string Name { get; set; } = string.Empty;                      // [LƯU DB] Tên mô tả thiết bị
        [BsonRepresentation(BsonType.String)]
        public DeviceType Type { get; set; }                                  // [LƯU DB] Phân loại thiết bị

        // --- CẤU HÌNH MẠNG & XÁC THỰC ---
        public string IpAddress { get; set; } = string.Empty;                 // [LƯU DB] Địa chỉ IP thiết bị
        public int Port { get; set; } = 8000;                                 // [LƯU DB] Port kết nối chính (Hik: 8000, NST: 3000, ZKTeco: 4370)
        public string? UserName { get; set; }                                 // [LƯU DB] Tên đăng nhập (Camera)
        public string? Password { get; set; }                                 // [LƯU DB] Mật khẩu (Camera hoặc ZKTeco CommPassword)
        public string? LaneId { get; set; }                                   // [LƯU DB] ID Làn kiểm soát gán thiết bị
        public string? LaneName { get; set; }                                 // [LƯU DB] Tên Làn kiểm soát (Làn Vào 1, Làn Ra 1...)
        public string? Note { get; set; }                                     // [LƯU DB] Ghi chú thiết bị
        public bool IsActive { get; set; } = true;                            // [LƯU DB] Trạng thái hoạt động

        // --- TRẠNG THÁI SỨC KHỎE THIẾT BỊ ---
        [BsonRepresentation(BsonType.String)]
        public DeviceStatus Status { get; set; } = DeviceStatus.Disconnected;// [LƯU DB] Trạng thái kết nối
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? LastHeartbeat { get; set; }                          // [LƯU DB] Thời điểm heartbeat / ping thành công gần nhất
        public string? ErrorMessage { get; set; }                             // [LƯU DB] Chi tiết lỗi kết nối gần nhất

        public Device() { }

        public Device(string code, string name, DeviceType type, string ipAddress, int port = 8000)
        {
            Code = code;
            Name = name;
            Type = type;
            IpAddress = ipAddress;
            Port = port;
        }

        public void MarkConnected()
        {
            Status = DeviceStatus.Connected;
            LastHeartbeat = DateTime.Now;
            ErrorMessage = null;
            UpdatedAt = DateTime.Now;
        }

        public void MarkError(string errorMessage)
        {
            Status = DeviceStatus.Error;
            ErrorMessage = errorMessage;
            UpdatedAt = DateTime.Now;
        }

        public void MarkDisconnected()
        {
            Status = DeviceStatus.Disconnected;
            UpdatedAt = DateTime.Now;
        }
    }
}
