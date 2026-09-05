using PhuXuanParkingSystem.Models.Data;
using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Storage
{
    /// <summary>
    /// Triển khai dịch vụ lưu ảnh linh hoạt: Ghi trực tiếp lên máy chủ khi online,
    /// tự động fallback lưu vào thư mục cục bộ OfflineCaptures/ khi offline và quản lý hàng đợi đồng bộ
    /// </summary>
    public class ImageStorageService : IImageStorageService
    {
        private readonly LiteDbContext _liteDb;
        private readonly string _primaryPath;
        private readonly string _offlinePath;

        public string PrimaryPath => _primaryPath;
        public string OfflinePath => _offlinePath;

        public ImageStorageService(LiteDbContext? liteDb = null, string? primaryPath = null, string? offlinePath = null)
        {
            _liteDb = liteDb ?? LiteDbContext.Instance;

            // Đọc đường dẫn chính từ config hoặc mặc định Captures
            string rawPrimary = primaryPath ?? ConfigurationManager.AppSettings?["CaptureSavePath"] ?? "Captures";
            _primaryPath = Path.IsPathRooted(rawPrimary) ? rawPrimary : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rawPrimary);

            // Đường dẫn ngoại tuyến trên máy bốt
            string rawOffline = offlinePath ?? "OfflineCaptures";
            _offlinePath = Path.IsPathRooted(rawOffline) ? rawOffline : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rawOffline);

            EnsureDirectoryExists(_offlinePath);
        }

        public async Task<ImageSaveResult> SaveImageBytesAsync(byte[] imageBytes, string subFolder, string fileName, bool isServerOnline)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return new ImageSaveResult { Success = false, ErrorMessage = "Dữ liệu ảnh rỗng" };
            }

            string relativeSubPath = Path.Combine(subFolder, fileName);

            // 1. Nếu Server Online, thử ghi trực tiếp vào Primary Path (Server Network Share)
            if (isServerOnline)
            {
                try
                {
                    string targetDir = Path.Combine(_primaryPath, subFolder);
                    EnsureDirectoryExists(targetDir);

                    string targetFile = Path.Combine(targetDir, fileName);
                    using (var fs = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, useAsync: true))
                    {
                        await fs.WriteAsync(imageBytes, 0, imageBytes.Length).ConfigureAwait(false);
                    }

                    return new ImageSaveResult
                    {
                        Success = true,
                        FinalPath = targetFile,
                        IsSavedLocally = false
                    };
                }
                catch
                {
                    // Lỗi I/O mạng hoặc mất quyền truy cập -> Chuyển sang fallback cục bộ
                }
            }

            // 2. Fallback ghi vào bộ nhớ cục bộ trên máy bốt (Offline)
            try
            {
                string localTargetDir = Path.Combine(_offlinePath, subFolder);
                EnsureDirectoryExists(localTargetDir);

                string localTargetFile = Path.Combine(localTargetDir, fileName);
                using (var fs = new FileStream(localTargetFile, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, useAsync: true))
                {
                    await fs.WriteAsync(imageBytes, 0, imageBytes.Length).ConfigureAwait(false);
                }

                // Đưa vào hàng đợi đồng bộ ảnh
                var imageTask = new OfflineImageSyncTask
                {
                    LocalFilePath = localTargetFile,
                    RemoteRelativePath = relativeSubPath,
                    CreatedAt = DateTime.Now,
                    IsSynced = false
                };
                _liteDb.GetImageSyncTasks().Insert(imageTask);

                return new ImageSaveResult
                {
                    Success = true,
                    FinalPath = localTargetFile,
                    IsSavedLocally = true
                };
            }
            catch (Exception ex)
            {
                return new ImageSaveResult
                {
                    Success = false,
                    ErrorMessage = $"Không thể lưu file ảnh cả Primary lẫn Offline: {ex.Message}"
                };
            }
        }

        public async Task<ImageSaveResult> SaveImageFileAsync(string sourceFilePath, string subFolder, string fileName, bool isServerOnline)
        {
            if (string.IsNullOrEmpty(sourceFilePath) || !File.Exists(sourceFilePath))
            {
                return new ImageSaveResult { Success = false, ErrorMessage = "File ảnh nguồn không tồn tại" };
            }

            try
            {
                byte[] bytes;
                using (var fs = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, useAsync: true))
                {
                    bytes = new byte[fs.Length];
                    await fs.ReadAsync(bytes, 0, (int)fs.Length).ConfigureAwait(false);
                }

                return await SaveImageBytesAsync(bytes, subFolder, fileName, isServerOnline).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                return new ImageSaveResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        public async Task<int> SyncPendingImagesAsync()
        {
            var taskCollection = _liteDb.GetImageSyncTasks();
            var pendingTasks = taskCollection.Find(t => !t.IsSynced);
            int syncedCount = 0;

            foreach (var task in pendingTasks)
            {
                if (!File.Exists(task.LocalFilePath))
                {
                    // File nguồn không còn tồn tại trên máy bốt
                    task.IsSynced = true;
                    task.LastError = "Local file not found";
                    task.SyncedAt = DateTime.Now;
                    taskCollection.Update(task);
                    continue;
                }

                try
                {
                    string remoteFullPath = Path.Combine(_primaryPath, task.RemoteRelativePath);
                    string? remoteDir = Path.GetDirectoryName(remoteFullPath);
                    if (!string.IsNullOrEmpty(remoteDir) && !Directory.Exists(remoteDir))
                    {
                        Directory.CreateDirectory(remoteDir);
                    }

                    // Copy file ảnh từ local lên share server
                    using (var sourceStream = new FileStream(task.LocalFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                    using (var destStream = new FileStream(remoteFullPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                    {
                        await sourceStream.CopyToAsync(destStream).ConfigureAwait(false);
                    }

                    task.IsSynced = true;
                    task.SyncedAt = DateTime.Now;
                    taskCollection.Update(task);
                    syncedCount++;
                }
                catch (Exception ex)
                {
                    task.RetryCount++;
                    task.LastError = ex.Message;
                    taskCollection.Update(task);
                    // Dừng vòng lặp nếu share mạng server vẫn chưa ghi được
                    break;
                }
            }

            return syncedCount;
        }

        private static void EnsureDirectoryExists(string dirPath)
        {
            if (!string.IsNullOrWhiteSpace(dirPath) && !Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
        }
    }
}
