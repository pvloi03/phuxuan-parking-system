using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using HPParkingSystem.Api.Controllers;
using HPParkingSystem.Api.DTOs;
using HPParkingSystem.Models.Entities;
using HPParkingSystem.Models.Enums;
using HPParkingSystem.Repositories;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace HPParkingSystem.Api.Tests.Controllers
{
    public class DashboardControllerTests
    {
        private readonly Mock<IRepository<ParkingSession>> _sessionRepoMock;
        private readonly DashboardController _controller;

        public DashboardControllerTests()
        {
            _sessionRepoMock = new Mock<IRepository<ParkingSession>>();
            _controller = new DashboardController(_sessionRepoMock.Object);
        }

        [Fact]
        public async Task GetMetrics_CustomPeriodWithExactDateTimeRange_ShouldReturnExpectedPeriodLabelAndFilterSessions()
        {
            // Arrange
            var fromDate = new DateTime(2026, 10, 19, 10, 0, 0);
            var toDate = new DateTime(2026, 10, 19, 14, 0, 0);

            var sampleSessions = new List<ParkingSession>
            {
                new ParkingSession
                {
                    Id = "s_01",
                    PlateNumber = "29C-11111",
                    InTime = new DateTime(2026, 10, 19, 10, 30, 0),
                    OutTime = new DateTime(2026, 10, 19, 11, 45, 0),
                    Status = ParkingSessionStatus.Completed
                },
                new ParkingSession
                {
                    Id = "s_02",
                    PlateNumber = "29C-22222",
                    InTime = new DateTime(2026, 10, 19, 12, 0, 0),
                    Status = ParkingSessionStatus.Active
                }
            };

            _sessionRepoMock
                .Setup(r => r.CountAsync(It.IsAny<Expression<Func<ParkingSession, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1); // 1 active in entire yard

            _sessionRepoMock
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ParkingSession, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sampleSessions);

            // Act
            var actionResult = await _controller.GetMetrics(
                period: "custom",
                fromDate: fromDate,
                toDate: toDate);

            // Assert
            actionResult.Should().BeOfType<OkObjectResult>();
            var okResult = (OkObjectResult)actionResult;
            var response = okResult.Value as ApiResponse<DashboardMetricsDto>;

            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();

            // Label phải hiển thị cả giờ phút khi có thời gian cụ thể
            response.Data!.PeriodLabel.Should().Be("19/10/2026 10:00 - 19/10/2026 14:00");
            response.Data.ActiveVehiclesCount.Should().Be(1);
            response.Data.PeriodInCount.Should().Be(2);
            response.Data.PeriodOutCount.Should().Be(1);
        }
    }
}
