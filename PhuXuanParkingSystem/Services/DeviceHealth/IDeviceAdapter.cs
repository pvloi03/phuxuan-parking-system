using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.DeviceHealth
{
    /// <summary>
    /// Giao diện adapter cho thiết bị phần cứng - phục vụ Health Check & Quản lý kết nối
    /// Tách biệt responsibility: SDK lo low-level, adapter này expose high-level state
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
        /// Ping TCP đến IP:Port của thiết bị
        /// Dùng cho health check - không trust SDK callbacks
        /// </summary>
        /// <param name="timeoutMs">Thời gian timeout (mặc định 2000ms)</param>
        /// <param name="cancellationToken">Token hủy</param>
        /// <returns>TRUE nếu kết nối TCP thành công</returns>
        Task<bool> PingAsync(int timeoutMs = 2000, CancellationToken cancellationToken = default);

        /// <summary>
        /// Thử kết nối hoặc reconnect tới thiết bị qua SDK
        /// </summary>
        /// <param name="device">Thông tin thiết bị từ MongoDB</param>
        /// <param name="cancellationToken">Token hủy</param>
        /// <returns>TRUE nếu kết nối thành công</returns>
        Task<bool> ConnectAsync(Device device, CancellationToken cancellationToken = default);

        /// <summary>
        /// Ngắt kết nối khỏi thiết bị
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Khởi động lại kết nối: Disconnect → Wait → Reconnect
        /// Dùng khi cần reset hardware connection
        /// </summary>
        /// <param name="device">Thông tin thiết bị từ MongoDB</param>
        /// <param name="cancellationToken">Token hủy</param>
        /// <returns>TRUE nếu restart thành công</returns>
        Task<bool> RestartAsync(Device device, CancellationToken cancellationToken = default);

        /// <summary>
        /// Bắt đầu hiển thị luồng trực tiếp (Live View) lên cửa sổ WinForms
        /// </summary>
        /// <param name="windowHandle">Handle của cửa sổ/Panel WinForms</param>
        /// <returns>TRUE nếu khởi chạy preview thành công</returns>
        bool StartPreview(IntPtr windowHandle);

        /// <summary>
        /// Dừng hiển thị luồng trực tiếp (Live View)
        /// </summary>
        void StopPreview();

        /// <summary>
        /// Event khi trạng thái kết nối thay đổi (Connected, Disconnected, Streaming, Error)
        /// </summary>
        event EventHandler<DeviceStatus>? OnConnectionStateChanged;
    }
}

