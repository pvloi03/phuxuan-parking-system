using FluentAssertions;
using PhuXuanParkingSystem.Services.Devices;
using PhuXuanParkingSystem.Services.Devices.Controller;
using System;
using System.Collections.Generic;
using Xunit;

namespace PhuXuanParkingSystem.Tests.Services
{
    public class ControllerServiceTests
    {
        [Fact]
        public void ControllerService_ShouldImplement_IDeviceAdapter()
        {
            // Arrange
            using var service = new ControllerService();

            // Assert
            service.Should().BeAssignableTo<IDeviceAdapter>();
            service.Should().BeAssignableTo<IControllerService>();
            service.IsConnected.Should().BeFalse();
            service.IsStreaming.Should().BeFalse();
        }

        [Fact]
        public void ParseAndDispatchLog_VehicleEnteringLaneIn_ShouldTriggerAuxEvent()
        {
            // Arrange
            using var service = new ControllerService();
            var events = new List<AuxTriggerEventArgs>();
            service.OnAuxInputTriggered += (s, e) => events.Add(e);

            // Time,Pin,CardNo,DoorOrAuxId,EventType,InOutState,VerifyMode
            string rawCsv = "2026-08-25 08:30:00,0,0,1,221,0,0";

            // Act
            service.ParseAndDispatchLog(rawCsv);

            // Assert
            events.Should().HaveCount(1);
            var evt = events[0];
            evt.AuxPort.Should().Be(1);
            evt.IsActive.Should().BeTrue();
            evt.LaneName.Should().Be("LÀN VÀO");
            evt.TriggerTime.Should().Be(new DateTime(2026, 8, 25, 8, 30, 0));
            evt.RawLog.Should().Be(rawCsv);
        }

        [Fact]
        public void ParseAndDispatchLog_VehicleEnteringLaneOut_ShouldTriggerAuxEvent()
        {
            // Arrange
            using var service = new ControllerService();
            var events = new List<AuxTriggerEventArgs>();
            service.OnAuxInputTriggered += (s, e) => events.Add(e);

            string rawCsv = "2026-08-25 08:35:10,0,0,2,221,1,0";

            // Act
            service.ParseAndDispatchLog(rawCsv);

            // Assert
            events.Should().HaveCount(1);
            var evt = events[0];
            evt.AuxPort.Should().Be(2);
            evt.IsActive.Should().BeTrue();
            evt.LaneName.Should().Be("LÀN RA");
            evt.TriggerTime.Should().Be(new DateTime(2026, 8, 25, 8, 35, 10));
        }

        [Fact]
        public void ParseAndDispatchLog_VehicleClearedLane_ShouldTriggerInactiveAuxEvent()
        {
            // Arrange
            using var service = new ControllerService();
            var events = new List<AuxTriggerEventArgs>();
            service.OnAuxInputTriggered += (s, e) => events.Add(e);

            string rawCsv = "2026-08-25 08:35:15,0,0,1,220,0,0";

            // Act
            service.ParseAndDispatchLog(rawCsv);

            // Assert
            events.Should().HaveCount(1);
            var evt = events[0];
            evt.AuxPort.Should().Be(1);
            evt.IsActive.Should().BeFalse();
            evt.LaneName.Should().Be("LÀN VÀO");
        }

        [Fact]
        public void ParseAndDispatchLog_StatusBit4Is255_ShouldIgnoreAndNotTrigger()
        {
            // Arrange
            using var service = new ControllerService();
            var events = new List<AuxTriggerEventArgs>();
            service.OnAuxInputTriggered += (s, e) => events.Add(e);

            string rawCsv = "2026-08-25 08:35:15,0,0,1,255,0,0";

            // Act
            service.ParseAndDispatchLog(rawCsv);

            // Assert
            events.Should().BeEmpty();
        }

        [Fact]
        public void ParseAndDispatchLog_NonLanePort_ShouldIgnoreAndNotTrigger()
        {
            // Arrange
            using var service = new ControllerService();
            var events = new List<AuxTriggerEventArgs>();
            service.OnAuxInputTriggered += (s, e) => events.Add(e);

            string rawCsv = "2026-08-25 08:35:15,0,0,5,221,0,0";

            // Act
            service.ParseAndDispatchLog(rawCsv);

            // Assert
            events.Should().BeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("invalid,csv")]
        [InlineData("1,2,3,4")] // Dưới 5 trường
        public void ParseAndDispatchLog_InvalidInput_ShouldNotTrigger(string? rawCsv)
        {
            // Arrange
            using var service = new ControllerService();
            var events = new List<AuxTriggerEventArgs>();
            service.OnAuxInputTriggered += (s, e) => events.Add(e);

            // Act
            service.ParseAndDispatchLog(rawCsv!);

            // Assert
            events.Should().BeEmpty();
        }
    }
}
