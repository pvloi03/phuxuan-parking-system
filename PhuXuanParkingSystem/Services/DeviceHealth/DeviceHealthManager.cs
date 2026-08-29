using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Services.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        public DeviceStatus OldState { get; }
        public DeviceStatus NewState { get; }
        public DateTime Timestamp { get; }

        public DeviceStateChangedEventArgs(string deviceId, DeviceStatus oldState, DeviceStatus newState)
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
        DeviceStatus GetState(string deviceId);

        /// <summary>
        /// Kiểm tra device có đang streaming không
        /// </summary>
        bool IsStreaming(string deviceId);

        /// <summary>
        /// Lấy device entity theo ID
        /// </summary>
        Device? GetDevice(string deviceId);

        /// <summary>
        /// Lấy danh sách tất cả các devices đang được quản lý
        /// </summary>
        IEnumerable<Device> GetAllDevices();

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
        /// Xóa toàn bộ danh sách devices đang quản lý
        /// </summary>
        void ClearAllDevices();

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
            public DeviceStatus State { get; set; } = DeviceStatus.Disconnected;
            public int RetryCount { get; set; } = 0;
            public DateTime LastPingTime { get; set; } = DateTime.MinValue;

            public DeviceHealthInfo(Device device, IDeviceAdapter adapter, IntPtr? previewHandle)
            {
                Device = device;
                Adapter = adapter;
                PreviewHandle = previewHandle;
                if (adapter.IsConnected)
                {
                    State = adapter.IsStreaming ? DeviceStatus.Streaming : DeviceStatus.Connected;
                }
            }
        }

        public DeviceStatus GetState(string deviceId)
        {
            return _devices.TryGetValue(deviceId, out var info) ? info.State : DeviceStatus.Disconnected;
        }

        public bool IsStreaming(string deviceId)
        {
            return _devices.TryGetValue(deviceId, out var info) && info.Adapter.IsStreaming;
        }

        public Device? GetDevice(string deviceId)
        {
            return _devices.TryGetValue(deviceId, out var info) ? info.Device : null;
        }

        public IEnumerable<Device> GetAllDevices() => _devices.Values.Select(v => v.Device);

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

        public void ClearAllDevices()
        {
            _devices.Clear();
            AppLogger.Information("[DeviceHealth] Cleared all registered devices.");
        }

        public async Task<bool> ConnectAsync(string deviceId, CancellationToken ct = default)
        {
            if (!_devices.TryGetValue(deviceId, out var info))
            {
                AppLogger.Warning($"[DeviceHealth] ConnectAsync: Device not found: {deviceId}");
                return false;
            }

            SetState(deviceId, DeviceStatus.Connecting);

            try
            {
                bool success = await info.Adapter.ConnectAsync(info.Device, ct);

                if (success)
                {
                    SetState(deviceId, DeviceStatus.Connected);
                    if (info.PreviewHandle.HasValue && info.PreviewHandle.Value != IntPtr.Zero)
                    {
                        StartPreview(deviceId, info);
                    }
                    AppLogger.Information($"[DeviceHealth] Connected: {info.Device.Name} ({deviceId})");
                    return true;
                }
                else
                {
                    SetState(deviceId, DeviceStatus.Error);
                    AppLogger.Warning($"[DeviceHealth] Connect failed: {info.Device.Name} ({deviceId})");
                    return false;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"[DeviceHealth] Connect exception: {info.Device.Name} ({deviceId})");
                SetState(deviceId, DeviceStatus.Error);
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
                SetState(deviceId, DeviceStatus.Disconnected);
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
                    SetState(deviceId, DeviceStatus.Connected);
                    if (info.PreviewHandle.HasValue && info.PreviewHandle.Value != IntPtr.Zero)
                    {
                        StartPreview(deviceId, info);
                    }
                    AppLogger.Information($"[DeviceHealth] Restart success: {info.Device.Name} ({deviceId})");
                    return true;
                }
                else
                {
                    SetState(deviceId, DeviceStatus.Error);
                    AppLogger.Warning($"[DeviceHealth] Restart failed: {info.Device.Name} ({deviceId})");
                    return false;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"[DeviceHealth] Restart exception: {info.Device.Name} ({deviceId})");
                SetState(deviceId, DeviceStatus.Error);
                return false;
            }
        }

        public Task ConnectAllAsync(CancellationToken ct = default) =>
            Task.WhenAll(_devices.Keys.Select(id => ConnectAsync(id, ct)));

        public Task DisconnectAllAsync() =>
            Task.WhenAll(_devices.Keys.Select(DisconnectAsync));

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
            if (info.State == DeviceStatus.Disconnected ||
                info.State == DeviceStatus.Error)
            {
                // Device vừa phục hồi → thử reconnect
                info.RetryCount = 0;
                await TryReconnectAsync(deviceId, info);
            }
            else if (info.State == DeviceStatus.Connected && !info.Adapter.IsStreaming)
            {
                // Đã connect nhưng chưa stream → start preview
                StartPreview(deviceId, info);
            }
        }

        private async Task HandleDeviceDeadAsync(string deviceId, DeviceHealthInfo info)
        {
            if (info.State == DeviceStatus.Connected ||
                info.State == DeviceStatus.Streaming)
            {
                // Device vừa mất kết nối → retry với backoff
                await RetryWithBackoffAsync(deviceId, info);
            }
        }

        private async Task TryReconnectAsync(string deviceId, DeviceHealthInfo info)
        {
            SetState(deviceId, DeviceStatus.Connecting);

            try
            {
                bool success = await info.Adapter.ConnectAsync(info.Device);

                if (success)
                {
                    SetState(deviceId, DeviceStatus.Connected);
                    if (info.PreviewHandle.HasValue && info.PreviewHandle.Value != IntPtr.Zero)
                    {
                        StartPreview(deviceId, info);
                    }
                }
                else
                {
                    SetState(deviceId, DeviceStatus.Error);
                }
            }
            catch
            {
                SetState(deviceId, DeviceStatus.Error);
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
            SetState(deviceId, DeviceStatus.Error);
            AppLogger.Warning($"[DeviceHealth] Device offline after {MAX_RETRIES} retries: {info.Device.Name}");
        }

        private void StartPreview(string deviceId, DeviceHealthInfo info)
        {
            if (info.Adapter.IsConnected && info.PreviewHandle.HasValue && info.PreviewHandle.Value != IntPtr.Zero)
            {
                AppLogger.Information($"[DeviceHealth] Starting preview for device: {info.Device.Name} ({deviceId})");
                bool success = info.Adapter.StartPreview(info.PreviewHandle.Value);
                if (success)
                {
                    SetState(deviceId, DeviceStatus.Streaming);
                }
            }
            else if (info.Adapter.IsConnected && (!info.PreviewHandle.HasValue || info.PreviewHandle.Value == IntPtr.Zero))
            {
                if (info.Adapter.IsStreaming)
                {
                    SetState(deviceId, DeviceStatus.Streaming);
                }
            }
        }

        private void OnAdapterStateChanged(string deviceId, DeviceStatus newState)
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

        private void SetState(string deviceId, DeviceStatus newState)
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

