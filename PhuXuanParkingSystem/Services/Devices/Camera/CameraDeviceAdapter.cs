using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Services.Devices;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Devices.Camera
{
    /// <summary>
    /// Adapter dùng chung cho tất cả các loại camera triển khai ICameraService
    /// Bọc ICameraService thành chuẩn IDeviceAdapter
    /// </summary>
    public class CameraDeviceAdapter : IDeviceAdapter
    {
        private readonly ICameraService _cameraService;

        public CameraDeviceAdapter(ICameraService cameraService)
        {
            _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
            _cameraService.OnConnectionStateChanged += (s, state) =>
                OnConnectionStateChanged?.Invoke(this, state);
        }

        public bool IsConnected => _cameraService.IsLoggedIn;
        public bool IsStreaming => _cameraService.IsStreaming;

        public event EventHandler<DeviceStatus>? OnConnectionStateChanged;

        public async Task<bool> PingAsync(int timeoutMs = 2000, CancellationToken cancellationToken = default)
        {
            if (_cameraService.Config == null || string.IsNullOrEmpty(_cameraService.Config.Ip))
                return false;

            try
            {
                using var client = new TcpClient();
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeoutMs);

                var connectTask = client.ConnectAsync(_cameraService.Config.Ip, _cameraService.Config.Port);
                var delayTask = Task.Delay(timeoutMs, cts.Token);
                var completed = await Task.WhenAny(connectTask, delayTask).ConfigureAwait(false);

                return completed == connectTask && client.Connected;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ConnectAsync(Device device, CancellationToken cancellationToken = default)
        {
            ApplyDeviceConfig(device);
            return await _cameraService.LoginAsync(cancellationToken).ConfigureAwait(false);
        }

        public Task DisconnectAsync()
        {
            _cameraService.Logout();
            return Task.CompletedTask;
        }

        public async Task<bool> RestartAsync(Device device, CancellationToken cancellationToken = default)
        {
            _cameraService.Logout();
            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            ApplyDeviceConfig(device);
            return await _cameraService.LoginAsync(cancellationToken).ConfigureAwait(false);
        }

        private void ApplyDeviceConfig(Device device)
        {
            if (_cameraService.Config != null && device != null)
            {
                _cameraService.Config.Ip = device.IpAddress;
                _cameraService.Config.Port = (ushort)device.Port;
                _cameraService.Config.UserName = device.UserName ?? string.Empty;
                _cameraService.Config.Password = device.Password ?? string.Empty;
            }
        }
    }
}
