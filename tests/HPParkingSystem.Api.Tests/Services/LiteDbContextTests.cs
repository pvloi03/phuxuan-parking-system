using FluentAssertions;
using HPParkingSystem.Models.Data;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace HPParkingSystem.Api.Tests.Services
{
    public class LiteDbContextTests : IDisposable
    {
        private readonly string _testDbPath;
        private readonly LiteDbContext _context;

        public LiteDbContextTests()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), "HPParkingTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            _testDbPath = Path.Combine(tempDir, "test_parking.db");
            _context = new LiteDbContext(_testDbPath);
        }

        public void Dispose()
        {
            try
            {
                _context.Dispose();
                string dir = Path.GetDirectoryName(_testDbPath)!;
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
            catch { }
        }

        [Fact]
        public void LiteDbContext_ShouldInitializeAndProvideCollectionsWithIndexes()
        {
            // Act
            var syncTasks = _context.GetSyncTasks();
            var imgTasks = _context.GetImageSyncTasks();

            // Assert
            syncTasks.Should().NotBeNull();
            imgTasks.Should().NotBeNull();

            // Insert a task to verify write works
            var task = new OfflineSyncTask
            {
                RecordId = "rec_001",
                EntityType = "ParkingSession",
                Operation = "Insert",
                IsSynced = false
            };
            syncTasks.Insert(task);

            var retrieved = syncTasks.FindOne(t => t.RecordId == "rec_001");
            retrieved.Should().NotBeNull();
            retrieved.RecordId.Should().Be("rec_001");
        }

        [Fact]
        public async Task LiteDbContext_MaterializedQueryAcrossAsyncAwait_ShouldNotThrow()
        {
            // Arrange: Thêm các task đồng bộ ảnh
            var imgTasks = _context.GetImageSyncTasks();
            for (int i = 0; i < 5; i++)
            {
                imgTasks.Insert(new OfflineImageSyncTask
                {
                    LocalFilePath = $"C:\\temp\\img_{i}.jpg",
                    RemoteRelativePath = $"2026/10/img_{i}.jpg",
                    IsSynced = false
                });
            }

            // Act: Sử dụng .ToList() để nạp trước vào RAM trước khi await Task.Delay (mô phỏng thread hop trong I/O)
            var pendingTasks = imgTasks.Find(t => !t.IsSynced).ToList();
            pendingTasks.Should().HaveCount(5);

            foreach (var task in pendingTasks)
            {
                // Mô phỏng Thread Hop qua I/O async
                await Task.Delay(10);

                task.IsSynced = true;
                task.SyncedAt = DateTime.Now;
                imgTasks.Update(task);
            }

            // Assert: Toàn bộ task đã được update thành công mà không gây lỗi khóa luồng
            var remaining = imgTasks.Find(t => !t.IsSynced).ToList();
            remaining.Should().BeEmpty();
        }

        [Fact]
        public async Task LiteDbContext_ConcurrentAccessAcrossMultipleThreads_ShouldBeThreadSafe()
        {
            // Arrange
            var syncTasks = _context.GetSyncTasks();
            const int taskCount = 20;

            // Act: 20 tác vụ chạy đồng thời trên nhiều luồng khác nhau từ ThreadPool
            var tasks = Enumerable.Range(0, taskCount).Select(i => Task.Run(async () =>
            {
                string expectedId = $"thread_task_{i}";
                var item = new OfflineSyncTask
                {
                    RecordId = expectedId,
                    EntityType = "ParkingSession",
                    Operation = "Insert",
                    IsSynced = false
                };
                syncTasks.Insert(item);

                await Task.Delay(5);

                var found = syncTasks.FindOne(t => t.RecordId == expectedId);
                found.Should().NotBeNull();
            }));

            await Task.WhenAll(tasks);

            // Assert
            var allItems = syncTasks.FindAll().ToList();
            allItems.Count.Should().BeGreaterOrEqualTo(taskCount);
        }

        [Fact]
        public void LiteDbContext_WhenDatabaseIsDisposed_ShouldSelfHealAndReturnValidCollection()
        {
            // Arrange
            var syncTasks = _context.GetSyncTasks();
            syncTasks.Insert(new OfflineSyncTask { RecordId = "heal_001", IsSynced = false });

            // Act: Cưỡng bức làm hỏng/hủy kết nối bên trong
            _context.ResetDatabase();

            // Assert: Khi gọi lại GetSyncTasks, cơ chế Self-Healing phải phục hồi kết nối hợp lệ
            var recoveredCol = _context.GetSyncTasks();
            recoveredCol.Should().NotBeNull();

            var record = recoveredCol.FindOne(t => t.RecordId == "heal_001");
            record.Should().NotBeNull();
            record.RecordId.Should().Be("heal_001");
        }
    }
}
