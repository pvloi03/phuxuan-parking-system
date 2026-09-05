using MongoDB.Driver;
using PhuXuanParkingSystem.Models.Data;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Services.Offline;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Repositories
{
    /// <summary>
    /// Triển khai Repository thông minh kết hợp MongoDB Server và LiteDB Ngoại tuyến:
    /// - Không gây nghẽn timeout tại barrier (< 2ms)
    /// - Giải quyết triệt để xung đột phiên vào ngoại tuyến chưa kịp sync khi xe ra
    /// </summary>
    public class HybridParkingSessionRepository : MongoRepository<ParkingSession>, IHybridParkingSessionRepository
    {
        private readonly LiteDbContext _liteDb;
        private readonly IServerHealthTracker _healthTracker;

        public HybridParkingSessionRepository(
            MongoDbContext? mongoContext = null,
            LiteDbContext? liteDb = null,
            IServerHealthTracker? healthTracker = null)
            : base(mongoContext ?? MongoDbContext.Instance)
        {
            _liteDb = liteDb ?? LiteDbContext.Instance;
            _healthTracker = healthTracker ?? ServerHealthTracker.Instance;
        }

        public async Task<ParkingSession?> GetActiveSessionByPlateAsync(string cleanPlateNumber)
        {
            if (string.IsNullOrWhiteSpace(cleanPlateNumber))
            {
                return null;
            }

            ParkingSession? sessionFromMongo = null;

            // 1. Thử truy vấn trên MongoDB nếu máy chủ đang Online
            if (_healthTracker.IsServerOnline)
            {
                try
                {
                    var filter = Builders<ParkingSession>.Filter.Eq(s => s.PlateNumber, cleanPlateNumber) &
                                 Builders<ParkingSession>.Filter.Eq(s => s.Status, ParkingSessionStatus.Active) &
                                 Builders<ParkingSession>.Filter.Eq(s => s.IsDeleted, false);

                    var candidates = await base.FindAsync(filter, Builders<ParkingSession>.Sort.Descending(s => s.InTime)).ConfigureAwait(false);
                    sessionFromMongo = candidates.FirstOrDefault();
                }
                catch (Exception ex)
                {
                    // Đánh dấu server offline khi gặp lỗi kết nối
                    _healthTracker.MarkOffline($"Truy vấn MongoDB thất bại: {ex.Message}");
                }
            }

            // 2. Luôn kiểm tra trong LiteDB cục bộ xem có phiên Active nào chưa được đồng bộ không
            ParkingSession? sessionFromLite = null;
            try
            {
                var localCol = _liteDb.GetCollection<ParkingSession>("offline_sessions");
                sessionFromLite = localCol.Find(s => s.PlateNumber == cleanPlateNumber && s.Status == ParkingSessionStatus.Active && !s.IsDeleted)
                                          .OrderByDescending(s => s.InTime)
                                          .FirstOrDefault();
            }
            catch
            {
                // Bỏ qua lỗi đọc LiteDB cục bộ nếu có
            }

            // 3. Quyết định ưu tiên bản ghi mới nhất
            if (sessionFromMongo != null && sessionFromLite != null)
            {
                // So sánh InTime, ưu tiên bản ghi mới nhất
                return (sessionFromLite.InTime ?? DateTime.MinValue) >= (sessionFromMongo.InTime ?? DateTime.MinValue)
                    ? sessionFromLite
                    : sessionFromMongo;
            }

            return sessionFromLite ?? sessionFromMongo;
        }

        public async Task CheckInAsync(ParkingSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            // Đảm bảo Id luôn là ObjectId hợp lệ
            if (string.IsNullOrEmpty(session.Id))
            {
                session.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
            }

            // 1. Nếu Server Online -> Ghi trực tiếp vào MongoDB
            if (_healthTracker.IsServerOnline)
            {
                try
                {
                    await base.AddAsync(session).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex)
                {
                    // Lỗi mạng bất ngờ -> Chuyển sang Offline ngay và fallback lưu LiteDB
                    _healthTracker.MarkOffline($"Thao tác CheckIn lên MongoDB thất bại: {ex.Message}");
                }
            }

            // 2. Fallback hoặc chế độ Offline -> Lưu tức thì vào LiteDB (< 2ms)
            SaveOfflineSession(session, "Insert");
        }

        public async Task CheckOutAsync(ParkingSession session)
        {
            if (session == null) throw new ArgumentNullException(nameof(session));

            // Kiểm tra nguồn gốc: Bản ghi này có đang nằm chờ sync trong LiteDB hay không?
            var syncTasksCol = _liteDb.GetSyncTasks();
            var localCol = _liteDb.GetCollection<ParkingSession>("offline_sessions");

            var pendingInsertTask = syncTasksCol.FindOne(t => t.RecordId == session.Id && t.Operation == "Insert" && !t.IsSynced);
            var localSession = localCol.FindById(session.Id);

            if (pendingInsertTask != null || localSession != null)
            {
                // ── TÌNH HUỐNG XUNG ĐỘT (Giải quyết Rủi ro 5): ──────────────────────
                // Bản ghi xe vào được tạo lúc offline và CHƯA TỒN TẠI trên MongoDB Server.
                // Cho dù hiện tại Server đã Online hay vẫn Offline:
                // Ta cập nhật thẳng vào LiteDB và cập nhật Payload của pendingInsertTask.
                // Khi tiến trình nền đồng bộ chạy, nó sẽ đẩy 1 bản ghi hoàn chỉnh (có cả In lẫn Out)
                // lên MongoDB mà không sợ lỗi update vào record không tồn tại!
                localCol.Upsert(session);

                if (pendingInsertTask != null)
                {
                    pendingInsertTask.PayloadJson = JsonSerializer.Serialize(session);
                    pendingInsertTask.CreatedAt = DateTime.Now; // Đẩy thời gian cập nhật mới
                    syncTasksCol.Update(pendingInsertTask);
                }
                else
                {
                    // Tạo mới task đồng bộ toàn diện
                    syncTasksCol.Insert(new OfflineSyncTask
                    {
                        RecordId = session.Id,
                        EntityType = nameof(ParkingSession),
                        Operation = "Insert",
                        PayloadJson = JsonSerializer.Serialize(session),
                        CreatedAt = DateTime.Now,
                        IsSynced = false
                    });
                }

                return;
            }

            // ── TÌNH HUỐNG BÌNH THƯỜNG: Bản ghi gốc ĐÃ NẰM TRÊN MONGODB ──────────
            if (_healthTracker.IsServerOnline)
            {
                try
                {
                    await base.UpdateAsync(session).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex)
                {
                    _healthTracker.MarkOffline($"Thao tác CheckOut lên MongoDB thất bại: {ex.Message}");
                }
            }

            // Fallback khi server offline: Lưu bản ghi cập nhật vào LiteDB kèm task Update
            SaveOfflineSession(session, "Update");
        }

        public Task<int> GetPendingSyncCountAsync()
        {
            try
            {
                int count = _liteDb.GetSyncTasks().Count(t => !t.IsSynced);
                return Task.FromResult(count);
            }
            catch
            {
                return Task.FromResult(0);
            }
        }

        private void SaveOfflineSession(ParkingSession session, string operation)
        {
            var localCol = _liteDb.GetCollection<ParkingSession>("offline_sessions");
            localCol.Upsert(session);

            var syncTasksCol = _liteDb.GetSyncTasks();
            syncTasksCol.Insert(new OfflineSyncTask
            {
                RecordId = session.Id,
                EntityType = nameof(ParkingSession),
                Operation = operation,
                PayloadJson = JsonSerializer.Serialize(session),
                CreatedAt = DateTime.Now,
                IsSynced = false
            });
        }
    }
}
