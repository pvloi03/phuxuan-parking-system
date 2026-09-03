using FluentAssertions;
using MongoDB.Driver;
using Moq;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using PhuXuanParkingSystem.Services.Anpr;
using PhuXuanParkingSystem.Services.Devices.Camera;
using PhuXuanParkingSystem.Services.Parking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PhuXuanParkingSystem.Tests.Services
{
    public class ParkingLaneServiceTests : IDisposable
    {
        private readonly string _testTempDir;
        private readonly Mock<IRepository<ParkingSession>> _mockSessionRepo;
        private readonly Mock<IRepository<Vehicle>> _mockVehicleRepo;
        private readonly Mock<IRepository<Person>> _mockPersonRepo;
        private readonly Mock<IRepository<Department>> _mockDeptRepo;
        private readonly Mock<IPlateRecognitionService> _mockAnprService;

        public ParkingLaneServiceTests()
        {
            _testTempDir = Path.Combine(Path.GetTempPath(), "PXParkingTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_testTempDir);

            _mockSessionRepo = new Mock<IRepository<ParkingSession>>();
            _mockVehicleRepo = new Mock<IRepository<Vehicle>>();
            _mockPersonRepo = new Mock<IRepository<Person>>();
            _mockDeptRepo = new Mock<IRepository<Department>>();
            _mockAnprService = new Mock<IPlateRecognitionService>();
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_testTempDir))
                {
                    Directory.Delete(_testTempDir, true);
                }
            }
            catch { }
        }

        private Mock<ICameraService> CreateMockCamera(bool success = true)
        {
            var mock = new Mock<ICameraService>();
            mock.Setup(c => c.CaptureToFileAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>((path, ct) =>
                {
                    if (success)
                    {
                        var dir = Path.GetDirectoryName(path);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                            Directory.CreateDirectory(dir);
                        File.WriteAllText(path, "dummy-image-data");
                        return Task.FromResult(true);
                    }
                    throw new InvalidOperationException("Camera connection failed");
                });
            return mock;
        }

        [Fact]
        public async Task ProcessInLaneAsync_CreatesActiveParkingSession_WithCleanPlateAndImages()
        {
            // Arrange
            var plateCam = CreateMockCamera(true);
            var ovwCam = CreateMockCamera(true);

            _mockAnprService
                .Setup(a => a.RecognizeAsync(It.IsAny<string>()))
                .ReturnsAsync(PlateRecognitionResult.Success("30A-123.45", 0.95f));

            var service = new ParkingLaneService(
                _mockSessionRepo.Object,
                _mockVehicleRepo.Object,
                _mockPersonRepo.Object,
                _mockDeptRepo.Object,
                _mockAnprService.Object);

            // Act
            var result = await service.ProcessInLaneAsync(
                inLaneName: "Làn Vào 1",
                plateCam: plateCam.Object,
                overviewCam: ovwCam.Object,
                triggerSource: "RADAR",
                captureDir: _testTempDir);

            // Assert
            result.Success.Should().BeTrue();
            result.PlateNumber.Should().Be("30A12345");
            result.PlateCamSuccess.Should().BeTrue();
            result.OverviewCamSuccess.Should().BeTrue();
            result.Session.Should().NotBeNull();
            result.Session!.Status.Should().Be(ParkingSessionStatus.Active);
            result.Session.InLaneName.Should().Be("Làn Vào 1");

            _mockSessionRepo.Verify(r => r.AddAsync(It.IsAny<ParkingSession>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessInLaneAsync_WithRegisteredVehicle_EnrichesPersonAndDepartmentInfo()
        {
            // Arrange
            var plateCam = CreateMockCamera(true);
            var ovwCam = CreateMockCamera(true);

            _mockAnprService
                .Setup(a => a.RecognizeAsync(It.IsAny<string>()))
                .ReturnsAsync(PlateRecognitionResult.Success("51F-999.99", 0.98f));

            var vehicle = new Vehicle("51F99999", VehicleType.Car, "person-01");
            _mockVehicleRepo
                .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Vehicle, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(vehicle);

            var person = new Person("EMP-01", "Nguyễn Văn Tuấn", PersonType.Employee)
            {
                Id = "person-01",
                DepartmentId = "dept-01"
            };
            _mockPersonRepo
                .Setup(r => r.GetByIdAsync("person-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(person);

            var dept = new Department("PKT", "Phòng Kỹ Thuật") { Id = "dept-01" };
            _mockDeptRepo
                .Setup(r => r.GetByIdAsync("dept-01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(dept);

            var service = new ParkingLaneService(
                _mockSessionRepo.Object,
                _mockVehicleRepo.Object,
                _mockPersonRepo.Object,
                _mockDeptRepo.Object,
                _mockAnprService.Object);

            // Act
            var result = await service.ProcessInLaneAsync(
                inLaneName: "Làn Vào 1",
                plateCam: plateCam.Object,
                overviewCam: ovwCam.Object,
                triggerSource: "RADAR",
                captureDir: _testTempDir);

            // Assert
            result.Success.Should().BeTrue();
            result.IsRegisteredVehicle.Should().BeTrue();
            result.PersonName.Should().Be("Nguyễn Văn Tuấn");
            result.DepartmentName.Should().Be("Phòng Kỹ Thuật");
            result.Session!.PersonName.Should().Be("Nguyễn Văn Tuấn");
            result.Session.DepartmentName.Should().Be("Phòng Kỹ Thuật");
        }

        [Fact]
        public async Task ProcessOutLaneAsync_WhenActiveSessionExists_CompletesSession()
        {
            // Arrange
            var plateCam = CreateMockCamera(true);
            var ovwCam = CreateMockCamera(true);

            _mockAnprService
                .Setup(a => a.RecognizeAsync(It.IsAny<string>()))
                .ReturnsAsync(PlateRecognitionResult.Success("43A-555.55", 0.92f));

            var activeSession = ParkingSession.CheckIn(
                inLaneName: "Làn Vào 1",
                plateNumber: "43A55555",
                inOverviewImagePath: "old_ovw.jpg",
                inPlateImagePath: "old_plt.jpg",
                personName: "Trần Thị B");
            activeSession.InTime = DateTime.Now.AddHours(-2);

            _mockSessionRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<FilterDefinition<ParkingSession>>(),
                    It.IsAny<SortDefinition<ParkingSession>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ParkingSession> { activeSession });

            var service = new ParkingLaneService(
                _mockSessionRepo.Object,
                _mockVehicleRepo.Object,
                _mockPersonRepo.Object,
                _mockDeptRepo.Object,
                _mockAnprService.Object);

            // Act
            var result = await service.ProcessOutLaneAsync(
                outLaneName: "Làn Ra 1",
                plateCam: plateCam.Object,
                overviewCam: ovwCam.Object,
                triggerSource: "RADAR",
                captureDir: _testTempDir);

            // Assert
            result.Success.Should().BeTrue();
            result.Session.Should().NotBeNull();
            result.Session!.Status.Should().Be(ParkingSessionStatus.Completed);
            result.Session.OutTime.Should().NotBeNull();
            result.Session.Duration.Should().NotBeNull();
            result.Session.Duration!.Value.TotalMinutes.Should().BeGreaterThan(100);

            _mockSessionRepo.Verify(r => r.UpdateAsync(It.Is<ParkingSession>(s => s.Status == ParkingSessionStatus.Completed), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessOutLaneAsync_WhenNoActiveSessionExists_CreatesUnmatchedOutSession()
        {
            // Arrange
            var plateCam = CreateMockCamera(true);
            var ovwCam = CreateMockCamera(true);

            _mockAnprService
                .Setup(a => a.RecognizeAsync(It.IsAny<string>()))
                .ReturnsAsync(PlateRecognitionResult.Success("99A-777.77", 0.90f));

            _mockSessionRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<FilterDefinition<ParkingSession>>(),
                    It.IsAny<SortDefinition<ParkingSession>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ParkingSession>()); // Empty

            var service = new ParkingLaneService(
                _mockSessionRepo.Object,
                _mockVehicleRepo.Object,
                _mockPersonRepo.Object,
                _mockDeptRepo.Object,
                _mockAnprService.Object);

            // Act
            var result = await service.ProcessOutLaneAsync(
                outLaneName: "Làn Ra 1",
                plateCam: plateCam.Object,
                overviewCam: ovwCam.Object,
                triggerSource: "RADAR",
                captureDir: _testTempDir);

            // Assert
            result.Success.Should().BeTrue();
            result.Session.Should().NotBeNull();
            result.Session!.Status.Should().Be(ParkingSessionStatus.UnmatchedOut);
            result.Session.OutLaneName.Should().Be("Làn Ra 1");

            _mockSessionRepo.Verify(r => r.AddAsync(It.Is<ParkingSession>(s => s.Status == ParkingSessionStatus.UnmatchedOut), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CrossLaneDeduplication_WhenSamePlateTriggersOppositeLaneWithin15Seconds_IgnoresCollision()
        {
            // Arrange
            var plateCam = CreateMockCamera(true);
            var ovwCam = CreateMockCamera(true);

            _mockAnprService
                .Setup(a => a.RecognizeAsync(It.IsAny<string>()))
                .ReturnsAsync(PlateRecognitionResult.Success("29B-111.11", 0.95f));

            var service = new ParkingLaneService(
                _mockSessionRepo.Object,
                _mockVehicleRepo.Object,
                _mockPersonRepo.Object,
                _mockDeptRepo.Object,
                _mockAnprService.Object);

            // Act 1: Xe Check-in tại Làn Vào
            var inResult = await service.ProcessInLaneAsync(
                inLaneName: "Làn Vào 1",
                plateCam: plateCam.Object,
                overviewCam: ovwCam.Object,
                triggerSource: "RADAR",
                captureDir: _testTempDir);

            // Act 2: Cùng biển số bị quét trúng tại Làn Ra ngay sau đó 1 giây (ảnh liếc chéo)
            var outResult = await service.ProcessOutLaneAsync(
                outLaneName: "Làn Ra 1",
                plateCam: plateCam.Object,
                overviewCam: ovwCam.Object,
                triggerSource: "RADAR",
                captureDir: _testTempDir);

            // Assert
            inResult.Success.Should().BeTrue();
            inResult.IsCrossLaneIgnored.Should().BeFalse();

            outResult.Success.Should().BeFalse();
            outResult.IsCrossLaneIgnored.Should().BeTrue();
            outResult.ErrorMessage.Should().Contain("Bỏ qua góc nhìn chéo");
        }

        [Fact]
        public async Task FaultTolerance_WhenCamerasThrowException_SessionStillCreatedWithWarningNote()
        {
            // Arrange: 2 Camera đều bị ngắt kết nối / lỗi ném ngoại lệ
            var failingPlateCam = CreateMockCamera(false);
            var failingOvwCam = CreateMockCamera(false);

            var service = new ParkingLaneService(
                _mockSessionRepo.Object,
                _mockVehicleRepo.Object,
                _mockPersonRepo.Object,
                _mockDeptRepo.Object,
                _mockAnprService.Object);

            // Act: Không ném Exception ra ngoài
            var result = await service.ProcessInLaneAsync(
                inLaneName: "Làn Vào 1",
                plateCam: failingPlateCam.Object,
                overviewCam: failingOvwCam.Object,
                triggerSource: "RADAR",
                captureDir: _testTempDir);

            // Assert
            result.Success.Should().BeTrue();
            result.PlateCamSuccess.Should().BeFalse();
            result.OverviewCamSuccess.Should().BeFalse();
            result.PlateNumber.Should().Be("Không đọc được");
            result.Session.Should().NotBeNull();
            result.Session!.Note.Should().Contain("mất kết nối");

            _mockSessionRepo.Verify(r => r.AddAsync(It.IsAny<ParkingSession>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ProcessInLaneAsync_WhenVehicleAlreadyHasActiveSession_BlocksCreationAndReturnsWarning()
        {
            // Arrange: Xe 30A-123.45 đã có phiên Active trong bãi
            var plateCam = CreateMockCamera(true);
            var ovwCam = CreateMockCamera(true);

            _mockAnprService
                .Setup(a => a.RecognizeAsync(It.IsAny<string>()))
                .ReturnsAsync(PlateRecognitionResult.Success("30A-123.45", 0.95f));

            var existingActiveSession = ParkingSession.CheckIn(
                inLaneName: "Làn Vào 1",
                plateNumber: "30A12345",
                inOverviewImagePath: "old_ovw.jpg",
                inPlateImagePath: "old_plt.jpg");

            _mockSessionRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<FilterDefinition<ParkingSession>>(),
                    It.IsAny<SortDefinition<ParkingSession>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ParkingSession> { existingActiveSession });

            var service = new ParkingLaneService(
                _mockSessionRepo.Object,
                _mockVehicleRepo.Object,
                _mockPersonRepo.Object,
                _mockDeptRepo.Object,
                _mockAnprService.Object);

            // Act: Xe cố tình vào lại khi chưa ra
            var result = await service.ProcessInLaneAsync(
                inLaneName: "Làn Vào 1",
                plateCam: plateCam.Object,
                overviewCam: ovwCam.Object,
                triggerSource: "RADAR",
                captureDir: _testTempDir);

            // Assert
            result.Success.Should().BeFalse();
            result.IsAlreadyInLot.Should().BeTrue();
            result.ErrorMessage.Should().Contain("đang ở trong bãi");
            result.Session.Should().Be(existingActiveSession);

            // Không được tạo thêm phiên mới vào DB
            _mockSessionRepo.Verify(r => r.AddAsync(It.IsAny<ParkingSession>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
