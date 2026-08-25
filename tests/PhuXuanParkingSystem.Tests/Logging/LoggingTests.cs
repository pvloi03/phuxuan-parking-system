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
            _testLogDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestLogs");
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
            // Arrange
            string logPath = Path.Combine(_testLogDir, "test-.log");

            // Act
            Action act = () => AppLogger.Initialize("Debug", logPath);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void AppLogger_LogWarning_ShouldRaiseOnLogEmittedEvent()
        {
            // Arrange
            string logPath = Path.Combine(_testLogDir, "event-test-.log");
            AppLogger.Initialize("Debug", logPath);

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
        public void AppLogger_LogErrorWithException_ShouldIncludeExceptionInEvent()
        {
            // Arrange
            string logPath = Path.Combine(_testLogDir, "error-test-.log");
            AppLogger.Initialize("Debug", logPath);

            LogMessageEventArgs? receivedArgs = null;
            EventHandler<LogMessageEventArgs> handler = (s, e) => receivedArgs = e;
            AppLogger.OnLogEmitted += handler;

            var testException = new InvalidOperationException("Camera IP unreachable");

            try
            {
                // Act
                AppLogger.Error(testException, "Failed to connect to Hikvision camera", "Hikvision");

                // Assert
                receivedArgs.Should().NotBeNull();
                receivedArgs!.Level.Should().Be(LogEventLevel.Error);
                receivedArgs.Message.Should().Contain("Failed to connect to Hikvision camera");
                receivedArgs.Exception.Should().Be(testException);
                receivedArgs.FormattedText.Should().Contain("Camera IP unreachable");
            }
            finally
            {
                AppLogger.OnLogEmitted -= handler;
            }
        }

        [Fact]
        public void LogMessageEventArgs_FormattedText_ShouldFormatCorrectly()
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
            text.Should().Contain("Socket timeout");
        }
    }
}
