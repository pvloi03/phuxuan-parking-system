using PhuXuanParkingSystem.Models.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.DeviceHealth
{
    /// <summary>
    /// Giao diện adapter cho thiết bị phần cứng - phục vụ Health Check
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
    }
}
