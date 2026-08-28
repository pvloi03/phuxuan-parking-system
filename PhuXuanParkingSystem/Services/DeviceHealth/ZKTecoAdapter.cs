using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Services.Controller;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.DeviceHealth
{
    /// <summary>
    /// Adapter cho Controller ZKTeco C3-200 (Pull SDK)
    /// </summary>
    public class ZKTecoAdapter : IDeviceAdapter
    {
        private readonly ZKTecoDeviceAdapter _controllerAdapter;

        public ZKTecoAdapter(ZKTecoDeviceAdapter controllerAdapter)
        {
            _controllerAdapter = controllerAdapter ?? throw new ArgumentNullException(nameof(controllerAdapter));
            _controllerAdapter.OnConnectionStateChanged += (s, state) =>
                OnConnectionStateChanged?.Invoke(this, state);
        }

        /// <summary>
        /// Trạng thái kết nối SDK: _handle != IntPtr.Zero
        /// </summary>
        public bool IsConnected => _controllerAdapter.IsConnected;

        /// <summary>
        /// TRUE = đang nhận log từ controller
        /// </summary>
        public bool IsStreaming => _controllerAdapter.IsStreaming;

        /// <summary>
        /// Event khi trạng thái kết nối thay đổi
        /// </summary>
        public event EventHandler<DeviceConnectionState>? OnConnectionStateChanged;

        /// <summary>
        /// Ping TCP đến controller IP:Port
        /// </summary>
        public Task<bool> PingAsync(int timeoutMs = 2000, CancellationToken cancellationToken = default)
        {
            return _controllerAdapter.PingAsync(timeoutMs, cancellationToken);
        }

        /// <summary>
        /// Thử kết nối/reconnect tới Controller ZKTeco
        /// </summary>
        public async Task<bool> ConnectAsync(Device device, CancellationToken cancellationToken = default)
        {
            return await _controllerAdapter.ConnectAsync(
                ipAddress: device.IpAddress,
                port: device.Port,
                password: device.Password,
                cancellationToken: cancellationToken);
        }

        public Task DisconnectAsync()
        {
            return _controllerAdapter.DisconnectAsync();
        }

        /// <summary>
        /// Khởi động lại: Disconnect → TaskDelay → Connect
        /// </summary>
        public async Task<bool> RestartAsync(Device device, CancellationToken cancellationToken = default)
        {
            await _controllerAdapter.DisconnectAsync();
            await Task.Delay(500, cancellationToken);

            return await _controllerAdapter.ConnectAsync(
                ipAddress: device.IpAddress,
                port: device.Port,
                password: device.Password,
                cancellationToken: cancellationToken);
        }
    }
}
