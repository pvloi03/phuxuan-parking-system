using System;
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

    /// <summary>
    /// Entity đại diện cho Tài khoản người dùng hệ thống
    /// </summary>
    [BsonIgnoreExtraElements]
    public class User : BaseEntity
    {
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public UserRole Role { get; set; } = UserRole.Operator;
        public bool IsActive { get; set; } = true;
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? LastLoginTime { get; set; }

        public User() { }

        public User(string username, string fullName, UserRole role = UserRole.Operator)
        {
            Username = username;
            FullName = fullName;
            Role = role;
        }
    }
}
