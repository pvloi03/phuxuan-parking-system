using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using PhuXuanParkingSystem.Forms;
using PhuXuanParkingSystem.Models.Data;
using PhuXuanParkingSystem.Repositories;
using PhuXuanParkingSystem.Services.Logging;
using Serilog;

namespace PhuXuanParkingSystem
{
    internal static class Program
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 1. Khởi tạo hệ thống Logging chuyên nghiệp (Mỗi ngày 1 file, tự động dọn log > 30 ngày)
            AppLogger.Initialize();

            // 2. Đăng ký Global Unhandled Exception Handlers (Không lộ chi tiết kỹ thuật lên UI)
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            Application.ThreadException += (s, e) =>
            {
                // Ghi log chi tiết kỹ thuật (Stack trace, inner exception) vào file log
                AppLogger.Fatal(e.Exception, "Unhandled UI Thread Exception", "WinForms");

                // Thông báo thân thiện lên UI cho người vận hành
                MessageBox.Show(
                    "Hệ thống đã ghi nhận một sự cố bất thường.\nLiên hệ bộ phận kỹ thuật nếu sự cố tiếp tục xảy ra.",
                    "Thông Báo Hệ Thống",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    AppLogger.Fatal(ex, "Unhandled AppDomain Exception", "AppDomain");
                }
                else
                {
                    AppLogger.Fatal(new Exception("Unknown non-exception object thrown"), $"Unhandled Object: {e.ExceptionObject}", "AppDomain");
                }
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                AppLogger.Error(e.Exception, "Unobserved Task Exception", "TaskScheduler");
                e.SetObserved();
            };

            Application.ApplicationExit += (s, e) =>
            {
                AppLogger.Information("Ứng dụng kết thúc. Đang lưu toàn bộ nhật ký...", "Application");
                AppLogger.CloseAndFlush();
            };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                // 3. Cấu hình DI Container
                var services = new ServiceCollection();
                ConfigureServices(services);

                // 4. Build ServiceProvider
                ServiceProvider = services.BuildServiceProvider();

                AppLogger.Information("Khởi động ứng dụng PhuXuanParkingSystem thành công.", "Application");

                // 5. Resolve Form chính và khởi chạy
                var mainForm = ServiceProvider.GetRequiredService<FrmMain>();
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                AppLogger.Fatal(ex, "Lỗi nghiêm trọng khi khởi chạy ứng dụng", "Program");
                MessageBox.Show(
                    "Không thể khởi động hệ thống bãi xe.\nVui lòng kiểm tra lại kết nối cơ sở dữ liệu và liên hệ bộ phận kỹ thuật.",
                    "Lỗi Khởi Động",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                AppLogger.CloseAndFlush();
            }
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Logging (Serilog ILogger)
            services.AddSingleton<ILogger>(Log.Logger);

            // Database Context (Singleton)
            services.AddSingleton(MongoDbContext.Instance);

            // Generic Repository bao quát toàn bộ Entities kế thừa BaseEntity
            services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));

            // ANPR License Plate Recognition Service (SimpleLPR3 x86 Engine)
            services.AddSingleton<Services.Anpr.IPlateRecognitionService, Services.Anpr.SimpleLprAnprService>();

            // Business Coordinator Service (Điều phối nghiệp vụ làn xe vào/ra)
            services.AddScoped<Services.Parking.IParkingLaneService, Services.Parking.ParkingLaneService>();

            // Device Health Monitor Service (Giám sát thiết bị & đồng bộ Web Admin)
            services.AddSingleton<Services.DeviceHealth.IDeviceAdapterFactory, Services.DeviceHealth.DeviceAdapterFactory>();
            services.AddSingleton<Services.DeviceHealth.IDeviceHealthMonitorService, Services.DeviceHealth.DeviceHealthMonitorService>();

            // Device Config Service (Nạp cấu hình động từ MongoDB + Reload khi Web Admin thay đổi)
            services.AddSingleton<Services.DeviceConfig.IDeviceConfigService, Services.DeviceConfig.DeviceConfigService>();

            // Forms
            services.AddTransient<FrmMain>();
            services.AddTransient<FrmDeviceMonitor>();
        }
    }
}
