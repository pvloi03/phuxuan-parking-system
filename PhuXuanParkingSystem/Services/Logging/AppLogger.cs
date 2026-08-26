using System;
using System.Configuration;
using System.IO;
using System.Threading;
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

        /// <summary>
        /// Thông báo thân thiện, an toàn dành cho bảo vệ / người vận hành (không lộ stack trace)
        /// </summary>
        public string UserFriendlyMessage { get; }

        public LogMessageEventArgs(DateTime timestamp, LogEventLevel level, string message, Exception? exception = null, string? sourceContext = null)
        {
            Timestamp = timestamp;
            Level = level;
            Message = message;
            Exception = exception;
            SourceContext = sourceContext;
            UserFriendlyMessage = GenerateUserFriendlyMessage(level, message);
        }

        private static string GenerateUserFriendlyMessage(LogEventLevel level, string rawMessage)
        {
            switch (level)
            {
                case LogEventLevel.Fatal:
                    return "Hệ thống gặp sự cố nghiêm trọng. Vui lòng liên hệ kỹ thuật viên để kiểm tra.";
                case LogEventLevel.Error:
                    return $"Đã xảy ra lỗi trong quá trình xử lý: {rawMessage}";
                case LogEventLevel.Warning:
                    return $"Cảnh báo hệ thống: {rawMessage}";
                default:
                    return rawMessage;
            }
        }

        public string FormattedText => $"[{Timestamp:HH:mm:ss}] [{Level}] {(string.IsNullOrEmpty(SourceContext) ? "" : $"[{SourceContext}] ")}{Message}";
    }

    /// <summary>
    /// Custom Enricher bổ sung ThreadId vào mỗi dòng Log mà không cần cài thêm package ngoài
    /// </summary>
    public class ThreadIdEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ThreadId", Thread.CurrentThread.ManagedThreadId.ToString("D2")));
        }
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
    /// Bộ quản lý ghi log trung tâm chuẩn Enterprise cho PhuXuanParkingSystem
    /// - Ghi file bất đồng bộ (Non-blocking I/O)
    /// - Mỗi ngày 1 file log riêng biệt trong thư mục Logs/
    /// - Tự động dọn dẹp các file log quá 30 ngày
    /// - Thông báo lỗi lên UI thân thiện, bảo mật, không lộ stack trace
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
        /// Khởi tạo hệ thống Logging chuyên nghiệp
        /// </summary>
        public static void Initialize(string? customLogLevel = null, string? customLogFolder = null)
        {
            lock (_initLock)
            {
                if (_isInitialized) return;

                // 1. Mức độ log từ App.config (mặc định Warning cho Production)
                string levelStr = customLogLevel
                    ?? ConfigurationManager.AppSettings["LogLevel"]
                    ?? "Warning";

                if (!Enum.TryParse(levelStr, true, out LogEventLevel minimumLevel))
                {
                    minimumLevel = LogEventLevel.Warning;
                }

                // 2. Thư mục lưu file log (mặc định: Logs/)
                string logFolder = customLogFolder
                    ?? ConfigurationManager.AppSettings["Log_Directory"]
                    ?? "Logs";

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string fullLogDirectory = Path.IsPathRooted(logFolder)
                    ? logFolder
                    : Path.Combine(baseDir, logFolder);

                if (!Directory.Exists(fullLogDirectory))
                {
                    Directory.CreateDirectory(fullLogDirectory);
                }

                // 3. Số ngày lưu giữ log (mặc định 30 ngày)
                int retainedDays = 30;
                if (int.TryParse(ConfigurationManager.AppSettings["Log_RetainedDays"], out int days) && days > 0)
                {
                    retainedDays = days;
                }

                // 4. Chủ động dọn dẹp các file log cũ hơn 30 ngày trong thư mục Logs
                CleanupOldLogFiles(fullLogDirectory, retainedDays);

                // 5. Cấu trúc tên file log mỗi ngày 1 file: Logs/app-yyyy-MM-dd.log
                string logFilePathFormat = Path.Combine(fullLogDirectory, "app-.log");

                long fileSizeBytes = 50L * 1024 * 1024; // 50MB
                if (long.TryParse(ConfigurationManager.AppSettings["Log_FileSizeLimitMB"], out long mb) && mb > 0)
                {
                    fileSizeBytes = mb * 1024 * 1024;
                }

                // Template ghi log chuyên nghiệp chuẩn Enterprise
                const string outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [T:{ThreadId}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

                // 6. Cấu hình Serilog Logger
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Is(minimumLevel)
                    .Enrich.With(new ThreadIdEnricher())
                    .WriteTo.Sink(new WinFormsEventSink())
                    .WriteTo.Async(a => a.File(
                        path: logFilePathFormat,
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

        /// <summary>
        /// Quét thư mục log và xóa các file log cũ hơn số ngày quy định (mặc định 30 ngày)
        /// </summary>
        public static int CleanupOldLogFiles(string logDirectory, int daysToKeep = 30)
        {
            int deletedCount = 0;
            try
            {
                if (!Directory.Exists(logDirectory)) return 0;

                DateTime thresholdDate = DateTime.Now.AddDays(-daysToKeep);
                string[] logFiles = Directory.GetFiles(logDirectory, "*.log");

                foreach (string file in logFiles)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.LastWriteTime < thresholdDate)
                        {
                            fileInfo.Delete();
                            deletedCount++;
                        }
                    }
                    catch
                    {
                        // File có thể đang bị mở bởi tiến trình khác
                    }
                }
            }
            catch
            {
                // Tránh để lỗi dọn dẹp log làm ảnh hưởng khởi động
            }

            return deletedCount;
        }

        public static void RaiseLogEmitted(LogMessageEventArgs args)
        {
            try
            {
                OnLogEmitted?.Invoke(null, args);
            }
            catch
            {
                // Tránh lỗi từ UI subscriber làm crash logger
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
        /// Đóng và flush toàn bộ log buffer trước khi tắt ứng dụng
        /// </summary>
        public static void CloseAndFlush()
        {
            Log.CloseAndFlush();
            _isInitialized = false;
        }
    }
}
