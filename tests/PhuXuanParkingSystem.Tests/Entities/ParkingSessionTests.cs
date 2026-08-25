using FluentAssertions;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using System;
using Xunit;

namespace PhuXuanParkingSystem.Tests.Entities
{
    public class ParkingSessionTests
    {
        [Fact]
        public void CheckIn_ShouldInitializeActiveSession_WithCleanPlateAndTimestamps()
        {
            // Arrange & Act
            var session = ParkingSession.CheckIn(
                inLaneId: "Lane_In_01",
                plateNumber: "29A-123.45",
                inOverviewImagePath: @"C:\Captures\2026\08\25\in_ov.jpg",
                inPlateImagePath: @"C:\Captures\2026\08\25\in_pl.jpg",
                personId: "P001",
                personName: "Nguyễn Văn A",
                departmentName: "Kỹ Thuật",
                vehicleType: VehicleType.Car,
                note: "Khách VIP"
            );

            // Assert
            session.Status.Should().Be(ParkingSessionStatus.Active);
            session.InLaneId.Should().Be("Lane_In_01");
            session.PlateNumber.Should().Be("29A12345"); // Cleaned
            session.PersonName.Should().Be("Nguyễn Văn A");
            session.DepartmentName.Should().Be("Kỹ Thuật");
            session.InTime.Should().NotBeNull();
            session.InTime.Value.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(2));
            session.OutTime.Should().BeNull();
            session.OutLaneId.Should().BeNull();
            session.IsUnknown.Should().BeFalse();
            session.Duration.Should().BeNull();
            session.IsDeleted.Should().BeFalse();
        }

        [Fact]
        public void CheckOut_ShouldSetCompletedStatus_AndCalculateDuration()
        {
            // Arrange
            var session = ParkingSession.CheckIn(
                inLaneId: "Lane_In_01",
                plateNumber: "29A-123.45",
                inOverviewImagePath: "in_ov.jpg",
                inPlateImagePath: "in_pl.jpg"
            );
            session.InTime = DateTime.Now.AddHours(-2).AddMinutes(-30); // Giả lập vào cách đây 2.5 giờ

            // Act
            session.CheckOut(
                outLaneId: "Lane_Out_01",
                outOverviewImagePath: "out_ov.jpg",
                outPlateImagePath: "out_pl.jpg",
                note: "Thu phí tiền mặt"
            );

            // Assert
            session.Status.Should().Be(ParkingSessionStatus.Completed);
            session.OutLaneId.Should().Be("Lane_Out_01");
            session.OutOverviewImagePath.Should().Be("out_ov.jpg");
            session.OutPlateImagePath.Should().Be("out_pl.jpg");
            session.OutTime.Should().NotBeNull();
            session.OutTime.Value.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(2));
            session.UpdatedAt.Should().NotBeNull();
            session.Note.Should().Be("Thu phí tiền mặt");

            session.Duration.Should().NotBeNull();
            session.Duration.Value.TotalMinutes.Should().BeApproximately(150, 1.0);
        }

        [Fact]
        public void CheckOut_WhenNoteAlreadyExists_ShouldAppendNote()
        {
            // Arrange
            var session = ParkingSession.CheckIn(
                inLaneId: "Lane_In_01",
                plateNumber: "29A-123.45",
                inOverviewImagePath: "in_ov.jpg",
                inPlateImagePath: "in_pl.jpg",
                note: "Vào ban ngày"
            );

            // Act
            session.CheckOut(
                outLaneId: "Lane_Out_01",
                outOverviewImagePath: "out_ov.jpg",
                outPlateImagePath: "out_pl.jpg",
                note: "Ra ca chiều"
            );

            // Assert
            session.Note.Should().Be("Vào ban ngày; Ra ca chiều");
        }

        [Fact]
        public void CreateUnmatchedOut_ShouldSetUnmatchedOutStatus_WithoutInTime()
        {
            // Arrange & Act
            var session = ParkingSession.CreateUnmatchedOut(
                outLaneId: "Lane_Out_01",
                plateNumber: "30F-999.99",
                outOverviewImagePath: "out_ov.jpg",
                outPlateImagePath: "out_pl.jpg",
                personName: "Khách vãng lai",
                vehicleType: VehicleType.Motorcycle,
                note: "Không tìm thấy dữ liệu xe vào"
            );

            // Assert
            session.Status.Should().Be(ParkingSessionStatus.UnmatchedOut);
            session.PlateNumber.Should().Be("30F99999");
            session.InTime.Should().BeNull();
            session.InLaneId.Should().BeNull();
            session.OutTime.Should().NotBeNull();
            session.OutLaneId.Should().Be("Lane_Out_01");
            session.Duration.Should().BeNull();
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("   ", true)]
        [InlineData("Trần Thị B", false)]
        public void IsUnknown_ShouldDependOnPersonName(string? personName, bool expectedIsUnknown)
        {
            // Arrange
            var session = new ParkingSession { PersonName = personName };

            // Assert
            session.IsUnknown.Should().Be(expectedIsUnknown);
        }
    }
}
