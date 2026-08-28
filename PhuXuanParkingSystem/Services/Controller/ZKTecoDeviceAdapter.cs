using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.SDK.ZKTeco;
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
    /// Adapter kết nối và lắng nghe sự kiện từ Bộ điều khiển ZKTeco C3-200 qua Pull SDK
    /// Implements IDeviceAdapter cho DeviceHealthManager
    /// </summary>
    public class ZKTecoDeviceAdapter : IDisposable
    {
        private IntPtr _handle = IntPtr.Zero;
        private readonly object _lockObj = new();
        private CancellationTokenSource? _listeningCts;
        private Task? _listeningTask;
        private bool _isDisposed;
        private string _lastRawLog = string.Empty;

        private string _lastIp = "192.168.1.202";
        private int _lastPort = 4370;
        private string? _lastPassword;

        public event EventHandler<AuxTriggerEventArgs>? OnAuxInputTriggered;

        /// <summary>
        /// TRUE = đang nhận log từ controller (ReadLog đang chạy)
        /// </summary>
        public bool IsStreaming => _listeningTask != null && !_listeningTask.IsCanceled && !_listeningTask.IsFaulted;

        /// <summary>
        /// Event khi trạng thái kết nối thay đổi
        /// </summary>
        public event EventHandler<DeviceConnectionState>? OnConnectionStateChanged;

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

                await client.ConnectAsync(_lastIp, _lastPort);
                return client.Connected;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Kết nối tới bộ điều khiển ZKTeco qua giao thức TCP
        /// </summary>
        public Task<bool> ConnectAsync(
            string ipAddress = "192.168.1.202",
            int port = 4370,
            string? password = null,
            CancellationToken cancellationToken = default)
        {
            _lastIp = ipAddress;
            _lastPort = port;
            _lastPassword = password;

            return Task.Run(() =>
            {
                lock (_lockObj)
                {
                    if (IsConnected)
                    {
                        DisconnectInternal();
                    }

                    var pwd = password ?? string.Empty;
                    var connectParams = $"protocol=TCP,ipaddress={ipAddress.Trim()},port={port},timeout=4000,passwd={pwd}";

                    _handle = ZKTecoPullSDK.Connect(connectParams);

                    if (_handle != IntPtr.Zero)
                    {
                        StartListening();
                        AppNotificationService.NotifySuccess(NotificationCategory.Controller, "Bộ Điều Khiển ZKTeco", $"Đã kết nối C3-200 ({ipAddress}:{port}) thành công.", ipAddress);
                        OnConnectionStateChanged?.Invoke(this, DeviceConnectionState.Connected);
                        return true;
                    }

                    int errCode = ZKTecoPullSDK.PullLastError();
                    AppLogger.Warning($"[ZKTeco Controller] Kết nối thất bại ({ipAddress}:{port}), Mã lỗi: {errCode}");
                    return false;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Ngắt kết nối tới thiết bị
        /// </summary>
        public Task DisconnectAsync()
        {
            return Task.Run(() =>
            {
                lock (_lockObj)
                {
                    DisconnectInternal();
                }
            });
        }

        private void DisconnectInternal()
        {
            StopListening();

            if (_handle != IntPtr.Zero)
            {
                ZKTecoPullSDK.Disconnect(_handle);
                _handle = IntPtr.Zero;
            }

            _lastRawLog = string.Empty;
            OnConnectionStateChanged?.Invoke(this, DeviceConnectionState.Disconnected);
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
        /// Vòng lặp ngầm (truly async, không block thread) đọc Real-time Log từ ZKTeco Controller.
        /// Tự động phục hồi kết nối nếu socket bị ngắt ngầm.
        /// </summary>
        private async Task ListenLoopAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[256];
            const int bufferSize = 256;
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
                        var rawString = Encoding.Default.GetString(buffer).Trim('\0');

                        if (!string.IsNullOrWhiteSpace(rawString))
                        {
                            // Tách từng dòng log nếu buffer chứa nhiều sự kiện liên tiếp
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
                    else
                    {
                        consecutiveErrors++;
                        if (consecutiveErrors >= 5) // Lỗi liên tiếp 5 chu kỳ (~1s) -> socket bị rớt -> Reconnect
                        {
                            AppLogger.Warning($"[ZKTeco Controller] Mất kết nối realtime (GetRTLog code={result}). Đang tự động kết nối lại...");
                            lock (_lockObj)
                            {
                                if (_handle != IntPtr.Zero)
                                {
                                    ZKTecoPullSDK.Disconnect(_handle);
                                    _handle = IntPtr.Zero;
                                }
                                var pwd = _lastPassword ?? string.Empty;
                                var connectParams = $"protocol=TCP,ipaddress={_lastIp.Trim()},port={_lastPort},timeout=4000,passwd={pwd}";
                                _handle = ZKTecoPullSDK.Connect(connectParams);
                            }

                            if (_handle != IntPtr.Zero)
                            {
                                consecutiveErrors = 0;
                                AppLogger.Information($"[ZKTeco Controller] Đã tự động phục hồi kết nối C3-200 ({_lastIp}:{_lastPort}) thành công!");
                            }
                            else
                            {
                                await Task.Delay(2000, cancellationToken);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Debug($"[ZKTeco ListenLoop Warning] {ex.Message}");
                }

                // TaskCompletionSource pattern:
                // - Không block thread (truly async)
                // - Không ném TaskCanceledException
                // - Thoát ngay lập tức khi token bị Cancel mà không phải chờ hết 200ms
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                using (cancellationToken.Register(static state => ((TaskCompletionSource<bool>)state!).TrySetResult(true), tcs))
                {
                    await Task.WhenAny(Task.Delay(200), tcs.Task);
                }

                if (cancellationToken.IsCancellationRequested) break;
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

            AppLogger.Information($"[ZKTeco RTLog] Nhận chuỗi log từ C3-200: {rawLog}", "ZKTecoController");

            var parts = rawLog.Split(',');
            if (parts.Length < 5)
            {
                AppLogger.Warning($"[ZKTeco RTLog] Bỏ qua log không đủ 5 trường: {rawLog}", "ZKTecoController");
                return;
            }

            // Cột DoorID / AuxID (index 3): 1 = Làn Vào, 2 = Làn Ra
            if (!int.TryParse(parts[3].Trim(), out var portIndex) || (portIndex != 1 && portIndex != 2))
            {
                AppLogger.Debug($"[ZKTeco RTLog] Cổng không thuộc Làn 1 hoặc 2 (Port={parts[3]}): {rawLog}");
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

            if (portIndex == 1)
            {
                if (isActive)
                    AppNotificationService.NotifyInfo(NotificationCategory.LaneIn, "Phát hiện xe vào", "Cảm biến Radar Làn Vào phát hiện có xe đến.", rawLog);
                else
                    AppNotificationService.NotifyInfo(NotificationCategory.LaneIn, "Xe đã qua cổng vào", "Xe đã di chuyển qua khỏi vùng cảm biến Làn Vào.", rawLog);
            }
            else if (portIndex == 2)
            {
                if (isActive)
                    AppNotificationService.NotifyInfo(NotificationCategory.LaneOut, "Phát hiện xe ra", "Cảm biến Radar Làn Ra phát hiện có xe đến.", rawLog);
                else
                    AppNotificationService.NotifyInfo(NotificationCategory.LaneOut, "Xe đã qua cổng ra", "Xe đã di chuyển qua khỏi vùng cảm biến Làn Ra.", rawLog);
            }

            AppLogger.Information($"[ZKTeco Trigger] Phát sự kiện OnAuxInputTriggered: Làn {portIndex} (Aux {portIndex}), IsActive={isActive}, EventType={eventType}");

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
