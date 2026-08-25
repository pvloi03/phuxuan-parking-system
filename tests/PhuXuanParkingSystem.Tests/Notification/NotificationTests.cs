using System;
using FluentAssertions;
using PhuXuanParkingSystem.Services.Notification;
using Xunit;

namespace PhuXuanParkingSystem.Tests.Notification
{
    public class NotificationTests
    {
        [Fact]
        public void Notify_ShouldRaiseOnNotificationReceivedEvent_WithCorrectProperties()
        {
            // Arrange
            AppNotification? received = null;
            EventHandler<AppNotification> handler = (s, e) => received = e;
            AppNotificationService.OnNotificationReceived += handler;

            try
            {
                // Act
                AppNotificationService.Notify(
                    NotificationType.Success,
                    NotificationCategory.Camera,
                    "Camera Toàn Cảnh",
                    "Đã kết nối thành công 192.168.1.61",
                    "192.168.1.61"
                );

                // Assert
                received.Should().NotBeNull();
                received!.Type.Should().Be(NotificationType.Success);
                received.Category.Should().Be(NotificationCategory.Camera);
                received.Title.Should().Be("Camera Toàn Cảnh");
                received.Message.Should().Be("Đã kết nối thành công 192.168.1.61");
                received.Data.Should().Be("192.168.1.61");
                received.Id.Should().NotBeEmpty();
                received.Timestamp.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(2));
            }
            finally
            {
                AppNotificationService.OnNotificationReceived -= handler;
            }
        }

        [Theory]
        [InlineData(NotificationType.Success, "🟢")]
        [InlineData(NotificationType.Info, "ℹ️")]
        [InlineData(NotificationType.Warning, "⚠️")]
        [InlineData(NotificationType.Error, "❌")]
        public void FormattedSummary_ShouldContainAppropriateIconAndContent(NotificationType type, string expectedIcon)
        {
            // Arrange
            var notification = new AppNotification(
                type,
                NotificationCategory.LaneIn,
                "Phát hiện xe",
                "Radar Làn Vào kích hoạt"
            );

            // Act
            string summary = notification.FormattedSummary;

            // Assert
            summary.Should().Contain(expectedIcon);
            summary.Should().Contain("LaneIn");
            summary.Should().Contain("Phát hiện xe");
            summary.Should().Contain("Radar Làn Vào kích hoạt");
        }

        [Fact]
        public void HelperMethods_ShouldSetCorrectNotificationTypes()
        {
            // Arrange
            AppNotification? lastNotification = null;
            EventHandler<AppNotification> handler = (s, e) => lastNotification = e;
            AppNotificationService.OnNotificationReceived += handler;

            try
            {
                // Act & Assert Success
                AppNotificationService.NotifySuccess(NotificationCategory.Controller, "ZKTeco", "Kết nối OK");
                lastNotification!.Type.Should().Be(NotificationType.Success);

                // Act & Assert Info
                AppNotificationService.NotifyInfo(NotificationCategory.LaneIn, "Xe vào", "Radar kích hoạt");
                lastNotification!.Type.Should().Be(NotificationType.Info);

                // Act & Assert Warning
                AppNotificationService.NotifyWarning(NotificationCategory.Database, "MongoDB", "Chưa kết nối DB");
                lastNotification!.Type.Should().Be(NotificationType.Warning);

                // Act & Assert Error
                AppNotificationService.NotifyError(NotificationCategory.Camera, "Camera Biển Số", "Mất tín hiệu");
                lastNotification!.Type.Should().Be(NotificationType.Error);
            }
            finally
            {
                AppNotificationService.OnNotificationReceived -= handler;
            }
        }

        [Fact]
        public void Notify_WhenSubscriberThrowsException_ShouldNotThrowOrCrashCaller()
        {
            // Arrange: Subscriber cố tình ném lỗi
            EventHandler<AppNotification> faultyHandler = (s, e) => throw new InvalidOperationException("UI Render Crash Simulation");
            AppNotificationService.OnNotificationReceived += faultyHandler;

            try
            {
                // Act
                Action act = () => AppNotificationService.NotifyError(NotificationCategory.System, "Lỗi Test", "Thông báo thử nghiệm");

                // Assert
                act.Should().NotThrow("AppNotificationService phải exception-safe, không được để crash caller");
            }
            finally
            {
                AppNotificationService.OnNotificationReceived -= faultyHandler;
            }
        }
    }
}