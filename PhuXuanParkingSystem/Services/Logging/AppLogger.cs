using System;
using System.Configuration;
using System.IO;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace PhuXuanParkingSystem.Services.Logging
{
    /// <summary>
    /// Model dữ liệu sự kiện Log phát ra cho giao diện WinForms
    /// </summary>
    public class LogMessageEventArgs : EventArgs
    {
        public DateTime Timestamp { get; }
        public LogEventLevel Level { get; }
        public string Message { get; }
        public Exception? Exception { get; }
        public string? SourceContext { get; }

        public LogMessageEventArgs(DateTime timestamp, LogEventLevel level, string message, Exception? exception = null, string? sourceContext = null)
        {
            Timestamp = timestamp;
            Level = level;
            Message = message;
            Exception = exception;
            SourceContext = sourceContext;
        }

        public string FormattedText => $"[{Timestamp:HH:mm:ss}] [{Level}] {(string.IsNullOrEmpty(SourceContext) ? "" : $"[{SourceContext}] ")}{Message}{(Exception != null ? $"\nException: {Exception.Message}" : "")}";
    }

    /// <summary>
    /// Sink tùy biến bắn sự kiện log thời gian thực lên giao diện WinForms
    /// </summary>
    public class WinFormsEventSink : ILogEventSink
    {
        private readonly IFormatProvider? _formatProvider;

        public WinFormsEventSink(IFormatProvider? formatProvider = null)
        {
            _formatProvider = formatProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null) return;

            string message = logEvent.RenderMessage(_formatProvider);
            string? sourceContext = null;
            if (logEvent.Properties.TryGetValue("SourceContext", out var propertyValue))
            {
                sourceContext = propertyValue.ToString().Trim('\"');
            }

            var args = new LogMessageEventArgs(
                logEvent.Timestamp.LocalDateTime,
                logEvent.Level,
                message,
                logEvent.Exception,
                sourceContext
            );

            AppLogger.RaiseLogEmitted(args);
        }
    }

    /// <summary>
    /// Bộ quản lý ghi log trung tâm toàn hệ thống dựa trên Serilog
    /// </summary>
    public static class AppLogger
    {
        private static bool _isInitialized;
        private static readonly object _initLock = new object();

        /// <summary>
        /// Sự kiện phát ra mỗi khi có log mới (dành cho UI WinForms lắng nghe)
        /// </summary>
        public static event EventHandler<LogMessageEventArgs>? OnLogEmitted;

        /// <summary>
        /// Khởi tạo Logger cấu hình từ App.config
        /// </summary>
        public static void Initialize(string? customLogLevel = null, string? customLogPath = null)
        {
            lock (_initLock)
            {
                if (_isInitialized) return;

                // 1. Đọc LogLevel từ App.config hoặc tham số truyền vào
                string levelStr = customLogLevel 
                    ?? ConfigurationManager.AppSettings["LogLevel"] 
                    ?? "Warning";

                if (!Enum.TryParse(levelStr, true, out LogEventLevel minimumLevel))
                {
                    minimumLevel = LogEventLevel.Warning;
                }

                // 2. Đọc đường dẫn lưu log
                string logPath = customLogPath 
                    ?? ConfigurationManager.AppSettings["Log_Path"] 
                    ?? "Logs/app-.log";

                // Đảm bảo thư mục Logs tồn tại
                string? dir = Path.GetDirectoryName(logPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // 3. Đọc số ngày lưu giữ log và kích thước tối đa
                int retainedDays = 30;
                if (int.TryParse(ConfigurationManager.AppSettings["Log_RetainedDays"], out int days) && days > 0)
                {
                    retainedDays = days;
                }

                long fileSizeBytes = 50L * 1024 * 1024; // 50MB
                if (long.TryParse(ConfigurationManager.AppSettings["Log_FileSizeLimitMB"], out long mb) && mb > 0)
                {
                    fileSizeBytes = mb * 1024 * 1024;
                }

                const string outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

                // 4. Thiết lập Serilog Logger với Async Rolling File Sink và UI Sink
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Is(minimumLevel)
                    .Enrich.FromLogContext()
                    .WriteTo.Sink(new WinFormsEventSink())
                    .WriteTo.Async(a => a.File(
                        path: logPath,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: retainedDays,
                        fileSizeLimitBytes: fileSizeBytes,
                        rollOnFileSizeLimit: true,
                        outputTemplate: outputTemplate,
                        buffered: false
                    ))
                    .CreateLogger();

                _isInitialized = true;
            }
        }

        public static void RaiseLogEmitted(LogMessageEventArgs args)
        {
            try
            {
                OnLogEmitted?.Invoke(null, args);
            }
            catch
            {
                // Tránh lỗi từ handler UI làm crash logger
            }
        }

        public static void Debug(string message, string? source = null)
        {
            var logger = string.IsNullOrEmpty(source) ? Log.Logger : Log.ForContext("SourceContext", source);
            logger.Debug(message);
        }

        public static void Information(string message, string? source = null)
        {
            var logger = string.IsNullOrEmpty(source) ? Log.Logger : Log.ForContext("SourceContext", source);
            logger.Information(message);
        }

        public static void Warning(string message, string? source = null)
        {
            var logger = string.IsNullOrEmpty(source) ? Log.Logger : Log.ForContext("SourceContext", source);
            logger.Warning(message);
        }

        public static void Warning(Exception ex, string message, string? source = null)
        {
            var logger = string.IsNullOrEmpty(source) ? Log.Logger : Log.ForContext("SourceContext", source);
            logger.Warning(ex, message);
        }

        public static void Error(string message, string? source = null)
        {
            var logger = string.IsNullOrEmpty(source) ? Log.Logger : Log.ForContext("SourceContext", source);
            logger.Error(message);
        }

        public static void Error(Exception ex, string message, string? source = null)
        {
            var logger = string.IsNullOrEmpty(source) ? Log.Logger : Log.ForContext("SourceContext", source);
            logger.Error(ex, message);
        }

        public static void Fatal(Exception ex, string message, string? source = null)
        {
            var logger = string.IsNullOrEmpty(source) ? Log.Logger : Log.ForContext("SourceContext", source);
            logger.Fatal(ex, message);
        }

        /// <summary>
        /// Đóng và flush toàn bộ log buffer trước khi tắt app
        /// </summary>
        public static void CloseAndFlush()
        {
            Log.CloseAndFlush();
            _isInitialized = false;
        }
    }
}
