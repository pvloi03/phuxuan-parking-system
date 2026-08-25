using HPParkingThaiThuy.SDK.ZKTeco;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HPParkingThaiThuy.Services.Controller
{
    /// <summary>
    /// Adapter kết nối và lắng nghe sự kiện từ Bộ điều khiển ZKTeco C3-200 qua Pull SDK
    /// </summary>
    public class ZKTecoDeviceAdapter : IDisposable
    {
        private IntPtr _handle = IntPtr.Zero;
        private readonly object _lockObj = new();
        private CancellationTokenSource? _listeningCts;
        private Task? _listeningTask;
        private bool _isDisposed;
        private string _lastRawLog = string.Empty;

        public event EventHandler<AuxTriggerEventArgs>? OnAuxInputTriggered;

        public event Action<bool, string>? OnStatusChanged;

        public bool IsConnected => _handle != IntPtr.Zero;

        /// <summary>
        /// Kết nối tới bộ điều khiển ZKTeco qua giao thức TCP
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
                        OnStatusChanged?.Invoke(true, $"Đã kết nối C3-200 ({ipAddress}:{port}) thành công.");
                        return true;
                    }

                    int errCode = ZKTecoPullSDK.PullLastError();
                    OnStatusChanged?.Invoke(false, $"Kết nối C3-200 thất bại. Mã lỗi: {errCode}");
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
                OnStatusChanged?.Invoke(false, "Đã ngắt kết nối Controller.");
            }

            _lastRawLog = string.Empty;
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
        /// Dùng TaskCompletionSource để chờ delay hoặc cancel ngay lập tức mà không ném Exception.
        /// </summary>
        private async Task ListenLoopAsync(CancellationToken cancellationToken)
        {
            var buffer = new byte[256];
            const int bufferSize = 256;

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
                        var rawString = Encoding.Default.GetString(buffer).Trim('\0', '\r', '\n');

                        if (!string.IsNullOrWhiteSpace(rawString) && rawString != _lastRawLog)
                        {
                            _lastRawLog = rawString;
                            ParseAndDispatchLog(rawString);
                        }
                    }
                }
                catch
                {
                    // Bỏ qua lỗi đọc dữ liệu tạm thời
                }

                // TaskCompletionSource pattern:
                // - Không block thread (truly async)
                // - Không ném TaskCanceledException
                // - Thoát ngay lập tức khi token bị Cancel mà không phải chờ hết 300ms
                var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                using (cancellationToken.Register(static state => ((TaskCompletionSource<bool>)state!).TrySetResult(true), tcs))
                {
                    await Task.WhenAny(Task.Delay(300), tcs.Task);
                }

                if (cancellationToken.IsCancellationRequested) break;
            }
        }

        /// <summary>
        /// Phân tích chuỗi RTLog và phát sự kiện OnAuxInputTriggered
        /// Định dạng: Time,Pin,CardNo,DoorID,EventType,InOutState,VerifyMode
        /// </summary>
        public void ParseAndDispatchLog(string rawLog)
        {
            var parts = rawLog.Split(',');
            if (parts.Length < 5) return;

            // Cột DoorID (index 3): 1 = Làn Vào, 2 = Làn Ra
            if (!int.TryParse(parts[3], out var portIndex) || (portIndex != 1 && portIndex != 2))
            {
                return;
            }

            // Cột EventType (index 4):
            // 221 = Có xe / Đang kích hoạt Radar
            // 220 = Hết xe / Đã ngắt Radar
            _ = int.TryParse(parts[4], out var eventType);

            bool isActive = eventType == 221;

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
