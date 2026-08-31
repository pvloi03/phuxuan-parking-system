using Moq;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using PhuXuanParkingSystem.Services.Devices;
using PhuXuanParkingSystem.Services.Devices.Camera;
using PhuXuanParkingSystem.Services.Devices.Health;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PhuXuanParkingSystem.Tests.Services
{
    public class DeviceHealthStreamTests
    {
        [Fact]
        public async Task PingDeviceAsync_WhenConnectedAndStreaming_SetsStateToStreamingAndFiresEvents()
        {
            // Arrange
            var mockRepo = new Mock<IRepository<Device>>();
            var mockFactory = new Mock<IDeviceAdapterFactory>();
            var mockAdapter = new Mock<IDeviceAdapter>();
            var device = new Device("CAM-01", "Camera Biển Số", DeviceType.PlateCamera, "192.168.1.100", 3000)
            {
                Id = "dev-cam-01"
            };

            mockAdapter.SetupGet(a => a.IsConnected).Returns(true);
            mockAdapter.SetupGet(a => a.IsStreaming).Returns(true);
            mockFactory.Setup(f => f.GetAdapter(device)).Returns(mockAdapter.Object);

            using var healthService = new DeviceHealthMonitorService(mockRepo.Object, mockFactory.Object);
            DeviceStateChangedEventArgs? stateChangedArgs = null;
            healthService.OnStateChanged += (_, e) => stateChangedArgs = e;

            // Act
            var pingResult = await healthService.PingDeviceAsync(device);

            // Assert
            Assert.True(pingResult.IsSuccess);
            Assert.Equal(DeviceStatus.Streaming, healthService.GetState(device.Id));
            Assert.NotNull(stateChangedArgs);
            Assert.Equal(device.Id, stateChangedArgs.DeviceId);
            Assert.Equal(DeviceStatus.Streaming, stateChangedArgs.NewState);
        }

        [Fact]
        public async Task PingDeviceAsync_WhenDisconnected_RetriesAndRecovers()
        {
            // Arrange
            var mockRepo = new Mock<IRepository<Device>>();
            var mockFactory = new Mock<IDeviceAdapterFactory>();
            var mockAdapter = new Mock<IDeviceAdapter>();
            var device = new Device("CAM-02", "Camera Toàn Cảnh", DeviceType.OverviewCamera, "192.168.1.101", 8000)
            {
                Id = "dev-cam-02"
            };

            mockAdapter.SetupGet(a => a.IsConnected).Returns(false);
            mockAdapter.Setup(a => a.ConnectAsync(device, It.IsAny<CancellationToken>())).ReturnsAsync(true);
            mockFactory.Setup(f => f.GetAdapter(device)).Returns(mockAdapter.Object);

            using var healthService = new DeviceHealthMonitorService(mockRepo.Object, mockFactory.Object);

            // Act
            var pingResult = await healthService.PingDeviceAsync(device);

            // Assert
            Assert.True(pingResult.IsSuccess);
            Assert.True(pingResult.WasReconnected);
            mockAdapter.Verify(a => a.ConnectAsync(device, It.IsAny<CancellationToken>()), Times.Once);
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
            var mockCamService = new Mock<ICameraService>();
            var camConfig = new CameraConfig();
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
