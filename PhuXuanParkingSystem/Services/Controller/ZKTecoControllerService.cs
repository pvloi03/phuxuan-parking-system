using PhuXuanParkingSystem.SDK.ZKTeco;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Controller
{
    public class ZKTecoControllerService : IDisposable
    {
        private readonly object _lockObj = new();
        private IntPtr _handle = IntPtr.Zero;
        private CancellationTokenSource? _pollingCts;
        private Task? _pollingTask;

        public ZKTecoControllerConfig Config { get; set; } = new();

        public bool IsConnected => _handle != IntPtr.Zero;

        public event Action<bool, string> OnStatusChanged = delegate { };

        /// <summary>
        /// Sự kiện nhận mọi bản ghi log từ Controller
        /// </summary>
        public event Action<ZKTecoLogEvent> OnLogReceived = delegate { };

        /// <summary>
        /// Sự kiện chuyên biệt khi Cảm biến Radar (AUX Input) phát hiện xe
        /// </summary>
        public event Action<ZKTecoLogEvent> OnRadarTriggered = delegate { };

        public async Task<bool> ConnectAsync()
        {
            return await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    if (IsConnected) return true;

                    string connStr = Config.ToConnectionString();
                    Debug.WriteLine($"[ZKTeco] Đang kết nối controller với tham số: {connStr}");

                    _handle = ZKTecoPullSDK.Connect(connStr);

                    if (_handle != IntPtr.Zero)
                    {
                        StartLogPolling();
                        OnStatusChanged?.Invoke(true, "Đã kết nối Controller (Cảm biến Radar AUX).");
                        return true;
                    }

                    int errCode = ZKTecoPullSDK.PullLastError();
                    Debug.WriteLine($"[ZKTeco] Kết nối thất bại. Mã lỗi: {errCode}");
                    OnStatusChanged?.Invoke(false, $"Kết nối Controller thất bại. Mã lỗi: {errCode}");
                    return false;
                }
            });
        }

        private void StartLogPolling()
        {
            StopLogPolling();

            _pollingCts = new CancellationTokenSource();
            var token = _pollingCts.Token;

            _pollingTask = Task.Factory.StartNew(async () =>
            {
                byte[] buffer = new byte[64 * 1024]; // 64KB buffer

                while (!token.IsCancellationRequested && _handle != IntPtr.Zero)
                {
                    try
                    {
                        int ret;
                        lock (_lockObj)
                        {
                            if (_handle == IntPtr.Zero) break;
                            ret = ZKTecoPullSDK.GetRTLog(_handle, ref buffer[0], buffer.Length);
                        }

                        if (ret >= 0)
                        {
                            string rawData = Encoding.Default.GetString(buffer, 0, buffer.Length);
                            int nullIdx = rawData.IndexOf('\0');
                            if (nullIdx >= 0)
                            {
                                rawData = rawData.Substring(0, nullIdx);
                            }

                            if (!string.IsNullOrWhiteSpace(rawData))
                            {
                                string[] lines = rawData.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var line in lines)
                                {
                                    var logEvt = ZKTecoLogEvent.Parse(line);
                                    if (logEvt != null)
                                    {
                                        OnLogReceived?.Invoke(logEvt);

                                        // Nếu là sự kiện radar phát hiện xe
                                        if (logEvt.IsVehicleDetected)
                                        {
                                            OnRadarTriggered?.Invoke(logEvt);
                                        }
                                    }
                                }
                            }

                            await Task.Delay(20, token);
                        }
                        else
                        {
                            await Task.Delay(100, token);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ZKTeco Polling Error] {ex.Message}");
                        await Task.Delay(500, token);
                    }
                }
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();
        }

        private void StopLogPolling()
        {
            if (_pollingCts != null)
            {
                try
                {
                    _pollingCts.Cancel();
                    _pollingTask?.Wait(500);
                }
                catch { }
                finally
                {
                    _pollingCts.Dispose();
                    _pollingCts = null;
                    _pollingTask = null;
                }
            }
        }

        public void Disconnect()
        {
            lock (_lockObj)
            {
                StopLogPolling();

                if (_handle != IntPtr.Zero)
                {
                    ZKTecoPullSDK.Disconnect(_handle);
                    _handle = IntPtr.Zero;
                    OnStatusChanged?.Invoke(false, "Đã ngắt kết nối Controller.");
                }
            }
        }

        public void Dispose()
        {
            Disconnect();
            GC.SuppressFinalize(this);
        }
    }
}
