using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Repositories;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Api.Services
{
    /// <summary>
    /// Background Service tiêu thụ các bản ghi AuditLog từ Channel và lưu vào MongoDB.
    /// Tự động thiết lập TTL Index trên MongoDB theo cấu hình RetentionDays.
    /// </summary>
    public class AuditLogBackgroundWorker : BackgroundService
    {
        private readonly IAuditLogQueue _queue;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuditLogBackgroundWorker> _logger;
        private readonly IConfiguration _configuration;

        public AuditLogBackgroundWorker(
            IAuditLogQueue queue,
            IServiceProvider serviceProvider,
            ILogger<AuditLogBackgroundWorker> logger,
            IConfiguration configuration)
        {
            _queue = queue;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AuditLogBackgroundWorker bắt đầu hoạt động...");

            // 1. Cấu hình TTL Index trên MongoDB nếu chưa có
            await EnsureTtlIndexAsync(stoppingToken);

            // 2. Lắng nghe và xử lý ghi log từ Channel
            try
            {
                await foreach (var log in _queue.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var repo = scope.ServiceProvider.GetRequiredService<IRepository<AuditLog>>();
                        await repo.AddAsync(log, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi khi lưu bản ghi AuditLog vào cơ sở dữ liệu: {Message}", ex.Message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("AuditLogBackgroundWorker nhận tín hiệu dừng hệ thống.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng trong AuditLogBackgroundWorker: {Message}", ex.Message);
            }
        }

        private async Task EnsureTtlIndexAsync(CancellationToken cancellationToken)
        {
            try
            {
                var retentionDays = _configuration.GetValue<int>("AuditLog:RetentionDays", 365);
                using var scope = _serviceProvider.CreateScope();
                var repo = scope.ServiceProvider.GetRequiredService<MongoRepository<AuditLog>>();
                var collection = repo.Collection;

                var indexKeys = Builders<AuditLog>.IndexKeys.Ascending(x => x.CreatedAt);
                var indexOptions = new CreateIndexOptions
                {
                    ExpireAfter = TimeSpan.FromDays(retentionDays),
                    Name = "ttl_auditlog_createdat"
                };

                await collection.Indexes.CreateOneAsync(
                    new CreateIndexModel<AuditLog>(indexKeys, indexOptions),
                    cancellationToken: cancellationToken);

                _logger.LogInformation("Đã khởi tạo TTL Index cho AuditLogs với thời hạn: {Days} ngày.", retentionDays);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Không thể tạo TTL Index cho AuditLogs: {Message}", ex.Message);
            }
        }
    }
}
