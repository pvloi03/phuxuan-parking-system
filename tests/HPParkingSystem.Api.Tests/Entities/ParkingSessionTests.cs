using FluentAssertions;
using HPParkingSystem.Models.Entities;
using HPParkingSystem.Models.Enums;

namespace HPParkingSystem.Api.Tests.Entities
{
    public class ParkingSessionTests
    {
        [Fact]
        public void HasExited_WhenActiveWithoutOutTime_ShouldReturnFalse()
        {
            var session = new ParkingSession
            {
                InTime = DateTime.Now,
                OutTime = null,
                Status = ParkingSessionStatus.Active
            };

            session.HasExited.Should().BeFalse();
        }

        [Fact]
        public void HasExited_WhenCompletedWithOutTime_ShouldReturnTrue()
        {
            var session = new ParkingSession
            {
                InTime = DateTime.Now.AddHours(-2),
                OutTime = DateTime.Now,
                Status = ParkingSessionStatus.Completed
            };

            session.HasExited.Should().BeTrue();
        }

        [Fact]
        public void HasExited_WhenUnmatchedOut_ShouldReturnTrue()
        {
            var session = new ParkingSession
            {
                InTime = null,
                OutTime = DateTime.Now,
                Status = ParkingSessionStatus.UnmatchedOut
            };

            session.HasExited.Should().BeTrue();
        }

        [Fact]
        public void HasExited_WhenOutTimeHasValueRegardlessOfStatus_ShouldReturnTrue()
        {
            var session = new ParkingSession
            {
                OutTime = DateTime.Now,
                Status = ParkingSessionStatus.Active
            };

            session.HasExited.Should().BeTrue();
        }

        [Theory]
        [InlineData(ParkingSessionStatus.Active, "Đang trong bãi")]
        [InlineData(ParkingSessionStatus.Completed, "Đã hoàn thành")]
        [InlineData(ParkingSessionStatus.UnmatchedOut, "Xe ra")]
        [InlineData(ParkingSessionStatus.Cancelled, "Đã hủy")]
        public void Status_ToDisplayName_ShouldReturnExpectedDisplayName(ParkingSessionStatus status, string expected)
        {
            status.ToDisplayName().Should().Be(expected);
        }

        [Fact]
        public void NullableStatus_ToDisplayName_ShouldReturnDashWhenNull()
        {
            ParkingSessionStatus? status = null;
            status.ToDisplayName().Should().Be("--");
        }

        [Fact]
        public void UndefinedStatus_ToDisplayName_ShouldReturnDash()
        {
            ((ParkingSessionStatus)0).ToDisplayName().Should().Be("--");
            ((ParkingSessionStatus)99).ToDisplayName().Should().Be("--");
        }
    }
}