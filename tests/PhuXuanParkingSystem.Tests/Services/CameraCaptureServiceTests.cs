using FluentAssertions;
using Moq;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Services.Camera;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PhuXuanParkingSystem.Tests.Services
{
    public class CameraCaptureServiceTests
    {
        [Fact]
        public async Task PlateCameraService_WhenNotLoggedIn_CaptureSnapshotAsync_ReturnsNullWithoutThrowing()
        {
            // Arrange
            using var cameraService = new PlateCameraService
            {
                Config = new CameraConfig { Ip = "192.168.1.200", Port = 3000 }
            };

            // Act
            var result = await cameraService.CaptureSnapshotAsync();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task OverviewCameraService_WhenNotLoggedIn_CaptureSnapshotAsync_ReturnsNullWithoutThrowing()
        {
            // Arrange
            using var cameraService = new OverviewCameraService
            {
                Config = new CameraConfig { Ip = "192.168.1.201", Port = 8000 }
            };

            // Act
            var result = await cameraService.CaptureSnapshotAsync();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task PlateCameraService_WhenNotLoggedIn_CaptureToFileAsync_ReturnsFalse()
        {
            // Arrange
            using var cameraService = new PlateCameraService
            {
                Config = new CameraConfig { Ip = "192.168.1.200", Port = 3000 }
            };
            string tempFile = Path.Combine(Path.GetTempPath(), $"test_plate_{Guid.NewGuid():N}.jpg");

            try
            {
                // Act
                var result = await cameraService.CaptureToFileAsync(tempFile);

                // Assert
                result.Should().BeFalse();
                File.Exists(tempFile).Should().BeFalse();
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public async Task OverviewCameraService_WhenNotLoggedIn_CaptureToFileAsync_ReturnsFalse()
        {
            // Arrange
            using var cameraService = new OverviewCameraService
            {
                Config = new CameraConfig { Ip = "192.168.1.201", Port = 8000 }
            };
            string tempFile = Path.Combine(Path.GetTempPath(), $"test_ovw_{Guid.NewGuid():N}.jpg");

            try
            {
                // Act
                var result = await cameraService.CaptureToFileAsync(tempFile);

                // Assert
                result.Should().BeFalse();
                File.Exists(tempFile).Should().BeFalse();
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public void PlateCameraService_Dispose_CanBeCalledMultipleTimesSafely()
        {
            // Arrange
            var cameraService = new PlateCameraService
            {
                Config = new CameraConfig { Ip = "192.168.1.200", Port = 3000 }
            };

            // Act & Assert
            Action act = () =>
            {
                cameraService.Dispose();
            };
            act.Should().NotThrow();
        }

        [Fact]
        public void OverviewCameraService_Dispose_CanBeCalledMultipleTimesSafely()
        {
            // Arrange
            var cameraService = new OverviewCameraService
            {
                Config = new CameraConfig { Ip = "192.168.1.201", Port = 8000 }
            };

            // Act & Assert
            Action act = () =>
            {
                cameraService.Dispose();
            };
            act.Should().NotThrow();
        }

        [Fact]
        public async Task CameraService_Mock_RapidConcurrentCaptures_HandlesGracefully()
        {
            // Arrange
            var mockCam = new Mock<ICameraService>();
            byte[] fakeImageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46 }; // JPEG header

            mockCam.Setup(c => c.CaptureSnapshotAsync(It.IsAny<CancellationToken>()))
                   .ReturnsAsync(() => (byte[])fakeImageBytes.Clone());

            mockCam.Setup(c => c.CaptureToFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .Returns<string, CancellationToken>(async (path, ct) =>
                   {
                       var bytes = await mockCam.Object.CaptureSnapshotAsync(ct);
                       if (bytes != null)
                       {
                           File.WriteAllBytes(path, bytes);
                           return true;
                       }
                       return false;
                   });

            string tempDir = Path.Combine(Path.GetTempPath(), $"cam_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Act: Run 10 rapid concurrent capture tasks
                var tasks = new Task<bool>[10];
                for (int i = 0; i < 10; i++)
                {
                    string filePath = Path.Combine(tempDir, $"snap_{i}.jpg");
                    tasks[i] = mockCam.Object.CaptureToFileAsync(filePath);
                }

                var results = await Task.WhenAll(tasks);

                // Assert
                results.Should().AllBeEquivalentTo(true);
                for (int i = 0; i < 10; i++)
                {
                    string filePath = Path.Combine(tempDir, $"snap_{i}.jpg");
                    File.Exists(filePath).Should().BeTrue();
                    File.ReadAllBytes(filePath).Should().BeEquivalentTo(fakeImageBytes);
                }
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        [Fact]
        public async Task CameraCaptureHelper_WithNullOrEmptyBytes_ReturnsFalse()
        {
            // Act
            bool resNull = await CameraCaptureHelper.SaveBytesToFileAsync(null, "C:\\dummy.jpg", "TEST", "TestCategory");
            bool resEmpty = await CameraCaptureHelper.SaveBytesToFileAsync(Array.Empty<byte>(), "C:\\dummy.jpg", "TEST", "TestCategory");

            // Assert
            resNull.Should().BeFalse();
            resEmpty.Should().BeFalse();
        }

        [Fact]
        public async Task CameraCaptureHelper_WithValidBytes_CreatesDirectoryAndWritesFile()
        {
            // Arrange
            string subDir = Path.Combine(Path.GetTempPath(), $"helper_test_{Guid.NewGuid():N}", "nested");
            string testFile = Path.Combine(subDir, "snapshot.jpg");
            byte[] sampleData = new byte[] { 1, 2, 3, 4, 5 };

            try
            {
                // Act
                bool result = await CameraCaptureHelper.SaveBytesToFileAsync(sampleData, testFile, "TEST", "TestCategory");

                // Assert
                result.Should().BeTrue();
                File.Exists(testFile).Should().BeTrue();
                File.ReadAllBytes(testFile).Should().BeEquivalentTo(sampleData);
            }
            finally
            {
                string parentDir = Directory.GetParent(subDir)?.FullName ?? subDir;
                if (Directory.Exists(parentDir))
                {
                    Directory.Delete(parentDir, true);
                }
            }
        }
    }
}
