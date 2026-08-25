using System;
using System.Threading;
using FluentAssertions;
using PhuXuanParkingSystem.Services.ANPR;
using Xunit;

namespace PhuXuanParkingSystem.Tests.ANPR
{
    public class AnprLaneCoordinatorTests
    {
        [Fact]
        public void ShouldProcessLaneTrigger_WhenWithinCooldown_ShouldReturnFalse()
        {
            // Arrange
            var coordinator = new AnprLaneCoordinator();
            coordinator.SameLaneCooldownSeconds = 1.0;

            // Act
            bool firstTrigger = coordinator.ShouldProcessLaneTrigger("LANE_1");
            bool rapidSecondTrigger = coordinator.ShouldProcessLaneTrigger("LANE_1");

            // Assert
            firstTrigger.Should().BeTrue("Lần kích hoạt đầu tiên phải được chấp thuận");
            rapidSecondTrigger.Should().BeFalse("Lần kích hoạt thứ 2 ngay lập tức phải bị chặn bởi Cooldown Lớp 1");
        }

        [Fact]
        public void IsDuplicatePlate_WhenSamePlateDetectedWithinCooldown_ShouldReturnTrue()
        {
            // Arrange
            var coordinator = new AnprLaneCoordinator();
            coordinator.DuplicatePlateCooldownSeconds = 1.0;

            // Act
            bool first = coordinator.IsDuplicatePlate("17B-123.45");
            bool duplicate = coordinator.IsDuplicatePlate("17B-123.45");

            // Assert
            first.Should().BeFalse("Lần đầu thấy biển số thì chưa bị trùng");
            duplicate.Should().BeTrue("Lần thứ 2 xuất hiện trong 1 giây là bị trùng");
        }
    }
}