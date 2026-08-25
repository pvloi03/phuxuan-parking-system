using FluentAssertions;
using HPParkingThaiThuy.Models.Entities;
using Humanizer;
using Xunit;

namespace HPParkingThaiThuy.Tests.Repositories
{
    public class MongoRepositoryUnitTests
    {
        [Theory]
        [InlineData(typeof(ParkingSession), "ParkingSessions")]
        [InlineData(typeof(Vehicle), "Vehicles")]
        [InlineData(typeof(Person), "People")] // Bất quy tắc tiếng Anh
        [InlineData(typeof(Department), "Departments")]
        [InlineData(typeof(Company), "Companies")] // Tận cùng bằng y -> ies
        [InlineData(typeof(Contractor), "Contractors")]
        [InlineData(typeof(Lane), "Lanes")]
        [InlineData(typeof(Device), "Devices")]
        [InlineData(typeof(User), "Users")]
        public void Humanizer_Pluralize_ShouldDeriveCorrectCollectionName(System.Type entityType, string expectedCollectionName)
        {
            // Act
            string collectionName = entityType.Name.Pluralize();

            // Assert
            collectionName.Should().Be(expectedCollectionName);
        }
    }
}
