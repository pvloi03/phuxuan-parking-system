using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using PhuXuanParkingSystem.Services.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Devices.Health
{
    /// <summary>
    /// Dịch vụ giám sát tình trạng thiết bị phần cứng - Động cơ trung tâm quản lý Ping, Retry, In-Memory State & Sync MongoDB
    /// </summary>
    public class DeviceHealthMonitorService : IDeviceHealthMonitorService
    {
        private readonly IRepository<Device> _deviceRepo;
        private readonly IDeviceAdapterFactory _adapterFactory;
        private readonly ConcurrentDictionary<string, DeviceStatus> _states = new();
        private Timer? _healthTimer;
        private bool _isDisposed;

        // Retry configuration
        private const int MAX_RETRIES = 3;
        private const int BASE_DELAY_MS = 100; // Exponential backoff: 100ms, 200ms, 400ms

        public event EventHandler<DevicePingResult>? OnDeviceChecked;
        public event EventHandler<DeviceStateChangedEventArgs>? OnStateChanged;

        public DeviceHealthMonitorService(
            IRepository<Device> deviceRepo,
            IDeviceAdapterFactory adapterFactory)
        {
            _deviceRepo = deviceRepo ?? throw new ArgumentNullException(nameof(deviceRepo));
            _adapterFactory = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));
        }

        public DeviceStatus GetState(string deviceId)
        {
            return _states.TryGetValue(deviceId, out var state) ? state : DeviceStatus.Disconnected;
        }

        public void StartHealthCheck(TimeSpan interval)
        {
            StopHealthCheck();
            _healthTimer = new Timer(
                async _ => await CheckAllAndSyncAsync().ConfigureAwait(false),
                null,
                interval,
                interval);
            AppLogger.Information($"[DeviceHealth] Bắt đầu tự động kiểm tra sức khỏe thiết bị định kỳ (chu kỳ: {interval.TotalSeconds}s)");
        }

        public void StopHealthCheck()
        {
            _healthTimer?.Dispose();
            _healthTimer = null;
        }

        /// <summary>
        /// Kiểm tra kết nối thiết bị với hybrid approach
        ///
        /// 1. Check SDK State (Priority 1) - SDK đã auto-reconnect
        /// 2. If disconnected → Try reconnect (Priority 2) với retry logic
        /// </summary>
        public async Task<DevicePingResult> PingDeviceAsync(
            Device device,
            int timeoutMs = 2000,
            CancellationToken cancellationToken = default)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));

            if (string.IsNullOrWhiteSpace(device.IpAddress))
            {
                var result = DevicePingResult.Fail(device, "Địa chỉ IP không hợp lệ", 0);
                UpdateState(device.Id, DeviceStatus.Error);
                OnDeviceChecked?.Invoke(this, result);
                return result;
            }

            var sw = Stopwatch.StartNew();
            var adapter = _adapterFactory.GetAdapter(device);

            // =========================================================================
            // PRIORITY 1: CHECK SDK STATE
            // =========================================================================
            if (adapter.IsConnected)
            {
                sw.Stop();
                AppLogger.Debug($"[HealthCheck] {device.Name} SDK Connected ({device.IpAddress})");

                var successResult = DevicePingResult.Success(
                    device,
                    sw.ElapsedMilliseconds,
                    "SDK Connected (auto-recovered)");

                DeviceStatus currentStatus = adapter.IsStreaming ? DeviceStatus.Streaming : DeviceStatus.Connected;
                UpdateState(device.Id, currentStatus);

                OnDeviceChecked?.Invoke(this, successResult);
                return successResult;
            }

            // =========================================================================
            // PRIORITY 2: TRY RECONNECT WITH RETRY
            // =========================================================================
            AppLogger.Information($"[HealthCheck] {device.Name} SDK Disconnected. Thử reconnect...");
            UpdateState(device.Id, DeviceStatus.Connecting);

            int successfulRetryCount = 0;

            for (int retry = 0; retry < MAX_RETRIES; retry++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                AppLogger.Debug($"[HealthCheck] {device.Name} reconnect lần {retry + 1}/{MAX_RETRIES}");

                try
                {
                    bool connected = await adapter.ConnectAsync(device, cancellationToken).ConfigureAwait(false);

                    if (connected)
                    {
                        sw.Stop();
                        successfulRetryCount = retry + 1;
                        AppLogger.Information($"[HealthCheck] {device.Name} reconnect thành công (lần {successfulRetryCount})");

                        var successResult = DevicePingResult.Success(
                            device,
                            sw.ElapsedMilliseconds,
                            $"Reconnected thành công (lần {successfulRetryCount})",
                            retryCount: successfulRetryCount);

                        DeviceStatus currentStatus = adapter.IsStreaming ? DeviceStatus.Streaming : DeviceStatus.Connected;
                        UpdateState(device.Id, currentStatus);

                        OnDeviceChecked?.Invoke(this, successResult);
                        return successResult;
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Warning($"[HealthCheck] {device.Name} reconnect lần {retry + 1} lỗi: {ex.Message}");
                }

                // Exponential backoff: 100ms, 200ms, 400ms
                if (retry < MAX_RETRIES - 1)
                {
                    int delayMs = BASE_DELAY_MS * (int)Math.Pow(2, retry);
                    await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                }
            }

            // =========================================================================
            // ALL RETRIES FAILED
            // =========================================================================
            sw.Stop();
            AppLogger.Warning($"[HealthCheck] {device.Name} kết nối thất bại sau {MAX_RETRIES} lần thử");

            var failResult = DevicePingResult.Fail(
                device,
                $"Kết nối thất bại sau {MAX_RETRIES} lần thử",
                sw.ElapsedMilliseconds,
                retryCount: MAX_RETRIES);

            UpdateState(device.Id, DeviceStatus.Error);
            OnDeviceChecked?.Invoke(this, failResult);
            return failResult;
        }

        /// <summary>
        /// Kiểm tra toàn bộ thiết bị đang active và sync vào MongoDB
        /// </summary>
        public async Task<IReadOnlyList<DevicePingResult>> CheckAllAndSyncAsync(
            CancellationToken cancellationToken = default)
        {
            var results = new List<DevicePingResult>();

            try
            {
                var devices = await _deviceRepo.FindAsync(d => !d.IsDeleted && d.IsActive, cancellationToken).ConfigureAwait(false);
                if (devices == null || devices.Count == 0)
                {
                    AppLogger.Debug("[DeviceHealth] Không có thiết bị nào đang hoạt động để kiểm tra");
                    return results;
                }

                AppLogger.Information($"[DeviceHealth] Bắt đầu kiểm tra {devices.Count} thiết bị đang hoạt động...");

                // Kiểm tra song song đồng thời tất cả thiết bị
                var checkTasks = devices.Select(d => PingDeviceAsync(d, 2000, cancellationToken)).ToList();
                var pingResults = await Task.WhenAll(checkTasks).ConfigureAwait(false);

                // Sync trạng thái vào MongoDB
                foreach (var res in pingResults)
                {
                    results.Add(res);
                    await SyncStatusToDbAsync(res, cancellationToken).ConfigureAwait(false);
                }

                int onlineCount = results.Count(r => r.IsSuccess);
                AppLogger.Information($"[DeviceHealth] Hoàn thành: {onlineCount}/{results.Count} thiết bị Online");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "[DeviceHealth] Lỗi kiểm tra danh sách thiết bị");
            }

            return results;
        }

        /// <summary>
        /// Đồng bộ trạng thái thiết bị vào MongoDB
        /// </summary>
        public async Task SyncStatusToDbAsync(
            DevicePingResult result,
            CancellationToken cancellationToken = default)
        {
            if (result?.Device == null || string.IsNullOrWhiteSpace(result.Device.Id)) return;

            try
            {
                var freshDev = await _deviceRepo.GetByIdAsync(result.Device.Id, cancellationToken).ConfigureAwait(false);
                if (freshDev == null) return;

                if (result.IsSuccess)
                {
                    freshDev.MarkConnected();
                    freshDev.ErrorMessage = null;
                }
                else
                {
                    freshDev.MarkDisconnected();
                    freshDev.ErrorMessage = result.ErrorMessage;
                }

                await _deviceRepo.UpdateAsync(freshDev, cancellationToken).ConfigureAwait(false);
                AppLogger.Debug($"[DeviceHealth] Sync {freshDev.Name}: {result.Status}");
            }
            catch (Exception ex)
            {
                AppLogger.Warning($"[DeviceHealth] Lỗi sync '{result.Device.Name}': {ex.Message}");
            }
        }

        private void UpdateState(string deviceId, DeviceStatus newStatus)
        {
            if (string.IsNullOrEmpty(deviceId)) return;

            var oldStatus = _states.TryGetValue(deviceId, out var s) ? s : DeviceStatus.Disconnected;
            if (oldStatus != newStatus)
            {
                _states[deviceId] = newStatus;
                AppLogger.Debug($"[DeviceHealth] State change: {deviceId} {oldStatus} → {newStatus}");
                OnStateChanged?.Invoke(this, new DeviceStateChangedEventArgs(deviceId, oldStatus, newStatus));
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            StopHealthCheck();
            _states.Clear();

            GC.SuppressFinalize(this);
        }
    }
}
