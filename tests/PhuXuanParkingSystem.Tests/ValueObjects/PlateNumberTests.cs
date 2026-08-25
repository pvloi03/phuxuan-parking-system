using FluentAssertions;
using PhuXuanParkingSystem.Models.ValueObjects;
using Xunit;

namespace PhuXuanParkingSystem.Tests.ValueObjects
{
    public class PlateNumberTests
    {
        [Theory]
        [InlineData("29A-123.45", "29A12345")]
        [InlineData(" 30F - 999.88 ", "30F99988")]
        [InlineData("14A1234", "14A1234")]
        [InlineData("51h-123.45", "51H12345")]
        [InlineData("29-B1\n678.90", "29B167890")]
        [InlineData("43A-123_45", "43A12345")]
        [InlineData("88-LD-001.22", "88LD00122")]
        public void Clean_ShouldNormalizePlateNumber_Correctly(string input, string expected)
        {
            // Act
            string result = PlateNumber.Clean(input);

            // Assert
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\t\n")]
        public void Clean_WithNullOrWhiteSpace_ShouldReturnEmptyString(string? input)
        {
            // Act
            string result = PlateNumber.Clean(input);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public void ValueObject_Equality_ShouldBeCaseInsensitive()
        {
            // Arrange
            var plate1 = new PlateNumber("29A-123.45");
            var plate2 = new PlateNumber("29a12345");
            var plate3 = new PlateNumber("30F-999.99");

            // Assert
            plate1.Should().Be(plate2);
            (plate1 == plate2).Should().BeTrue();
            (plate1 != plate3).Should().BeTrue();
            plate1.GetHashCode().Should().Be(plate2.GetHashCode());
        }

        [Theory]
        [InlineData("29A12345", "29A-123.45")]
        [InlineData("30F99988", "30F-999.88")]
        [InlineData("51H12345", "51H-123.45")]
        [InlineData("29B167890", "29B1-678.90")]
        [InlineData("14A1234", "14A-1234")]
        [InlineData("88LD00122", "88LD-001.22")]
        [InlineData("29NG12345", "29NG-123.45")]
        [InlineData(null, "")]
        [InlineData("", "")]
        public void FormatDisplay_ShouldFormat_Correctly(string? input, string expected)
        {
            // Act
            string result = PlateNumber.FormatDisplay(input);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void FormattedValue_ShouldReturnFormattedString()
        {
            // Arrange
            var plate = new PlateNumber("29A12345");

            // Assert
            plate.FormattedValue.Should().Be("29A-123.45");
        }

        [Fact]
        public void ImplicitConversions_ShouldWorkSeamlessly()
        {
            // Act
            PlateNumber vo = "29A-123.45";
            string rawStr = vo;

            // Assert
            vo.Value.Should().Be("29A12345");
            rawStr.Should().Be("29A12345");
        }
    }
}
