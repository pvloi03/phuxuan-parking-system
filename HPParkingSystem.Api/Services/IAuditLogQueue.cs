using HPParkingSystem.Models.Entities;

namespace HPParkingSystem.Api.Services
{
    /// <summary>
    /// Kênh hàng đợi nhật ký kiểm toán bất đồng bộ (In-Memory Channel Queue)
    /// </summary>
    public interface IAuditLogQueue
    {
        ValueTask QueueLogAsync(AuditLog log, CancellationToken cancellationToken = default);
        IAsyncEnumerable<AuditLog> ReadAllAsync(CancellationToken cancellationToken);
    }
}
