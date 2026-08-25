using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
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
            // 1. Khởi tạo hệ thống Logging Serilog
            AppLogger.Initialize();

            // 2. Đăng ký Global Unhandled Exception Handlers
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
            {
                AppLogger.Fatal(e.Exception, "Unhandled UI Thread Exception", "WinForms");
                MessageBox.Show($"Đã xảy ra sự cố giao diện: {e.Exception.Message}\nVui lòng kiểm tra file log.", "Lỗi Hệ Thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                AppLogger.Information("Ứng dụng kết thúc. Flushing logs...", "Application");
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
                MessageBox.Show($"Không thể khởi động ứng dụng: {ex.Message}", "Lỗi Khởi Động", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            // Forms
            services.AddTransient<FrmMain>();
        }
    }
}
