using PhuXuanParkingSystem.Models.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Api.Services
{
    /// <summary>
    /// Triển khai hàng đợi Channel với System.Threading.Channels
    /// Non-blocking, thread-safe, tối ưu hóa throughput cho API
    /// </summary>
    public class AuditLogQueue : IAuditLogQueue
    {
        private readonly Channel<AuditLog> _channel;

        public AuditLogQueue(int capacity = 5000)
        {
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            };
            _channel = Channel.CreateBounded<AuditLog>(options);
        }

        public async ValueTask QueueLogAsync(AuditLog log, CancellationToken cancellationToken = default)
        {
            if (log == null) return;
            await _channel.Writer.WriteAsync(log, cancellationToken);
        }

        public IAsyncEnumerable<AuditLog> ReadAllAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }
    }
}
