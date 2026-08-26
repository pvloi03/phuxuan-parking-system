using Microsoft.Extensions.DependencyInjection;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Repositories;
using PhuXuanParkingSystem.Services.Anpr;
using PhuXuanParkingSystem.Services.DeviceConfig;
using PhuXuanParkingSystem.Services.DeviceHealth;
using PhuXuanParkingSystem.Services.Logging;
using PhuXuanParkingSystem.Services.Notification;
using PhuXuanParkingSystem.Services.Parking;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhuXuanParkingSystem.Forms
{
    public enum DeviceConnectionState
    {
        Connecting,
        Connected,
        Failed
    }

    /// <summary>
    /// Giao diện chính Hệ Thống Kiểm Soát Bãi Xe (WinForms Presentation Layer)
    /// Đã được tái cấu trúc Clean Architecture: Tách Camera (FrmMain.Cameras.cs),
    /// Điều phối làn (FrmMain.LaneControl.cs) và Nghiệp vụ (ParkingLaneService.cs).
    /// </summary>
    public partial class FrmMain : Form
    {
        // ── Dịch vụ Nghiệp vụ & Thiết bị ─────────────────────────────────────
        private readonly IParkingLaneService _parkingLaneService;
        private readonly IPlateRecognitionService _anprService;
        private readonly IRepository<Device> _deviceRepo;
        private readonly IRepository<Lane> _laneRepo;
        private readonly IDeviceHealthMonitorService _deviceHealthService;
        private readonly IDeviceConfigService _deviceConfigService = null!;

        private string _captureDir = "";
        private string _controllerIp = "192.168.1.202";
        private int _controllerPort = 4370;
        private readonly object _lockDebounce = new();
        private Timer? _deviceSyncTimer;

        public FrmMain()
        {
            InitializeComponent();

            if (Program.ServiceProvider != null)
            {
                _parkingLaneService = Program.ServiceProvider.GetService<IParkingLaneService>()
                    ?? new ParkingLaneService(
                        new MongoRepository<ParkingSession>(),
                        new MongoRepository<Vehicle>(),
                        new MongoRepository<Person>(),
                        new MongoRepository<Department>(),
                        new MongoRepository<Company>(),
                        new MongoRepository<Contractor>());
                _anprService = Program.ServiceProvider.GetService<IPlateRecognitionService>() ?? new SimpleLprAnprService();
                _deviceRepo = Program.ServiceProvider.GetService<IRepository<Device>>() ?? new MongoRepository<Device>();
                _laneRepo = Program.ServiceProvider.GetService<IRepository<Lane>>() ?? new MongoRepository<Lane>();
                _deviceHealthService = Program.ServiceProvider.GetService<IDeviceHealthMonitorService>() ?? new DeviceHealthMonitorService(_deviceRepo);
                _deviceConfigService = new DeviceConfigService(_laneRepo, _deviceRepo);
            }
            else
            {
                _parkingLaneService = new ParkingLaneService(
                    new MongoRepository<ParkingSession>(),
                    new MongoRepository<Vehicle>(),
                    new MongoRepository<Person>(),
                    new MongoRepository<Department>(),
                    new MongoRepository<Company>(),
                    new MongoRepository<Contractor>());
                _anprService = new SimpleLprAnprService();
                _deviceRepo = new MongoRepository<Device>();
                _laneRepo = new MongoRepository<Lane>();
                _deviceHealthService = new DeviceHealthMonitorService(_deviceRepo);
                _deviceConfigService = new DeviceConfigService(_laneRepo, _deviceRepo);
            }

            // Đăng ký sự kiện khi cấu hình thay đổi (Web Admin sửa IP, etc.)
            _deviceConfigService.OnConfigChanged += DeviceConfigService_OnConfigChanged;

            // Đăng ký sự kiện Controller ZKTeco
            _controller.OnAuxInputTriggered += Controller_OnAuxInputTriggered;

            // Hỗ trợ phím tắt tiện lợi cho vận hành
            KeyPreview = true;
            KeyDown += FrmMain_KeyDown;
        }

        public FrmMain(
            IParkingLaneService parkingLaneService,
            IPlateRecognitionService anprService,
            IRepository<Device> deviceRepo,
            IRepository<Lane>? laneRepo = null,
            IDeviceHealthMonitorService? deviceHealthService = null)
        {
            InitializeComponent();

            _parkingLaneService = parkingLaneService ?? throw new ArgumentNullException(nameof(parkingLaneService));
            _anprService = anprService ?? new SimpleLprAnprService();
            _deviceRepo = deviceRepo ?? new MongoRepository<Device>();
            _laneRepo = laneRepo ?? new MongoRepository<Lane>();
            _deviceHealthService = deviceHealthService ?? new DeviceHealthMonitorService(_deviceRepo);
            _deviceConfigService = new DeviceConfigService(_laneRepo, _deviceRepo);

            _controller.OnAuxInputTriggered += Controller_OnAuxInputTriggered;
            KeyPreview = true;
            KeyDown += FrmMain_KeyDown;
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            LoadConfigurations();
            UpdateClock();
        }

        private async void FrmMain_Shown(object sender, EventArgs e)
        {
            await AutoConnectAllAsync();

            // Khởi động đồng bộ trạng thái thiết bị lên Web Admin định kỳ 30s
            StartDeviceSyncBackgroundWorker();

            // Bắt đầu giám sát thay đổi cấu hình từ Web Admin mỗi 5 phút
            StartConfigMonitoring();
        }

        private void StartDeviceSyncBackgroundWorker()
        {
            try
            {
                // Chạy 1 lần ngay sau khi kết nối
                _ = _deviceHealthService.CheckAllAndSyncAsync();

                _deviceSyncTimer = new Timer
                {
                    Interval = 30000 // 30 giây
                };
                _deviceSyncTimer.Tick += async (s, e) =>
                {
                    await _deviceHealthService.CheckAllAndSyncAsync();
                };
                _deviceSyncTimer.Start();
            }
            catch (Exception ex)
            {
                AppLogger.Warning($"Lỗi khởi tạo Background Device Sync: {ex.Message}");
            }
        }

        private void TimerClock_Tick(object sender, EventArgs e)
        {
            UpdateClock();
        }

        private void UpdateClock()
        {
            lblClock.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        }

        /// <summary>
        /// Bắt đầu giám sát thay đổi cấu hình định kỳ (5 phút)
        /// </summary>
        private void StartConfigMonitoring()
        {
            try
            {
                _deviceConfigService.StartMonitoring(TimeSpan.FromMinutes(5));
                AppLogger.Information("[FrmMain] Đã bắt đầu giám sát thay đổi cấu hình thiết bị (5 phút/lần)");
            }
            catch (Exception ex)
            {
                AppLogger.Warning($"Lỗi khởi tạo Config Monitoring: {ex.Message}");
            }
        }

        /// <summary>
        /// Xử lý khi Web Admin thay đổi cấu hình thiết bị
        /// </summary>
        private async void DeviceConfigService_OnConfigChanged(object? sender, ConfigChangeEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => DeviceConfigService_OnConfigChanged(sender, e)));
                return;
            }

            AppLogger.Warning($"[FrmMain] Phát hiện thay đổi cấu hình từ Web Admin: {string.Join(", ", e.ChangedDevices)}");

            // Hiển thị thông báo cho người vận hành
            SetHeaderStatus("⚠️ Cấu hình thiết bị đã thay đổi từ Web Admin!");
            AppNotificationService.NotifyWarning(NotificationCategory.System, "Cấu hình thay đổi",
                $"Các thiết bị sau đã thay đổi: {string.Join(", ", e.ChangedDevices)}. Nhấn Ctrl+R để kết nối lại.");

            // Tự động ngắt kết nối cũ và kết nối lại với cấu hình mới
            await Task.Delay(1000); // Chờ 1s để người dùng nhận biết
            await AutoConnectAllAsync();
        }

        #region Phím Tắt & Status Helpers

        private void FrmMain_KeyDown(object? sender, KeyEventArgs e)
        {
            // Phím Space hoặc F5: Chụp thủ công Làn Vào
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.F5)
            {
                _ = CaptureInLaneAsync("MANUAL_LAN_VAO");
                e.Handled = true;
            }
            // Phím F6: Chụp thủ công Làn Ra
            else if (e.KeyCode == Keys.F6)
            {
                _ = CaptureOutLaneAsync("MANUAL_LAN_RA");
                e.Handled = true;
            }
            // Phím F9: Mở Trung Tâm Giám Sát Thiết Bị
            else if (e.KeyCode == Keys.F9)
            {
                OpenDeviceMonitor();
                e.Handled = true;
            }
            // Phím Ctrl + R: Tự động kết nối lại
            else if (e.Control && e.KeyCode == Keys.R)
            {
                _ = AutoConnectAllAsync();
                e.Handled = true;
            }
            // Phím Ctrl + O: Mở thư mục ảnh
            else if (e.Control && e.KeyCode == Keys.O)
            {
                if (Directory.Exists(_captureDir))
                {
                    Process.Start("explorer.exe", _captureDir);
                }
                e.Handled = true;
            }
        }

        public void OpenDeviceMonitor()
        {
            try
            {
                var frm = Program.ServiceProvider != null
                    ? (Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<FrmDeviceMonitor>(Program.ServiceProvider) ?? new FrmDeviceMonitor(_deviceHealthService, _deviceRepo))
                    : new FrmDeviceMonitor(_deviceHealthService, _deviceRepo);

                frm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Lỗi mở màn hình Giám sát thiết bị");
                MessageBox.Show($"Không thể mở màn hình Giám sát thiết bị: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDeviceMonitor_Click(object? sender, EventArgs e)
        {
            OpenDeviceMonitor();
        }

        private void SetHeaderStatus(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetHeaderStatus(message)));
                return;
            }

            lblSystemStatus.Text = message;
        }

        private void SetFooterStatus(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetFooterStatus(message)));
                return;
            }

            lblFooterStatus.Text = $"[{DateTime.Now:HH:mm:ss}] {message}";
        }

        #endregion

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // Dừng và giải phóng Camera & GDI+ Cache
                CleanupCameras();

                // Giải phóng dịch vụ ANPR
                _anprService.Dispose();

                // Dừng và giải phóng Controller C3-200
                _controller.Dispose();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"[FormClosing Error] {ex.Message}", "FrmMain");
            }
        }
    }
}
