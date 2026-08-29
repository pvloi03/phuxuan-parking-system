using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.SDK.ZKTeco;
using PhuXuanParkingSystem.Services.DeviceHealth;
using PhuXuanParkingSystem.Services.Logging;
using PhuXuanParkingSystem.Services.Notification;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Controller
{
    /// <summary>
    /// Adapter kết nối và lắng nghe sự kiện từ Bộ điều khiển trung tâm (Access Controller) qua giao thức TCP
    /// Implements IDeviceAdapter cho DeviceHealthManager
    /// </summary>
    public class ZKTecoDeviceAdapter : IDeviceAdapter, IDisposable
    {
        private IntPtr _handle = IntPtr.Zero;
        private readonly object _lockObj = new();
        private CancellationTokenSource? _listeningCts;
        private Task? _listeningTask;
        private bool _isDisposed;

        private string _lastIp = "192.168.1.202";
        private int _lastPort = 4370;
        private string? _lastPassword;
        private DateTime _drainUntil = DateTime.MinValue;

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
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeoutMs);

                var connectTask = client.ConnectAsync(_lastIp, _lastPort);
                var delayTask = Task.Delay(timeoutMs, cts.Token);
                var completedTask = await Task.WhenAny(connectTask, delayTask);

                if (completedTask == connectTask && client.Connected)
                {
                    return true;
                }
                return false;
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
            string ipAddress = "192.168.1.202",
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
                        _drainUntil = DateTime.Now.AddSeconds(1.5);
                        StartListening();
                        AppNotificationService.NotifySuccess(NotificationCategory.Controller, "Access Controller", $"Đã kết nối Access Controller ({ipAddress}:{port}) thành công.", ipAddress);
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

        /// <summary>
        /// Controller không có màn hình preview
        /// </summary>
        public bool StartPreview(IntPtr windowHandle) => true;

        /// <summary>
        /// Dừng preview (no-op cho Controller)
        /// </summary>
        public void StopPreview() { }

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
                try
                {
                    _listeningCts.Cancel();
                    _listeningCts.Dispose();
                }
                catch
                {
                    // Bỏ qua lỗi hủy token
                }
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
                        consecutiveErrors = 0;
                        int nullIdx = Array.IndexOf(buffer, (byte)0);
                        int validBytes = nullIdx >= 0 ? nullIdx : buffer.Length;
                        var rawString = validBytes > 0 ? Encoding.Default.GetString(buffer, 0, validBytes) : string.Empty;

                        if (!string.IsNullOrWhiteSpace(rawString))
                        {
                            var lines = rawString.Split(["\r\n", "\n", "\r"], StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in lines)
                            {
                                var trimmedLine = line.Trim();
                                if (!string.IsNullOrEmpty(trimmedLine))
                                {
                                    ParseAndDispatchLog(trimmedLine);
                                }
                            }
                        }
                    }
                    else if (result == -2)
                    {
                        // Mã -2: Hàng đợi log rỗng (không có sự kiện mới)
                        consecutiveErrors = 0;
                    }
                    else
                    {
                        // Lỗi kết nối thực sự từ SDK
                        consecutiveErrors++;
                        if (consecutiveErrors >= 5) // Lỗi liên tiếp 5 chu kỳ (~1s) -> Reconnect
                        {
                            AppLogger.Warning($"[Access Controller] Mất kết nối realtime (GetRTLog code={result}). Đang tự động kết nối lại...");
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
                            }

                            if (_handle != IntPtr.Zero)
                            {
                                consecutiveErrors = 0;
                                AppLogger.Information($"[Access Controller] Đã tự động phục hồi kết nối ({_lastIp}:{_lastPort}) thành công!");
                            }
                            else
                            {
                                await Task.Delay(2000, cancellationToken);
                            }
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
        /// Định dạng: Time,Pin,CardNo,DoorID,EventType,InOutState,VerifyMode
        /// Ví dụ: 2026-08-26 10:47:41,0,0,1,221,2,200
        /// </summary>
        public void ParseAndDispatchLog(string rawLog)
        {
            if (string.IsNullOrWhiteSpace(rawLog)) return;

            var parts = rawLog.Split(',');
            if (parts.Length < 5)
            {
                AppLogger.Warning($"[Access Controller RTLog] Bỏ qua log không đủ 5 trường: {rawLog}", "AccessController");
                return;
            }

            // Nếu Bit 4 là 255 -> Đây là bản ghi trạng thái Door/Alarm status, không phải realtime event
            if (parts[4].Trim() == "255")
            {
                AppLogger.Debug($"[Access Controller RTLog] Nhận gói tin trạng thái định kỳ Door/Alarm (Bit4=255): {rawLog}");
                return;
            }

            // Xả các bản ghi cũ trong bộ đệm controller trong 1.5 giây đầu sau khi kết nối
            if (DateTime.Now < _drainUntil)
            {
                AppLogger.Information($"[Access Controller RTLog] Xả sự kiện tồn đọng lúc khởi động: {rawLog}");
                return;
            }

            AppLogger.Information($"[Access Controller RTLog] Nhận chuỗi sự kiện: {rawLog}", "AccessController");

            // Cột DoorID / AuxID (index 3): 1 = Làn Vào, 2 = Làn Ra
            if (!int.TryParse(parts[3].Trim(), out var portIndex) || (portIndex != 1 && portIndex != 2))
            {
                AppLogger.Debug($"[Access Controller RTLog] Cổng không thuộc Làn 1 hoặc 2 (Port={parts[3]}): {rawLog}");
                return;
            }

            // Cột EventType (index 4):
            // 221 = CÓ XE / Đang kích hoạt cảm biến Radar / Vòng từ (Aux In Closed)
            // 220 = HẾT XE / Đã ngắt cảm biến (Aux In Opened / Restored)
            // 25  = Báo động Aux Input
            // 1   = Kích hoạt cảm biến
            _ = int.TryParse(parts[4].Trim(), out var eventType);

            // 221, 25, 1: CÓ XE ĐẾN (Active = true)
            // 220: HẾT XE / ĐÃ QUA (Active = false)
            bool isActive = eventType == 221 || eventType == 25 || eventType == 1;

            var (category, title, message) = (portIndex, isActive) switch
            {
                (1, true) => (NotificationCategory.LaneIn, "Phát hiện xe vào", "Cảm biến Radar Làn Vào phát hiện có xe đến."),
                (1, false) => (NotificationCategory.LaneIn, "Xe đã qua cổng vào", "Xe đã di chuyển qua khỏi vùng cảm biến Làn Vào."),
                (2, true) => (NotificationCategory.LaneOut, "Phát hiện xe ra", "Cảm biến Radar Làn Ra phát hiện có xe đến."),
                _ => (NotificationCategory.LaneOut, "Xe đã qua cổng ra", "Xe đã di chuyển qua khỏi vùng cảm biến Làn Ra.")
            };
            AppNotificationService.NotifyInfo(category, title, message, rawLog);

            AppLogger.Information($"[Access Controller Trigger] Phát sự kiện OnAuxInputTriggered: Làn {portIndex} (Aux {portIndex}), IsActive={isActive}, EventType={eventType}");

            OnAuxInputTriggered?.Invoke(this, new AuxTriggerEventArgs(
                auxPort: portIndex,
                isActive: isActive,
                triggerTime: DateTime.Now,
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

