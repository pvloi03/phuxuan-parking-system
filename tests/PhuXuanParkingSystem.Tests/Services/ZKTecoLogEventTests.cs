using FluentAssertions;
using PhuXuanParkingSystem.Services.Controller;
using System;
using Xunit;

namespace PhuXuanParkingSystem.Tests.Services
{
    public class ZKTecoLogEventTests
    {
        [Fact]
        public void Parse_VehicleEnteringLaneIn_ShouldParseCorrectly()
        {
            // Arrange
            // Time,Pin,CardNo,DoorOrAuxId,EventType,InOutState,VerifyMode
            string rawCsv = "2026-08-25 08:30:00,0,0,1,221,0,0";

            // Act
            var evt = ZKTecoLogEvent.Parse(rawCsv);

            // Assert
            evt.Should().NotBeNull();
            evt!.DoorOrAuxId.Should().Be(1);
            evt.EventType.Should().Be(221); // 221 = AUX radar activated (Có xe)
            evt.IsRadarAuxEvent.Should().BeTrue();
            evt.IsVehicleDetected.Should().BeTrue();
            evt.InOutState.Should().Be(0); // 0 = In
            evt.InOutDescription.Should().Be("Vào (In)");
            evt.EventDescription.Should().Contain("Radar AUX");
            evt.Time.Should().Be(new DateTime(2026, 8, 25, 8, 30, 0));
        }

        [Fact]
        public void Parse_VehicleEnteringLaneOut_ShouldParseCorrectly()
        {
            // Arrange
            string rawCsv = "2026-08-25 08:35:10,0,0,2,221,1,0";

            // Act
            var evt = ZKTecoLogEvent.Parse(rawCsv);

            // Assert
            evt.Should().NotBeNull();
            evt!.DoorOrAuxId.Should().Be(2);
            evt.EventType.Should().Be(221);
            evt.IsRadarAuxEvent.Should().BeTrue();
            evt.IsVehicleDetected.Should().BeTrue();
            evt.InOutState.Should().Be(1); // 1 = Out
            evt.InOutDescription.Should().Be("Ra (Out)");
        }

        [Fact]
        public void Parse_RadarDeactivatedEvent_ShouldRecognizeRadarAuxEvent()
        {
            // Arrange
            string rawCsv = "2026-08-25 08:35:15,0,0,1,220,0,0"; // 220 = Radar ngắt (Xe đã ra khỏi vùng quét)

            // Act
            var evt = ZKTecoLogEvent.Parse(rawCsv);

            // Assert
            evt.Should().NotBeNull();
            evt!.EventType.Should().Be(220);
            evt.IsRadarAuxEvent.Should().BeTrue();
            evt.IsVehicleDetected.Should().BeFalse(); // 220 is not new vehicle entry trigger
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("invalid,csv")]
        [InlineData("1,2,3,4,5,6")] // Chỉ có 6 phần tử thay vì >= 7
        public void Parse_InvalidInput_ShouldReturnNull(string? rawCsv)
        {
            // Act
            var evt = ZKTecoLogEvent.Parse(rawCsv!);

            // Assert
            evt.Should().BeNull();
        }
    }
}
