using FluentAssertions;
using HPParkingSystem.Api.Services;
using HPParkingSystem.Models.Entities;
using HPParkingSystem.Repositories;
using Moq;
using System.Linq.Expressions;

namespace HPParkingSystem.Api.Tests.Services
{
    public class OrganizationLookupServiceTests
    {
        private readonly Mock<IRepository<Department>> _deptRepoMock;
        private readonly Mock<IRepository<Company>> _companyRepoMock;
        private readonly Mock<IRepository<Contractor>> _contractorRepoMock;

        public OrganizationLookupServiceTests()
        {
            _deptRepoMock = new Mock<IRepository<Department>>();
            _companyRepoMock = new Mock<IRepository<Company>>();
            _contractorRepoMock = new Mock<IRepository<Contractor>>();
        }

        [Fact]
        public async Task LoadCatalogAsync_ResolvesEntitiesByIdCodeOrName_CaseInsensitive()
        {
            // Arrange
            _deptRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Department>
                {
                    new Department { Id = "dept-1", Name = "Phòng Kế Toán", Code = "PKT" }
                });

            _companyRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Company, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Company>
                {
                    new Company { Id = "comp-1", Name = "Công ty CP Phú Xuân", Code = "PX" }
                });

            _contractorRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Contractor, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Contractor>
                {
                    new Contractor { Id = "contr-1", Name = "Nhà thầu Nam Cường", Code = "NC" }
                });

            var service = new OrganizationLookupService(
                _deptRepoMock.Object,
                _companyRepoMock.Object,
                _contractorRepoMock.Object);

            // Act
            var catalog = await service.LoadCatalogAsync();

            // Assert Department
            catalog.ResolveDepartmentId("dept-1", null).Should().Be("dept-1");
            catalog.ResolveDepartmentId(null, "Phòng Kế Toán").Should().Be("dept-1");
            catalog.ResolveDepartmentId(null, "  phòng kế toán  ").Should().Be("dept-1");
            catalog.ResolveDepartmentId("pkt", null).Should().Be("dept-1");
            catalog.ResolveDepartmentId("unknown", null).Should().BeNull();

            // Assert Company
            catalog.ResolveCompanyId("comp-1", null).Should().Be("comp-1");
            catalog.ResolveCompanyId(null, "Công ty CP Phú Xuân").Should().Be("comp-1");
            catalog.ResolveCompanyId("px", null).Should().Be("comp-1");
            catalog.ResolveCompanyId("unknown", null).Should().BeNull();

            // Assert Contractor
            catalog.ResolveContractorId("contr-1", null).Should().Be("contr-1");
            catalog.ResolveContractorId(null, "Nhà thầu Nam Cường").Should().Be("contr-1");
            catalog.ResolveContractorId("nc", null).Should().Be("contr-1");
            catalog.ResolveContractorId("unknown", null).Should().BeNull();
        }

        [Fact]
        public async Task LoadCatalogAsync_WhenInputIsNullOrEmpty_ReturnsNull()
        {
            // Arrange
            _deptRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Department, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Department>());
            _companyRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Company, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Company>());
            _contractorRepoMock.Setup(r => r.FindAsync(It.IsAny<Expression<Func<Contractor, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Contractor>());

            var service = new OrganizationLookupService(
                _deptRepoMock.Object,
                _companyRepoMock.Object,
                _contractorRepoMock.Object);

            // Act
            var catalog = await service.LoadCatalogAsync();

            // Assert
            catalog.ResolveDepartmentId(null, null).Should().BeNull();
            catalog.ResolveDepartmentId("  ", "").Should().BeNull();
            catalog.ResolveCompanyId(null, null).Should().BeNull();
            catalog.ResolveContractorId(null, null).Should().BeNull();
        }
    }
}
