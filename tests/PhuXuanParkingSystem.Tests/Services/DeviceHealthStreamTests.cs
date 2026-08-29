using Moq;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Services.DeviceHealth;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PhuXuanParkingSystem.Tests.Services
{
    public class DeviceHealthStreamTests
    {
        [Fact]
        public async Task ConnectAsync_WhenSuccessfulAndPreviewHandleProvided_StartsLiveStreamAndSetsStateToStreaming()
        {
            // Arrange
            var healthManager = new DeviceHealthManager();
            var mockAdapter = new Mock<IDeviceAdapter>();
            var device = new Device("CAM-01", "Camera Biển Số", DeviceType.PlateCamera, "192.168.1.100", 3000)
            {
                Id = "dev-cam-01"
            };

            var fakeHandle = new IntPtr(12345);

            mockAdapter.Setup(a => a.ConnectAsync(It.IsAny<Device>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(true);
            mockAdapter.SetupGet(a => a.IsConnected)
                       .Returns(true);
            mockAdapter.Setup(a => a.StartPreview(fakeHandle))
                       .Returns(true);

            healthManager.RegisterDevice(device.Id, device, mockAdapter.Object, fakeHandle);

            // Act
            bool connectResult = await healthManager.ConnectAsync(device.Id);

            // Assert
            Assert.True(connectResult);
            mockAdapter.Verify(a => a.ConnectAsync(device, It.IsAny<CancellationToken>()), Times.Once);
            mockAdapter.Verify(a => a.StartPreview(fakeHandle), Times.Once);
            Assert.Equal(DeviceStatus.Streaming, healthManager.GetState(device.Id));
        }

        [Fact]
        public async Task RestartAsync_WhenSuccessfulAndPreviewHandleProvided_RestoresPreviewAndSetsStateToStreaming()
        {
            // Arrange
            var healthManager = new DeviceHealthManager();
            var mockAdapter = new Mock<IDeviceAdapter>();
            var device = new Device("CAM-02", "Camera Toàn Cảnh", DeviceType.OverviewCamera, "192.168.1.101", 8000)
            {
                Id = "dev-cam-02"
            };

            var fakeHandle = new IntPtr(99999);

            mockAdapter.Setup(a => a.RestartAsync(It.IsAny<Device>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(true);
            mockAdapter.SetupGet(a => a.IsConnected)
                       .Returns(true);
            mockAdapter.Setup(a => a.StartPreview(fakeHandle))
                       .Returns(true);

            healthManager.RegisterDevice(device.Id, device, mockAdapter.Object, fakeHandle);

            // Act
            bool restartResult = await healthManager.RestartAsync(device.Id);

            // Assert
            Assert.True(restartResult);
            mockAdapter.Verify(a => a.RestartAsync(device, It.IsAny<CancellationToken>()), Times.Once);
            mockAdapter.Verify(a => a.StartPreview(fakeHandle), Times.Once);
            Assert.Equal(DeviceStatus.Streaming, healthManager.GetState(device.Id));
        }

        [Fact]
        public void DeviceStatus_EnumValues_MatchExpectedSpecification()
        {
            // Verify core integer values for backwards compatibility
            Assert.Equal(1, (int)DeviceStatus.Connected);
            Assert.Equal(2, (int)DeviceStatus.Disconnected);
            Assert.Equal(3, (int)DeviceStatus.Error);
            Assert.Equal(4, (int)DeviceStatus.Maintenance);
            Assert.Equal(5, (int)DeviceStatus.Connecting);
            Assert.Equal(6, (int)DeviceStatus.Streaming);
        }

        [Fact]
        public async Task CameraDeviceAdapter_ConnectAsync_AppliesConfigAndCallsLoginAsync()
        {
            // Arrange
            var mockCamService = new Mock<PhuXuanParkingSystem.Services.Camera.ICameraService>();
            var camConfig = new PhuXuanParkingSystem.Services.Camera.CameraConfig();
            mockCamService.SetupGet(c => c.Config).Returns(camConfig);
            mockCamService.Setup(c => c.LoginAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);
            mockCamService.SetupGet(c => c.IsLoggedIn).Returns(true);

            var adapter = new CameraDeviceAdapter(mockCamService.Object);
            var device = new Device("CAM-01", "Camera Biển Số", DeviceType.PlateCamera, "192.168.1.150", 8080)
            {
                UserName = "admin",
                Password = "pwd"
            };

            // Act
            bool result = await adapter.ConnectAsync(device);

            // Assert
            Assert.True(result);
            Assert.Equal("192.168.1.150", camConfig.Ip);
            Assert.Equal((ushort)8080, camConfig.Port);
            Assert.Equal("admin", camConfig.UserName);
            Assert.Equal("pwd", camConfig.Password);
            mockCamService.Verify(c => c.LoginAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public void DeviceAdapterFactory_ResolvesByIdAndFallbackByIp()
        {
            // Arrange
            using var factory = new DeviceAdapterFactory();
            var mockAdapter1 = new Mock<IDeviceAdapter>();
            var mockAdapter2 = new Mock<IDeviceAdapter>();

            factory.RegisterAdapter("dev-01", mockAdapter1.Object, "192.168.1.101");
            factory.RegisterAdapter("dev-02", mockAdapter2.Object, "192.168.1.102");

            var devById = new Device("CAM-01", "Cam 1", DeviceType.PlateCamera, "192.168.1.999", 3000) { Id = "dev-01" };
            var devByIpFallback = new Device("CAM-02", "Cam 2", DeviceType.OverviewCamera, "192.168.1.102", 8000) { Id = "dev-unregistered" };
            var devNotFound = new Device("CAM-03", "Cam 3", DeviceType.PlateCamera, "10.0.0.1", 3000) { Id = "dev-99" };

            // Act
            var resolved1 = factory.GetAdapter(devById);
            var resolved2 = factory.GetAdapter(devByIpFallback);
            var resolved3 = factory.GetAdapter(devNotFound);
            var resolvedNull = factory.GetAdapter(null!);

            // Assert
            Assert.Same(mockAdapter1.Object, resolved1);
            Assert.Same(mockAdapter2.Object, resolved2);
            Assert.False(resolved3.IsConnected);
            Assert.False(resolvedNull.IsConnected);
        }
    }
}
