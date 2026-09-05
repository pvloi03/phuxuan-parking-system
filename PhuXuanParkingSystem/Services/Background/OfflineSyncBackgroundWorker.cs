using MongoDB.Driver;
using PhuXuanParkingSystem.Models.Data;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Services.Logging;
using PhuXuanParkingSystem.Services.Offline;
using PhuXuanParkingSystem.Services.Storage;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Background
{
    public class SyncStatusEventArgs : EventArgs
    {
        public bool IsServerOnline { get; set; }
        public int PendingRecordCount { get; set; }
        public int PendingImageCount { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Tiến trình chạy ngầm phụ trách:
    /// 1. Heartbeat kiểm tra kết nối Server định kỳ (3-5s)
    /// 2. Đồng bộ tự động File ảnh và Bản ghi từ LiteDB lên MongoDB Server khi Online trở lại
    /// 3. Cung cấp sự kiện cập nhật trạng thái thời gian thực lên giao diện WinForms
    /// </summary>
    public class OfflineSyncBackgroundWorker : IDisposable
    {
        private readonly IServerHealthTracker _healthTracker;
        private readonly LiteDbContext _liteDb;
        private readonly MongoDbContext _mongoDb;
        private readonly IImageStorageService _imageStorage;

        private CancellationTokenSource? _cts;
        private Task? _workerTask;
        private bool _isSyncing = false;
        private bool _disposed = false;

        public event EventHandler<SyncStatusEventArgs>? SyncStatusChanged;

        public bool IsRunning => _workerTask != null && !_workerTask.IsCompleted;

        public OfflineSyncBackgroundWorker(
            IServerHealthTracker? healthTracker = null,
            LiteDbContext? liteDb = null,
            MongoDbContext? mongoDb = null,
            IImageStorageService? imageStorage = null)
        {
            _healthTracker = healthTracker ?? ServerHealthTracker.Instance;
            _liteDb = liteDb ?? LiteDbContext.Instance;
            _mongoDb = mongoDb ?? MongoDbContext.Instance;
            _imageStorage = imageStorage ?? new ImageStorageService(_liteDb);

            _healthTracker.ServerStatusChanged += OnServerStatusChanged;
        }

        public void Start()
        {
            if (_workerTask != null && !_workerTask.IsCompleted)
            {
                return;
            }

            _cts = new CancellationTokenSource();
            _workerTask = Task.Run(() => WorkerLoopAsync(_cts.Token));
            AppLogger.Information("OfflineSyncBackgroundWorker đã khởi động.", "BackgroundSync");
        }

        public void Stop()
        {
            try
            {
                _cts?.Cancel();
                _workerTask?.Wait(2000);
            }
            catch { }
            finally
            {
                _cts?.Dispose();
                _cts = null;
                _workerTask = null;
            }
        }

        private async Task WorkerLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // 1. Kiểm tra kết nối máy chủ
                    bool isOnline = await _healthTracker.CheckHealthAsync(token).ConfigureAwait(false);

                    // 2. Nếu máy chủ Online, tiến hành đồng bộ dữ liệu tồn đọng
                    if (isOnline && !_isSyncing)
                    {
                        await RunSynchronizationAsync(token).ConfigureAwait(false);
                    }
                    else
                    {
                        NotifyStatus();
                    }

                    // 3. Nghỉ theo chu kỳ thích ứng (3s nếu Online, 5s nếu Offline)
                    int delayMs = isOnline ? 3000 : 5000;
                    await Task.Delay(delayMs, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    AppLogger.Warning($"Lỗi trong vòng lặp Background Sync: {ex.Message}", "BackgroundSync");
                    await Task.Delay(5000, token).ConfigureAwait(false);
                }
            }
        }

        private async Task RunSynchronizationAsync(CancellationToken token)
        {
            _isSyncing = true;
            try
            {
                // Bước A: Đồng bộ file ảnh từ thư mục OfflineCaptures lên Server Network Share
                int syncedImages = await _imageStorage.SyncPendingImagesAsync().ConfigureAwait(false);
                if (syncedImages > 0)
                {
                    AppLogger.Information($"[SYNC] Đã đồng bộ thành công {syncedImages} file ảnh lên máy chủ.", "BackgroundSync");
                }

                // Bước B: Đồng bộ các bản ghi dữ liệu (ParkingSessions...) lên MongoDB Server
                var syncTasksCol = _liteDb.GetSyncTasks();
                var pendingTasks = syncTasksCol.Find(t => !t.IsSynced).OrderBy(t => t.CreatedAt).Take(20).ToList();

                if (pendingTasks.Count > 0)
                {
                    int syncedRecords = 0;
                    var mongoSessionCol = _mongoDb.GetCollection<ParkingSession>();

                    foreach (var task in pendingTasks)
                    {
                        if (token.IsCancellationRequested) break;

                        try
                        {
                            if (task.EntityType == nameof(ParkingSession) && !string.IsNullOrEmpty(task.PayloadJson))
                            {
                                var session = JsonSerializer.Deserialize<ParkingSession>(task.PayloadJson);
                                if (session != null)
                                {
                                    // Sử dụng ReplaceOne với Upsert = true (Idempotent: an toàn tuyệt đối khi thử lại)
                                    var filter = Builders<ParkingSession>.Filter.Eq(s => s.Id, session.Id);
                                    await mongoSessionCol.ReplaceOneAsync(filter, session, new ReplaceOptions { IsUpsert = true }, token).ConfigureAwait(false);

                                    task.IsSynced = true;
                                    task.SyncedAt = DateTime.Now;
                                    syncTasksCol.Update(task);

                                    // Xóa khỏi LiteDB local session để tiết kiệm dung lượng
                                    _liteDb.GetCollection<ParkingSession>("offline_sessions").Delete(session.Id);
                                    syncedRecords++;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            task.RetryCount++;
                            task.LastError = ex.Message;
                            syncTasksCol.Update(task);

                            // Nếu lỗi mạng khi sync, đánh dấu server offline và dừng đợt sync này
                            _healthTracker.MarkOffline($"Lỗi khi đẩy bản ghi {task.RecordId} lên MongoDB: {ex.Message}");
                            break;
                        }
                    }

                    if (syncedRecords > 0)
                    {
                        AppLogger.Information($"[SYNC] Đã đồng bộ thành công {syncedRecords} lượt xe lên máy chủ MongoDB.", "BackgroundSync");
                    }
                }
            }
            finally
            {
                _isSyncing = false;
                NotifyStatus();
            }
        }

        private void OnServerStatusChanged(object? sender, bool isOnline)
        {
            NotifyStatus();
        }

        private void NotifyStatus()
        {
            try
            {
                int pendingRecords = _liteDb.GetSyncTasks().Count(t => !t.IsSynced);
                int pendingImages = _liteDb.GetImageSyncTasks().Count(t => !t.IsSynced);
                bool isOnline = _healthTracker.IsServerOnline;

                string msg;
                if (isOnline)
                {
                    msg = pendingRecords > 0
                        ? $"🔵 Đang đồng bộ... (còn {pendingRecords} lượt xe, {pendingImages} ảnh)"
                        : "🟢 Máy chủ: Trực tuyến (MongoDB)";
                }
                else
                {
                    msg = pendingRecords > 0
                        ? $"🟠 Ngoại tuyến (LiteDB): {pendingRecords} lượt xe chờ đồng bộ"
                        : "🟠 Ngoại tuyến (LiteDB): Đang lưu cục bộ";
                }

                SyncStatusChanged?.Invoke(this, new SyncStatusEventArgs
                {
                    IsServerOnline = isOnline,
                    PendingRecordCount = pendingRecords,
                    PendingImageCount = pendingImages,
                    StatusMessage = msg
                });
            }
            catch { }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _healthTracker.ServerStatusChanged -= OnServerStatusChanged;
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
