using FluentAssertions;
using PhuXuanParkingSystem.Api.Services;
using Xunit;

namespace PhuXuanParkingSystem.Api.Tests
{
    public class CapturesCleanupBackgroundWorkerTests : IDisposable
    {
        private readonly string _testCapturesDir;

        public CapturesCleanupBackgroundWorkerTests()
        {
            _testCapturesDir = Path.Combine(Path.GetTempPath(), "PhuXuan_Test_Captures_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testCapturesDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testCapturesDir))
                {
                    Directory.Delete(_testCapturesDir, recursive: true);
                }
            }
            catch { }
        }

        [Fact]
        public void CleanupOldCaptures_WhenFoldersOlderThanRetentionDays_ShouldDeleteOldAndKeepRecent()
        {
            // Arrange: Tạo thư mục cũ (2 năm trước) và thư mục mới (hôm nay)
            var oldFolder = Path.Combine(_testCapturesDir, "2024-01-01");
            var recentFolder = Path.Combine(_testCapturesDir, DateTime.Today.ToString("yyyy-MM-dd"));

            Directory.CreateDirectory(oldFolder);
            Directory.CreateDirectory(recentFolder);

            File.WriteAllText(Path.Combine(oldFolder, "plate_old.jpg"), "dummy image content");
            File.WriteAllText(Path.Combine(recentFolder, "plate_recent.jpg"), "dummy image content");

            // Act: Dọn dẹp ảnh cũ hơn 365 ngày
            var (deletedFolders, deletedFiles) = CapturesCleanupBackgroundWorker.CleanupOldCaptures(_testCapturesDir, retentionDays: 365);

            // Assert:
            deletedFolders.Should().Be(1);
            Directory.Exists(oldFolder).Should().BeFalse("Thư mục ảnh 2024-01-01 phải bị xóa vì quá hạn 365 ngày");
            Directory.Exists(recentFolder).Should().BeTrue("Thư mục ảnh hôm nay phải được giữ lại");
            File.Exists(Path.Combine(recentFolder, "plate_recent.jpg")).Should().BeTrue();
        }

        [Fact]
        public void CleanupOldCaptures_WhenDirectoryDoesNotExist_ShouldReturnZeroWithoutError()
        {
            var nonExistentDir = Path.Combine(Path.GetTempPath(), "non_existent_" + Guid.NewGuid().ToString("N"));
            var (deletedFolders, deletedFiles) = CapturesCleanupBackgroundWorker.CleanupOldCaptures(nonExistentDir, retentionDays: 30);

            deletedFolders.Should().Be(0);
            deletedFiles.Should().Be(0);
        }

        [Fact]
        public void CleanupOldCaptures_WhenRetentionDaysInvalid_ShouldReturnZeroWithoutDeleting()
        {
            var folder = Path.Combine(_testCapturesDir, "2024-01-01");
            Directory.CreateDirectory(folder);

            var (deletedFolders, deletedFiles) = CapturesCleanupBackgroundWorker.CleanupOldCaptures(_testCapturesDir, retentionDays: 0);

            deletedFolders.Should().Be(0);
            Directory.Exists(folder).Should().BeTrue();
        }
    }
}
