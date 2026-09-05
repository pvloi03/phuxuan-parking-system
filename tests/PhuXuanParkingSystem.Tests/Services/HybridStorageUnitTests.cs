using FluentAssertions;
using PhuXuanParkingSystem.Models.Common;
using PhuXuanParkingSystem.Models.Data;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using PhuXuanParkingSystem.Services.Offline;
using PhuXuanParkingSystem.Services.Storage;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace PhuXuanParkingSystem.Tests.Services
{
    public class MockServerHealthTracker : IServerHealthTracker
    {
        public bool IsServerOnline { get; set; } = false;
        public int ConsecutiveSuccesses { get; set; } = 0;
        public int ConsecutiveFailures { get; set; } = 0;

        public event EventHandler<bool>? ServerStatusChanged;

        public void MarkOffline(string? reason = null)
        {
            IsServerOnline = false;
            ServerStatusChanged?.Invoke(this, false);
        }

        public void MarkOnline()
        {
            IsServerOnline = true;
            ServerStatusChanged?.Invoke(this, true);
        }

        public Task<bool> CheckHealthAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            return Task.FromResult(IsServerOnline);
        }
    }

    public class HybridStorageUnitTests : IDisposable
    {
        private readonly string _testTempDir;
        private readonly LiteDbContext _liteDb;

        public HybridStorageUnitTests()
        {
            _testTempDir = Path.Combine(Path.GetTempPath(), "HybridStorageTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testTempDir);

            string testDbPath = Path.Combine(_testTempDir, "test_offline.db");
            _liteDb = new LiteDbContext(testDbPath);
        }

        public void Dispose()
        {
            _liteDb.Dispose();
            try
            {
                if (Directory.Exists(_testTempDir))
                {
                    Directory.Delete(_testTempDir, true);
                }
            }
            catch { }
        }

        [Fact]
        public void BaseEntity_ShouldGenerateValidObjectIdAtClient()
        {
            // Act
            var session = new ParkingSession();

            // Assert
            session.Id.Should().NotBeNullOrWhiteSpace();
            session.Id.Length.Should().Be(24);
            MongoDB.Bson.ObjectId.TryParse(session.Id, out _).Should().BeTrue();
        }

        [Fact]
        public void ServerHealthTracker_MarkOfflineAndMarkOnline_ShouldToggleStateAndFireEvent()
        {
            // Arrange
            var tracker = new MockServerHealthTracker { IsServerOnline = true };
            bool eventFired = false;
            bool eventNewState = true;

            tracker.ServerStatusChanged += (s, isOnline) =>
            {
                eventFired = true;
                eventNewState = isOnline;
            };

            // Act: Mark Offline
            tracker.MarkOffline("Test connection dropped");

            // Assert
            tracker.IsServerOnline.Should().BeFalse();
            eventFired.Should().BeTrue();
            eventNewState.Should().BeFalse();

            // Act: Mark Online
            eventFired = false;
            tracker.MarkOnline();

            // Assert
            tracker.IsServerOnline.Should().BeTrue();
            eventFired.Should().BeTrue();
            eventNewState.Should().BeTrue();
        }

        [Fact]
        public async Task HybridRepository_WhenServerOffline_ShouldSaveAndRetrieveFromLiteDbWithoutTimeout()
        {
            // Arrange
            var tracker = new MockServerHealthTracker { IsServerOnline = false };
            var repo = new HybridParkingSessionRepository(null, _liteDb, tracker);

            var session = ParkingSession.CheckIn(
                inLaneName: "Làn Vào 1",
                plateNumber: "75A-123.45",
                inOverviewImagePath: "test_ovw.jpg",
                inPlateImagePath: "test_plate.jpg",
                personName: "Nguyễn Văn A"
            );

            // Act: Check-in khi Server Offline
            await repo.CheckInAsync(session);

            // Assert 1: Phiên được lưu vào LiteDB
            var found = await repo.GetActiveSessionByPlateAsync("75A12345");
            found.Should().NotBeNull();
            found!.Id.Should().Be(session.Id);
            found.PlateNumber.Should().Be("75A12345");
            found.Status.Should().Be(ParkingSessionStatus.Active);

            // Assert 2: Hàng đợi đồng bộ có 1 task Insert
            var pendingCount = await repo.GetPendingSyncCountAsync();
            pendingCount.Should().Be(1);

            // Act: Check-out giải quyết xung đột (lượt vào chưa sync mà đã ra)
            found.CheckOut(
                outLaneName: "Làn Ra 1",
                outOverviewImagePath: "out_ovw.jpg",
                outPlateImagePath: "out_plate.jpg"
            );
            await repo.CheckOutAsync(found);

            // Assert 3: Trạng thái trong LiteDB đã được cập nhật thành Completed
            var localCol = _liteDb.GetCollection<ParkingSession>("offline_sessions");
            var updatedLocal = localCol.FindById(session.Id);
            updatedLocal.Should().NotBeNull();
            updatedLocal.Status.Should().Be(ParkingSessionStatus.Completed);
            updatedLocal.OutLaneName.Should().Be("Làn Ra 1");

            // Assert 4: Vẫn chỉ có 1 task Insert với payload đầy đủ In + Out
            var syncTasks = _liteDb.GetSyncTasks().Find(t => t.RecordId == session.Id);
            syncTasks.Should().ContainSingle();
        }

        [Fact]
        public async Task ImageStorageService_WhenOffline_ShouldSaveToOfflineFolderAndQueueTask()
        {
            // Arrange
            string primaryPath = Path.Combine(_testTempDir, "PrimaryServerShare");
            string offlinePath = Path.Combine(_testTempDir, "OfflineCaptures");

            var storage = new ImageStorageService(_liteDb, primaryPath, offlinePath);
            byte[] fakeImageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x01, 0x02 }; // Fake JPEG header

            // Act: Lưu ảnh khi Server Offline
            var result = await storage.SaveImageBytesAsync(
                fakeImageBytes,
                subFolder: "2026-09-04",
                fileName: "test_plate.jpg",
                isServerOnline: false
            );

            // Assert
            result.Success.Should().BeTrue();
            result.IsSavedLocally.Should().BeTrue();
            File.Exists(result.FinalPath).Should().BeTrue();
            result.FinalPath.Should().StartWith(offlinePath);

            // Kiểm tra hàng đợi đồng bộ ảnh
            var imageTasks = _liteDb.GetImageSyncTasks().Find(t => !t.IsSynced);
            imageTasks.Should().ContainSingle();
        }
    }
}
