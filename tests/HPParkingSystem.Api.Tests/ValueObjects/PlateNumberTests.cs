using FluentAssertions;
using HPParkingSystem.Models.Entities;
using HPParkingSystem.Models.ValueObjects;
using System;
using Xunit;

namespace HPParkingSystem.Api.Tests.ValueObjects
{
    public class PlateNumberTests
    {
        [Theory]
        [InlineData("29A-123.45")]
        [InlineData("29A12345")]
        [InlineData("29B1-123.45")]
        [InlineData("35B263371")]
        [InlineData("30H-999.88")]
        [InlineData("51D12345")]
        [InlineData("88LD-001.22")]
        [InlineData("29A-1234")]
        [InlineData("QA1234")]
        public void IsValid_WithValidPlateFormats_ShouldReturnTrue(string plate)
        {
            PlateNumber.IsValid(plate).Should().BeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("--")]
        [InlineData("...")]
        [InlineData("---")]
        [InlineData("12345")]
        [InlineData("ABCDE")]
        [InlineData("29A")]
        [InlineData("29A1234567890123")]
        [InlineData("LOI_NHAN_DIEN")]
        [InlineData("Lỗi nhận diện")]
        [InlineData("Không đọc được")]
        public void IsValid_WithInvalidPlates_ShouldReturnFalse(string? plate)
        {
            PlateNumber.IsValid(plate).Should().BeFalse();
        }

        [Fact]
        public void Vehicle_SettingInvalidPlateNumber_ShouldThrowArgumentException()
        {
            var vehicle = new Vehicle();
            var act = () => vehicle.PlateNumber = "--";

            act.Should().Throw<ArgumentException>()
               .WithMessage("*Biển số xe không hợp lệ*");
        }

        [Fact]
        public void Vehicle_SettingValidPlateNumber_ShouldCleanAndSet()
        {
            var vehicle = new Vehicle();
            vehicle.PlateNumber = "29C-123.45";

            vehicle.PlateNumber.Should().Be("29C12345");
        }
    }
}
