using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Services.Logging;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Devices.Camera
{
    /// <summary>
    /// Lớp cơ sở trừu tượng cho tất cả Camera Service (NST PlateCamera, Hikvision OverviewCamera,...)
    /// Quản lý cấu hình, vòng đời kết nối, serialize snapshot và tích hợp IDeviceAdapter
    /// </summary>
    public abstract class CameraServiceBase : ICameraService
    {
        protected readonly object _lockObj = new();
        protected readonly SemaphoreSlim _captureSemaphore = new(1, 1);
        private bool _isDisposed;

        public CameraConfig Config { get; set; } = new();

        public bool IsLoggedIn { get; protected set; }
        public bool IsStreaming { get; protected set; }
        public bool IsConnected => IsLoggedIn;

        public event EventHandler<DeviceStatus>? OnConnectionStateChanged;

        protected abstract string LogTag { get; }
        protected abstract string LogCategory { get; }

        public async Task<bool> PingAsync(int timeoutMs = 2000, CancellationToken cancellationToken = default)
        {
            if (Config == null || string.IsNullOrEmpty(Config.Ip))
                return false;

            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(Config.Ip, Config.Port);
                var delayTask = Task.Delay(timeoutMs, cancellationToken);
                var completed = await Task.WhenAny(connectTask, delayTask).ConfigureAwait(false);

                return completed == connectTask && client.Connected;
            }
            catch
            {
                return false;
            }
        }

        public virtual async Task<bool> ConnectAsync(Device device, CancellationToken cancellationToken = default)
        {
            ApplyDeviceConfig(device);
            return await LoginAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual Task DisconnectAsync()
        {
            Logout();
            return Task.CompletedTask;
        }

        public virtual async Task<bool> RestartAsync(Device device, CancellationToken cancellationToken = default)
        {
            Logout();
            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            ApplyDeviceConfig(device);
            return await LoginAsync(cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task<bool> CaptureToFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var bytes = await CaptureSnapshotAsync(cancellationToken).ConfigureAwait(false);
            return await CameraCaptureHelper.SaveBytesToFileAsync(bytes, filePath, LogTag, LogCategory, cancellationToken).ConfigureAwait(false);
        }

        public abstract Task<bool> LoginAsync(CancellationToken cancellationToken = default);
        public abstract bool StartPreview(IntPtr windowHandle);
        public abstract Task<byte[]?> CaptureSnapshotAsync(CancellationToken cancellationToken = default);
        public abstract void StopPreview();
        public abstract void Logout();

        protected void RaiseConnectionStateChanged(DeviceStatus status)
        {
            OnConnectionStateChanged?.Invoke(this, status);
        }

        public virtual void ApplyDeviceConfig(Device device)
        {
            if (device != null)
            {
                Config.Ip = device.IpAddress ?? string.Empty;
                Config.Port = (ushort)device.Port;
                Config.UserName = device.UserName ?? string.Empty;
                Config.Password = device.Password ?? string.Empty;
            }
        }

        public virtual void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            StopPreview();
            Logout();
            _captureSemaphore.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
