using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using PhuXuanParkingSystem.Services.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.DeviceHealth
{
    /// <summary>
    /// Triển khai dịch vụ giám sát tình trạng thiết bị phần cứng
    /// Tự động kiểm tra Socket TCP, Ping ICMP và cập nhật trạng thái vào MongoDB để đồng bộ lên Web Admin
    /// </summary>
    public class DeviceHealthMonitorService : IDeviceHealthMonitorService
    {
        private readonly IRepository<Device> _deviceRepo;

        public event EventHandler<DevicePingResult>? OnDeviceChecked;

        public DeviceHealthMonitorService(IRepository<Device> deviceRepo)
        {
            _deviceRepo = deviceRepo ?? throw new ArgumentNullException(nameof(deviceRepo));
        }

        public async Task<DevicePingResult> PingDeviceAsync(Device device, int timeoutMs = 2000, CancellationToken cancellationToken = default)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));

            if (string.IsNullOrWhiteSpace(device.IpAddress))
            {
                var r = DevicePingResult.Fail(device, "Địa chỉ IP không hợp lệ");
                OnDeviceChecked?.Invoke(this, r);
                return r;
            }

            var sw = Stopwatch.StartNew();

            // 1. Thử kết nối TCP Socket tới Port của thiết bị (Hikvision 8000, NST 3000, ZKTeco 4370...)
            if (device.Port > 0)
            {
                try
                {
                    using var tcpClient = new TcpClient();
                    var connectTask = tcpClient.ConnectAsync(device.IpAddress, device.Port);
                    var timeoutTask = Task.Delay(timeoutMs, cancellationToken);

                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                    sw.Stop();

                    if (completedTask == connectTask && tcpClient.Connected)
                    {
                        var successResult = DevicePingResult.Success(device, sw.ElapsedMilliseconds, $"TCP Port {device.Port} phản hồi OK ({sw.ElapsedMilliseconds}ms)");
                        OnDeviceChecked?.Invoke(this, successResult);
                        return successResult;
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Debug($"[Socket Check] {device.Name} ({device.IpAddress}:{device.Port}) lỗi: {ex.Message}");
                }
            }

            // 2. Fallback sang ICMP Ping nếu kiểm tra Port TCP chưa thành công
            try
            {
                using var pinger = new Ping();
                var pingReply = await pinger.SendPingAsync(device.IpAddress, timeoutMs);
                sw.Stop();

                if (pingReply.Status == IPStatus.Success)
                {
                    long latency = pingReply.RoundtripTime > 0 ? pingReply.RoundtripTime : sw.ElapsedMilliseconds;
                    var pingSuccess = DevicePingResult.Success(device, latency, $"Ping ICMP phản hồi OK ({latency}ms)");
                    OnDeviceChecked?.Invoke(this, pingSuccess);
                    return pingSuccess;
                }
                else
                {
                    var pingFail = DevicePingResult.Fail(device, $"Ping thất bại: {pingReply.Status}", sw.ElapsedMilliseconds);
                    OnDeviceChecked?.Invoke(this, pingFail);
                    return pingFail;
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                var failResult = DevicePingResult.Fail(device, ex.Message, sw.ElapsedMilliseconds);
                OnDeviceChecked?.Invoke(this, failResult);
                return failResult;
            }
        }

        public async Task<IReadOnlyList<DevicePingResult>> CheckAllAndSyncAsync(CancellationToken cancellationToken = default)
        {
            var results = new List<DevicePingResult>();

            try
            {
                var devices = await _deviceRepo.FindAsync(d => !d.IsDeleted, cancellationToken);
                if (devices == null || devices.Count == 0)
                {
                    return results;
                }

                // Kiểm tra song song đồng thời tất cả thiết bị
                var checkTasks = devices.Select(d => PingDeviceAsync(d, 2000, cancellationToken)).ToList();
                var pingResults = await Task.WhenAll(checkTasks);

                // Đồng bộ trạng thái vào MongoDB
                foreach (var res in pingResults)
                {
                    results.Add(res);
                    await SyncStatusToDbAsync(res, cancellationToken);
                }

                int onlineCount = results.Count(r => r.IsSuccess);
                AppLogger.Information($"[DeviceHealth] Đã kiểm tra {results.Count} thiết bị: {onlineCount} Online, {results.Count - onlineCount} Offline.");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Lỗi kiểm tra danh sách thiết bị và đồng bộ CSDL");
            }

            return results;
        }

        public async Task SyncStatusToDbAsync(DevicePingResult result, CancellationToken cancellationToken = default)
        {
            if (result?.Device == null) return;

            try
            {
                var dev = result.Device;
                if (result.IsSuccess)
                {
                    dev.MarkConnected();
                }
                else
                {
                    dev.MarkDisconnected();
                    dev.ErrorMessage = result.ErrorMessage;
                }

                await _deviceRepo.UpdateAsync(dev, cancellationToken);
            }
            catch (Exception ex)
            {
                AppLogger.Warning($"Lỗi đồng bộ trạng thái thiết bị '{result.Device.Name}' vào MongoDB: {ex.Message}");
            }
        }
    }
}
