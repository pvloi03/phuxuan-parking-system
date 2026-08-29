using Moq;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using PhuXuanParkingSystem.Services.DeviceConfig;
using PhuXuanParkingSystem.Services.DeviceHealth;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace PhuXuanParkingSystem.Tests.Services
{
    public class DeviceConfigSyncTests
    {
        [Fact]
        public async Task CheckAndReloadIfChangedAsync_WhenCameraIpChanges_FiresOnConfigChangedEvent()
        {
            // Arrange
            var mockLaneRepo = new Mock<IRepository<Lane>>();
            var mockDeviceRepo = new Mock<IRepository<Device>>();

            var inLane = new Lane("IN-01", "Làn Vào", LaneDirection.In)
            {
                Id = "lane-in-01",
                PlateCameraDeviceId = "dev-cam-01"
            };

            var devCamInitial = new Device("CAM-01", "Cam Biển Số", DeviceType.PlateCamera, "192.168.1.100", 3000)
            {
                Id = "dev-cam-01",
                IsActive = true
            };

            mockLaneRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Lane, bool>>>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<Lane> { inLane });

            mockDeviceRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Device, bool>>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new List<Device> { devCamInitial });

            var configService = new DeviceConfigService(mockLaneRepo.Object, mockDeviceRepo.Object);

            // Initial load
            await configService.LoadConfigAsync();

            bool eventFired = false;
            ConfigChangeEventArgs? capturedArgs = null;
            configService.OnConfigChanged += (sender, args) =>
            {
                eventFired = true;
                capturedArgs = args;
            };

            // Simulate updating IP on Web Admin
            var devCamUpdated = new Device("CAM-01", "Cam Biển Số", DeviceType.PlateCamera, "192.168.1.200", 3000)
            {
                Id = "dev-cam-01",
                IsActive = true
            };

            mockDeviceRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Device, bool>>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new List<Device> { devCamUpdated });

            // Act
            var (hasChanged, newConfig) = await configService.CheckAndReloadIfChangedAsync();

            // Assert
            Assert.True(hasChanged);
            Assert.NotNull(newConfig);
            Assert.True(eventFired);
            Assert.NotNull(capturedArgs);
            Assert.Equal("192.168.1.200", newConfig.InPlateCamera?.IpAddress);
        }

        [Fact]
        public async Task LoadConfigAsync_WhenDeviceIsInactive_IgnoresInactiveDevice()
        {
            // Arrange
            var mockLaneRepo = new Mock<IRepository<Lane>>();
            var mockDeviceRepo = new Mock<IRepository<Device>>();

            var inLane = new Lane("IN-01", "Làn Vào", LaneDirection.In)
            {
                Id = "lane-in-01",
                PlateCameraDeviceId = "dev-cam-01"
            };

            mockLaneRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Lane, bool>>>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<Lane> { inLane });

            // Returning empty list because query filters d.IsActive == true
            mockDeviceRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Device, bool>>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new List<Device>());

            var configService = new DeviceConfigService(mockLaneRepo.Object, mockDeviceRepo.Object);

            // Act
            var result = await configService.LoadConfigAsync();

            // Assert
            Assert.Null(result.InPlateCamera);
        }

        [Fact]
        public async Task SyncStatusToDbAsync_LoadsFreshEntityFromDb_PreservingWebAdminModifications()
        {
            // Arrange
            var mockDeviceRepo = new Mock<IRepository<Device>>();
            var mockAdapterFactory = new Mock<IDeviceAdapterFactory>();

            // Stale in-memory device that WinForms holds
            var staleDev = new Device("CAM-01", "Old Name", DeviceType.PlateCamera, "192.168.1.100", 3000)
            {
                Id = "dev-cam-01",
                UserName = "old_user"
            };

            // Fresh device in MongoDB updated by Web Admin
            var freshDevInDb = new Device("CAM-01", "New Updated Name", DeviceType.PlateCamera, "192.168.1.250", 8000)
            {
                Id = "dev-cam-01",
                UserName = "new_admin"
            };

            mockDeviceRepo.Setup(r => r.GetByIdAsync("dev-cam-01", It.IsAny<CancellationToken>()))
                          .ReturnsAsync(freshDevInDb);

            Device? updatedEntitySaved = null;
            mockDeviceRepo.Setup(r => r.UpdateAsync(It.IsAny<Device>(), It.IsAny<CancellationToken>()))
                          .Callback<Device, CancellationToken>((d, ct) => updatedEntitySaved = d)
                          .ReturnsAsync(true);

            var healthService = new DeviceHealthMonitorService(mockDeviceRepo.Object, mockAdapterFactory.Object);
            var pingResult = DevicePingResult.Success(staleDev, 20);

            // Act
            await healthService.SyncStatusToDbAsync(pingResult);

            // Assert
            Assert.NotNull(updatedEntitySaved);
            // Verify status was updated
            Assert.Equal(DeviceStatus.Connected, updatedEntitySaved.Status);
            // Verify new IP, Port, Name, and UserName from Web Admin were PRESERVED (NOT overwritten by staleDev)
            Assert.Equal("192.168.1.250", updatedEntitySaved.IpAddress);
            Assert.Equal(8000, updatedEntitySaved.Port);
            Assert.Equal("New Updated Name", updatedEntitySaved.Name);
            Assert.Equal("new_admin", updatedEntitySaved.UserName);
        }

        [Fact]
        public void DeviceHealthManager_ClearAllDevices_RemovesAllDevices()
        {
            // Arrange
            var healthManager = new DeviceHealthManager();
            var dev = new Device("CAM-01", "Cam Biển Số", DeviceType.PlateCamera, "192.168.1.100", 3000)
            {
                Id = "dev-01"
            };
            var mockAdapter = new Mock<IDeviceAdapter>();
            mockAdapter.Setup(a => a.IsConnected).Returns(true);

            healthManager.RegisterDevice("dev-01", dev, mockAdapter.Object);

            Assert.NotNull(healthManager.GetDevice("dev-01"));

            // Act
            healthManager.ClearAllDevices();

            // Assert
            Assert.Null(healthManager.GetDevice("dev-01"));
            Assert.Empty(healthManager.GetAllDevices());
        }

        [Fact]
        public async Task CheckAndReloadIfChangedAsync_WhenOverviewCameraMovedBetweenLanes_DetectsChangeCorrectly()
        {
            // Arrange
            var mockLaneRepo = new Mock<IRepository<Lane>>();
            var mockDeviceRepo = new Mock<IRepository<Device>>();

            var inLaneInitial = new Lane("IN-01", "Làn Vào", LaneDirection.In)
            {
                Id = "lane-in-01",
                PlateCameraDeviceId = "dev-plate-in",
                OverviewCameraDeviceId = null
            };

            var outLaneInitial = new Lane("OUT-01", "Làn Ra", LaneDirection.Out)
            {
                Id = "lane-out-01",
                PlateCameraDeviceId = "dev-plate-out",
                OverviewCameraDeviceId = "dev-ovw-shared"
            };

            var plateInDev = new Device("CAM-P-IN", "Plate In", DeviceType.PlateCamera, "192.168.1.101", 3000) { Id = "dev-plate-in", IsActive = true };
            var plateOutDev = new Device("CAM-P-OUT", "Plate Out", DeviceType.PlateCamera, "192.168.1.102", 3000) { Id = "dev-plate-out", IsActive = true };
            var ovwSharedDev = new Device("CAM-OVW", "Overview Shared", DeviceType.OverviewCamera, "192.168.1.103", 8000) { Id = "dev-ovw-shared", IsActive = true };

            mockLaneRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Lane, bool>>>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<Lane> { inLaneInitial, outLaneInitial });

            mockDeviceRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Device, bool>>>(), It.IsAny<CancellationToken>()))
                          .ReturnsAsync(new List<Device> { plateInDev, plateOutDev, ovwSharedDev });

            var configService = new DeviceConfigService(mockLaneRepo.Object, mockDeviceRepo.Object);
            var initialConfig = await configService.LoadConfigAsync();

            Assert.Null(initialConfig.InOverviewCamera);
            Assert.NotNull(initialConfig.OutOverviewCamera);
            Assert.Equal("192.168.1.103", initialConfig.OutOverviewCamera.IpAddress);

            // Now simulate user moving Overview Camera to In-Lane and clearing Out-Lane Overview
            var inLaneUpdated = new Lane("IN-01", "Làn Vào", LaneDirection.In)
            {
                Id = "lane-in-01",
                PlateCameraDeviceId = "dev-plate-in",
                OverviewCameraDeviceId = "dev-ovw-shared"
            };

            var outLaneUpdated = new Lane("OUT-01", "Làn Ra", LaneDirection.Out)
            {
                Id = "lane-out-01",
                PlateCameraDeviceId = "dev-plate-out",
                OverviewCameraDeviceId = null
            };

            mockLaneRepo.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Lane, bool>>>(), It.IsAny<CancellationToken>()))
                        .ReturnsAsync(new List<Lane> { inLaneUpdated, outLaneUpdated });

            // Act
            var (hasChanged, newConfig) = await configService.CheckAndReloadIfChangedAsync();

            // Assert
            Assert.True(hasChanged);
            Assert.NotNull(newConfig);
            Assert.NotNull(newConfig.InOverviewCamera);
            Assert.Equal("192.168.1.103", newConfig.InOverviewCamera.IpAddress);
            Assert.Null(newConfig.OutOverviewCamera);
        }
    }
}
