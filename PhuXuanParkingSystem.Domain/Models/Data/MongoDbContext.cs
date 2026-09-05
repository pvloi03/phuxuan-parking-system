using Humanizer;
using MongoDB.Driver;
using System;
using System.Configuration;

namespace PhuXuanParkingSystem.Models.Data
{
    // Database Context quản lý kết nối MongoDB và các Collection đối tượng trong hệ thống
    public class MongoDbContext
    {
        private static readonly Lazy<MongoDbContext> _instance = new(() => new MongoDbContext());
        public static MongoDbContext Instance => _instance.Value;

        private readonly IMongoDatabase _database;
        private readonly MongoClient _client;

        public IMongoClient Client => _client;
        public IMongoDatabase Database => _database;

        private readonly string _serverHost = "127.0.0.1";
        private readonly int _serverPort = 27017;

        public string ServerHost => _serverHost;
        public int ServerPort => _serverPort;

        // Tự động lấy Collection theo tên Type được số nhiều hóa (Pluralize) qua Humanizer
        public IMongoCollection<T> GetCollection<T>() => _database.GetCollection<T>(typeof(T).Name.Pluralize());

        public MongoDbContext() : this(
            ConfigurationManager.AppSettings?["MongoDb_ConnectionString"] is string connStr && !string.IsNullOrWhiteSpace(connStr)
                ? connStr
                : throw new InvalidOperationException("Chưa cấu hình chuỗi kết nối 'MongoDb_ConnectionString' trong file cấu hình."),
            ConfigurationManager.AppSettings?["MongoDb_DatabaseName"] is string dbName && !string.IsNullOrWhiteSpace(dbName)
                ? dbName
                : "PhuXuanParkingSystemDb")
        {
        }

        public MongoDbContext(string connectionString, string databaseName)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Chuỗi kết nối MongoDB không được để trống.", nameof(connectionString));
            }

            var settings = MongoClientSettings.FromConnectionString(connectionString);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(3);

            if (settings.Server != null)
            {
                _serverHost = settings.Server.Host;
                _serverPort = settings.Server.Port;
            }

            _client = new MongoClient(settings);
            _database = _client.GetDatabase(string.IsNullOrWhiteSpace(databaseName) ? "PhuXuanParkingSystemDb" : databaseName);
        }

        /// <summary>
        /// Kiểm tra nhanh trạng thái sẵn sàng của dịch vụ MongoDB bằng lệnh ping
        /// </summary>
        public async System.Threading.Tasks.Task<bool> PingAsync(int timeoutMs = 1500, System.Threading.CancellationToken cancellationToken = default)
        {
            try
            {
                using var cts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeoutMs);

                var pingCommand = new MongoDB.Bson.BsonDocument("ping", 1);
                await _database.RunCommandAsync<MongoDB.Bson.BsonDocument>(pingCommand, cancellationToken: cts.Token);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
