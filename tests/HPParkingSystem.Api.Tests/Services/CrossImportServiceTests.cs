using FluentAssertions;
using HPParkingSystem.Api.DTOs;
using HPParkingSystem.Api.Services;
using HPParkingSystem.Models.Entities;
using HPParkingSystem.Models.Enums;
using HPParkingSystem.Repositories;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace HPParkingSystem.Api.Tests.Services
{
    public class CrossImportServiceTests
    {
        private readonly Mock<IRepository<Person>> _personRepoMock;
        private readonly Mock<IRepository<Vehicle>> _vehicleRepoMock;
        private readonly Mock<IRepository<Department>> _deptRepoMock;
        private readonly Mock<IRepository<Company>> _companyRepoMock;
        private readonly Mock<IRepository<Contractor>> _contractorRepoMock;
        private readonly Mock<IAuditLogQueue> _auditQueueMock;

        private readonly List<Person> _peopleInDb;
        private readonly List<Vehicle> _vehiclesInDb;
        private readonly List<Department> _deptsInDb;
        private readonly List<Company> _companiesInDb;
        private readonly List<Contractor> _contractorsInDb;

        public CrossImportServiceTests()
        {
            _personRepoMock = new Mock<IRepository<Person>>();
            _vehicleRepoMock = new Mock<IRepository<Vehicle>>();
            _deptRepoMock = new Mock<IRepository<Department>>();
            _companyRepoMock = new Mock<IRepository<Company>>();
            _contractorRepoMock = new Mock<IRepository<Contractor>>();
            _auditQueueMock = new Mock<IAuditLogQueue>();

            _peopleInDb = new List<Person>();
            _vehiclesInDb = new List<Vehicle>();
            _deptsInDb = new List<Department>
            {
                new Department { Id = "dept1", Name = "Phòng Kỹ Thuật", Code = "PKT" }
            };
            _companiesInDb = new List<Company>
            {
                new Company { Id = "comp1", Name = "Công ty CP Phú Xuân", Code = "PX" }
            };
            _contractorsInDb = new List<Contractor>
            {
                new Contractor { Id = "contr1", Name = "Nhà thầu Nam Cường", Code = "NC" }
            };

            // Setup Person Mock
            _personRepoMock.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Person, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<Person, bool>> predicate, CancellationToken _) =>
                    _peopleInDb.AsQueryable().FirstOrDefault(predicate));

            _personRepoMock.Setup(r => r.AddAsync(It.IsAny<Person>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Person p, CancellationToken _) =>
                {
                    if (string.IsNullOrEmpty(p.Id)) p.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
                    _peopleInDb.Add(p);
                    return p;
                });

            _personRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Person>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Person p, CancellationToken _) =>
                {
                    var idx = _peopleInDb.FindIndex(x => x.Id == p.Id);
                    if (idx >= 0) _peopleInDb[idx] = p;
                    return true;
                });

            // Setup Vehicle Mock
            _vehicleRepoMock.Setup(r => r.FindOneAsync(It.IsAny<Expression<Func<Vehicle, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Expression<Func<Vehicle, bool>> predicate, CancellationToken _) =>
                    _vehiclesInDb.AsQueryable().FirstOrDefault(predicate));

            _vehicleRepoMock.Setup(r => r.AddAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Vehicle v, CancellationToken _) =>
                {
                    if (string.IsNullOrEmpty(v.Id)) v.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
                    _vehiclesInDb.Add(v);
                    return v;
                });

            _vehicleRepoMock.Setup(r => r.UpdateAsync(It.IsAny<Vehicle>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Vehicle v, CancellationToken _) =>
                {
                    var idx = _vehiclesInDb.FindIndex(x => x.Id == v.Id);
                    if (idx >= 0) _vehiclesInDb[idx] = v;
                    return true;
                });

            // Setup Lookup Repos
            _deptRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_deptsInDb);

            _companyRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Company, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_companiesInDb);

            _contractorRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Contractor, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(_contractorsInDb);
        }

        private ICrossImportService CreateService()
        {
            var orgLookup = new OrganizationLookupService(
                _deptRepoMock.Object,
                _companyRepoMock.Object,
                _contractorRepoMock.Object);

            return new CrossImportService(
                _personRepoMock.Object,
                _vehicleRepoMock.Object,
                orgLookup,
                _auditQueueMock.Object);
        }

        [Fact]
        public async Task ImportAsync_WithBothPersonAndVehicle_CreatesAndLinksBoth()
        {
            // Arrange
            var service = CreateService();
            var items = new List<VehiclePersonCrossImportItemDto>
            {
                new VehiclePersonCrossImportItemDto
                {
                    PersonCode = "NS001",
                    FullName = "Nguyễn Văn A",
                    PhoneNumber = "0901234567",
                    PlateNumber = "29C-123.45",
                    VehicleType = VehicleType.Truck,
                    Weight = 15.5
                }
            };

            // Act
            var result = await service.ImportAsync(items);

            // Assert
            result.TotalRows.Should().Be(1);
            result.PeopleCreated.Should().Be(1);
            result.VehiclesCreated.Should().Be(1);
            result.LinksCreated.Should().Be(1);

            _peopleInDb.Should().HaveCount(1);
            var person = _peopleInDb[0];
            person.Code.Should().Be("NS001");
            person.FullName.Should().Be("Nguyễn Văn A");

            _vehiclesInDb.Should().HaveCount(1);
            var vehicle = _vehiclesInDb[0];
            vehicle.PlateNumber.Should().Be("29C12345");
            vehicle.Type.Should().Be(VehicleType.Truck);
            vehicle.Weight.Should().Be(15.5);
            vehicle.OwnerPersonId.Should().Be(person.Id);
        }

        [Fact]
        public async Task ImportAsync_WithOnlyPerson_CreatesPersonWithoutVehicle()
        {
            // Arrange
            var service = CreateService();
            var items = new List<VehiclePersonCrossImportItemDto>
            {
                new VehiclePersonCrossImportItemDto
                {
                    PersonCode = "NS002",
                    FullName = "Trần Thị B",
                    PhoneNumber = "0912345678"
                }
            };

            // Act
            var result = await service.ImportAsync(items);

            // Assert
            result.TotalRows.Should().Be(1);
            result.PeopleCreated.Should().Be(1);
            result.VehiclesCreated.Should().Be(0);
            result.LinksCreated.Should().Be(0);

            _peopleInDb.Should().HaveCount(1);
            _vehiclesInDb.Should().BeEmpty();
        }

        [Fact]
        public async Task ImportAsync_WithOnlyVehicle_CreatesVehicleWithoutOwner()
        {
            // Arrange
            var service = CreateService();
            var items = new List<VehiclePersonCrossImportItemDto>
            {
                new VehiclePersonCrossImportItemDto
                {
                    PlateNumber = "30H-999.88",
                    VehicleType = VehicleType.Car
                }
            };

            // Act
            var result = await service.ImportAsync(items);

            // Assert
            result.TotalRows.Should().Be(1);
            result.VehiclesCreated.Should().Be(1);
            result.PeopleCreated.Should().Be(0);
            result.LinksCreated.Should().Be(0);

            _vehiclesInDb.Should().HaveCount(1);
            _vehiclesInDb[0].PlateNumber.Should().Be("30H99988");
            _vehiclesInDb[0].OwnerPersonId.Should().BeNull();
        }

        [Fact]
        public async Task ImportAsync_WithExistingPersonAndVehicle_UpdatesBothAndLinks()
        {
            // Arrange
            var existingPerson = new Person
            {
                Id = "person_old",
                Code = "NS001",
                FullName = "Nguyễn Văn Cũ",
                PhoneNumber = "0111"
            };
            _peopleInDb.Add(existingPerson);

            var existingVehicle = new Vehicle
            {
                Id = "vehicle_old",
                PlateNumber = "29C12345",
                Type = VehicleType.Car,
                Weight = 2.0,
                OwnerPersonId = null
            };
            _vehiclesInDb.Add(existingVehicle);

            var service = CreateService();
            var items = new List<VehiclePersonCrossImportItemDto>
            {
                new VehiclePersonCrossImportItemDto
                {
                    PersonCode = "NS001",
                    FullName = "Nguyễn Văn Mới",
                    PhoneNumber = "0999888777",
                    PlateNumber = "29C-12345",
                    VehicleType = VehicleType.Truck,
                    Weight = 18.0
                }
            };

            // Act
            var result = await service.ImportAsync(items);

            // Assert
            result.PeopleUpdated.Should().Be(1);
            result.VehiclesUpdated.Should().Be(1);
            result.LinksCreated.Should().Be(1);

            existingPerson.FullName.Should().Be("Nguyễn Văn Mới");
            existingPerson.PhoneNumber.Should().Be("0999888777");

            existingVehicle.Type.Should().Be(VehicleType.Truck);
            existingVehicle.Weight.Should().Be(18.0);
            existingVehicle.OwnerPersonId.Should().Be("person_old");
        }

        [Fact]
        public async Task ImportAsync_WithOrgNames_ResolvesIdsCorrectly()
        {
            // Arrange
            var service = CreateService();
            var items = new List<VehiclePersonCrossImportItemDto>
            {
                new VehiclePersonCrossImportItemDto
                {
                    PersonCode = "NS003",
                    FullName = "Lê Văn C",
                    DepartmentName = "Phòng Kỹ Thuật",
                    CompanyName = "Công ty CP Phú Xuân"
                }
            };

            // Act
            var result = await service.ImportAsync(items);

            // Assert
            result.PeopleCreated.Should().Be(1);
            _peopleInDb.Should().HaveCount(1);
            _peopleInDb[0].DepartmentId.Should().Be("dept1");
            _peopleInDb[0].CompanyId.Should().Be("comp1");
        }

        [Fact]
        public async Task ImportAsync_WithInvalidPlateNumber_ReportsErrorAndDoesNotCreateVehicle()
        {
            // Arrange
            var service = CreateService();
            var items = new List<VehiclePersonCrossImportItemDto>
            {
                new VehiclePersonCrossImportItemDto
                {
                    PersonCode = "NS004",
                    FullName = "Phạm Văn D",
                    PlateNumber = "--" // Ký tự lạ, rỗng sau clean
                }
            };

            // Act
            var result = await service.ImportAsync(items);

            // Assert
            result.TotalRows.Should().Be(1);
            result.PeopleCreated.Should().Be(1);
            result.VehiclesCreated.Should().Be(0);
            result.LinksCreated.Should().Be(0);
            result.Errors.Should().HaveCount(1);
            result.Errors[0].Should().Contain("Biển số xe '--' không hợp lệ");

            _vehiclesInDb.Should().BeEmpty();
            _peopleInDb.Should().HaveCount(1);
        }

        [Fact]
        public async Task ImportAsync_WhenCreatingPersonAndVehicle_QueuesAuditLogsForBoth()
        {
            // Arrange
            var service = CreateService();
            var items = new List<VehiclePersonCrossImportItemDto>
            {
                new VehiclePersonCrossImportItemDto
                {
                    PersonCode = "NS_AUDIT_1",
                    FullName = "Kiểm Toán Viên 1",
                    PlateNumber = "29A88888"
                }
            };

            // Act
            var result = await service.ImportAsync(items);

            // Assert
            result.PeopleCreated.Should().Be(1);
            result.VehiclesCreated.Should().Be(1);

            _auditQueueMock.Verify(
                q => q.QueueLogAsync(
                    It.Is<AuditLog>(l => l.ActionType == AuditActionType.Create && l.TargetEntity == "Person" && l.TargetDisplay == "Kiểm Toán Viên 1"),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _auditQueueMock.Verify(
                q => q.QueueLogAsync(
                    It.Is<AuditLog>(l => l.ActionType == AuditActionType.Create && l.TargetEntity == "Vehicle" && l.TargetDisplay == "29A88888"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ImportAsync_WhenUpdatingExistingPersonAndVehicle_QueuesUpdateAuditLogsWithDiff()
        {
            // Arrange
            var initialPerson = Person.Create("NS_AUDIT_2", "Tên Ban Đầu", PersonType.Employee, null, null, null, null, null, true);
            initialPerson.Id = "p-audit-2";
            _peopleInDb.Add(initialPerson);

            var initialVehicle = Vehicle.Create("30E99999", VehicleType.Car, initialPerson.Id, 1.5, true);
            initialVehicle.Id = "v-audit-2";
            _vehiclesInDb.Add(initialVehicle);

            var service = CreateService();
            var items = new List<VehiclePersonCrossImportItemDto>
            {
                new VehiclePersonCrossImportItemDto
                {
                    PersonCode = "NS_AUDIT_2",
                    FullName = "Tên Mới Sau Khi Cập Nhật",
                    PlateNumber = "30E99999",
                    Weight = 2.8
                }
            };

            // Act
            var result = await service.ImportAsync(items);

            // Assert
            result.PeopleUpdated.Should().Be(1);
            result.VehiclesUpdated.Should().Be(1);

            _auditQueueMock.Verify(
                q => q.QueueLogAsync(
                    It.Is<AuditLog>(l => l.ActionType == AuditActionType.Update && l.TargetEntity == "Person" && l.ChangedProperties.Contains("FullName")),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _auditQueueMock.Verify(
                q => q.QueueLogAsync(
                    It.Is<AuditLog>(l => l.ActionType == AuditActionType.Update && l.TargetEntity == "Vehicle" && l.ChangedProperties.Contains("Weight")),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ImportAsync_WhenPersonMissingCode_ReportsErrorAndDoesNotCreatePerson()
        {
            // Arrange
            var service = CreateService();
            var items = new List<VehiclePersonCrossImportItemDto>
            {
                new VehiclePersonCrossImportItemDto
                {
                    FullName = "Trần Thị E" // Thiếu PersonCode
                }
            };

            // Act
            var result = await service.ImportAsync(items);

            // Assert
            result.TotalRows.Should().Be(1);
            result.PeopleCreated.Should().Be(0);
            result.Errors.Should().HaveCount(1);
            result.Errors[0].Should().Contain("thiếu mã định danh bắt buộc (PersonCode)");
            _peopleInDb.Should().BeEmpty();
        }

        [Fact]
        public async Task ImportAsync_WhenNewPersonMissingFullName_ReportsErrorAndDoesNotCreatePerson()
        {
            // Arrange
            var service = CreateService();
            var items = new List<VehiclePersonCrossImportItemDto>
            {
                new VehiclePersonCrossImportItemDto
                {
                    PersonCode = "NS_NO_NAME" // Thiếu FullName và chưa có trong DB
                }
            };

            // Act
            var result = await service.ImportAsync(items);

            // Assert
            result.TotalRows.Should().Be(1);
            result.PeopleCreated.Should().Be(0);
            result.Errors.Should().HaveCount(1);
            result.Errors[0].Should().Contain("thiếu họ và tên bắt buộc (FullName)");
            _peopleInDb.Should().BeEmpty();
        }

        [Fact]
        public async Task ImportAsync_WhenPersonHasErrorAndRowHasNewVehicle_AbortsVehicleOperationAndEnforcesRowLevelAtomicity()
        {
            // Arrange
            var service = CreateService();
            var items = new List<VehiclePersonCrossImportItemDto>
            {
                new VehiclePersonCrossImportItemDto
                {
                    FullName = "Người Chưa Có Mã", // Thiếu PersonCode
                    PlateNumber = "29A11111",
                    VehicleType = VehicleType.Car
                }
            };

            // Act
            var result = await service.ImportAsync(items);

            // Assert
            result.TotalRows.Should().Be(1);
            result.PeopleCreated.Should().Be(0);
            result.VehiclesCreated.Should().Be(0);
            result.LinksCreated.Should().Be(0);
            result.Errors.Should().HaveCount(2);
            result.Errors.Should().Contain(e => e.Contains("thiếu mã định danh bắt buộc (PersonCode)"));
            result.Errors.Should().Contain(e => e.Contains("Row-level Atomicity"));

            _vehiclesInDb.Should().BeEmpty();
            _peopleInDb.Should().BeEmpty();
        }

        [Fact]
        public async Task ImportAsync_WhenPersonHasErrorAndRowHasExistingVehicle_DoesNotModifyVehicleOwnerOrAttributes()
        {
            // Arrange
            var existingVehicle = Vehicle.Create("30E99999", VehicleType.Car, "owner-original", 1.5, true);
            existingVehicle.Id = "v-orig-1";
            _vehiclesInDb.Add(existingVehicle);

            var service = CreateService();
            var items = new List<VehiclePersonCrossImportItemDto>
            {
                new VehiclePersonCrossImportItemDto
                {
                    PersonCode = "NS_NO_NAME_NEW", // Thiếu FullName, chưa có trong DB
                    PlateNumber = "30E99999",
                    Weight = 9.9
                }
            };

            // Act
            var result = await service.ImportAsync(items);

            // Assert
            result.TotalRows.Should().Be(1);
            result.PeopleCreated.Should().Be(0);
            result.VehiclesUpdated.Should().Be(0);
            result.Errors.Should().HaveCount(2);
            result.Errors.Should().Contain(e => e.Contains("thiếu họ và tên bắt buộc (FullName)"));
            result.Errors.Should().Contain(e => e.Contains("Row-level Atomicity"));

            // Vehicle trong DB phải giữ nguyên 100% dữ liệu gốc
            existingVehicle.Weight.Should().Be(1.5);
            existingVehicle.OwnerPersonId.Should().Be("owner-original");
        }
    }
}
