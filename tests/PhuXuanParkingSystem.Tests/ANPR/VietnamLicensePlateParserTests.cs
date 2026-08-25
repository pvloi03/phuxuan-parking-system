using System;
using System.Collections.Generic;
using System.Drawing;
using FluentAssertions;
using PhuXuanParkingSystem.Models.ValueObjects;
using PhuXuanParkingSystem.Services.ANPR;
using Xunit;

namespace PhuXuanParkingSystem.Tests.ANPR
{
    public class VietnamLicensePlateParserTests
    {
        [Theory]
        [InlineData("51F-123.45", "51F-123.45")]
        [InlineData("30A-999.99", "30A-999.99")]
        [InlineData("17B-123.45", "17B-123.45")]
        [InlineData("80A-123.45", "80A-123.45")]
        [InlineData("51LD-123.45", "51LD-123.45")]
        [InlineData("29A-1234", "29A-1234")]
        public void Parse_SingleLinePlate_ShouldReturnValidFormattedPlate(string rawText, string expectedPlate)
        {
            // Arrange
            var blocks = new List<OcrTextBlock>
            {
                new OcrTextBlock(rawText, 0.95f, new PointF[] { new PointF(100, 100), new PointF(300, 100), new PointF(300, 150), new PointF(100, 150) }, new RectangleF(100, 100, 200, 50))
            };

            // Act
            var result = VietnamLicensePlateParser.Parse(blocks);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.LicensePlate.Should().Be(expectedPlate);
        }

        [Fact]
        public void Parse_TwoLinePlate_Motorbike_ShouldCombineTopAndBottomLinesCorrectly()
        {
            // Arrange
            var blocks = new List<OcrTextBlock>
            {
                // Dòng trên: 17-B1
                new OcrTextBlock("17-B1", 0.94f, new PointF[] { new PointF(100, 100), new PointF(250, 100), new PointF(250, 140), new PointF(100, 140) }, new RectangleF(100, 100, 150, 40)),
                // Dòng dưới: 123.45
                new OcrTextBlock("123.45", 0.96f, new PointF[] { new PointF(100, 150), new PointF(250, 150), new PointF(250, 190), new PointF(100, 190) }, new RectangleF(100, 150, 150, 40))
            };

            // Act
            var result = VietnamLicensePlateParser.Parse(blocks);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.LicensePlate.Should().Be("17B1-123.45");
            result.Confidence.Should().BeApproximately(0.95, 0.01);
        }

        [Theory]
        [InlineData("5IF-I23.4S", "51F12345")] // I->1, S->5
        [InlineData("298-123.45", "29B12345")] // 8 ở vị trí seri -> B
        public void PositionalSemanticCorrectionSingleLine_ShouldCorrectOcrMistakes(string flawedOcr, string expectedClean)
        {
            // Act
            string fixedText = VietnamLicensePlateParser.ApplyPositionalCorrectionSingleLine(flawedOcr);

            // Assert
            fixedText.Should().Be(expectedClean);
        }

        [Theory]
        [InlineData("OI-HI", "01H1")]          // OI ở mã tỉnh -> 01, HI -> H1
        [InlineData("I7-BI", "17B1")]          // I7 -> 17, BI -> B1
        public void PositionalSemanticCorrectionTopLine_ShouldCorrectOcrMistakes(string flawedOcr, string expectedClean)
        {
            // Act
            string fixedText = VietnamLicensePlateParser.ApplyPositionalCorrectionTopLine(flawedOcr);

            // Assert
            fixedText.Should().Be(expectedClean);
        }

        [Theory]
        [InlineData("HONDA")]
        [InlineData("YAMAHA")]
        [InlineData("TAXI")]
        [InlineData("AIRBLADE")]
        [InlineData("0912345678")] // Số điện thoại quảng cáo
        public void Parse_WhenOnlyBlacklistedWordsPresent_ShouldReturnFailure(string noiseText)
        {
            // Arrange
            var blocks = new List<OcrTextBlock>
            {
                new OcrTextBlock(noiseText, 0.99f, new PointF[0], new RectangleF(10, 10, 100, 30))
            };

            // Act
            var result = VietnamLicensePlateParser.Parse(blocks);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.LicensePlate.Should().BeEmpty();
        }

        [Fact]
        public void Parse_WhenImageContainsBothBrandNameAndRealPlate_ShouldExtractRealPlate()
        {
            // Arrange
            var blocks = new List<OcrTextBlock>
            {
                new OcrTextBlock("HONDA", 0.99f, new PointF[0], new RectangleF(50, 50, 80, 25)),
                new OcrTextBlock("AIRBLADE", 0.98f, new PointF[0], new RectangleF(50, 80, 100, 25)),
                new OcrTextBlock("29H1-678.90", 0.92f, new PointF[0], new RectangleF(100, 200, 200, 50))
            };

            // Act
            var result = VietnamLicensePlateParser.Parse(blocks);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.LicensePlate.Should().Be("29H1-678.90");
        }
    }
}