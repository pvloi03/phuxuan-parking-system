using HPParkingSystem.Licensing;
using HPParkingSystem.Models.Entities;
using HPParkingSystem.Models.Enums;
using HPParkingSystem.Repositories;
using HPParkingSystem.Services.Anpr;
using HPParkingSystem.Services.Background;
using HPParkingSystem.Services.Devices.Config;
using HPParkingSystem.Services.Devices.Health;
using HPParkingSystem.Services.License;
using HPParkingSystem.Services.Logging;
using HPParkingSystem.Services.Offline;
using HPParkingSystem.Services.Parking;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HPParkingSystem.Forms
{

    /// <summary>
    /// Giao diện chính Hệ Thống Kiểm Soát Bãi Xe (WinForms Presentation Layer)
    /// Đã được tái cấu trúc Clean Architecture: Tách Camera (FrmMain.Cameras.cs),
    /// Điều phối làn (FrmMain.LaneControl.cs) và Nghiệp vụ (ParkingLaneService.cs).
    /// </summary>
    public partial class FrmMain : Form
    {
        // ── Dịch vụ Nghiệp vụ & Thiết bị ─────────────────────────────────────
        private readonly IPlateRecognitionService _anprService;
        private readonly IRepository<Device> _deviceRepo;
        private readonly IRepository<Lane> _laneRepo;
        private readonly IDeviceHealthMonitorService _deviceHealthService;
        private readonly IDeviceConfigService _deviceConfigService = null!;
        private readonly IDeviceAdapterFactory _adapterFactory;
        private readonly LicenseManager _licenseManager;
        private readonly IParkingLaneService _laneService;
        private readonly OfflineSyncBackgroundWorker? _syncWorker;

        // ── Quản lý Thiết bị & Camera Slots động (Mở rộng cho N thiết bị/làn) ──
        private readonly Dictionary<string, Device> _activeDevices = new();
        private readonly Dictionary<string, CameraSlot> _deviceIdToSlotMap = new();
        private readonly Dictionary<CameraSlot, DeviceStatus> _slotStates = new()
        {
            [CameraSlot.InPlate] = DeviceStatus.Disconnected,
            [CameraSlot.InOverview] = DeviceStatus.Disconnected,
            [CameraSlot.OutPlate] = DeviceStatus.Disconnected,
            [CameraSlot.OutOverview] = DeviceStatus.Disconnected
        };

        private string _captureDir = "";
        private string _controllerIp = "";
        private int _controllerPort = 0;
        private readonly object _lockDebounce = new();
        private bool _hasServerIncident = false;
        private string _currentIncidentDetails = string.Empty;

        public FrmMain()
        {
            InitializeComponent();

            _licenseManager = new LicenseManager(new MongoRepository<LicenseInfo>());

            if (Program.ServiceProvider != null)
            {
                _syncWorker = Program.ServiceProvider.GetService<OfflineSyncBackgroundWorker>();
                _anprService = Program.ServiceProvider.GetService<IPlateRecognitionService>() ?? new SimpleLprAnprService();
                _deviceRepo = Program.ServiceProvider.GetService<IRepository<Device>>() ?? new MongoRepository<Device>();
                _laneRepo = Program.ServiceProvider.GetService<IRepository<Lane>>() ?? new MongoRepository<Lane>();
                _adapterFactory = Program.ServiceProvider.GetService<IDeviceAdapterFactory>() ?? new DeviceAdapterFactory();
                _deviceHealthService = Program.ServiceProvider.GetService<IDeviceHealthMonitorService>() ?? new DeviceHealthMonitorService(_deviceRepo, _adapterFactory);
                _deviceConfigService = new DeviceConfigService(_laneRepo, _deviceRepo);

                var sessionRepo = Program.ServiceProvider.GetService<IRepository<ParkingSession>>() ??
                                  (IRepository<ParkingSession>)new HybridParkingSessionRepository();
                var vehicleRepo = Program.ServiceProvider.GetService<IRepository<Vehicle>>() ?? new MongoRepository<Vehicle>();
                var personRepo = Program.ServiceProvider.GetService<IRepository<Person>>() ?? new MongoRepository<Person>();
                var deptRepo = Program.ServiceProvider.GetService<IRepository<Department>>() ?? new MongoRepository<Department>();

                _laneService = Program.ServiceProvider.GetService<IParkingLaneService>() ??
                    new ParkingLaneService(
                        sessionRepo,
                        vehicleRepo,
                        personRepo,
                        deptRepo,
                        _anprService,
                        _deviceHealthService,
                        _deviceRepo,
                        _laneRepo,
                        ServerHealthTracker.Instance);
            }
            else
            {
                _syncWorker = null;
                _anprService = new SimpleLprAnprService();
                _deviceRepo = new MongoRepository<Device>();
                _laneRepo = new MongoRepository<Lane>();
                _adapterFactory = new DeviceAdapterFactory();
                _deviceHealthService = new DeviceHealthMonitorService(_deviceRepo, _adapterFactory);
                _deviceConfigService = new DeviceConfigService(_laneRepo, _deviceRepo);
                _laneService = new ParkingLaneService(
                    new HybridParkingSessionRepository(),
                    new MongoRepository<Vehicle>(),
                    new MongoRepository<Person>(),
                    new MongoRepository<Department>(),
                    _anprService,
                    _deviceHealthService,
                    _deviceRepo,
                    _laneRepo,
                    ServerHealthTracker.Instance);
            }

            _syncWorker?.SyncStatusChanged += SyncWorker_SyncStatusChanged;

            // Đăng ký sự kiện khi cấu hình thay đổi (Web Admin sửa IP, etc.)
            _deviceConfigService.OnConfigChanged += DeviceConfigService_OnConfigChanged;

            // Đăng ký sự kiện Controller ZKTeco
            _controller.OnAuxInputTriggered += Controller_OnAuxInputTriggered;
            KeyPreview = true;
            KeyDown += FrmMain_KeyDown;
        }

        public FrmMain(
            IPlateRecognitionService anprService,
            IRepository<Device> deviceRepo,
            IRepository<Lane>? laneRepo = null,
            IDeviceHealthMonitorService? deviceHealthService = null,
            IDeviceAdapterFactory? adapterFactory = null,
            IParkingLaneService? laneService = null,
            OfflineSyncBackgroundWorker? syncWorker = null)
        {
            InitializeComponent();

            _syncWorker = syncWorker ?? Program.ServiceProvider?.GetService<OfflineSyncBackgroundWorker>();
            _syncWorker?.SyncStatusChanged += SyncWorker_SyncStatusChanged;

            _licenseManager = new LicenseManager(new MongoRepository<LicenseInfo>());
            _anprService = anprService ?? new SimpleLprAnprService();
            _deviceRepo = deviceRepo ?? new MongoRepository<Device>();
            _laneRepo = laneRepo ?? new MongoRepository<Lane>();
            _adapterFactory = adapterFactory ?? new DeviceAdapterFactory();
            _deviceHealthService = deviceHealthService ?? new DeviceHealthMonitorService(_deviceRepo, _adapterFactory);
            _deviceConfigService = new DeviceConfigService(_laneRepo, _deviceRepo);
            _deviceConfigService.OnConfigChanged += DeviceConfigService_OnConfigChanged;
            _laneService = laneService ??
                new ParkingLaneService(
                    new MongoRepository<ParkingSession>(),
                    new MongoRepository<Vehicle>(),
                    new MongoRepository<Person>(),
                    new MongoRepository<Department>(),
                    _anprService,
                    _deviceHealthService,
                    _deviceRepo);

            _controller.OnAuxInputTriggered += Controller_OnAuxInputTriggered;
            KeyPreview = true;
            KeyDown += FrmMain_KeyDown;
        }

        private void FrmMain_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                // Phím tắt chụp thủ công Cột 1 (F1) - theo Direction của Làn 1
                var cfg = _deviceConfigService?.CurrentConfig;
                var lane1 = cfg?.Lane1;
                if (lane1 == null)
                {
                    AppLogger.Warning("[CỘT 1] Chưa được gán làn xe.", "LaneControl");
                    return;
                }
                var dir1 = lane1.Direction;
                _ = Task.Run(async () => await HandleLaneSlotAsync(1, dir1, "MANUAL"));
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F2)
            {
                // Phím tắt chụp thủ công Cột 2 (F2) - theo Direction của Làn 2
                var cfg = _deviceConfigService?.CurrentConfig;
                var lane2 = cfg?.Lane2;
                if (lane2 == null)
                {
                    AppLogger.Warning("[CỘT 2] Chưa được gán làn xe.", "LaneControl");
                    return;
                }
                var dir2 = lane2.Direction;
                _ = Task.Run(async () => await HandleLaneSlotAsync(2, dir2, "MANUAL"));
                e.Handled = true;
            }
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            lblFooterMachineCode.Text = $"Mã Máy: {HardwareFingerprint.GetMachineCode()}";
            lblFooterServerStatus.Text = "⚪ Máy chủ: Đang kiểm tra...";
            lblFooterServerStatus.ForeColor = Color.FromArgb(100, 116, 139);
            lblFooterServerStatus.MouseEnter += (s, ev) =>
            {
                lblFooterServerStatus.Owner?.Cursor = Cursors.Hand;
            };
            lblFooterServerStatus.MouseLeave += (s, ev) =>
            {
                lblFooterServerStatus.Owner?.Cursor = Cursors.Default;
            };
            lblFooterServerStatus.Click += LblFooterServerStatus_Click;

            // Hiệu ứng Hover, Click và đổi màu/con trỏ chuột cho thanh Header System Status
            Color headerOriginalColor = lblSystemStatus.ForeColor;
            lblSystemStatus.Cursor = Cursors.Hand;
            lblSystemStatus.MouseEnter += (s, ev) =>
            {
                headerOriginalColor = lblSystemStatus.ForeColor;
                lblSystemStatus.ForeColor = Color.FromArgb(255, 255, 150);
                lblSystemStatus.Font = new Font(lblSystemStatus.Font, FontStyle.Underline);
            };
            lblSystemStatus.MouseLeave += (s, ev) =>
            {
                lblSystemStatus.ForeColor = headerOriginalColor;
                lblSystemStatus.Font = new Font(lblSystemStatus.Font, FontStyle.Regular);
            };
            lblSystemStatus.MouseDown += (s, ev) =>
            {
                lblSystemStatus.ForeColor = Color.FromArgb(255, 200, 60);
            };
            lblSystemStatus.MouseUp += (s, ev) =>
            {
                lblSystemStatus.ForeColor = Color.FromArgb(255, 255, 150);
            };
            lblSystemStatus.Click += (s, ev) => OpenDeviceMonitor();
            lblSystemStatus.DoubleClick += (s, ev) => OpenDeviceMonitor();

            LoadConfigurations();
            UpdateClock();
        }

        private async Task CheckAndEnforceLicenseAsync()
        {
            string? currentKey = await _licenseManager.GetCurrentLicenseKeyAsync();
            var validation = !string.IsNullOrWhiteSpace(currentKey)
                ? LicenseCrypto.ValidateLicense(currentKey!)
                : new LicenseValidationResult { IsValid = false, Message = "Chưa tìm thấy thông tin bản quyền trên máy trạm này." };

            if (!validation.IsValid)
            {
                lblFooterLicense.Text = "🔴 Bản quyền: Hết hạn / Chưa kích hoạt";
                lblFooterLicense.ForeColor = Color.Red;

                // Tự động mở form kích hoạt / hết hạn
                using var expiredForm = new LicenseExpiredForm(validation.Message);
                var dialogResult = expiredForm.ShowDialog(this);
                if (dialogResult == DialogResult.OK && expiredForm.IsActivatedSuccessfully)
                {
                    var newValidation = LicenseCrypto.ValidateLicense(expiredForm.ActivatedKey);
                    if (newValidation.Payload != null)
                    {
                        try
                        {
                            await _licenseManager.SaveLicenseKeyAsync(expiredForm.ActivatedKey, newValidation.Payload);
                            UpdateLicenseFooter(newValidation);
                            return;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                $"Không thể lưu KEY bản quyền vào CSDL MongoDB: {ex.Message}\n\nVui lòng kiểm tra lại kết nối mạng hoặc máy chủ cơ sở dữ liệu.",
                                "Lỗi Lưu KEY Bản Quyền",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );
                            Environment.Exit(0);
                            return;
                        }
                    }
                }
                else
                {
                    // Người dùng đóng form hoặc bấm Thoát ➔ Đóng ứng dụng ngay lập tức
                    Environment.Exit(0);
                    return;
                }
            }

            UpdateLicenseFooter(validation);
        }

        private void UpdateLicenseFooter(LicenseValidationResult validation)
        {
            if (validation.Payload?.IsPermanent == true)
            {
                lblFooterLicense.Text = "🛡️ Bản quyền: Vĩnh viễn (Đã kích hoạt)";
                lblFooterLicense.ForeColor = Color.FromArgb(16, 185, 129); // Green
            }
            else if (validation.DaysRemaining > 15)
            {
                lblFooterLicense.Text = $"🛡️ Thời gian sử dụng: {validation.DaysRemaining} ngày (Đến {validation.Payload?.ExpiryDate:dd/MM/yyyy})";
                lblFooterLicense.ForeColor = Color.FromArgb(16, 185, 129); // Green
            }
            else if (validation.DaysRemaining > 0)
            {
                lblFooterLicense.Text = $"⚠️ Sắp hết hạn: Còn {validation.DaysRemaining} ngày (Đến {validation.Payload?.ExpiryDate:dd/MM/yyyy})";
                lblFooterLicense.ForeColor = Color.FromArgb(245, 158, 11); // Orange
            }
            else
            {
                lblFooterLicense.Text = "🔴 Bản quyền đã hết hạn - Click để nạp Key";
                lblFooterLicense.ForeColor = Color.Red;
            }
        }

        private async void LblFooterLicense_DoubleClick(object sender, EventArgs e)
        {
            // Cho phép người dùng click đúp vào footer để nạp key gia hạn sớm
            using (var activateForm = new LicenseExpiredForm("Quản Lý & Gia Hạn Bản Quyền Phần Mềm"))
            {
                if (activateForm.ShowDialog(this) == DialogResult.OK && activateForm.IsActivatedSuccessfully)
                {
                    var newValidation = LicenseCrypto.ValidateLicense(activateForm.ActivatedKey);
                    if (newValidation.Payload != null)
                    {
                        try
                        {
                            await _licenseManager.SaveLicenseKeyAsync(activateForm.ActivatedKey, newValidation.Payload);
                            UpdateLicenseFooter(newValidation);
                            MessageBox.Show("Đã cập nhật bản quyền mới thành công!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(
                                $"Không thể lưu bản quyền vào CSDL MongoDB: {ex.Message}\n\nVui lòng kiểm tra lại kết nối máy chủ cơ sở dữ liệu.",
                                "Lỗi Lưu Bản Quyền",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );
                        }
                    }
                }
            }
        }

        private async void FrmMain_Shown(object sender, EventArgs e)
        {
            // Kiểm tra và thực thi bản quyền phần mềm
            await CheckAndEnforceLicenseAsync();

            // Đăng ký event handler TRƯỚC KHI kết nối - tránh miss events
            _deviceHealthService.OnStateChanged += DeviceHealthService_OnStateChanged;

            await AutoConnectAllAsync();

            // Đăng ký adapters với factory để DeviceHealthMonitor sử dụng
            RegisterDeviceAdapters();

            // Bắt đầu health check định kỳ (30 giây) và tự động đồng bộ MongoDB
            _deviceHealthService.StartHealthCheck(TimeSpan.FromSeconds(30));

            // Bắt đầu giám sát thay đổi cấu hình từ Web Admin mỗi 5 phút
            StartConfigMonitoring();
        }

        /// <summary>
        /// Xử lý sự kiện thay đổi trạng thái từ DeviceHealthMonitorService
        /// </summary>
        private void DeviceHealthService_OnStateChanged(object? sender, DeviceStateChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => DeviceHealthService_OnStateChanged(sender, e)));
                return;
            }

            AppLogger.Debug($"[FrmMain] Device state changed: {e.DeviceId} {e.OldState} → {e.NewState}");

            // Cập nhật trạng thái Slot nếu thiết bị là Camera
            if (_deviceIdToSlotMap.TryGetValue(e.DeviceId, out var slot))
            {
                _slotStates[slot] = e.NewState;
                var cam = GetCameraService(slot);
                var panel = GetCameraPanel(slot);

                // Nếu vừa chuyển sang Connected và chưa Streaming, tự động kích hoạt lại Stream
                if (e.NewState == DeviceStatus.Connected && !cam.IsStreaming && cam.IsLoggedIn)
                {
                    cam.StartPreview(panel.Handle);
                }

                panel.Invalidate();
            }
        }

        /// <summary>
        /// Đăng ký device adapters với factory để HealthMonitor sử dụng
        /// </summary>
        private void RegisterDeviceAdapters()
        {
            try
            {
                if (_adapterFactory is DeviceAdapterFactory factory)
                {
                    string inPlateId = _deviceIdToSlotMap.FirstOrDefault(x => x.Value == CameraSlot.InPlate).Key ?? string.Empty;
                    string outPlateId = _deviceIdToSlotMap.FirstOrDefault(x => x.Value == CameraSlot.OutPlate).Key ?? string.Empty;
                    string inOvwId = _deviceIdToSlotMap.FirstOrDefault(x => x.Value == CameraSlot.InOverview).Key ?? string.Empty;
                    string outOvwId = _deviceIdToSlotMap.FirstOrDefault(x => x.Value == CameraSlot.OutOverview).Key ?? string.Empty;
                    string controllerId = _activeDevices.Values.FirstOrDefault(d => d.Type == DeviceType.Controller)?.Id ?? string.Empty;

                    factory.RegisterAdapter(inPlateId, _inPlateCam, _inPlateCam.Config.Ip);
                    factory.RegisterAdapter(outPlateId, _outPlateCam, _outPlateCam.Config.Ip);
                    factory.RegisterAdapter(inOvwId, _inOverviewCam, _inOverviewCam.Config.Ip);
                    factory.RegisterAdapter(outOvwId, _outOverviewCam, _outOverviewCam.Config.Ip);
                    factory.RegisterAdapter(controllerId, _controller, _controllerIp);
                    AppLogger.Information("[FrmMain] Đã đăng ký device adapters với factory");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warning($"[FrmMain] Lỗi đăng ký device adapters: {ex.Message}");
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
        /// Bắt đầu giám sát thay đổi cấu hình định kỳ (15 giây)
        /// </summary>
        private void StartConfigMonitoring()
        {
            try
            {
                _deviceConfigService.StartMonitoring(TimeSpan.FromSeconds(15));
                AppLogger.Information("[FrmMain] Đã bắt đầu giám sát thay đổi cấu hình thiết bị (15 giây/lần)");
            }
            catch (Exception ex)
            {
                AppLogger.Warning($"Lỗi khởi tạo Config Monitoring: {ex.Message}");
            }
        }

        /// <summary>
        /// Xử lý khi Web Admin thay đổi cấu hình thiết bị (Hot-Reload phân sai chỉ thiết bị thay đổi)
        /// </summary>
        private async void DeviceConfigService_OnConfigChanged(object? sender, ConfigChangeEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => DeviceConfigService_OnConfigChanged(sender, e)));
                return;
            }

            AppLogger.Warning($"[FrmMain] Phát hiện thay đổi cấu hình từ Web Admin: {string.Join(", ", e.ChangedDevices)}");
            await ApplyDifferentialConfigAsync(e.OldConfig, e.NewConfig);
        }

        public void OpenDeviceMonitor()
        {
            try
            {
                var frm = Program.ServiceProvider != null
                    ? (ServiceProviderServiceExtensions.GetService<FrmDeviceMonitor>(Program.ServiceProvider) ?? new FrmDeviceMonitor(_deviceHealthService, _deviceRepo, _adapterFactory))
                    : new FrmDeviceMonitor(_deviceHealthService, _deviceRepo, _adapterFactory);

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

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                _syncWorker?.SyncStatusChanged -= SyncWorker_SyncStatusChanged;
                lblFooterServerStatus.Click -= LblFooterServerStatus_Click;

                // Dừng và giải phóng Camera & GDI+ Cache
                CleanupCameras();

                // Giải phóng dịch vụ ANPR
                _anprService.Dispose();

                // Dừng và giải phóng Controller
                _controller.Dispose();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"[FormClosing Error] {ex.Message}", "FrmMain");
            }
        }

        private void SyncWorker_SyncStatusChanged(object? sender, SyncStatusEventArgs e)
        {
            if (IsDisposed || Disposing) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => UpdateServerStatusUi(e))); } catch { }
                return;
            }
            UpdateServerStatusUi(e);
        }

        private void UpdateServerStatusUi(SyncStatusEventArgs e)
        {
            if (lblFooterServerStatus == null || IsDisposed || Disposing) return;

            lblFooterServerStatus.Text = e.StatusMessage;
            _hasServerIncident = e.HasIncident || !e.IsServerOnline;
            _currentIncidentDetails = e.IncidentDetails;

            if (_hasServerIncident)
            {
                lblFooterServerStatus.ForeColor = Color.DarkOrange;
            }
            else if (e.PendingRecordCount > 0 || e.PendingImageCount > 0)
            {
                lblFooterServerStatus.ForeColor = Color.DodgerBlue;
            }
            else
            {
                lblFooterServerStatus.ForeColor = Color.FromArgb(16, 185, 129);
            }
        }

        private void LblFooterServerStatus_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentIncidentDetails))
            {
                MessageBox.Show(
                    "Máy chủ và hệ thống lưu trữ đang hoạt động trực tuyến bình thường.",
                    "Trạng thái máy chủ",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            MessageBox.Show(
                _currentIncidentDetails,
                _hasServerIncident ? "Chi tiết sự cố máy chủ & Trạng thái lưu trữ" : "Trạng thái máy chủ & Hệ thống lưu trữ",
                MessageBoxButtons.OK,
                _hasServerIncident ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }
    }
}
