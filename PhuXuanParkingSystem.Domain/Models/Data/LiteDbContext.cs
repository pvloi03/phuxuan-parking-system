using LiteDB;
using System;
using System.IO;

namespace PhuXuanParkingSystem.Models.Data
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

        private readonly LiteDatabase _database;
        private readonly string _dbPath;
        private bool _disposed = false;

        public LiteDbContext() : this(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "offline_parking.db"))
        {
        }

        public LiteDbContext(string dbPath)
        {
            _dbPath = dbPath;
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
                Connection = ConnectionType.Shared
            };

            _database = new LiteDatabase(connectionString, mapper);
        }

        public ILiteDatabase Database => _database;

        public ILiteCollection<T> GetCollection<T>(string? name = null)
        {
            return string.IsNullOrEmpty(name)
                ? _database.GetCollection<T>()
                : _database.GetCollection<T>(name);
        }

        public ILiteCollection<OfflineSyncTask> GetSyncTasks()
        {
            var col = _database.GetCollection<OfflineSyncTask>("offline_sync_tasks");
            col.EnsureIndex(x => x.IsSynced);
            col.EnsureIndex(x => x.RecordId);
            return col;
        }

        public ILiteCollection<OfflineImageSyncTask> GetImageSyncTasks()
        {
            var col = _database.GetCollection<OfflineImageSyncTask>("offline_image_sync_tasks");
            col.EnsureIndex(x => x.IsSynced);
            return col;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _database?.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }
}
