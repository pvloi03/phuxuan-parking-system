using HPParkingSystem.Helpers;
using HPParkingSystem.Models.Entities;
using HPParkingSystem.Models.Enums;
using HPParkingSystem.SDK.ZKTeco;
using HPParkingSystem.Services.Logging;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HPParkingSystem.Services.Devices.Controller
{
    /// <summary>
    /// Triển khai IControllerService - Quản lý kết nối TCP, luồng nhận RTLog ngầm và dispatch sự kiện Radar
    /// </summary>
    public class ControllerService : IControllerService, IDisposable
    {
        private IntPtr _handle = IntPtr.Zero;
        private readonly object _lockObj = new();
        private CancellationTokenSource? _listeningCts;
        private Task? _listeningTask;
        private bool _isDisposed;

        private string _lastIp = "";
        private int _lastPort = 4370;
        private string? _lastPassword;
        private DateTime _drainUntil = DateTime.MinValue;
        private DateTime _lastCommunicationTime = DateTime.MinValue;
        private const int BUFFER_SIZE = 64 * 1024;
        private const int HEARTBEAT_INTERVAL_SECONDS = 5;
        private const int WATCHDOG_TIMEOUT_SECONDS = 12;

        public event EventHandler<AuxTriggerEventArgs>? OnAuxInputTriggered;

        /// <summary>
        /// TRUE = đang nhận log từ controller (ReadLog đang chạy)
        /// </summary>
        public bool IsStreaming => _listeningTask != null && !_listeningTask.IsCanceled && !_listeningTask.IsFaulted;

        /// <summary>
        /// Event khi trạng thái kết nối thay đổi
        /// </summary>
        public event EventHandler<DeviceStatus>? OnConnectionStateChanged;

        public bool IsConnected => _handle != IntPtr.Zero;

        /// <summary>
        /// Ping TCP đến controller IP:Port
        /// </summary>
        public async Task<bool> PingAsync(int timeoutMs = 2000, CancellationToken cancellationToken = default)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(_lastIp, _lastPort);
                var delayTask = Task.Delay(timeoutMs, cancellationToken);
                var completedTask = await Task.WhenAny(connectTask, delayTask).ConfigureAwait(false);

                return completedTask == connectTask && client.Connected;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Kết nối tới bộ điều khiển trung tâm qua giao thức TCP
        /// </summary>
        public Task<bool> ConnectAsync(
            string ipAddress = "",
            int port = 4370,
            string? password = null,
            CancellationToken cancellationToken = default)
        {
            return Task.Run(() =>
            {
                lock (_lockObj)
                {
                    if (IsConnected && _lastIp == ipAddress && _lastPort == port && _lastPassword == password)
                    {
                        return true;
                    }

                    if (IsConnected)
                    {
                        DisconnectInternal();
                    }

                    _lastIp = ipAddress;
                    _lastPort = port;
                    _lastPassword = password;

                    var pwd = password ?? string.Empty;
                    var connectParams = $"protocol=TCP,ipaddress={ipAddress.Trim()},port={port},timeout=5000,passwd={pwd}";

                    _handle = ZKTecoPullSDK.Connect(connectParams);

                    if (_handle != IntPtr.Zero)
                    {
                        try
                        {
                            long encodedTime = ZKTecoRtLogParser.EncodeDateTime(DateTime.Now);
                            ZKTecoPullSDK.SetDeviceParam(_handle, $"DateTime={encodedTime}");
                        }
                        catch { }

                        // Xả sạch nhanh toàn bộ log cũ còn tồn đọng trong Controller ngay khi vừa kết nối
                        try
                        {
                            var drainBuf = new byte[BUFFER_SIZE];
                            int drainCount = 0;
                            while (ZKTecoPullSDK.GetRTLog(_handle, ref drainBuf[0], BUFFER_SIZE) >= 0 && drainCount < 500)
                            {
                                drainCount++;
                            }
                            if (drainCount > 0)
                            {
                                AppLogger.Information($"[Access Controller] Đã xả sạch {drainCount} gói tin cũ trong bộ đệm lúc khởi động.");
                            }
                        }
                        catch { }

                        _lastCommunicationTime = DateTime.Now;
                        _drainUntil = DateTime.Now.AddSeconds(2.0);
                        StartListening();
                        OnConnectionStateChanged?.Invoke(this, DeviceStatus.Connected);
                        return true;
                    }

                    int errCode = ZKTecoPullSDK.PullLastError();
                    AppLogger.Warning($"[Access Controller] Kết nối thất bại ({ipAddress}:{port}), Mã lỗi: {errCode}");
                    return false;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Kết nối tới Controller dựa trên cấu hình Device entity từ MongoDB
        /// </summary>
        public Task<bool> ConnectAsync(Device device, CancellationToken cancellationToken = default)
        {
            if (device == null) return Task.FromResult(false);
            return ConnectAsync(
                ipAddress: device.IpAddress,
                port: device.Port,
                password: device.Password,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Ngắt kết nối tới thiết bị
        /// </summary>
        public Task DisconnectAsync()
        {
            lock (_lockObj)
            {
                DisconnectInternal();
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Khởi động lại kết nối thiết bị
        /// </summary>
        public async Task<bool> RestartAsync(Device device, CancellationToken cancellationToken = default)
        {
            await DisconnectAsync().ConfigureAwait(false);
            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
            return await ConnectAsync(device, cancellationToken).ConfigureAwait(false);
        }

        private void DisconnectInternal()
        {
            StopListening();

            if (_handle != IntPtr.Zero)
            {
                ZKTecoPullSDK.Disconnect(_handle);
                _handle = IntPtr.Zero;
            }

            OnConnectionStateChanged?.Invoke(this, DeviceStatus.Disconnected);
        }

        private void StartListening()
        {
            if (_listeningTask != null) return;

            _listeningCts = new CancellationTokenSource();
            _listeningTask = Task.Run(() => ListenLoopAsync(_listeningCts.Token));
        }

        private void StopListening()
        {
            if (_listeningCts != null)
            {
                try { _listeningCts.Cancel(); } catch { }
                _listeningCts.Dispose();
                _listeningCts = null;
            }

            _listeningTask = null;
        }

        /// <summary>
        /// Vòng lặp ngầm đọc Real-time Log từ Access Controller.
        /// Tự động phục hồi kết nối nếu socket bị ngắt ngầm.
        /// </summary>
        private async Task ListenLoopAsync(CancellationToken cancellationToken)
        {
            const int bufferSize = 64 * 1024; // 64KB buffer
            var buffer = new byte[bufferSize];
            var probeBuffer = new byte[64];
            int consecutiveErrors = 0;

            while (!cancellationToken.IsCancellationRequested && IsConnected)
            {
                try
                {
                    int result;
                    lock (_lockObj)
                    {
                        if (!IsConnected) break;
                        Array.Clear(buffer, 0, buffer.Length);
                        result = ZKTecoPullSDK.GetRTLog(_handle, ref buffer[0], bufferSize);
                    }

                    if (result >= 0)
                    {
                        _lastCommunicationTime = DateTime.Now;
                        consecutiveErrors = 0;
                        int nullIdx = Array.IndexOf(buffer, (byte)0);
                        int validBytes = nullIdx >= 0 ? nullIdx : buffer.Length;
                        var rawString = validBytes > 0 ? Encoding.Default.GetString(buffer, 0, validBytes) : string.Empty;

                        if (!string.IsNullOrWhiteSpace(rawString))
                        {
                            var lines = rawString.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in lines)
                            {
                                var trimmed = line.Trim();
                                if (!string.IsNullOrEmpty(trimmed))
                                {
                                    ParseAndDispatchLog(trimmed);
                                }
                            }
                        }
                    }
                    else if (result == -2)
                    {
                        // Mã -2: Hàng đợi log rỗng (không có sự kiện mới).
                        // Smart Heartbeat: nếu đã quá HEARTBEAT_INTERVAL_SECONDS không có bất kỳ giao tiếp nào:
                        var idleSeconds = (DateTime.Now - _lastCommunicationTime).TotalSeconds;
                        if (idleSeconds >= HEARTBEAT_INTERVAL_SECONDS)
                        {
                            int probeRes;
                            lock (_lockObj)
                            {
                                if (!IsConnected) break;
                                Array.Clear(probeBuffer, 0, probeBuffer.Length);
                                probeRes = ZKTecoPullSDK.GetDeviceParam(_handle, ref probeBuffer[0], probeBuffer.Length, "DateTime");
                            }

                            if (probeRes >= 0)
                            {
                                _lastCommunicationTime = DateTime.Now;
                                consecutiveErrors = 0;
                            }
                            else
                            {
                                consecutiveErrors++;
                                AppLogger.Warning($"[Access Controller Heartbeat] Thăm dò thất bại (Mã lỗi: {probeRes}, lần {consecutiveErrors}).");
                            }
                        }
                    }
                    else
                    {
                        // Lỗi kết nối thực sự từ SDK (< 0 và != -2)
                        consecutiveErrors++;
                    }

                    // Watchdog: Nếu gặp lỗi liên tiếp >= 2 lần HOẶC không có phản hồi quá WATCHDOG_TIMEOUT_SECONDS
                    var silentSeconds = (DateTime.Now - _lastCommunicationTime).TotalSeconds;
                    if (consecutiveErrors >= 2 || (silentSeconds >= WATCHDOG_TIMEOUT_SECONDS && _lastCommunicationTime != DateTime.MinValue))
                    {
                        AppLogger.Warning($"[Access Controller Watchdog] Phát hiện kết nối bị treo ngầm (Lỗi: {consecutiveErrors}, im lặng: {silentSeconds:F0}s). Đang tự động kết nối lại...");
                        lock (_lockObj)
                        {
                            if (_handle != IntPtr.Zero)
                            {
                                ZKTecoPullSDK.Disconnect(_handle);
                                _handle = IntPtr.Zero;
                            }
                            var pwd = _lastPassword ?? string.Empty;
                            var connectParams = $"protocol=TCP,ipaddress={_lastIp.Trim()},port={_lastPort},timeout=5000,passwd={pwd}";
                            _handle = ZKTecoPullSDK.Connect(connectParams);
                            if (_handle != IntPtr.Zero)
                            {
                                try
                                {
                                    long encodedTime = ZKTecoRtLogParser.EncodeDateTime(DateTime.Now);
                                    ZKTecoPullSDK.SetDeviceParam(_handle, $"DateTime={encodedTime}");
                                }
                                catch { }

                                try
                                {
                                    int drainCount = 0;
                                    while (ZKTecoPullSDK.GetRTLog(_handle, ref buffer[0], bufferSize) >= 0 && drainCount < 500)
                                    {
                                        drainCount++;
                                    }
                                    if (drainCount > 0)
                                    {
                                        AppLogger.Information($"[Access Controller Watchdog] Đã xả sạch {drainCount} gói tin cũ trong bộ đệm lúc kết nối lại.");
                                    }
                                }
                                catch { }

                                _lastCommunicationTime = DateTime.Now;
                                _drainUntil = DateTime.Now.AddSeconds(2.0);
                                consecutiveErrors = 0;
                                AppLogger.Information($"[Access Controller Watchdog] Đã tự động phục hồi kết nối ({_lastIp}:{_lastPort}) thành công!");
                            }
                        }

                        if (_handle == IntPtr.Zero)
                        {
                            await Task.Delay(2000, cancellationToken);
                        }
                    }

                    await Task.Delay(200, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    AppLogger.Debug($"[Access Controller ListenLoop Warning] {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Phân tích chuỗi RTLog và phát sự kiện OnAuxInputTriggered
        /// </summary>
        public void ParseAndDispatchLog(string rawLog)
        {
            var parseResult = ZKTecoRtLogParser.Parse(rawLog);
            switch (parseResult.Status)
            {
                case ZKTecoRtLogParseStatus.IgnoredMalformed:
                    AppLogger.Warning($"[Access Controller RTLog] Bỏ qua log không hợp lệ: {rawLog} ({parseResult.Reason})", "AccessController");
                    return;

                case ZKTecoRtLogParseStatus.IgnoredStatusPacket:
                    AppLogger.Debug($"[Access Controller RTLog] {parseResult.Reason}: {rawLog}");
                    return;

                case ZKTecoRtLogParseStatus.IgnoredInvalidPort:
                    AppLogger.Debug($"[Access Controller RTLog] {parseResult.Reason}: {rawLog}");
                    return;

                case ZKTecoRtLogParseStatus.IgnoredStaleEvent:
                    return;

                case ZKTecoRtLogParseStatus.Success:
                    break;
            }

            // Xả các bản ghi cũ trong bộ đệm controller trong 2 giây đầu sau khi kết nối
            if (DateTime.Now < _drainUntil)
            {
                AppLogger.Information($"[Access Controller RTLog] Xả sự kiện tồn đọng lúc khởi động: {rawLog}");
                return;
            }

            AppLogger.Information($"[Access Controller RTLog] Nhận chuỗi sự kiện: {rawLog}", "AccessController");
            AppLogger.Information($"[Access Controller Trigger] Phát sự kiện OnAuxInputTriggered: Làn {parseResult.AuxPort} (Aux {parseResult.AuxPort}), IsActive={parseResult.IsActive}, EventType={parseResult.EventType}");

            OnAuxInputTriggered?.Invoke(this, new AuxTriggerEventArgs(
                auxPort: parseResult.AuxPort,
                isActive: parseResult.IsActive,
                triggerTime: parseResult.TriggerTime,
                rawLog: rawLog));
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            lock (_lockObj)
            {
                DisconnectInternal();
            }
            GC.SuppressFinalize(this);
        }
    }
}
