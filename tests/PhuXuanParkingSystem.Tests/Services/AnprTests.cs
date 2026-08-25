using FluentAssertions;
using Moq;
using PhuXuanParkingSystem.Services.Anpr;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace PhuXuanParkingSystem.Tests.Services
{
    public class AnprTests
    {
        [Fact]
        public void PlateRecognitionResult_Success_ShouldComputeFormattedAndCleanPlate()
        {
            // Arrange
            string raw = " 29A-123.45 ";
            float conf = 0.95f;
            var bbox = new Rectangle(10, 20, 100, 50);

            // Act
            var result = PlateRecognitionResult.Success(raw, conf, bbox, null, 45);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.CleanPlate.Should().Be("29A12345");
            result.FormattedPlate.Should().Be("29A-123.45");
            result.Confidence.Should().Be(0.95f);
            result.BoundingBox.Should().Be(bbox);
            result.DurationMs.Should().Be(45);
            result.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void PlateRecognitionResult_Failed_ShouldSetIsSuccessFalseAndStoreError()
        {
            // Act
            var result = PlateRecognitionResult.Failed("Không tìm thấy biển số", 30);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Không tìm thấy biển số");
            result.DurationMs.Should().Be(30);
            result.CleanPlate.Should().BeEmpty();
            result.FormattedPlate.Should().BeEmpty();
        }

        [Fact]
        public async Task MockAnprService_RecognizeAsync_ShouldReturnExpectedResult()
        {
            // Arrange
            var mockService = new Mock<IPlateRecognitionService>();
            var expectedResult = PlateRecognitionResult.Success("30F-999.88", 0.98f, new Rectangle(0, 0, 50, 20), null, 35);

            mockService
                .Setup(s => s.RecognizeAsync(It.IsAny<string>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await mockService.Object.RecognizeAsync("test_image.jpg");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.FormattedPlate.Should().Be("30F-999.88");
            result.CleanPlate.Should().Be("30F99988");
            result.Confidence.Should().Be(0.98f);
        }

        [Fact]
        public void SimpleLprAnprService_Recognize_WithNonExistentFile_ShouldReturnFailed()
        {
            // Arrange
            using var service = new SimpleLprAnprService();

            // Act
            var result = service.Recognize("non_existent_file_path_12345.jpg");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("không tồn tại");
        }

        [Fact]
        public async Task SimpleLprAnprService_RecognizeAsync_WithNonExistentFile_ShouldReturnFailed()
        {
            // Arrange
            using var service = new SimpleLprAnprService();

            // Act
            var result = await service.RecognizeAsync("non_existent_file_path_67890.jpg");

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("không tồn tại");
        }

        [Fact]
        public void SimpleLprAnprService_DownscalingConfiguration_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            using var service = new SimpleLprAnprService();

            // Assert
            service.EnableImageDownscaling.Should().BeTrue();
            service.MaxAnprWidth.Should().Be(1280);
        }

        [Fact]
        public void SimpleLprAnprService_Recognize_WithLargeBitmap_ShouldDownscaleWithoutCrashing()
        {
            // Arrange
            using var service = new SimpleLprAnprService();
            using var largeBmp = new Bitmap(1920, 1080);

            // Act
            var result = service.Recognize(largeBmp);

            // Assert
            result.Should().NotBeNull();
            result.DurationMs.Should().BeGreaterThanOrEqualTo(0);
        }
    }
}
