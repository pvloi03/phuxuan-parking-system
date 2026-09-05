using FluentAssertions;
using PhuXuanParkingSystem.Models.Common;
using System;
using Xunit;

namespace PhuXuanParkingSystem.Tests.Common
{
    public class TestEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
    }

    public class BaseEntityTests
    {
        [Fact]
        public void NewEntity_ShouldHaveDefaultValues()
        {
            // Act
            var entity = new TestEntity { Name = "Test" };

            // Assert
            entity.Id.Should().NotBeNullOrWhiteSpace();
            MongoDB.Bson.ObjectId.TryParse(entity.Id, out _).Should().BeTrue();
            entity.IsDeleted.Should().BeFalse();
            entity.DeletedAt.Should().BeNull();
            entity.UpdatedAt.Should().BeNull();
            entity.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void MarkDeleted_ShouldSetIsDeletedTrue_AndSetTimestamps()
        {
            // Arrange
            var entity = new TestEntity { Name = "DeleteMe" };

            // Act
            entity.MarkDeleted();

            // Assert
            entity.IsDeleted.Should().BeTrue();
            entity.DeletedAt.Should().NotBeNull();
            entity.DeletedAt!.Value.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(2));
            entity.UpdatedAt.Should().NotBeNull();
            entity.UpdatedAt!.Value.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void Restore_ShouldResetIsDeletedFalse_AndClearDeletedAt()
        {
            // Arrange
            var entity = new TestEntity { Name = "RestoreMe" };
            entity.MarkDeleted();

            // Act
            entity.Restore();

            // Assert
            entity.IsDeleted.Should().BeFalse();
            entity.DeletedAt.Should().BeNull();
            entity.UpdatedAt.Should().NotBeNull();
        }
    }
}
