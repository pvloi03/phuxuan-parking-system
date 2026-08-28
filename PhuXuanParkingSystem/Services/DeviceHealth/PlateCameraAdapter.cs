using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Services.Camera;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.DeviceHealth
{
    /// <summary>
    /// Adapter cho Camera Biển Số (NST SDK)
    /// Implements IDeviceAdapter cho DeviceHealthManager
    /// </summary>
    public class PlateCameraAdapter : IDeviceAdapter
    {
        private readonly PlateCameraService _cameraService;

        public PlateCameraAdapter(PlateCameraService cameraService)
        {
            _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
            _cameraService.OnConnectionStateChanged += (s, state) =>
                OnConnectionStateChanged?.Invoke(this, state);
        }

        /// <summary>
        /// Trạng thái kết nối SDK
        /// </summary>
        public bool IsConnected => _cameraService.IsLoggedIn;

        /// <summary>
        /// TRUE = đang streaming video
        /// </summary>
        public bool IsStreaming => _cameraService.IsStreaming;

        /// <summary>
        /// Event khi trạng thái kết nối thay đổi
        /// </summary>
        public event EventHandler<DeviceConnectionState>? OnConnectionStateChanged;

        /// <summary>
        /// Ping TCP đến camera IP:Port
        /// </summary>
        public async Task<bool> PingAsync(int timeoutMs = 2000, CancellationToken cancellationToken = default)
        {
            if (_cameraService.Config == null || string.IsNullOrEmpty(_cameraService.Config.Ip))
                return false;

            try
            {
                using var client = new TcpClient();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeoutMs);

                await client.ConnectAsync(_cameraService.Config.Ip, _cameraService.Config.Port);
                return client.Connected;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Thử kết nối/reconnect tới Camera Biển Số
        /// </summary>
        public async Task<bool> ConnectAsync(Device device, CancellationToken cancellationToken = default)
        {
            // Cập nhật config từ device entity
            if (_cameraService.Config != null)
            {
                _cameraService.Config.Ip = device.IpAddress;
                _cameraService.Config.Port = (ushort)device.Port;
                _cameraService.Config.UserName = device.UserName ?? string.Empty;
                _cameraService.Config.Password = device.Password ?? string.Empty;
            }

            return await _cameraService.LoginAsync(cancellationToken);
        }

        public Task DisconnectAsync()
        {
            _cameraService.Logout();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Khởi động lại: Logout → TaskDelay → Login
        /// </summary>
        public async Task<bool> RestartAsync(Device device, CancellationToken cancellationToken = default)
        {
            _cameraService.Logout();
            await Task.Delay(500, cancellationToken);

            if (_cameraService.Config != null)
            {
                _cameraService.Config.Ip = device.IpAddress;
                _cameraService.Config.Port = (ushort)device.Port;
                _cameraService.Config.UserName = device.UserName ?? string.Empty;
                _cameraService.Config.Password = device.Password ?? string.Empty;
            }

            return await _cameraService.LoginAsync(cancellationToken);
        }
    }
}
