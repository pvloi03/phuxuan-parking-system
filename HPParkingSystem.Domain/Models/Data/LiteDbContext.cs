using HPParkingSystem.Models.Entities;
using HPParkingSystem.Models.Enums;
using LiteDB;
using System;
using System.IO;
using System.Linq;

namespace HPParkingSystem.Models.Data
{
    /// <summary>
    /// Bản ghi tác vụ đồng bộ dữ liệu ngoại tuyến cần đẩy lên MongoDB Server
    /// </summary>
    public class OfflineSyncTask
    {
        public int Id { get; set; }
        public string RecordId { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string Operation { get; set; } = "Insert"; // "Insert" hoặc "Update"
        public string PayloadJson { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsSynced { get; set; } = false;
        public DateTime? SyncedAt { get; set; }
        public int RetryCount { get; set; } = 0;
        public string? LastError { get; set; }
    }

    /// <summary>
    /// Bản ghi tác vụ đồng bộ file ảnh từ máy bốt lên máy chủ (Network Share)
    /// </summary>
    public class OfflineImageSyncTask
    {
        public int Id { get; set; }
        public string LocalFilePath { get; set; } = string.Empty;
        public string RemoteRelativePath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsSynced { get; set; } = false;
        public DateTime? SyncedAt { get; set; }
        public int RetryCount { get; set; } = 0;
        public string? LastError { get; set; }
    }

    /// <summary>
    /// Database Context quản lý CSDL nhúng LiteDB phục vụ lưu trữ cục bộ khi mất mạng
    /// </summary>
    public class LiteDbContext : IDisposable
    {
        private static readonly Lazy<LiteDbContext> _instance = new(() => new LiteDbContext());
        public static LiteDbContext Instance => _instance.Value;

        private readonly object _dbLock = new object();
        private LiteDatabase? _database;
        private readonly string _dbPath;
        private bool _disposed = false;

        public LiteDbContext() : this(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "offline_parking.db"))
        {
        }

        public LiteDbContext(string dbPath)
        {
            _dbPath = dbPath;
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            lock (_dbLock)
            {
                var directory = Path.GetDirectoryName(_dbPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Cấu hình BsonMapper của LiteDB để nhận dạng Id làm khóa chính
                var mapper = new BsonMapper();
                mapper.EmptyStringToNull = false;

                var connectionString = new ConnectionString
                {
                    Filename = _dbPath,
                    Connection = ConnectionType.Direct
                };

                _database = new LiteDatabase(connectionString, mapper);
                EnsureIndexes();
                MigrateOfflineSessionsToUnmatchedOut();
            }
        }

        private void EnsureIndexes()
        {
            try
            {
                if (_database == null) return;
                var syncCol = _database.GetCollection<OfflineSyncTask>("offline_sync_tasks");
                syncCol.EnsureIndex(x => x.IsSynced);
                syncCol.EnsureIndex(x => x.RecordId);

                var imgCol = _database.GetCollection<OfflineImageSyncTask>("offline_image_sync_tasks");
                imgCol.EnsureIndex(x => x.IsSynced);
            }
            catch
            {
                // Không chặn khởi tạo nếu kiểm tra chỉ mục gặp lỗi ngoại cảnh
            }
        }

        private void EnsureOpen()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(LiteDbContext), "Không thể truy cập cơ sở dữ liệu LiteDB sau khi Context đã bị Dispose.");
            }

            lock (_dbLock)
            {
                if (_database != null)
                {
                    try
                    {
                        // Kiểm tra tính sẵn sàng của engine thông qua thao tác siêu nhẹ
                        _ = _database.GetCollectionNames();
                    }
                    catch (Exception ex) when (IsEngineDisposedException(ex))
                    {
                        // Engine bên dưới đã bị hủy đột ngột -> Giải phóng reference để kích hoạt khởi tạo lại
                        try { _database.Dispose(); } catch { }
                        _database = null;
                    }
                }

                if (_database == null)
                {
                    InitializeDatabase();
                }
            }
        }

        /// <summary>
        /// Tái tạo và thiết lập lại kết nối LiteDB một cách an toàn đa luồng.
        /// </summary>
        public void ResetDatabase()
        {
            lock (_dbLock)
            {
                try
                {
                    _database?.Dispose();
                }
                catch { }
                _database = null;
                if (!_disposed)
                {
                    InitializeDatabase();
                }
            }
        }

        /// <summary>
        /// Đối tượng LiteDatabase gốc đã được đảm bảo mở và tự phục hồi khi có sự cố.
        /// </summary>
        public ILiteDatabase Database
        {
            get
            {
                EnsureOpen();
                return _database!;
            }
        }

        /// <summary>
        /// Lấy collection theo entity type và tên collection tương ứng.
        /// </summary>
        public ILiteCollection<T> GetCollection<T>(string? name = null)
        {
            EnsureOpen();
            try
            {
                return ResolveCollection<T>(name);
            }
            catch (Exception ex) when (IsEngineDisposedException(ex))
            {
                ResetDatabase();
                return ResolveCollection<T>(name);
            }
        }

        private ILiteCollection<T> ResolveCollection<T>(string? name)
        {
            return string.IsNullOrEmpty(name)
                ? _database!.GetCollection<T>()
                : _database!.GetCollection<T>(name);
        }

        /// <summary>
        /// Lấy collection chứa các tác vụ đồng bộ bản ghi ngoại tuyến.
        /// </summary>
        public ILiteCollection<OfflineSyncTask> GetSyncTasks()
        {
            return GetCollection<OfflineSyncTask>("offline_sync_tasks");
        }

        /// <summary>
        /// Lấy collection chứa các tác vụ đồng bộ hình ảnh ngoại tuyến.
        /// </summary>
        public ILiteCollection<OfflineImageSyncTask> GetImageSyncTasks()
        {
            return GetCollection<OfflineImageSyncTask>("offline_image_sync_tasks");
        }

        private static bool IsEngineDisposedException(Exception ex)
        {
            return ex is ObjectDisposedException ||
                   (ex is LiteException && ex.Message.IndexOf("disposed", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// Giải phóng toàn bộ tài nguyên của LiteDB context.
        /// </summary>
        /// <summary>
        /// Tự động chuyển đổi các bản ghi xe ra ngoại tuyến cũ (InTime == null && OutTime != null)
        /// đang mang trạng thái Completed sang UnmatchedOut ("Xe ra") trong LiteDB.
        /// Đồng thời cập nhật các task đồng bộ offline đang chờ sync lên MongoDB.
        /// </summary>
        public int MigrateOfflineSessionsToUnmatchedOut()
        {
            EnsureOpen();
            int updatedCount = 0;
            lock (_dbLock)
            {
                try
                {
                    if (_database == null) return 0;

                    // 1. Cập nhật collection offline_sessions trong LiteDB
                    var sessionCol = _database.GetCollection<ParkingSession>("offline_sessions");
                    var oldSessions = sessionCol.Find(s => s.InTime == null && s.OutTime != null && s.Status == ParkingSessionStatus.Completed).ToList();
                    foreach (var s in oldSessions)
                    {
                        s.Status = ParkingSessionStatus.UnmatchedOut;
                        sessionCol.Update(s);
                        updatedCount++;
                    }

                    // 2. Cập nhật các tác vụ đồng bộ offline đang chờ sync nếu payload chứa phiên cũ
                    var syncTasksCol = GetSyncTasks();
                    var pendingTasks = syncTasksCol.Find(t => !t.IsSynced && t.EntityType == nameof(ParkingSession)).ToList();
                    foreach (var task in pendingTasks)
                    {
                        if (string.IsNullOrEmpty(task.PayloadJson)) continue;
                        try
                        {
                            var session = System.Text.Json.JsonSerializer.Deserialize<ParkingSession>(task.PayloadJson);
                            if (session != null && session.InTime == null && session.OutTime != null && session.Status == ParkingSessionStatus.Completed)
                            {
                                session.Status = ParkingSessionStatus.UnmatchedOut;
                                task.PayloadJson = System.Text.Json.JsonSerializer.Serialize(session);
                                syncTasksCol.Update(task);
                            }
                        }
                        catch { }
                    }
                }
                catch
                {
                    // Tránh gây crash ứng dụng nếu migration gặp ngoại lệ
                }
            }
            return updatedCount;
        }
        public void Dispose()
        {
            lock (_dbLock)
            {
                if (!_disposed)
                {
                    try
                    {
                        _database?.Dispose();
                    }
                    catch { }
                    _database = null;
                    _disposed = true;
                }
            }
            GC.SuppressFinalize(this);
        }
    }
}
