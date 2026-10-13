using LiteDB;
using System;
using System.IO;

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
            if (_disposed) return;
            lock (_dbLock)
            {
                if (_database == null)
                {
                    InitializeDatabase();
                }
            }
        }

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

        public ILiteDatabase Database
        {
            get
            {
                EnsureOpen();
                return _database!;
            }
        }

        public ILiteCollection<T> GetCollection<T>(string? name = null)
        {
            EnsureOpen();
            try
            {
                return string.IsNullOrEmpty(name)
                    ? _database!.GetCollection<T>()
                    : _database!.GetCollection<T>(name);
            }
            catch (Exception ex) when (IsEngineDisposedException(ex))
            {
                ResetDatabase();
                return string.IsNullOrEmpty(name)
                    ? _database!.GetCollection<T>()
                    : _database!.GetCollection<T>(name);
            }
        }

        public ILiteCollection<OfflineSyncTask> GetSyncTasks()
        {
            return GetCollection<OfflineSyncTask>("offline_sync_tasks");
        }

        public ILiteCollection<OfflineImageSyncTask> GetImageSyncTasks()
        {
            return GetCollection<OfflineImageSyncTask>("offline_image_sync_tasks");
        }

        private static bool IsEngineDisposedException(Exception ex)
        {
            return ex is ObjectDisposedException ||
                   (ex is LiteException && ex.Message.IndexOf("disposed", StringComparison.OrdinalIgnoreCase) >= 0);
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
