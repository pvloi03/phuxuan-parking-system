using PhuXuanParkingSystem.Models.Data;
using PhuXuanParkingSystem.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Forms;

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
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 1. Cấu hình DI Container
            var services = new ServiceCollection();
            ConfigureServices(services);

            // 2. Build ServiceProvider
            ServiceProvider = services.BuildServiceProvider();

            // 3. Resolve Form chính và khởi chạy
            var mainForm = ServiceProvider.GetRequiredService<FrmMain>();
            Application.Run(mainForm);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Database Context (Singleton)
            services.AddSingleton(MongoDbContext.Instance);

            // Generic Repository bao quát toàn bộ Entities kế thừa BaseEntity
            services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));

            // Forms
            services.AddTransient<FrmMain>();
        }
    }
}
