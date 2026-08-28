using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using PhuXuanParkingSystem.Services.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.DeviceHealth
{
    /// <summary>
    /// Dịch vụ giám sát tình trạng thiết bị phần cứng - Phiên bản tối ưu
    ///
    /// Luồng hoạt động:
    /// 1. Priority 1: Check SDK State (IsConnected) - Single source of truth
    /// 2. Priority 2: Nếu disconnected → Try reconnect với retry (3 lần, exponential backoff)
    /// 3. Fire event AFTER return - tránh race condition
    /// 4. Sync to MongoDB
    ///
    /// Responsibility Split:
    /// - SDK lo: Low-level (socket, protocol, auth, auto-reconnect)
    /// - Our Code lo: High-level (monitoring, retry logic, sync, notification)
    /// </summary>
    public class DeviceHealthMonitorService : IDeviceHealthMonitorService
    {
        private readonly IRepository<Device> _deviceRepo;
        private readonly IDeviceAdapterFactory _adapterFactory;

        // Retry configuration
        private const int MAX_RETRIES = 3;
        private const int BASE_DELAY_MS = 100; // Exponential backoff: 100ms, 200ms, 400ms

        public event EventHandler<DevicePingResult>? OnDeviceChecked;

        public DeviceHealthMonitorService(
            IRepository<Device> deviceRepo,
            IDeviceAdapterFactory adapterFactory)
        {
            _deviceRepo = deviceRepo ?? throw new ArgumentNullException(nameof(deviceRepo));
            _adapterFactory = adapterFactory ?? throw new ArgumentNullException(nameof(adapterFactory));
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
                OnDeviceChecked?.Invoke(this, result);
                return result;
            }

            var sw = Stopwatch.StartNew();
            var adapter = _adapterFactory.GetAdapter(device);

            // =========================================================================
            // PRIORITY 1: CHECK SDK STATE
            // =========================================================================
            // SDK đã handle auto-reconnect nếu có cable hiccup
            // Nếu IsConnected = true → Device online
            if (adapter.IsConnected)
            {
                sw.Stop();
                AppLogger.Debug($"[HealthCheck] {device.Name} SDK Connected ({device.IpAddress})");

                var successResult = DevicePingResult.Success(
                    device,
                    sw.ElapsedMilliseconds,
                    $"SDK Connected (auto-recovered)");

                // Fire event AFTER return - tránh race condition
                OnDeviceChecked?.Invoke(this, successResult);
                return successResult;
            }

            // =========================================================================
            // PRIORITY 2: TRY RECONNECT WITH RETRY
            // =========================================================================
            // Device disconnected → Thử reconnect với retry logic
            // Dùng exponential backoff để tránh spam device
            AppLogger.Information($"[HealthCheck] {device.Name} SDK Disconnected. Thử reconnect...");

            int successfulRetryCount = 0;

            for (int retry = 0; retry < MAX_RETRIES; retry++)
            {
                if (cancellationToken.IsCancellationRequested) break;

                AppLogger.Debug($"[HealthCheck] {device.Name} reconnect lần {retry + 1}/{MAX_RETRIES}");

                try
                {
                    bool connected = await adapter.ConnectAsync(device, cancellationToken);

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
                    await Task.Delay(delayMs, cancellationToken);
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

            OnDeviceChecked?.Invoke(this, failResult);
            return failResult;
        }

        /// <summary>
        /// Kiểm tra toàn bộ thiết bị và sync vào MongoDB
        /// </summary>
        public async Task<IReadOnlyList<DevicePingResult>> CheckAllAndSyncAsync(
            CancellationToken cancellationToken = default)
        {
            var results = new List<DevicePingResult>();

            try
            {
                var devices = await _deviceRepo.FindAsync(d => !d.IsDeleted, cancellationToken);
                if (devices == null || devices.Count == 0)
                {
                    AppLogger.Debug("[DeviceHealth] Không có thiết bị nào để kiểm tra");
                    return results;
                }

                AppLogger.Information($"[DeviceHealth] Bắt đầu kiểm tra {devices.Count} thiết bị...");

                // Kiểm tra song song đồng thời tất cả thiết bị
                var checkTasks = devices.Select(d => PingDeviceAsync(d, 2000, cancellationToken)).ToList();
                var pingResults = await Task.WhenAll(checkTasks);

                // Sync trạng thái vào MongoDB
                foreach (var res in pingResults)
                {
                    results.Add(res);
                    await SyncStatusToDbAsync(res, cancellationToken);
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
            if (result?.Device == null) return;

            try
            {
                var dev = result.Device;

                if (result.IsSuccess)
                {
                    dev.MarkConnected();
                    dev.ErrorMessage = null; // Clear error
                }
                else
                {
                    dev.MarkDisconnected();
                    dev.ErrorMessage = result.ErrorMessage;
                }

                await _deviceRepo.UpdateAsync(dev, cancellationToken);
                AppLogger.Debug($"[DeviceHealth] Sync {dev.Name}: {result.Status}");
            }
            catch (Exception ex)
            {
                AppLogger.Warning($"[DeviceHealth] Lỗi sync '{result.Device.Name}': {ex.Message}");
            }
        }
    }
}
