using System;
using PhuXuanParkingSystem.Models.Common;
using PhuXuanParkingSystem.Models.Enums;
using MongoDB.Bson.Serialization.Attributes;

namespace PhuXuanParkingSystem.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho một thiết bị phần cứng trong hệ thống
    /// </summary>
    [BsonIgnoreExtraElements]
    public class Device : BaseEntity
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DeviceType Type { get; set; }

        public string IpAddress { get; set; } = string.Empty;
        public int Port { get; set; } = 8000;
        public string? UserName { get; set; }
        public string? Password { get; set; }

        public int? CameraChannel { get; set; } = 1;
        public string? RtspUrl { get; set; }

        public DeviceStatus Status { get; set; } = DeviceStatus.Disconnected;
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? LastHeartbeat { get; set; }
        public string? ErrorMessage { get; set; }

        public Device() { }

        public Device(string code, string name, DeviceType type, string ipAddress, int port = 8000)
        {
            Code = code;
            Name = name;
            Type = type;
            IpAddress = ipAddress;
            Port = port;
        }
    }
}
