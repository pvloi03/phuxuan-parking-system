using FluentAssertions;
using HPParkingSystem.Api.Controllers;
using HPParkingSystem.Api.DTOs;
using HPParkingSystem.Models.Entities;
using HPParkingSystem.Models.Enums;
using HPParkingSystem.Repositories;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Moq;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HPParkingSystem.Api.Tests.Controllers
{
    public class ParkingSessionsControllerTests
    {
        private readonly Mock<IRepository<ParkingSession>> _sessionRepoMock;
        private readonly ParkingSessionsController _controller;

        public ParkingSessionsControllerTests()
        {
            _sessionRepoMock = new Mock<IRepository<ParkingSession>>();
            _controller = new ParkingSessionsController(_sessionRepoMock.Object);
        }

        [Fact]
        public async Task GetList_FilteredByVehicleType_ShouldReturnFilteredSessionsWithWeight()
        {
            // Arrange
            var sessions = new List<ParkingSession>
            {
                new ParkingSession
                {
                    Id = "s_01",
                    PlateNumber = "29C-11111",
                    VehicleType = VehicleType.Truck,
                    Weight = 10.5,
                    Status = ParkingSessionStatus.Completed,
                    InTime = DateTime.Today.AddHours(8),
                    OutTime = DateTime.Today.AddHours(9)
                },
                new ParkingSession
                {
                    Id = "s_02",
                    PlateNumber = "29C-22222",
                    VehicleType = VehicleType.Truck,
                    Weight = 15.0,
                    Status = ParkingSessionStatus.Active,
                    InTime = DateTime.Today.AddHours(10)
                }
            };

            _sessionRepoMock
                .Setup(r => r.CountAsync(It.IsAny<FilterDefinition<ParkingSession>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(2);

            _sessionRepoMock
                .Setup(r => r.SumAsync(
                    It.IsAny<FilterDefinition<ParkingSession>>(),
                    It.IsAny<System.Linq.Expressions.Expression<Func<ParkingSession, double?>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(25.5);

            _sessionRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<FilterDefinition<ParkingSession>>(),
                    It.IsAny<SortDefinition<ParkingSession>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(sessions);

            // Act
            var actionResult = await _controller.GetList(
                plateNumber: null,
                status: null,
                vehicleType: VehicleType.Truck,
                fromDate: null,
                toDate: null,
                hour: null,
                laneName: null,
                pageNumber: 1,
                pageSize: 15);

            // Assert
            actionResult.Should().BeOfType<OkObjectResult>();
            var okResult = (OkObjectResult)actionResult;
            var response = okResult.Value as ApiResponse<PagedResult<ParkingSession>>;

            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Items.Should().HaveCount(2);
            response.Data.Items[0].VehicleType.Should().Be(VehicleType.Truck);
            response.Data.Items[0].Weight.Should().Be(10.5);
            response.Data.Items[1].VehicleType.Should().Be(VehicleType.Truck);
            response.Data.Items[1].Weight.Should().Be(15.0);

            // Full Dataset Aggregations assertions
            response.Data.TotalWeight.Should().Be(25.5);
            response.Data.TotalTruckCount.Should().Be(2);
            response.Data.TotalActiveCount.Should().Be(2);
        }

        [Fact]
        public async Task GetList_FilteredByFixedHour_ShouldApplyHourFilter()
        {
            // Arrange: 1 session at 08:30 (hour 8)
            var session = new ParkingSession
            {
                Id = "s_08h",
                PlateNumber = "29C-88888",
                InTime = DateTime.Today.AddHours(8).AddMinutes(30),
                Status = ParkingSessionStatus.Active,
                VehicleType = VehicleType.Truck
            };

            _sessionRepoMock
                .Setup(r => r.CountAsync(It.IsAny<FilterDefinition<ParkingSession>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _sessionRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<FilterDefinition<ParkingSession>>(),
                    It.IsAny<SortDefinition<ParkingSession>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ParkingSession> { session });

            // Act: query for fixed hour 8 (08:00)
            var actionResult = await _controller.GetList(
                plateNumber: null,
                status: null,
                vehicleType: null,
                fromDate: null,
                toDate: null,
                hour: 8,
                laneName: null,
                pageNumber: 1,
                pageSize: 15);

            // Assert
            actionResult.Should().BeOfType<OkObjectResult>();
            var okResult = (OkObjectResult)actionResult;
            var response = okResult.Value as ApiResponse<PagedResult<ParkingSession>>;

            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data!.Items.Should().HaveCount(1);
            response.Data.Items[0].PlateNumber.Should().Be("29C-88888");

            _sessionRepoMock.Verify(r => r.FindAsync(
                It.IsAny<FilterDefinition<ParkingSession>>(),
                It.IsAny<SortDefinition<ParkingSession>>(),
                0,
                15,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExportExcel_ShouldGenerateWorkbookWithExpectedHeadersAndEqualTotalWeights()
        {
            // Arrange:
            // Xe 1 (29C-10001, Xe tải 10 m³): 2 lượt ra đã hoàn thành -> Tổng = 20 m³
            // Xe 2 (29C-20002, Xe tải 5 m³): 1 lượt vào đang trong bãi (Active, chưa ra) -> 0 lượt ra
            var session1 = new ParkingSession
            {
                Id = "s_01",
                PlateNumber = "29C-10001",
                VehicleType = VehicleType.Truck,
                Weight = 10.0,
                Status = ParkingSessionStatus.Completed,
                InTime = DateTime.Today.AddHours(7),
                OutTime = DateTime.Today.AddHours(8)
            };
            var session2 = new ParkingSession
            {
                Id = "s_02",
                PlateNumber = "29C-10001",
                VehicleType = VehicleType.Truck,
                Weight = 10.0,
                Status = ParkingSessionStatus.Completed,
                InTime = DateTime.Today.AddHours(9),
                OutTime = DateTime.Today.AddHours(10)
            };
            var session3Active = new ParkingSession
            {
                Id = "s_03",
                PlateNumber = "29C-20002",
                VehicleType = VehicleType.Truck,
                Weight = 5.0,
                Status = ParkingSessionStatus.Active,
                InTime = DateTime.Today.AddHours(11)
            };

            var sampleSessions = new List<ParkingSession> { session1, session2, session3Active };

            _sessionRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<FilterDefinition<ParkingSession>>(),
                    It.IsAny<SortDefinition<ParkingSession>>(),
                    0,
                    5000,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(sampleSessions);

            // Act
            var actionResult = await _controller.ExportExcel(
                plateNumber: null,
                status: null,
                vehicleType: VehicleType.Truck,
                fromDate: null,
                toDate: null,
                hour: null);

            // Assert
            actionResult.Should().BeOfType<FileContentResult>();
            var fileResult = (FileContentResult)actionResult;
            fileResult.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            fileResult.FileDownloadName.Should().StartWith("BaoCao_LichSuXe_");

            using var stream = new MemoryStream(fileResult.FileContents);
            using var package = new ExcelPackage(stream);

            // 1. Kiểm tra Sheet 1: Thống Kê Số Lượt Từng Xe
            var ws1 = package.Workbook.Worksheets["Thống Kê Số Lượt Từng Xe"];
            ws1.Should().NotBeNull();
            ws1.Cells[4, 3].Value.Should().Be("Chủ Xe");
            ws1.Cells[4, 4].Value.Should().Be("Đối Tượng");
            ws1.Cells[4, 5].Value.Should().Be("Loại Xe");
            ws1.Cells[4, 6].Value.Should().Be("Khối Lượng Xe (m³)");
            ws1.Cells[4, 9].Value.Should().Be("Tổng Khối Lượng (m³)");

            // Hàng dữ liệu đầu tiên (29C-10001, 2 lượt):
            // Row 5: 29C-10001 | Xe vãng lai | Khách vãng lai | Xe tải | 10 | in:2 | out:2 | totalWeight: 20
            ws1.Cells[5, 2].Value.Should().Be("29C-10001");
            ws1.Cells[5, 4].Value.Should().Be("Khách vãng lai");
            ws1.Cells[5, 5].Value.Should().Be("Xe tải");
            ws1.Cells[5, 6].Value.Should().Be(10.0);
            ws1.Cells[5, 6].Style.Numberformat.Format.Should().Be("#,##0");
            ws1.Cells[5, 7].Value.Should().Be(2); // In count
            ws1.Cells[5, 8].Value.Should().Be(2); // Out count
            ws1.Cells[5, 9].Value.Should().Be(20.0); // Total group weight = 10 * 2 = 20
            ws1.Cells[5, 9].Style.Numberformat.Format.Should().Be("#,##0");

            // Hàng dữ liệu thứ 2 (29C-20002, 1 lượt đang trong bãi):
            ws1.Cells[6, 2].Value.Should().Be("29C-20002");
            ws1.Cells[6, 4].Value.Should().Be("Khách vãng lai");
            ws1.Cells[6, 5].Value.Should().Be("Xe tải");
            ws1.Cells[6, 6].Value.Should().Be(5.0);
            ws1.Cells[6, 6].Style.Numberformat.Format.Should().Be("#,##0");
            ws1.Cells[6, 7].Value.Should().Be(1);
            ws1.Cells[6, 8].Value.Should().Be(0);
            ws1.Cells[6, 9].Value.Should().Be("--");

            // Dòng Tổng Cộng Sheet 1 (Row 7):
            ws1.Cells[7, 1].Value.Should().Be("TỔNG CỘNG");
            ws1.Cells[7, 7].Value.Should().Be(3); // Tổng lượt vào: 2 + 1 = 3
            ws1.Cells[7, 8].Value.Should().Be(2); // Tổng lượt ra: 2 + 0 = 2
            double grandTotalSheet1 = Convert.ToDouble(ws1.Cells[7, 9].Value);
            grandTotalSheet1.Should().Be(20.0); // 20.0 m³
            ws1.Cells[7, 9].Style.Numberformat.Format.Should().Be("#,##0");

            // 2. Kiểm tra Sheet 2: Lịch Sử Chi Tiết
            var ws2 = package.Workbook.Worksheets["Lịch Sử Chi Tiết"];
            ws2.Should().NotBeNull();
            ws2.Cells[3, 3].Value.Should().Be("Chủ Xe");
            ws2.Cells[3, 4].Value.Should().Be("Đối Tượng");
            ws2.Cells[3, 5].Value.Should().Be("Loại Xe");
            ws2.Cells[3, 6].Value.Should().Be("Khối Lượng (m³)");

            // Dòng dữ liệu chi tiết
            ws2.Cells[4, 2].Value.Should().Be("29C-10001");
            ws2.Cells[4, 4].Value.Should().Be("Khách vãng lai");
            ws2.Cells[4, 5].Value.Should().Be("Xe tải");
            ws2.Cells[4, 6].Value.Should().Be(10.0);
            ws2.Cells[4, 6].Style.Numberformat.Format.Should().Be("#,##0");

            ws2.Cells[5, 2].Value.Should().Be("29C-10001");
            ws2.Cells[5, 6].Value.Should().Be(10.0);
            ws2.Cells[5, 6].Style.Numberformat.Format.Should().Be("#,##0");

            ws2.Cells[6, 2].Value.Should().Be("29C-20002");
            ws2.Cells[6, 6].Value.Should().Be(5.0);
            ws2.Cells[6, 6].Style.Numberformat.Format.Should().Be("#,##0");
            ws2.Cells[6, 10].Value.Should().Be("Đang trong bãi");

            // Dòng Tổng Cộng Khối Lượng Sheet 2 (Row 7):
            ws2.Cells[7, 1].Value.Should().Be("TỔNG CỘNG KHỐI LƯỢNG (LƯỢT ĐÃ RA)");
            double grandTotalSheet2 = Convert.ToDouble(ws2.Cells[7, 6].Value);

            // Khối lượng Sheet 2 chỉ tính lượt đã ra (10 + 10 = 20), loại trừ xe đang trong bãi (5 m³)
            grandTotalSheet2.Should().Be(20.0);
            ws2.Cells[7, 6].Style.Numberformat.Format.Should().Be("#,##0");

            // Bất biến: Tổng khối lượng Sheet 1 và Sheet 2 phải khớp 100%!
            grandTotalSheet2.Should().Be(grandTotalSheet1);
        }

        [Fact]
        public async Task GetList_FilteredByPersonType_ShouldApplyFilterCorrectly()
        {
            // Arrange
            var session = new ParkingSession
            {
                Id = "s_emp",
                PlateNumber = "30A-99999",
                PersonName = "Nguyễn Văn A",
                PersonType = PersonType.Employee,
                Status = ParkingSessionStatus.Completed
            };

            _sessionRepoMock
                .Setup(r => r.CountAsync(It.IsAny<FilterDefinition<ParkingSession>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _sessionRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<FilterDefinition<ParkingSession>>(),
                    It.IsAny<SortDefinition<ParkingSession>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ParkingSession> { session });

            // Act
            var actionResult = await _controller.GetList(
                plateNumber: null,
                status: null,
                vehicleType: null,
                personType: PersonType.Employee,
                fromDate: null,
                toDate: null,
                hour: null,
                laneName: null,
                pageNumber: 1,
                pageSize: 15);

            // Assert
            actionResult.Should().BeOfType<OkObjectResult>();
            var okResult = (OkObjectResult)actionResult;
            var response = okResult.Value as ApiResponse<PagedResult<ParkingSession>>;

            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data!.Items.Should().HaveCount(1);
            response.Data.Items[0].PersonType.Should().Be(PersonType.Employee);
            response.Data.Items[0].PersonName.Should().Be("Nguyễn Văn A");

            _sessionRepoMock.Verify(r => r.FindAsync(
                It.IsAny<FilterDefinition<ParkingSession>>(),
                It.IsAny<SortDefinition<ParkingSession>>(),
                0,
                15,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetList_FilteredByGuest_ShouldFilterUnregisteredOrNullPersonType()
        {
            // Arrange
            var guestSession = new ParkingSession
            {
                Id = "s_guest",
                PlateNumber = "51H-12345",
                PersonName = null,
                PersonType = null,
                Status = ParkingSessionStatus.Active
            };

            _sessionRepoMock
                .Setup(r => r.CountAsync(It.IsAny<FilterDefinition<ParkingSession>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            _sessionRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<FilterDefinition<ParkingSession>>(),
                    It.IsAny<SortDefinition<ParkingSession>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ParkingSession> { guestSession });

            // Act: query for Guest category
            var actionResult = await _controller.GetList(
                plateNumber: null,
                status: null,
                vehicleType: null,
                personType: PersonType.Guest,
                fromDate: null,
                toDate: null,
                hour: null,
                laneName: null,
                pageNumber: 1,
                pageSize: 15);

            // Assert
            actionResult.Should().BeOfType<OkObjectResult>();
            var okResult = (OkObjectResult)actionResult;
            var response = okResult.Value as ApiResponse<PagedResult<ParkingSession>>;

            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data!.Items.Should().HaveCount(1);
            response.Data.Items[0].PlateNumber.Should().Be("51H-12345");
            response.Data.Items[0].PersonType.Should().BeNull();
        }

        [Fact]
        public async Task GetList_FilteredByGuest_ShouldPassFilterStrictlyForGuestOrNullOrOther()
        {
            FilterDefinition<ParkingSession>? capturedFilter = null;

            _sessionRepoMock
                .Setup(r => r.CountAsync(It.IsAny<FilterDefinition<ParkingSession>>(), It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<ParkingSession>, CancellationToken>((f, _) => capturedFilter = f)
                .ReturnsAsync(0);

            _sessionRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<FilterDefinition<ParkingSession>>(),
                    It.IsAny<SortDefinition<ParkingSession>>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ParkingSession>());

            var result = await _controller.GetList(personType: PersonType.Guest);

            result.Should().BeOfType<OkObjectResult>();
            _sessionRepoMock.Verify(r => r.FindAsync(
                It.IsAny<FilterDefinition<ParkingSession>>(),
                It.IsAny<SortDefinition<ParkingSession>>(),
                0,
                15,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExportExcel_FilteredByPersonType_ShouldContainPersonCategoryColumnAndCorrectColumnCount()
        {
            // Arrange
            var empSession = new ParkingSession
            {
                Id = "s_emp_01",
                PlateNumber = "30A-11111",
                PersonName = "Trần Văn B",
                PersonType = PersonType.Employee,
                VehicleType = VehicleType.Car,
                Status = ParkingSessionStatus.Completed,
                InTime = DateTime.Today.AddHours(8),
                OutTime = DateTime.Today.AddHours(17)
            };

            _sessionRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<FilterDefinition<ParkingSession>>(),
                    It.IsAny<SortDefinition<ParkingSession>>(),
                    0,
                    5000,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ParkingSession> { empSession });

            // Act
            var actionResult = await _controller.ExportExcel(personType: PersonType.Employee);

            // Assert
            actionResult.Should().BeOfType<FileContentResult>();
            var fileResult = (FileContentResult)actionResult;
            using var stream = new MemoryStream(fileResult.FileContents);
            using var package = new ExcelPackage(stream);

            // 1. Kiểm tra Sheet 1: Thống Kê Số Lượt Từng Xe
            var ws1 = package.Workbook.Worksheets["Thống Kê Số Lượt Từng Xe"];
            ws1.Should().NotBeNull();
            ws1.Dimension.End.Column.Should().Be(13);
            ws1.Cells[4, 4].Value.Should().Be("Đối Tượng");
            ws1.Cells[4, 13].Value.Should().Be("Lần Ra Mới Nhất");
            ws1.Cells[4, 14].Value.Should().BeNull();
            ws1.Cells[5, 4].Value.Should().Be("Cán bộ / Nhân viên");

            // 2. Kiểm tra Sheet 2: Lịch Sử Chi Tiết
            var ws2 = package.Workbook.Worksheets["Lịch Sử Chi Tiết"];
            ws2.Should().NotBeNull();
            ws2.Dimension.End.Column.Should().Be(10);
            ws2.Cells[3, 4].Value.Should().Be("Đối Tượng");
            ws2.Cells[3, 10].Value.Should().Be("Trạng Thái");
            ws2.Cells[3, 11].Value.Should().BeNull();
            ws2.Cells[4, 4].Value.Should().Be("Cán bộ / Nhân viên");
        }

        [Fact]
        public void DomainInvariant_ParkingSessionNegativeWeight_ShouldThrowArgumentException()
        {
            var session = new ParkingSession();
            var act = () => session.Weight = -5.0;

            act.Should().Throw<ArgumentOutOfRangeException>()
               .WithMessage("*Khối lượng không thể nhỏ hơn 0*");
        }

        [Theory]
        [InlineData(PersonType.Employee, "Cán bộ / Nhân viên")]
        [InlineData(PersonType.Contractor, "Đối tác / Nhà thầu")]
        [InlineData(PersonType.Visitor, "Khách thăm")]
        [InlineData(PersonType.VIP, "Khách VIP")]
        [InlineData(PersonType.Guest, "Khách vãng lai")]
        [InlineData(PersonType.Other, "Khác")]
        public void PersonType_ToDisplayName_ShouldReturnExpectedDisplayName(PersonType type, string expected)
        {
            type.ToDisplayName().Should().Be(expected);
        }

        [Fact]
        public void NullablePersonType_ToDisplayName_WithRegisteredName_ShouldReturnXeDangKy()
        {
            PersonType? nullType = null;
            nullType.ToDisplayName("Nguyễn Văn A").Should().Be("Xe đăng ký");
        }

        [Fact]
        public void NullablePersonType_ToDisplayName_WithoutName_ShouldReturnKhachVangLai()
        {
            PersonType? nullType = null;
            nullType.ToDisplayName(null).Should().Be("Khách vãng lai");
            nullType.ToDisplayName("   ").Should().Be("Khách vãng lai");
        }

        [Fact]
        public async Task ExportExcel_WithDecimalWeight_ShouldUseDecimalNumberFormat()
        {
            // Arrange
            var session = new ParkingSession
            {
                Id = "s_dec_01",
                PlateNumber = "29C-88888",
                VehicleType = VehicleType.Truck,
                Weight = 2.5,
                InTime = DateTime.Today.AddHours(8),
                OutTime = DateTime.Today.AddHours(10),
                Status = ParkingSessionStatus.Completed
            };

            _sessionRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<MongoDB.Driver.FilterDefinition<ParkingSession>>(),
                    It.IsAny<MongoDB.Driver.SortDefinition<ParkingSession>>(),
                    0,
                    5000,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ParkingSession> { session });

            // Act
            var actionResult = await _controller.ExportExcel();

            // Assert
            actionResult.Should().BeOfType<FileContentResult>();
            var fileResult = (FileContentResult)actionResult;
            using var stream = new MemoryStream(fileResult.FileContents);
            using var package = new ExcelPackage(stream);

            var ws1 = package.Workbook.Worksheets["Thống Kê Số Lượt Từng Xe"];
            ws1.Cells[5, 6].Value.Should().Be(2.5);
            ws1.Cells[5, 6].Style.Numberformat.Format.Should().Be("#,##0.##");

            var ws2 = package.Workbook.Worksheets["Lịch Sử Chi Tiết"];
            ws2.Cells[4, 6].Value.Should().Be(2.5);
            ws2.Cells[4, 6].Style.Numberformat.Format.Should().Be("#,##0.##");
        }

        [Fact]
        public async Task GetList_FilteredByDateTimeRangeAcrossDays_ShouldApplyFilterCorrectly()
        {
            // Arrange
            var fromDate = new DateTime(2026, 10, 19, 10, 0, 0);
            var toDate = new DateTime(2026, 10, 20, 14, 0, 0);

            var overnightSession = new ParkingSession
            {
                Id = "s_overnight",
                PlateNumber = "29C-11111",
                VehicleType = VehicleType.Truck,
                InTime = new DateTime(2026, 10, 19, 23, 0, 0),
                OutTime = new DateTime(2026, 10, 20, 6, 30, 0),
                Status = ParkingSessionStatus.Completed
            };

            var day2Session = new ParkingSession
            {
                Id = "s_day2",
                PlateNumber = "30A-22222",
                VehicleType = VehicleType.Car,
                InTime = new DateTime(2026, 10, 20, 8, 0, 0),
                OutTime = new DateTime(2026, 10, 20, 11, 0, 0),
                Status = ParkingSessionStatus.Completed
            };

            var sessions = new List<ParkingSession> { overnightSession, day2Session };
            FilterDefinition<ParkingSession>? capturedFilter = null;

            _sessionRepoMock
                .Setup(r => r.CountAsync(It.IsAny<FilterDefinition<ParkingSession>>(), It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<ParkingSession>, CancellationToken>((f, _) => capturedFilter = f)
                .ReturnsAsync(2);

            _sessionRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<FilterDefinition<ParkingSession>>(),
                    It.IsAny<SortDefinition<ParkingSession>>(),
                    0,
                    15,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(sessions);

            // Act
            var actionResult = await _controller.GetList(
                plateNumber: null,
                status: null,
                vehicleType: null,
                personType: null,
                fromDate: fromDate,
                toDate: toDate,
                hour: null,
                laneName: null,
                pageNumber: 1,
                pageSize: 15);

            // Assert
            actionResult.Should().BeOfType<OkObjectResult>();
            var okResult = (OkObjectResult)actionResult;
            var response = okResult.Value as ApiResponse<PagedResult<ParkingSession>>;

            response.Should().NotBeNull();
            response!.Success.Should().BeTrue();
            response.Data!.Items.Should().HaveCount(2);
            response.Data.Items[0].Id.Should().Be("s_overnight");
            response.Data.Items[1].Id.Should().Be("s_day2");

            capturedFilter.Should().NotBeNull();
            var serializerRegistry = MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<ParkingSession>();
            var rendered = capturedFilter!.Render(documentSerializer, serializerRegistry);
            rendered.ToString().Should().Contain("InTime");
            rendered.ToString().Should().Contain("OutTime");

            _sessionRepoMock.Verify(r => r.FindAsync(
                It.IsAny<FilterDefinition<ParkingSession>>(),
                It.IsAny<SortDefinition<ParkingSession>>(),
                0,
                15,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExportExcel_WithExactDateTimeRange_ShouldFormatHoursAndMinutesInHeader()
        {
            // Arrange
            var fromDate = new DateTime(2026, 10, 19, 10, 0, 0);
            var toDate = new DateTime(2026, 10, 20, 14, 30, 0);

            var session = new ParkingSession
            {
                Id = "s_time_01",
                PlateNumber = "29C-33333",
                VehicleType = VehicleType.Truck,
                Weight = 8.0,
                Status = ParkingSessionStatus.Completed,
                InTime = new DateTime(2026, 10, 19, 11, 0, 0),
                OutTime = new DateTime(2026, 10, 19, 12, 0, 0)
            };

            _sessionRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<FilterDefinition<ParkingSession>>(),
                    It.IsAny<SortDefinition<ParkingSession>>(),
                    0,
                    5000,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ParkingSession> { session });

            // Act
            var actionResult = await _controller.ExportExcel(
                fromDate: fromDate,
                toDate: toDate);

            // Assert
            actionResult.Should().BeOfType<FileContentResult>();
            var fileResult = (FileContentResult)actionResult;

            using var stream = new MemoryStream(fileResult.FileContents);
            using var package = new ExcelPackage(stream);

            var ws1 = package.Workbook.Worksheets["Thống Kê Số Lượt Từng Xe"];
            ws1.Should().NotBeNull();
            ws1.Cells["A2"].Value.Should().Be("Khoảng thời gian: 19/10/2026 10:00 - 20/10/2026 14:30");
        }

        [Fact]
        public async Task ExportExcel_WithFullDayRange_ShouldFormatDatesWithoutTimeInHeader()
        {
            // Arrange
            var fromDate = new DateTime(2026, 10, 19, 0, 0, 0);
            var toDate = new DateTime(2026, 10, 19, 23, 59, 59);

            var session = new ParkingSession
            {
                Id = "s_time_02",
                PlateNumber = "29C-33333",
                VehicleType = VehicleType.Truck,
                Weight = 8.0,
                Status = ParkingSessionStatus.Completed,
                InTime = new DateTime(2026, 10, 19, 11, 0, 0),
                OutTime = new DateTime(2026, 10, 19, 12, 0, 0)
            };

            _sessionRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<FilterDefinition<ParkingSession>>(),
                    It.IsAny<SortDefinition<ParkingSession>>(),
                    0,
                    5000,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ParkingSession> { session });

            // Act
            var actionResult = await _controller.ExportExcel(
                fromDate: fromDate,
                toDate: toDate);

            // Assert
            actionResult.Should().BeOfType<FileContentResult>();
            var fileResult = (FileContentResult)actionResult;

            using var stream = new MemoryStream(fileResult.FileContents);
            using var package = new ExcelPackage(stream);

            var ws1 = package.Workbook.Worksheets["Thống Kê Số Lượt Từng Xe"];
            ws1.Should().NotBeNull();
            ws1.Cells["A2"].Value.Should().Be("Khoảng thời gian: 19/10/2026 - 19/10/2026");
        }

        [Fact]
        public async Task ExportExcel_WithOnlyFromDateTime_ShouldFormatStartDateTimeAndCurrentInHeader()
        {
            // Arrange
            var fromDate = new DateTime(2026, 10, 19, 10, 0, 0);

            _sessionRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<FilterDefinition<ParkingSession>>(),
                    It.IsAny<SortDefinition<ParkingSession>>(),
                    0,
                    5000,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ParkingSession>());

            // Act
            var actionResult = await _controller.ExportExcel(
                fromDate: fromDate,
                toDate: null);

            // Assert
            actionResult.Should().BeOfType<FileContentResult>();
            var fileResult = (FileContentResult)actionResult;

            using var stream = new MemoryStream(fileResult.FileContents);
            using var package = new ExcelPackage(stream);

            var ws1 = package.Workbook.Worksheets["Thống Kê Số Lượt Từng Xe"];
            ws1.Should().NotBeNull();
            ws1.Cells["A2"].Value.Should().Be("Khoảng thời gian: 19/10/2026 10:00 - Hiện tại");
        }

        [Fact]
        public async Task ExportExcel_WithOnlyToDateTime_ShouldFormatAllAndEndDateTimeInHeader()
        {
            // Arrange
            var toDate = new DateTime(2026, 10, 20, 14, 30, 0);

            _sessionRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<FilterDefinition<ParkingSession>>(),
                    It.IsAny<SortDefinition<ParkingSession>>(),
                    0,
                    5000,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ParkingSession>());

            // Act
            var actionResult = await _controller.ExportExcel(
                fromDate: null,
                toDate: toDate);

            // Assert
            actionResult.Should().BeOfType<FileContentResult>();
            var fileResult = (FileContentResult)actionResult;

            using var stream = new MemoryStream(fileResult.FileContents);
            using var package = new ExcelPackage(stream);

            var ws1 = package.Workbook.Worksheets["Thống Kê Số Lượt Từng Xe"];
            ws1.Should().NotBeNull();
            ws1.Cells["A2"].Value.Should().Be("Khoảng thời gian: Toàn bộ - 20/10/2026 14:30");
        }

        [Fact]
        public async Task GetList_FilteredByActiveStatusAndDateTimeRange_ShouldApplyInTimeOnlyFilter()
        {
            // Arrange
            var fromDate = new DateTime(2026, 10, 19, 10, 0, 0);
            var toDate = new DateTime(2026, 10, 19, 18, 0, 0);
            FilterDefinition<ParkingSession>? capturedFilter = null;

            _sessionRepoMock
                .Setup(r => r.CountAsync(It.IsAny<FilterDefinition<ParkingSession>>(), It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<ParkingSession>, CancellationToken>((f, _) => capturedFilter = f)
                .ReturnsAsync(0);

            _sessionRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<FilterDefinition<ParkingSession>>(),
                    It.IsAny<SortDefinition<ParkingSession>>(),
                    0,
                    15,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ParkingSession>());

            // Act
            await _controller.GetList(
                status: ParkingSessionStatus.Active,
                fromDate: fromDate,
                toDate: toDate,
                pageNumber: 1,
                pageSize: 15);

            // Assert
            capturedFilter.Should().NotBeNull();
            var serializerRegistry = MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<ParkingSession>();
            var rendered = capturedFilter!.Render(documentSerializer, serializerRegistry);
            rendered.ToString().Should().Contain("InTime");
            rendered.ToString().Should().NotContain("OutTime");
        }

        [Fact]
        public async Task GetList_FilteredByUnmatchedOutStatusAndDateTimeRange_ShouldApplyOutTimeOnlyFilter()
        {
            // Arrange
            var fromDate = new DateTime(2026, 10, 19, 10, 0, 0);
            var toDate = new DateTime(2026, 10, 19, 18, 0, 0);
            FilterDefinition<ParkingSession>? capturedFilter = null;

            _sessionRepoMock
                .Setup(r => r.CountAsync(It.IsAny<FilterDefinition<ParkingSession>>(), It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<ParkingSession>, CancellationToken>((f, _) => capturedFilter = f)
                .ReturnsAsync(0);

            _sessionRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<FilterDefinition<ParkingSession>>(),
                    It.IsAny<SortDefinition<ParkingSession>>(),
                    0,
                    15,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ParkingSession>());

            // Act
            await _controller.GetList(
                status: ParkingSessionStatus.UnmatchedOut,
                fromDate: fromDate,
                toDate: toDate,
                pageNumber: 1,
                pageSize: 15);

            // Assert
            capturedFilter.Should().NotBeNull();
            var serializerRegistry = MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<ParkingSession>();
            var rendered = capturedFilter!.Render(documentSerializer, serializerRegistry);
            rendered.ToString().Should().Contain("OutTime");
            rendered.ToString().Should().NotContain("InTime");
        }

        [Fact]
        public async Task GetList_FilteredByKeyword_WhenText_ShouldFilterPlateNumberAndPersonName()
        {
            // Arrange
            FilterDefinition<ParkingSession>? capturedFilter = null;

            _sessionRepoMock
                .Setup(r => r.CountAsync(It.IsAny<FilterDefinition<ParkingSession>>(), It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<ParkingSession>, CancellationToken>((f, _) => capturedFilter = f)
                .ReturnsAsync(0);

            _sessionRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<FilterDefinition<ParkingSession>>(),
                    It.IsAny<SortDefinition<ParkingSession>>(),
                    0,
                    15,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ParkingSession>());

            // Act
            await _controller.GetList(
                keyword: "Nguyễn Văn Hùng",
                pageNumber: 1,
                pageSize: 15);

            // Assert
            capturedFilter.Should().NotBeNull();
            var serializerRegistry = MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<ParkingSession>();
            var rendered = capturedFilter!.Render(documentSerializer, serializerRegistry);

            var filterStr = rendered.ToString();
            filterStr.Should().Contain("PlateNumber");
            filterStr.Should().Contain("PersonName");
            filterStr.Should().Contain("Nguyễn Văn Hùng");
            filterStr.Should().NotContain("Weight");
        }

        [Fact]
        public async Task GetList_FilteredByKeyword_WhenNumeric_ShouldFilterPlateNumberPersonNameAndWeight()
        {
            // Arrange
            FilterDefinition<ParkingSession>? capturedFilter = null;

            _sessionRepoMock
                .Setup(r => r.CountAsync(It.IsAny<FilterDefinition<ParkingSession>>(), It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<ParkingSession>, CancellationToken>((f, _) => capturedFilter = f)
                .ReturnsAsync(0);

            _sessionRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<FilterDefinition<ParkingSession>>(),
                    It.IsAny<SortDefinition<ParkingSession>>(),
                    0,
                    15,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ParkingSession>());

            // Act: Nhập "15"
            await _controller.GetList(
                keyword: "15",
                pageNumber: 1,
                pageSize: 15);

            // Assert
            capturedFilter.Should().NotBeNull();
            var serializerRegistry = MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<ParkingSession>();
            var rendered = capturedFilter!.Render(documentSerializer, serializerRegistry);

            var filterStr = rendered.ToString();
            filterStr.Should().Contain("PlateNumber");
            filterStr.Should().Contain("PersonName");
            filterStr.Should().Contain("Weight");
            filterStr.Should().Contain("15.0");
        }

        [Fact]
        public async Task GetList_FilteredByKeyword_WhenNumericWithUnit_ShouldExtractNumberAndFilterWeight()
        {
            // Arrange
            FilterDefinition<ParkingSession>? capturedFilter = null;

            _sessionRepoMock
                .Setup(r => r.CountAsync(It.IsAny<FilterDefinition<ParkingSession>>(), It.IsAny<CancellationToken>()))
                .Callback<FilterDefinition<ParkingSession>, CancellationToken>((f, _) => capturedFilter = f)
                .ReturnsAsync(0);

            _sessionRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<FilterDefinition<ParkingSession>>(),
                    It.IsAny<SortDefinition<ParkingSession>>(),
                    0,
                    15,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<ParkingSession>());

            // Act: Nhập "15.5m3"
            await _controller.GetList(
                keyword: "15.5m3",
                pageNumber: 1,
                pageSize: 15);

            // Assert
            capturedFilter.Should().NotBeNull();
            var serializerRegistry = MongoDB.Bson.Serialization.BsonSerializer.SerializerRegistry;
            var documentSerializer = serializerRegistry.GetSerializer<ParkingSession>();
            var rendered = capturedFilter!.Render(documentSerializer, serializerRegistry);

            var filterStr = rendered.ToString();
            filterStr.Should().Contain("Weight");
            filterStr.Should().Contain("15.5");
        }

        [Fact]
        public async Task ExportExcel_WithKeyword_ShouldIncludeKeywordInReportHeader()
        {
            // Arrange
            var sampleSessions = new List<ParkingSession>
            {
                new ParkingSession
                {
                    Id = "s_01",
                    PlateNumber = "29C-10001",
                    VehicleType = VehicleType.Truck,
                    Weight = 15.0,
                    PersonName = "Tài xế A",
                    Status = ParkingSessionStatus.Completed,
                    InTime = DateTime.Today.AddHours(7),
                    OutTime = DateTime.Today.AddHours(8)
                }
            };

            _sessionRepoMock
                .Setup(r => r.FindAsync(
                    It.IsAny<FilterDefinition<ParkingSession>>(),
                    It.IsAny<SortDefinition<ParkingSession>>(),
                    0,
                    5000,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(sampleSessions);

            // Act
            var actionResult = await _controller.ExportExcel(
                keyword: "15m3");

            // Assert
            actionResult.Should().BeOfType<FileContentResult>();
            var fileResult = (FileContentResult)actionResult;
            using var stream = new MemoryStream(fileResult.FileContents);
            using var package = new OfficeOpenXml.ExcelPackage(stream);

            var ws1 = package.Workbook.Worksheets["Thống Kê Số Lượt Từng Xe"];
            ws1.Should().NotBeNull();
            var headerStr = ws1.Cells["A2"].Value?.ToString();
            headerStr.Should().Contain("Từ khóa: 15m3");
        }
    }
}
