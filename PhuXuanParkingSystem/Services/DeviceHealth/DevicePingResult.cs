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

        /// <summary>
        /// Số lần retry đã thực hiện (0 = thành công ngay, >0 = cần retry)
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// TRUE = kết nối mới (first-time connect), FALSE = reconnect
        /// </summary>
        public bool WasReconnected { get; set; } = false;

        public static DevicePingResult Success(
            Device device,
            long latencyMs,
            string details = "Kết nối thành công",
            int retryCount = 0)
        {
            return new DevicePingResult
            {
                Device = device,
                IsSuccess = true,
                Status = DeviceStatus.Connected,
                LatencyMs = latencyMs,
                CheckedAt = DateTime.Now,
                Details = details,
                RetryCount = retryCount,
                WasReconnected = retryCount > 0
            };
        }

        public static DevicePingResult Fail(Device device, string error, long latencyMs = 0, int retryCount = 0)
        {
            return new DevicePingResult
            {
                Device = device,
                IsSuccess = false,
                Status = DeviceStatus.Disconnected,
                LatencyMs = latencyMs,
                CheckedAt = DateTime.Now,
                ErrorMessage = error,
                Details = $"Mất kết nối: {error}",
                RetryCount = retryCount
            };
        }
    }
}
