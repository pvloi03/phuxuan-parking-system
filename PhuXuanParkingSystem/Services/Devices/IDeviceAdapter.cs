using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Devices
{
    /// <summary>
    /// Giao diện adapter chuẩn cho mọi loại thiết bị phần cứng trong hệ thống bãi xe (Camera, Controller, Barrier,...)
    /// Tách biệt responsibility: Low-level SDK do từng service thiết bị quản lý, IDeviceAdapter expose trạng thái & chu kỳ sống
    /// </summary>
    public interface IDeviceAdapter
    {
        /// <summary>
        /// Trạng thái kết nối SDK hiện tại
        /// TRUE = thiết bị đã login/connect thành công với SDK
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// TRUE = đang streaming video hoặc nhận log (Controller)
        /// </summary>
        bool IsStreaming { get; }

        /// <summary>
        /// Ping TCP Socket đến IP:Port của thiết bị
        /// Dùng cho health check - không phụ thuộc SDK callbacks
        /// </summary>
        Task<bool> PingAsync(int timeoutMs = 2000, CancellationToken cancellationToken = default);

        /// <summary>
        /// Thử kết nối hoặc reconnect tới thiết bị qua SDK
        /// </summary>
        Task<bool> ConnectAsync(Device device, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ngắt kết nối khỏi thiết bị
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Khởi động lại kết nối: Disconnect → Wait → Reconnect
        /// </summary>
        Task<bool> RestartAsync(Device device, CancellationToken cancellationToken = default);

        /// <summary>
        /// Event khi trạng thái kết nối thay đổi (Connected, Disconnected, Streaming, Error)
        /// </summary>
        event EventHandler<DeviceStatus>? OnConnectionStateChanged;
    }
}
