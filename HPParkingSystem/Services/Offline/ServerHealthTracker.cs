using HPParkingSystem.Models.Data;
using HPParkingSystem.Services.Logging;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HPParkingSystem.Services.Offline
{
    /// <summary>
    /// Triển khai giám sát kết nối máy chủ MongoDB và Thư mục ảnh theo cơ chế Hysteresis / Circuit Breaker
    /// Lớp 1: Socket TCP Ping cổng 27017 (< 100ms)
    /// Lớp 2: Mongo Driver Command Ping
    /// </summary>
    public class ServerHealthTracker : IServerHealthTracker
    {
        private static readonly Lazy<ServerHealthTracker> _instance = new(() => new ServerHealthTracker(MongoDbContext.Instance));
        public static ServerHealthTracker Instance => _instance.Value;

        private readonly MongoDbContext _context;
        private readonly string _host;
        private readonly int _port;
        private readonly object _lockObj = new();

        private volatile bool _isServerOnline = true;
        private volatile bool _isImageShareOnline = true;
        private volatile string? _lastMongoError = null;
        private volatile string? _lastImageShareError = null;

        private int _consecutiveSuccesses = 0;
        private int _consecutiveFailures = 0;

        private const int REQUIRED_SUCCESS_COUNT = 2; // Cần 2 lần thành công liên tiếp để xác nhận Online (chống chập chờn)
        private const int REQUIRED_FAILURE_COUNT = 2; // Cần 2 lần thất bại liên tiếp để xác nhận Offline

        public bool IsServerOnline => _isServerOnline;
        public bool IsImageShareOnline => _isImageShareOnline;
        public string? LastMongoError => _lastMongoError;
        public string? LastImageShareError => _lastImageShareError;

        public bool HasIncident => !_isServerOnline || !_isImageShareOnline || !string.IsNullOrEmpty(_lastMongoError) || !string.IsNullOrEmpty(_lastImageShareError);

        public int ConsecutiveSuccesses => _consecutiveSuccesses;
        public int ConsecutiveFailures => _consecutiveFailures;

        public event EventHandler<bool>? ServerStatusChanged;

        public ServerHealthTracker(MongoDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _host = !string.IsNullOrWhiteSpace(_context.ServerHost) ? _context.ServerHost : "127.0.0.1";
            _port = _context.ServerPort > 0 ? _context.ServerPort : 27017;
        }

        public void MarkOffline(string? reason = null)
        {
            bool statusChanged = false;
            lock (_lockObj)
            {
                _lastMongoError = reason;
                _consecutiveSuccesses = 0;
                _consecutiveFailures = REQUIRED_FAILURE_COUNT;
                if (_isServerOnline)
                {
                    _isServerOnline = false;
                    statusChanged = true;
                }
            }

            if (statusChanged)
            {
                ServerStatusChanged?.Invoke(this, false);
            }
        }

        public void MarkOnline()
        {
            bool statusChanged = false;
            lock (_lockObj)
            {
                _lastMongoError = null;
                _consecutiveFailures = 0;
                _consecutiveSuccesses = REQUIRED_SUCCESS_COUNT;
                if (!_isServerOnline)
                {
                    _isServerOnline = true;
                    statusChanged = true;
                }
            }

            if (statusChanged)
            {
                ServerStatusChanged?.Invoke(this, true);
            }
        }

        public void MarkImageShareStatus(bool isOk, string? error = null)
        {
            bool statusChanged = false;
            lock (_lockObj)
            {
                if (_isImageShareOnline != isOk || _lastImageShareError != error)
                {
                    _isImageShareOnline = isOk;
                    _lastImageShareError = isOk ? null : error;
                    statusChanged = true;
                }
            }

            if (statusChanged)
            {
                ServerStatusChanged?.Invoke(this, _isServerOnline && _isImageShareOnline);
            }
        }

        public void MarkMongoStatus(bool isOk, string? error = null)
        {
            if (!isOk)
            {
                MarkOffline(error);
            }
            else
            {
                lock (_lockObj)
                {
                    _lastMongoError = null;
                }
            }
        }

        public string GetIncidentSummary(int pendingRecords, int pendingImages)
        {
            var sb = new StringBuilder();
            sb.AppendLine("CHI TIẾT TRẠNG THÁI MÁY CHỦ:");
            sb.AppendLine("──────────────────────────────────────────────────");

            // 1. Cơ sở dữ liệu
            if (!_isServerOnline)
            {
                sb.AppendLine("• Cơ sở dữ liệu (MongoDB): ❌ MẤT KẾT NỐI");
                if (!string.IsNullOrEmpty(_lastMongoError))
                {
                    sb.AppendLine($"  -> Lý do: {_lastMongoError}");
                }
            }
            else if (!string.IsNullOrEmpty(_lastMongoError))
            {
                sb.AppendLine("• Cơ sở dữ liệu (MongoDB): ⚠️ KẾT NỐI NHƯNG LỖI GHI DỮ LIỆU");
                sb.AppendLine($"  -> Lý do: {_lastMongoError}");
            }
            else
            {
                sb.AppendLine("• Cơ sở dữ liệu (MongoDB): 🟢 Trực tuyến bình thường");
            }

            // 2. Thư mục chia sẻ ảnh
            if (!_isImageShareOnline)
            {
                sb.AppendLine("• Thư mục ảnh máy chủ (Captures): ❌ KHÔNG THỂ LƯU ẢNH");
                if (!string.IsNullOrEmpty(_lastImageShareError))
                {
                    sb.AppendLine($"  -> Lý do: {_lastImageShareError}");
                }
            }
            else
            {
                sb.AppendLine("• Thư mục ảnh máy chủ (Captures): 🟢 Trực tuyến bình thường");
            }

            // 3. Dữ liệu đang lưu tạm
            sb.AppendLine();
            sb.AppendLine("• Dữ liệu đang lưu tạm trên máy bốt:");
            sb.AppendLine($"  -> {pendingRecords} lượt xe (lưu trong LiteDB cục bộ)");
            sb.AppendLine($"  -> {pendingImages} file ảnh (lưu trong OfflineCaptures cục bộ)");

            sb.AppendLine("──────────────────────────────────────────────────");
            sb.AppendLine("💡 Hướng dẫn: Kiểm tra dây mạng LAN, kiểm tra máy chủ hoặc dung lượng ổ đĩa.");
            sb.AppendLine("Hệ thống sẽ tự động đồng bộ lại ngay khi máy chủ hoạt động bình thường.");

            return sb.ToString();
        }

        public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            bool currentCheckSuccess = false;
            string? checkError = null;

            try
            {
                // Lớp 1: Socket TCP Ping cổng MongoDB với timeout 1000ms (tránh false alarm do jitter mạng LAN)
                bool tcpOk = await CheckTcpPortAsync(_host, _port, 1000, cancellationToken).ConfigureAwait(false);
                if (tcpOk)
                {
                    // Lớp 2: Kiểm tra database engine thực sự phản hồi lệnh ping
                    bool mongoOk = await _context.PingAsync(1500, cancellationToken).ConfigureAwait(false);
                    currentCheckSuccess = mongoOk;
                    if (!mongoOk)
                    {
                        checkError = "Lệnh Ping MongoDB không phản hồi trong 1500ms";
                    }
                }
                else
                {
                    checkError = $"Không thể kết nối cổng TCP {_host}:{_port} (quá thời gian chờ 1000ms)";
                }
            }
            catch (Exception ex)
            {
                currentCheckSuccess = false;
                checkError = ex.Message;
            }

            bool statusChanged = false;
            bool newStatus = _isServerOnline;

            lock (_lockObj)
            {
                if (currentCheckSuccess)
                {
                    _consecutiveFailures = 0;
                    _consecutiveSuccesses++;
                    _lastMongoError = null; // Luôn xóa sạch lỗi cũ khi kiểm tra thành công!

                    if (!_isServerOnline && _consecutiveSuccesses >= REQUIRED_SUCCESS_COUNT)
                    {
                        _isServerOnline = true;
                        newStatus = true;
                        statusChanged = true;
                    }
                }
                else
                {
                    _consecutiveSuccesses = 0;
                    _consecutiveFailures++;

                    // Chỉ ghi nhận lỗi khi đã xác nhận thất bại liên tiếp (Hysteresis) để tránh báo động giả
                    if (_consecutiveFailures >= REQUIRED_FAILURE_COUNT)
                    {
                        _lastMongoError = checkError;
                        if (_isServerOnline)
                        {
                            _isServerOnline = false;
                            newStatus = false;
                            statusChanged = true;
                        }
                    }
                }
            }

            if (statusChanged)
            {
                if (newStatus)
                {
                    AppLogger.Information("[ServerHealthTracker] Kết nối máy chủ MongoDB đã phục hồi (Online).", "HealthTracker");
                }
                else
                {
                    AppLogger.Warning($"[ServerHealthTracker] Mất kết nối máy chủ MongoDB (Offline). Lý do: {_lastMongoError}", "HealthTracker");
                }
                ServerStatusChanged?.Invoke(this, newStatus);
            }

            return currentCheckSuccess;
        }

        private static async Task<bool> CheckTcpPortAsync(string host, int port, int timeoutMs, CancellationToken cancellationToken)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(host, port);
                var delayTask = Task.Delay(timeoutMs, cancellationToken);

                var completed = await Task.WhenAny(connectTask, delayTask).ConfigureAwait(false);
                if (completed == connectTask && client.Connected)
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
    }
}
