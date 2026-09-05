using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Globalization;

namespace PhuXuanParkingSystem.Api.Services
{
    /// <summary>
    /// Background Service tự động dọn dẹp các thư mục ảnh Captures cũ hơn số ngày quy định
    /// Chạy định kỳ vào khung giờ cấu hình (mặc định 02:00 AM) để bảo toàn dung lượng đĩa máy chủ.
    /// </summary>
    public class CapturesCleanupBackgroundWorker : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<CapturesCleanupBackgroundWorker> _logger;

        public CapturesCleanupBackgroundWorker(
            IConfiguration configuration,
            ILogger<CapturesCleanupBackgroundWorker> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[CapturesCleanup] Worker đã khởi tạo.");

            // Đợi 1 phút sau khi khởi động hệ thống để tránh xung đột I/O lúc khởi động
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    bool enableAutoCleanup = _configuration.GetValue<bool>("CapturesSettings:EnableAutoCleanup", true);
                    if (enableAutoCleanup)
                    {
                        await RunCleanupAsync(stoppingToken);
                    }
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogError(ex, "[CapturesCleanup] Lỗi trong quá trình quét dọn dẹp ảnh cũ.");
                }

                // Tính thời gian chờ đến 02:00 AM tiếp theo
                int targetHour = _configuration.GetValue<int>("CapturesSettings:CleanupHour", 2);
                var now = DateTime.Now;
                var nextRun = now.Date.AddHours(targetHour);
                if (now >= nextRun)
                {
                    nextRun = nextRun.AddDays(1);
                }

                var delay = nextRun - now;
                _logger.LogInformation("[CapturesCleanup] Lần quét tiếp theo dự kiến lúc: {NextRun:yyyy-MM-dd HH:mm:ss} (sau {Hours:F1} giờ)", nextRun, delay.TotalHours);

                try
                {
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            _logger.LogInformation("[CapturesCleanup] Worker đã dừng.");
        }

        private Task RunCleanupAsync(CancellationToken stoppingToken)
        {
            string? capturesFolder = _configuration["CapturesSettings:FolderPath"]
                ?? _configuration["CapturesFolder"]
                ?? _configuration["CaptureSavePath"];

            if (string.IsNullOrWhiteSpace(capturesFolder))
            {
                _logger.LogDebug("[CapturesCleanup] Chua cau hinh duong dan thu muc anh Captures, bo qua don dep.");
                return Task.CompletedTask;
            }

            if (!Path.IsPathRooted(capturesFolder))
            {
                capturesFolder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, capturesFolder));
            }

            int retentionDays = _configuration.GetValue<int>("CapturesSettings:RetentionDays", 365);
            CleanupOldCaptures(capturesFolder, retentionDays, _logger, stoppingToken);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Thực hiện dọn dẹp các thư mục và file ảnh cũ hơn số ngày chỉ định
        /// </summary>
        public static (int deletedFolders, int deletedFiles) CleanupOldCaptures(
            string capturesFolder,
            int retentionDays,
            ILogger? logger = null,
            CancellationToken cancellationToken = default)
        {
            if (!Directory.Exists(capturesFolder))
            {
                logger?.LogDebug("[CapturesCleanup] Thư mục ảnh không tồn tại: {Folder}", capturesFolder);
                return (0, 0);
            }

            if (retentionDays <= 0)
            {
                logger?.LogWarning("[CapturesCleanup] RetentionDays cấu hình không hợp lệ ({Days}), bỏ qua dọn dẹp.", retentionDays);
                return (0, 0);
            }

            var cutoffDate = DateTime.Today.AddDays(-retentionDays);
            logger?.LogInformation("[CapturesCleanup] Bắt đầu quét thư mục '{Folder}' dọn dẹp ảnh cũ hơn ngày {CutoffDate:yyyy-MM-dd} (>{Days} ngày)...",
                capturesFolder, cutoffDate, retentionDays);

            int deletedFolders = 0;
            int deletedFiles = 0;

            var dirInfo = new DirectoryInfo(capturesFolder);

            // 1. Quét các thư mục con (ví dụ: yyyy-MM-dd hoặc yyyyMMdd)
            foreach (var subDir in dirInfo.GetDirectories())
            {
                if (cancellationToken.IsCancellationRequested) break;

                bool shouldDelete = false;

                // Thử parse tên thư mục theo định dạng ngày tháng
                if (DateTime.TryParseExact(subDir.Name, new[] { "yyyy-MM-dd", "yyyyMMdd", "yyyy_MM_dd" },
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var folderDate))
                {
                    if (folderDate < cutoffDate)
                    {
                        shouldDelete = true;
                    }
                }
                else
                {
                    // Nếu không phải định dạng ngày, kiểm tra LastWriteTime & CreationTime
                    if (subDir.LastWriteTimeUtc < cutoffDate && subDir.CreationTimeUtc < cutoffDate)
                    {
                        shouldDelete = true;
                    }
                }

                if (shouldDelete)
                {
                    try
                    {
                        subDir.Delete(recursive: true);
                        deletedFolders++;
                        logger?.LogInformation("[CapturesCleanup] Đã xóa thư mục ảnh cũ: {SubDir}", subDir.FullName);
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, "[CapturesCleanup] Không thể xóa thư mục '{SubDir}': {Message}", subDir.FullName, ex.Message);
                    }
                }
            }

            // 2. Quét các file lẻ trực tiếp nằm trong thư mục gốc Captures (nếu có)
            foreach (var file in dirInfo.GetFiles())
            {
                if (cancellationToken.IsCancellationRequested) break;

                if (file.LastWriteTimeUtc < cutoffDate)
                {
                    try
                    {
                        file.Delete();
                        deletedFiles++;
                    }
                    catch (Exception ex)
                    {
                        logger?.LogWarning(ex, "[CapturesCleanup] Không thể xóa file '{File}': {Message}", file.FullName, ex.Message);
                    }
                }
            }

            logger?.LogInformation("[CapturesCleanup] Hoàn tất dọn dẹp. Đã xóa {Folders} thư mục và {Files} file ảnh quá hạn.", deletedFolders, deletedFiles);
            return (deletedFolders, deletedFiles);
        }
    }
}
