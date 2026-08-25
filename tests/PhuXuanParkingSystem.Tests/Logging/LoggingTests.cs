using System;
using System.IO;
using FluentAssertions;
using PhuXuanParkingSystem.Services.Logging;
using Serilog.Events;
using Xunit;

namespace PhuXuanParkingSystem.Tests.Logging
{
    public class LoggingTests : IDisposable
    {
        private readonly string _testLogDir;

        public LoggingTests()
        {
            _testLogDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestLogs_" + Guid.NewGuid().ToString("N"));
            if (!Directory.Exists(_testLogDir))
            {
                Directory.CreateDirectory(_testLogDir);
            }
        }

        public void Dispose()
        {
            AppLogger.CloseAndFlush();
            try
            {
                if (Directory.Exists(_testLogDir))
                {
                    Directory.Delete(_testLogDir, true);
                }
            }
            catch
            {
                // Ignore cleanup errors in test teardown
            }
        }

        [Fact]
        public void AppLogger_Initialize_ShouldInitializeSuccessfully()
        {
            // Act
            Action act = () => AppLogger.Initialize("Debug", _testLogDir);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void AppLogger_LogWarning_ShouldRaiseOnLogEmittedEvent()
        {
            // Arrange
            AppLogger.Initialize("Debug", _testLogDir);

            LogMessageEventArgs? receivedArgs = null;
            EventHandler<LogMessageEventArgs> handler = (s, e) => receivedArgs = e;
            AppLogger.OnLogEmitted += handler;

            try
            {
                // Act
                AppLogger.Warning("Sensor AUX1 Disconnected", "Controller");

                // Assert
                receivedArgs.Should().NotBeNull();
                receivedArgs!.Level.Should().Be(LogEventLevel.Warning);
                receivedArgs.Message.Should().Contain("Sensor AUX1 Disconnected");
                receivedArgs.SourceContext.Should().Be("Controller");
            }
            finally
            {
                AppLogger.OnLogEmitted -= handler;
            }
        }

        [Fact]
        public void AppLogger_LogErrorWithException_ShouldIncludeExceptionInTechnicalDetailsAndSafeUserMessage()
        {
            // Arrange
            AppLogger.Initialize("Debug", _testLogDir);

            LogMessageEventArgs? receivedArgs = null;
            EventHandler<LogMessageEventArgs> handler = (s, e) => receivedArgs = e;
            AppLogger.OnLogEmitted += handler;

            var testException = new InvalidOperationException("Camera IP unreachable at 192.168.1.61");

            try
            {
                // Act
                AppLogger.Error(testException, "Failed to connect to Hikvision camera", "Hikvision");

                // Assert
                receivedArgs.Should().NotBeNull();
                receivedArgs!.Level.Should().Be(LogEventLevel.Error);
                receivedArgs.Message.Should().Contain("Failed to connect to Hikvision camera");
                receivedArgs.Exception.Should().Be(testException);

                // User-friendly message must not contain raw internal exception trace
                receivedArgs.UserFriendlyMessage.Should().NotBeNullOrEmpty();
                receivedArgs.UserFriendlyMessage.Should().Contain("Đã xảy ra lỗi");
            }
            finally
            {
                AppLogger.OnLogEmitted -= handler;
            }
        }

        [Fact]
        public void CleanupOldLogFiles_ShouldDeleteFilesOlderThanRetentionDays()
        {
            // Arrange: Tạo 1 file cũ 35 ngày và 1 file mới 5 ngày
            string oldFile = Path.Combine(_testLogDir, "app-2026-07-20.log");
            string recentFile = Path.Combine(_testLogDir, "app-2026-08-20.log");

            File.WriteAllText(oldFile, "Old log content from 35 days ago");
            File.WriteAllText(recentFile, "Recent log content from 5 days ago");

            File.SetLastWriteTime(oldFile, DateTime.Now.AddDays(-35));
            File.SetLastWriteTime(recentFile, DateTime.Now.AddDays(-5));

            // Act: Dọn dẹp log quá 30 ngày
            int deleted = AppLogger.CleanupOldLogFiles(_testLogDir, 30);

            // Assert
            deleted.Should().Be(1);
            File.Exists(oldFile).Should().BeFalse("File cũ hơn 30 ngày phải bị xóa");
            File.Exists(recentFile).Should().BeTrue("File trong vòng 30 ngày phải được giữ lại");
        }

        [Fact]
        public void LogMessageEventArgs_FormattedText_ShouldFormatCleanly()
        {
            // Arrange
            var timestamp = new DateTime(2026, 8, 25, 14, 30, 0);
            var args = new LogMessageEventArgs(
                timestamp,
                LogEventLevel.Warning,
                "Radar timeout",
                new Exception("Socket timeout"),
                "ZKTeco"
            );

            // Act
            string text = args.FormattedText;

            // Assert
            text.Should().Contain("[14:30:00]");
            text.Should().Contain("[Warning]");
            text.Should().Contain("[ZKTeco]");
            text.Should().Contain("Radar timeout");
        }
    }
}
