using System;
using FluentAssertions;
using HPParkingSystem.Helpers;
using Xunit;

namespace HPParkingSystem.Api.Tests.Helpers
{
    public class ZKTecoRtLogParserTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Parse_WhenRawLogIsNullOrEmpty_ShouldReturnIgnoredMalformed(string? rawLog)
        {
            var result = ZKTecoRtLogParser.Parse(rawLog);

            result.Status.Should().Be(ZKTecoRtLogParseStatus.IgnoredMalformed);
        }

        [Fact]
        public void Parse_WhenRawLogHasLessThan5Parts_ShouldReturnIgnoredMalformed()
        {
            var rawLog = "2026-10-21 15:00:00,0,0,1";

            var result = ZKTecoRtLogParser.Parse(rawLog);

            result.Status.Should().Be(ZKTecoRtLogParseStatus.IgnoredMalformed);
            result.Reason.Should().Contain("không đủ 5 trường");
        }

        [Fact]
        public void Parse_WhenBit4Is255_ShouldReturnIgnoredStatusPacket()
        {
            var rawLog = "2026-10-21 15:00:00,0,0,1,255,2,200";

            var result = ZKTecoRtLogParser.Parse(rawLog);

            result.Status.Should().Be(ZKTecoRtLogParseStatus.IgnoredStatusPacket);
            result.Reason.Should().Contain("255");
        }

        [Theory]
        [InlineData("2026-10-21 15:00:00,0,0,3,221,2,200")]
        [InlineData("2026-10-21 15:00:00,0,0,0,221,2,200")]
        [InlineData("2026-10-21 15:00:00,0,0,INVALID,221,2,200")]
        public void Parse_WhenPortIndexIsNot1Or2_ShouldReturnIgnoredInvalidPort(string rawLog)
        {
            var result = ZKTecoRtLogParser.Parse(rawLog);

            result.Status.Should().Be(ZKTecoRtLogParseStatus.IgnoredInvalidPort);
            result.Reason.Should().Contain("không thuộc Làn 1 hoặc 2");
        }

        [Fact]
        public void Parse_WhenEventIsOlderThanMaxAge_ShouldReturnIgnoredStaleEvent()
        {
            var now = new DateTime(2026, 10, 21, 15, 0, 30);
            // Sự kiện xảy ra lúc 14:35:00 (cũ hơn 25 phút)
            var staleLog = "2026-10-21 14:35:00,0,0,1,221,2,200";

            var result = ZKTecoRtLogParser.Parse(staleLog, referenceTime: now, maxAgeSeconds: 5.0);

            result.Status.Should().Be(ZKTecoRtLogParseStatus.IgnoredStaleEvent);
            result.AgeSeconds.Should().BeGreaterThan(5.0);
            result.Reason.Should().Contain("Sự kiện tồn đọng cũ");
            result.AuxPort.Should().Be(1);
            result.IsActive.Should().BeTrue();
        }

        [Theory]
        [InlineData("2026-10-21 15:00:28,0,0,1,221,2,200", 1, true, 221)]  // Aux 1, Cạnh lên (có xe)
        [InlineData("2026-10-21 15:00:29,0,0,1,220,2,200", 1, false, 220)] // Aux 1, Cạnh xuống (hết xe)
        [InlineData("2026-10-21 15:00:28,0,0,2,221,2,200", 2, true, 221)]  // Aux 2, Cạnh lên (có xe)
        [InlineData("2026-10-21 15:00:29,0,0,2,220,2,200", 2, false, 220)] // Aux 2, Cạnh xuống (hết xe)
        [InlineData("2026-10-21 15:00:28,0,0,1,25,2,200", 1, true, 25)]    // Aux 1, Báo động
        [InlineData("2026-10-21 15:00:28,0,0,1,1,2,200", 1, true, 1)]      // Aux 1, Sensor
        public void Parse_WhenEventIsFreshAndValid_ShouldReturnSuccess(
            string rawLog,
            int expectedPort,
            bool expectedActive,
            int expectedEventType)
        {
            var now = new DateTime(2026, 10, 21, 15, 0, 30);

            var result = ZKTecoRtLogParser.Parse(rawLog, referenceTime: now, maxAgeSeconds: 5.0);

            result.Status.Should().Be(ZKTecoRtLogParseStatus.Success);
            result.AuxPort.Should().Be(expectedPort);
            result.IsActive.Should().Be(expectedActive);
            result.EventType.Should().Be(expectedEventType);
            result.AgeSeconds.Should().BeLessThanOrEqualTo(5.0);
        }
    }
}
