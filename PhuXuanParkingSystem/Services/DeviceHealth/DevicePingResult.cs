using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using System;

namespace PhuXuanParkingSystem.Services.DeviceHealth
{
    /// <summary>
    /// Kết quả kiểm tra kết nối & sức khỏe của một thiết bị phần cứng
    /// </summary>
    public class DevicePingResult
    {
        public Device Device { get; set; } = null!;
        public bool IsSuccess { get; set; }
        public DeviceStatus Status { get; set; } = DeviceStatus.Disconnected;
        public long LatencyMs { get; set; }
        public DateTime CheckedAt { get; set; } = DateTime.Now;
        public string? ErrorMessage { get; set; }
        public string Details { get; set; } = string.Empty;

        public static DevicePingResult Success(Device device, long latencyMs, string details = "Kết nối thành công")
        {
            return new DevicePingResult
            {
                Device = device,
                IsSuccess = true,
                Status = DeviceStatus.Connected,
                LatencyMs = latencyMs,
                CheckedAt = DateTime.Now,
                Details = details
            };
        }

        public static DevicePingResult Fail(Device device, string error, long latencyMs = 0)
        {
            return new DevicePingResult
            {
                Device = device,
                IsSuccess = false,
                Status = DeviceStatus.Disconnected,
                LatencyMs = latencyMs,
                CheckedAt = DateTime.Now,
                ErrorMessage = error,
                Details = $"Mất kết nối: {error}"
            };
        }
    }
}
