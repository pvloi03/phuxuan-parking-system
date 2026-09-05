using PhuXuanParkingSystem.Models.Data;
using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Offline
{
    /// <summary>
    /// Triển khai giám sát kết nối máy chủ MongoDB theo cơ chế Hysteresis / Circuit Breaker
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
        private int _consecutiveSuccesses = 0;
        private int _consecutiveFailures = 0;

        private const int REQUIRED_SUCCESS_COUNT = 2; // Cần 2 lần thành công liên tiếp để xác nhận Online (chống chập chờn)
        private const int REQUIRED_FAILURE_COUNT = 2; // Cần 2 lần thất bại liên tiếp để xác nhận Offline

        public bool IsServerOnline => _isServerOnline;
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

        public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
        {
            bool currentCheckSuccess = false;

            try
            {
                // Lớp 1: Socket TCP Ping siêu nhẹ (< 150ms)
                bool tcpOk = await CheckTcpPortAsync(_host, _port, 150, cancellationToken).ConfigureAwait(false);
                if (tcpOk)
                {
                    // Lớp 2: Kiểm tra database engine thực sự phản hồi lệnh ping
                    bool mongoOk = await _context.PingAsync(1000, cancellationToken).ConfigureAwait(false);
                    currentCheckSuccess = mongoOk;
                }
            }
            catch
            {
                currentCheckSuccess = false;
            }

            bool statusChanged = false;
            bool newStatus = _isServerOnline;

            lock (_lockObj)
            {
                if (currentCheckSuccess)
                {
                    _consecutiveFailures = 0;
                    _consecutiveSuccesses++;

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

                    if (_isServerOnline && _consecutiveFailures >= REQUIRED_FAILURE_COUNT)
                    {
                        _isServerOnline = false;
                        newStatus = false;
                        statusChanged = true;
                    }
                }
            }

            if (statusChanged)
            {
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
                var timeoutTask = Task.Delay(timeoutMs, cancellationToken);

                var completedTask = await Task.WhenAny(connectTask, timeoutTask).ConfigureAwait(false);
                if (completedTask == connectTask && client.Connected)
                {
                    return true;
                }
            }
            catch
            {
                // Bỏ qua lỗi kết nối socket để trả về false
            }

            return false;
        }
    }
}
