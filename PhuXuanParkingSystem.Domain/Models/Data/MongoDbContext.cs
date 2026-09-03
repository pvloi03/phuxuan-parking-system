using PhuXuanParkingSystem.Models.Entities;
using MongoDB.Driver;
using System;
using System.Configuration;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Models.Data
{
    /// <summary>
    /// Database Context quản lý kết nối MongoDB và các Collection đối tượng trong hệ thống
    /// </summary>
    public class MongoDbContext
    {
        private static readonly Lazy<MongoDbContext> _instance = new(() => new MongoDbContext());
        public static MongoDbContext Instance => _instance.Value;

        private readonly IMongoDatabase _database;
        private readonly MongoClient _client;

        public IMongoClient Client => _client;
        public IMongoDatabase Database => _database;

        // --- CÁC COLLECTIONS QUẢN LÝ ---
        public IMongoCollection<ParkingSession> ParkingSessions => _database.GetCollection<ParkingSession>("ParkingSessions");
        public IMongoCollection<Vehicle> Vehicles => _database.GetCollection<Vehicle>("Vehicles");
        public IMongoCollection<Person> Persons => _database.GetCollection<Person>("People");
        public IMongoCollection<Department> Departments => _database.GetCollection<Department>("Departments");
        public IMongoCollection<Company> Companies => _database.GetCollection<Company>("Companies");
        public IMongoCollection<Contractor> Contractors => _database.GetCollection<Contractor>("Contractors");
        public IMongoCollection<Lane> Lanes => _database.GetCollection<Lane>("Lanes");
        public IMongoCollection<Device> Devices => _database.GetCollection<Device>("Devices");
        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");

        public MongoDbContext() : this(
            Environment.GetEnvironmentVariable("MongoDb_ConnectionString")
                ?? ConfigurationManager.AppSettings?["MongoDb_ConnectionString"]
                ?? "mongodb://localhost:27017",
            Environment.GetEnvironmentVariable("MongoDb_DatabaseName")
                ?? ConfigurationManager.AppSettings?["MongoDb_DatabaseName"]
                ?? "PhuXuanParkingSystemDb")
        {
        }

        public MongoDbContext(string connectionString, string databaseName)
        {
            var settings = MongoClientSettings.FromConnectionString(string.IsNullOrWhiteSpace(connectionString) ? "mongodb://localhost:27017" : connectionString);
            settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);

            _client = new MongoClient(settings);
            _database = _client.GetDatabase(string.IsNullOrWhiteSpace(databaseName) ? "PhuXuanParkingSystemDb" : databaseName);

            // Tự động khởi tạo Index hỗ trợ truy vấn siêu nhanh
            _ = CreateIndexesAsync();
        }

        /// <summary>
        /// Tạo các Index tìm kiếm tối ưu cho bãi đỗ xe
        /// </summary>
        private async Task CreateIndexesAsync()
        {
            try
            {
                // Index cho ParkingSessions: Biển số, Trạng thái, Thời gian vào
                var sessionIndexKeys = Builders<ParkingSession>.IndexKeys
                    .Ascending(s => s.PlateNumber)
                    .Ascending(s => s.Status)
                    .Descending(s => s.InTime);
                await ParkingSessions.Indexes.CreateOneAsync(new CreateIndexModel<ParkingSession>(sessionIndexKeys));

                // Index cho Vehicles: Biển số duy nhất
                var vehicleIndexKeys = Builders<Vehicle>.IndexKeys.Ascending(v => v.PlateNumber);
                await Vehicles.Indexes.CreateOneAsync(new CreateIndexModel<Vehicle>(vehicleIndexKeys));

                // Index cho Persons: Mã nhân viên
                var personIndexKeys = Builders<Person>.IndexKeys.Ascending(p => p.Code);
                await Persons.Indexes.CreateOneAsync(new CreateIndexModel<Person>(personIndexKeys));
            }
            catch
            {
                // Bỏ qua nếu Index đã tồn tại hoặc chưa bật MongoDB service
            }
        }
    }
}
