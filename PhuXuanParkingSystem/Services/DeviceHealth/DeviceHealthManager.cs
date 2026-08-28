using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Services.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.DeviceHealth
{
    /// <summary>
    /// Event arguments cho thay đổi trạng thái thiết bị
    /// </summary>
    public class DeviceStateChangedEventArgs : EventArgs
    {
        public string DeviceId { get; }
        public DeviceConnectionState OldState { get; }
        public DeviceConnectionState NewState { get; }
        public DateTime Timestamp { get; }

        public DeviceStateChangedEventArgs(string deviceId, DeviceConnectionState oldState, DeviceConnectionState newState)
        {
            DeviceId = deviceId;
            OldState = oldState;
            NewState = newState;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Interface cho DeviceHealthManager - quản lý health check và state của tất cả devices
    /// </summary>
    public interface IDeviceHealthManager
    {
        /// <summary>
        /// Lấy trạng thái hiện tại của một device
        /// </summary>
        DeviceConnectionState GetState(string deviceId);

        /// <summary>
        /// Kiểm tra device có đang streaming không
        /// </summary>
        bool IsStreaming(string deviceId);

        /// <summary>
        /// Lấy device entity theo ID
        /// </summary>
        Device? GetDevice(string deviceId);

        /// <summary>
        /// Event khi trạng thái device thay đổi
        /// </summary>
        event EventHandler<DeviceStateChangedEventArgs>? OnStateChanged;

        /// <summary>
        /// Đăng ký một device với adapter
        /// </summary>
        void RegisterDevice(string deviceId, Device device, IDeviceAdapter adapter, IntPtr? previewHandle = null);

        /// <summary>
        /// Hủy đăng ký device
        /// </summary>
        void UnregisterDevice(string deviceId);

        /// <summary>
        /// Kết nối một device
        /// </summary>
        Task<bool> ConnectAsync(string deviceId, CancellationToken ct = default);

        /// <summary>
        /// Ngắt kết nối một device
        /// </summary>
        Task DisconnectAsync(string deviceId);

        /// <summary>
        /// Khởi động lại một device
        /// </summary>
        Task<bool> RestartAsync(string deviceId, CancellationToken ct = default);

        /// <summary>
        /// Bắt đầu health check định kỳ
        /// </summary>
        void StartHealthCheck(TimeSpan interval);

        /// <summary>
        /// Dừng health check
        /// </summary>
        void StopHealthCheck();

        /// <summary>
        /// Kết nối tất cả devices đã đăng ký
        /// </summary>
        Task ConnectAllAsync(CancellationToken ct = default);

        /// <summary>
        /// Ngắt kết nối tất cả devices
        /// </summary>
        Task DisconnectAllAsync();
    }

    /// <summary>
    /// Quản lý health check và state của tất cả devices
    /// - Ping định kỳ (không trust SDK callbacks)
    /// - Retry với exponential backoff
    /// - Fire events khi state thay đổi
    /// - Sync LiveView với connection state
    /// </summary>
    public class DeviceHealthManager : IDeviceHealthManager, IDisposable
    {
        private readonly ConcurrentDictionary<string, DeviceHealthInfo> _devices = new();
        private Timer? _healthCheckTimer;
        private bool _isDisposed;

        // Retry configuration
        private const int MAX_RETRIES = 3;
        private static readonly TimeSpan[] RETRY_DELAYS = {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4)
        };

        public event EventHandler<DeviceStateChangedEventArgs>? OnStateChanged;

        /// <summary>
        /// Thông tin về một device đang được quản lý
        /// </summary>
        private class DeviceHealthInfo
        {
            public Device Device { get; }
            public IDeviceAdapter Adapter { get; }
            public IntPtr? PreviewHandle { get; }
            public DeviceConnectionState State { get; set; } = DeviceConnectionState.Disconnected;
            public int RetryCount { get; set; } = 0;
            public DateTime LastPingTime { get; set; } = DateTime.MinValue;

            public DeviceHealthInfo(Device device, IDeviceAdapter adapter, IntPtr? previewHandle)
            {
                Device = device;
                Adapter = adapter;
                PreviewHandle = previewHandle;
            }
        }

        public DeviceConnectionState GetState(string deviceId)
        {
            return _devices.TryGetValue(deviceId, out var info) ? info.State : DeviceConnectionState.Disconnected;
        }

        public bool IsStreaming(string deviceId)
        {
            return _devices.TryGetValue(deviceId, out var info) && info.Adapter.IsStreaming;
        }

        public Device? GetDevice(string deviceId)
        {
            return _devices.TryGetValue(deviceId, out var info) ? info.Device : null;
        }

        public void RegisterDevice(string deviceId, Device device, IDeviceAdapter adapter, IntPtr? previewHandle = null)
        {
            var info = new DeviceHealthInfo(device, adapter, previewHandle);

            // Subscribe to adapter events
            adapter.OnConnectionStateChanged += (s, newState) =>
            {
                OnAdapterStateChanged(deviceId, newState);
            };

            _devices.AddOrUpdate(deviceId, info, (_, _) => info);
            AppLogger.Information($"[DeviceHealth] Registered device: {device.Name} ({deviceId})");
        }

        public void UnregisterDevice(string deviceId)
        {
            if (_devices.TryRemove(deviceId, out var info))
            {
                AppLogger.Information($"[DeviceHealth] Unregistered device: {info.Device.Name} ({deviceId})");
            }
        }

        public async Task<bool> ConnectAsync(string deviceId, CancellationToken ct = default)
        {
            if (!_devices.TryGetValue(deviceId, out var info))
            {
                AppLogger.Warning($"[DeviceHealth] ConnectAsync: Device not found: {deviceId}");
                return false;
            }

            SetState(deviceId, DeviceConnectionState.Connecting);

            try
            {
                bool success = await info.Adapter.ConnectAsync(info.Device, ct);

                if (success)
                {
                    SetState(deviceId, DeviceConnectionState.Connected);
                    AppLogger.Information($"[DeviceHealth] Connected: {info.Device.Name} ({deviceId})");
                    return true;
                }
                else
                {
                    SetState(deviceId, DeviceConnectionState.Error);
                    AppLogger.Warning($"[DeviceHealth] Connect failed: {info.Device.Name} ({deviceId})");
                    return false;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"[DeviceHealth] Connect exception: {info.Device.Name} ({deviceId})");
                SetState(deviceId, DeviceConnectionState.Error);
                return false;
            }
        }

        public async Task DisconnectAsync(string deviceId)
        {
            if (!_devices.TryGetValue(deviceId, out var info))
                return;

            try
            {
                await info.Adapter.DisconnectAsync();
                SetState(deviceId, DeviceConnectionState.Disconnected);
                AppLogger.Information($"[DeviceHealth] Disconnected: {info.Device.Name} ({deviceId})");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"[DeviceHealth] Disconnect exception: {info.Device.Name} ({deviceId})");
            }
        }

        public async Task<bool> RestartAsync(string deviceId, CancellationToken ct = default)
        {
            if (!_devices.TryGetValue(deviceId, out var info))
                return false;

            AppLogger.Information($"[DeviceHealth] Restarting: {info.Device.Name} ({deviceId})");

            try
            {
                bool success = await info.Adapter.RestartAsync(info.Device, ct);

                if (success)
                {
                    SetState(deviceId, DeviceConnectionState.Connected);
                    AppLogger.Information($"[DeviceHealth] Restart success: {info.Device.Name} ({deviceId})");
                    return true;
                }
                else
                {
                    SetState(deviceId, DeviceConnectionState.Error);
                    AppLogger.Warning($"[DeviceHealth] Restart failed: {info.Device.Name} ({deviceId})");
                    return false;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"[DeviceHealth] Restart exception: {info.Device.Name} ({deviceId})");
                SetState(deviceId, DeviceConnectionState.Error);
                return false;
            }
        }

        public async Task ConnectAllAsync(CancellationToken ct = default)
        {
            var tasks = new List<Task>();
            foreach (var kvp in _devices)
            {
                tasks.Add(ConnectAsync(kvp.Key, ct));
            }
            await Task.WhenAll(tasks);
        }

        public async Task DisconnectAllAsync()
        {
            var tasks = new List<Task>();
            foreach (var kvp in _devices)
            {
                tasks.Add(DisconnectAsync(kvp.Key));
            }
            await Task.WhenAll(tasks);
        }

        public void StartHealthCheck(TimeSpan interval)
        {
            StopHealthCheck();
            _healthCheckTimer = new Timer(
                async _ => await CheckAllDevicesAsync(),
                null,
                interval,
                interval);
            AppLogger.Information($"[DeviceHealth] Health check started (interval: {interval.TotalSeconds}s)");
        }

        public void StopHealthCheck()
        {
            _healthCheckTimer?.Dispose();
            _healthCheckTimer = null;
        }

        private async Task CheckAllDevicesAsync()
        {
            foreach (var kvp in _devices)
            {
                var deviceId = kvp.Key;
                var info = kvp.Value;
                try
                {
                    bool pingOk = await info.Adapter.PingAsync(2000);
                    info.LastPingTime = DateTime.Now;

                    if (pingOk)
                    {
                        // Device is alive
                        await HandleDeviceAliveAsync(deviceId, info);
                    }
                    else
                    {
                        // Device is not responding
                        await HandleDeviceDeadAsync(deviceId, info);
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, $"[DeviceHealth] Check error: {info.Device.Name} ({deviceId})");
                }
            }
        }

        private async Task HandleDeviceAliveAsync(string deviceId, DeviceHealthInfo info)
        {
            // Device vừa online hoặc đang online
            if (info.State == DeviceConnectionState.Disconnected ||
                info.State == DeviceConnectionState.Error)
            {
                // Device vừa phục hồi → thử reconnect
                info.RetryCount = 0;
                await TryReconnectAsync(deviceId, info);
            }
            else if (info.State == DeviceConnectionState.Connected && !info.Adapter.IsStreaming)
            {
                // Đã connect nhưng chưa stream → start preview
                await StartPreviewAsync(deviceId, info);
            }
        }

        private async Task HandleDeviceDeadAsync(string deviceId, DeviceHealthInfo info)
        {
            if (info.State == DeviceConnectionState.Connected ||
                info.State == DeviceConnectionState.Streaming)
            {
                // Device vừa mất kết nối → retry với backoff
                await RetryWithBackoffAsync(deviceId, info);
            }
        }

        private async Task TryReconnectAsync(string deviceId, DeviceHealthInfo info)
        {
            SetState(deviceId, DeviceConnectionState.Connecting);

            try
            {
                bool success = await info.Adapter.ConnectAsync(info.Device);

                if (success)
                {
                    SetState(deviceId, DeviceConnectionState.Connected);
                    await StartPreviewAsync(deviceId, info);
                }
                else
                {
                    SetState(deviceId, DeviceConnectionState.Error);
                }
            }
            catch
            {
                SetState(deviceId, DeviceConnectionState.Error);
            }
        }

        private async Task RetryWithBackoffAsync(string deviceId, DeviceHealthInfo info)
        {
            info.RetryCount++;

            if (info.RetryCount <= MAX_RETRIES)
            {
                var delayIndex = Math.Min(info.RetryCount - 1, RETRY_DELAYS.Length - 1);
                var delay = RETRY_DELAYS[delayIndex];

                AppLogger.Information($"[DeviceHealth] Retry {info.RetryCount}/{MAX_RETRIES} for {info.Device.Name} in {delay.TotalSeconds}s");

                await Task.Delay(delay);

                // Thử ping lại
                bool pingOk = await info.Adapter.PingAsync(2000);
                if (pingOk)
                {
                    info.RetryCount = 0;
                    await TryReconnectAsync(deviceId, info);
                    return;
                }
            }

            // All retries failed
            info.RetryCount = 0;
            SetState(deviceId, DeviceConnectionState.Error);
            AppLogger.Warning($"[DeviceHealth] Device offline after {MAX_RETRIES} retries: {info.Device.Name}");
        }

        private async Task StartPreviewAsync(string deviceId, DeviceHealthInfo info)
        {
            // TODO: Implement start preview based on device type
            // For now, just set streaming state if adapter supports it
            if (info.Adapter.IsConnected)
            {
                AppLogger.Debug($"[DeviceHealth] Device ready to stream: {info.Device.Name}");
            }
        }

        private void OnAdapterStateChanged(string deviceId, DeviceConnectionState newState)
        {
            if (_devices.TryGetValue(deviceId, out var info))
            {
                var oldState = info.State;
                if (oldState != newState)
                {
                    info.State = newState;
                    OnStateChanged?.Invoke(this, new DeviceStateChangedEventArgs(deviceId, oldState, newState));
                }
            }
        }

        private void SetState(string deviceId, DeviceConnectionState newState)
        {
            if (_devices.TryGetValue(deviceId, out var info))
            {
                var oldState = info.State;
                if (oldState != newState)
                {
                    info.State = newState;
                    AppLogger.Debug($"[DeviceHealth] State change: {info.Device.Name} {oldState} → {newState}");
                    OnStateChanged?.Invoke(this, new DeviceStateChangedEventArgs(deviceId, oldState, newState));
                }
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            StopHealthCheck();
            _devices.Clear();

            GC.SuppressFinalize(this);
        }
    }
}
