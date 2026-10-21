using FluentAssertions;
using HPParkingSystem.Api.Controllers;
using HPParkingSystem.Api.DTOs;
using HPParkingSystem.Api.Services;
using HPParkingSystem.Models.Entities;
using HPParkingSystem.Models.Enums;
using HPParkingSystem.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Linq.Expressions;
using System.Security.Claims;

namespace HPParkingSystem.Api.Tests.Controllers
{
    public class VehiclesControllerTests
    {
        private readonly Mock<IRepository<Vehicle>> _vehicleRepoMock;
        private readonly Mock<IAuditLogQueue> _auditQueueMock;
        private readonly VehiclesController _controller;

        public VehiclesControllerTests()
        {
            _vehicleRepoMock = new Mock<IRepository<Vehicle>>();
            _auditQueueMock = new Mock<IAuditLogQueue>();

            _controller = new VehiclesController(_vehicleRepoMock.Object, _auditQueueMock.Object);

            var httpContext = new DefaultHttpContext();
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "test_admin"),
                new Claim(ClaimTypes.NameIdentifier, "admin_1")
            }, "TestAuth"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task Create_WithValidWeight_ShouldSaveAndReturnVehicle()
        {
            // Arrange
            var vehicle = new Vehicle
            {
                PlateNumber = "29C-12345",
                Type = VehicleType.Truck,
                Weight = 12.5
            };

            _vehicleRepoMock
                .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Vehicle, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Vehicle?)null);

            _vehicleRepoMock
                .Setup(r => r.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Vehicle v, CancellationToken _) => v);

            // Act
            var actionResult = await _controller.Create(vehicle);

            // Assert
            actionResult.Should().BeOfType<OkObjectResult>();
            var okResult = (OkObjectResult)actionResult;
            var response = okResult.Value as ApiResponse<Vehicle>;

            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.PlateNumber.Should().Be("29C12345");
            response.Data.Type.Should().Be(VehicleType.Truck);
            response.Data.Weight.Should().Be(12.5);

            _vehicleRepoMock.Verify(r => r.AddAsync(It.Is<Vehicle>(v => v.Weight == 12.5), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Create_WithNegativeWeight_ShouldReturnBadRequest()
        {
            // Arrange
            var vehicle = new Vehicle
            {
                PlateNumber = "29C-99999",
                Type = VehicleType.Truck
            };
            // Set negative weight using backing field reflection to simulate raw deserialization bypass
            typeof(Vehicle).GetField("_weight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(vehicle, -2.5);

            _vehicleRepoMock
                .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Vehicle, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Vehicle?)null);

            // Act
            var actionResult = await _controller.Create(vehicle);

            // Assert
            actionResult.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = (BadRequestObjectResult)actionResult;
            var response = badRequest.Value as ApiResponse;

            response.Should().NotBeNull();
            response!.Success.Should().BeFalse();
            response.Message.Should().Contain("Khối lượng xe không thể nhỏ hơn 0");

            _vehicleRepoMock.Verify(r => r.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Update_WithValidWeight_ShouldUpdateAndReturnExisting()
        {
            // Arrange
            var existing = new Vehicle
            {
                Id = "veh_01",
                PlateNumber = "29C-11111",
                Type = VehicleType.Truck,
                Weight = 5.0
            };

            var updateDto = new Vehicle
            {
                PlateNumber = "29C-11111",
                Type = VehicleType.Truck,
                Weight = 15.0
            };

            _vehicleRepoMock
                .Setup(r => r.GetByIdAsync("veh_01", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            _vehicleRepoMock
                .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Vehicle, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Vehicle?)null);

            _vehicleRepoMock
                .Setup(r => r.UpdateAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var actionResult = await _controller.Update("veh_01", updateDto);

            // Assert
            actionResult.Should().BeOfType<OkObjectResult>();
            var okResult = (OkObjectResult)actionResult;
            var response = okResult.Value as ApiResponse<Vehicle>;

            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data!.Weight.Should().Be(15.0);

            _vehicleRepoMock.Verify(r => r.UpdateAsync(It.Is<Vehicle>(v => v.Weight == 15.0), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Update_WithNegativeWeight_ShouldReturnBadRequest()
        {
            // Arrange
            var existing = new Vehicle
            {
                Id = "veh_02",
                PlateNumber = "29C-22222",
                Type = VehicleType.Truck,
                Weight = 8.0
            };

            var updateDto = new Vehicle
            {
                PlateNumber = "29C-22222",
                Type = VehicleType.Truck
            };
            typeof(Vehicle).GetField("_weight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(updateDto, -3.0);

            _vehicleRepoMock
                .Setup(r => r.GetByIdAsync("veh_02", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            _vehicleRepoMock
                .Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Vehicle, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Vehicle?)null);

            // Act
            var actionResult = await _controller.Update("veh_02", updateDto);

            // Assert
            actionResult.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = (BadRequestObjectResult)actionResult;
            var response = badRequest.Value as ApiResponse;

            response.Should().NotBeNull();
            response!.Success.Should().BeFalse();
            response.Message.Should().Contain("Khối lượng xe không thể nhỏ hơn 0");

            _vehicleRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public void DomainInvariant_SettingNegativeWeight_ShouldThrowArgumentException()
        {
            // Assert domain property invariant validation
            var vehicle = new Vehicle();
            var act = () => vehicle.Weight = -1.0;

            act.Should().Throw<ArgumentOutOfRangeException>()
               .WithMessage("*Khối lượng xe không thể nhỏ hơn 0*");
        }

        [Fact]
        public async Task CrossImport_WithEmptyList_ShouldReturnBadRequest()
        {
            // Arrange
            var crossImportServiceMock = new Mock<ICrossImportService>();

            // Act
            var result = await _controller.CrossImport(new List<VehiclePersonCrossImportItemDto>(), crossImportServiceMock.Object);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
            var badRequest = (BadRequestObjectResult)result;
            var response = badRequest.Value as ApiResponse;
            response!.Success.Should().BeFalse();
            response.Message.Should().Contain("không được rỗng");
        }

        [Fact]
        public async Task CrossImport_WithValidItems_ShouldDelegateToServiceAndReturnOk()
        {
            // Arrange
            var crossImportServiceMock = new Mock<ICrossImportService>();
            var expectedResult = new CrossImportResultDto
            {
                TotalRows = 1,
                VehiclesCreated = 1,
                PeopleCreated = 1,
                LinksCreated = 1
            };

            crossImportServiceMock
                .Setup(s => s.ImportAsync(It.IsAny<List<VehiclePersonCrossImportItemDto>>(), It.IsAny<ClaimsPrincipal>(), It.IsAny<HttpContext>()))
                .ReturnsAsync(expectedResult);

            var items = new List<VehiclePersonCrossImportItemDto>
            {
                new VehiclePersonCrossImportItemDto { PlateNumber = "29C-12345", PersonCode = "NS01" }
            };

            // Act
            var result = await _controller.CrossImport(items, crossImportServiceMock.Object);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = (OkObjectResult)result;
            var response = okResult.Value as ApiResponse<CrossImportResultDto>;
            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().BeEquivalentTo(expectedResult);
        }
    }
}
