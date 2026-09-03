using Microsoft.Extensions.DependencyInjection;
using PhuXuanParkingSystem.Licensing;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using PhuXuanParkingSystem.Services.Anpr;
using PhuXuanParkingSystem.Services.Devices;
using PhuXuanParkingSystem.Services.Devices.Camera;
using PhuXuanParkingSystem.Services.Devices.Config;
using PhuXuanParkingSystem.Services.Devices.Health;
using PhuXuanParkingSystem.Services.Devices.Controller;
using PhuXuanParkingSystem.Services.License;
using PhuXuanParkingSystem.Services.Logging;
using PhuXuanParkingSystem.Services.Parking;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;

namespace PhuXuanParkingSystem.Forms
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
        private string _controllerIp = "192.168.1.202";
        private int _controllerPort = 4370;
        private readonly object _lockDebounce = new();

        public FrmMain()
        {
            InitializeComponent();

            _licenseManager = new LicenseManager(new MongoRepository<LicenseInfo>());

            if (Program.ServiceProvider != null)
            {
                _anprService = Program.ServiceProvider.GetService<IPlateRecognitionService>() ?? new SimpleLprAnprService();
                _deviceRepo = Program.ServiceProvider.GetService<IRepository<Device>>() ?? new MongoRepository<Device>();
                _laneRepo = Program.ServiceProvider.GetService<IRepository<Lane>>() ?? new MongoRepository<Lane>();
                _adapterFactory = Program.ServiceProvider.GetService<IDeviceAdapterFactory>() ?? new DeviceAdapterFactory();
                _deviceHealthService = Program.ServiceProvider.GetService<IDeviceHealthMonitorService>() ?? new DeviceHealthMonitorService(_deviceRepo, _adapterFactory);
                _deviceConfigService = new DeviceConfigService(_laneRepo, _deviceRepo);
                _laneService = Program.ServiceProvider.GetService<IParkingLaneService>() ??
                    new ParkingLaneService(
                        new MongoRepository<ParkingSession>(),
                        new MongoRepository<Vehicle>(),
                        new MongoRepository<Person>(),
                        new MongoRepository<Department>(),
                        _anprService,
                        _deviceHealthService,
                        _deviceRepo);
            }
            else
            {
                _anprService = new SimpleLprAnprService();
                _deviceRepo = new MongoRepository<Device>();
                _laneRepo = new MongoRepository<Lane>();
                _adapterFactory = new DeviceAdapterFactory();
                _deviceHealthService = new DeviceHealthMonitorService(_deviceRepo, _adapterFactory);
                _deviceConfigService = new DeviceConfigService(_laneRepo, _deviceRepo);
                _laneService = new ParkingLaneService(
                    new MongoRepository<ParkingSession>(),
                    new MongoRepository<Vehicle>(),
                    new MongoRepository<Person>(),
                    new MongoRepository<Department>(),
                    _anprService,
                    _deviceHealthService,
                    _deviceRepo);
            }

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
            IParkingLaneService? laneService = null)
        {
            InitializeComponent();

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
                // Phím tắt chụp thủ công Làn Vào (F1)
                _ = Task.Run(async () => await HandleInLaneTriggerAsync("MANUAL"));
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.F2)
            {
                // Phím tắt chụp thủ công Làn Ra (F2)
                _ = Task.Run(async () => await HandleOutLaneTriggerAsync("MANUAL"));
                e.Handled = true;
            }
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            lblFooterMachineCode.Text = $"Mã Máy: {HardwareFingerprint.GetMachineCode()}";

            // Hiệu ứng Hover, Click và đổi màu/con trỏ chuột cho thanh Footer Status
            lblFooterStatus.DoubleClickEnabled = true;
            lblFooterStatus.MouseEnter += (s, ev) =>
            {
                statusStrip.Cursor = Cursors.Hand;
                lblFooterStatus.Font = new Font(lblFooterStatus.Font, FontStyle.Bold | FontStyle.Underline);
                lblFooterStatus.BackColor = Color.FromArgb(220, 230, 245);
            };
            lblFooterStatus.MouseLeave += (s, ev) =>
            {
                statusStrip.Cursor = Cursors.Default;
                lblFooterStatus.Font = new Font(lblFooterStatus.Font, FontStyle.Bold);
                lblFooterStatus.BackColor = Color.Transparent;
            };
            lblFooterStatus.MouseDown += (s, ev) =>
            {
                lblFooterStatus.BackColor = Color.FromArgb(195, 215, 240);
            };
            lblFooterStatus.MouseUp += (s, ev) =>
            {
                lblFooterStatus.BackColor = Color.FromArgb(220, 230, 245);
            };
            lblFooterStatus.Click += (s, ev) => OpenDeviceMonitor();
            lblFooterStatus.DoubleClick += (s, ev) => OpenDeviceMonitor();

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
                        await _licenseManager.SaveLicenseKeyAsync(expiredForm.ActivatedKey, newValidation.Payload);
                        UpdateLicenseFooter(newValidation);
                        return;
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
                        await _licenseManager.SaveLicenseKeyAsync(activateForm.ActivatedKey, newValidation.Payload);
                        UpdateLicenseFooter(newValidation);
                        MessageBox.Show("Đã cập nhật bản quyền mới thành công!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            // Cập nhật header & footer status từ DeviceHealthService
            UpdateHeaderStatusFromAllStates();
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

        /// <summary>
        /// Cập nhật header và footer status dựa trên trạng thái của tất cả cameras và Controller
        /// </summary>
        private void UpdateHeaderStatusFromAllStates()
        {
            bool inPlateOk = _slotStates[CameraSlot.InPlate] == DeviceStatus.Connected || _slotStates[CameraSlot.InPlate] == DeviceStatus.Streaming;
            bool inOvwOk = _slotStates[CameraSlot.InOverview] == DeviceStatus.Connected || _slotStates[CameraSlot.InOverview] == DeviceStatus.Streaming;
            bool outPlateOk = _slotStates[CameraSlot.OutPlate] == DeviceStatus.Connected || _slotStates[CameraSlot.OutPlate] == DeviceStatus.Streaming;
            bool outOvwOk = _slotStates[CameraSlot.OutOverview] == DeviceStatus.Connected || _slotStates[CameraSlot.OutOverview] == DeviceStatus.Streaming;
            bool ctrlOk = _controller.IsConnected;

            bool inLaneOk = (string.IsNullOrEmpty(_inPlateCam.Config.Ip) || inPlateOk) && (string.IsNullOrEmpty(_inOverviewCam.Config.Ip) || inOvwOk);
            bool outLaneOk = (string.IsNullOrEmpty(_outPlateCam.Config.Ip) || outPlateOk) && (string.IsNullOrEmpty(_outOverviewCam.Config.Ip) || outOvwOk);

            string inStatus = inLaneOk ? "Làn Vào: Sẵn sàng" : "Làn Vào: Chưa sẵn sàng";
            string outStatus = outLaneOk ? "Làn Ra: Sẵn sàng" : "Làn Ra: Chưa sẵn sàng";
            string ctrlStatus = string.IsNullOrEmpty(_controllerIp) ? "Access Controller: Chưa cấu hình" : (ctrlOk ? "Access Controller: Đã kết nối" : "Access Controller: Mất tín hiệu");

            SetHeaderStatus($"{inStatus}  |  {outStatus}  |  {ctrlStatus}");

            // Tự động kiểm tra trạng thái tất cả thiết bị từ _activeDevices
            var disconnected = new List<string>();
            foreach (var kvp in _activeDevices)
            {
                var devId = kvp.Key;
                var dev = kvp.Value;
                if (_deviceIdToSlotMap.TryGetValue(devId, out var slot))
                {
                    if (_slotStates.TryGetValue(slot, out var state) && state != DeviceStatus.Streaming && state != DeviceStatus.Connected)
                    {
                        disconnected.Add(dev.Name);
                    }
                }
                else if (dev.Type == DeviceType.Controller && !_controller.IsConnected)
                {
                    disconnected.Add(dev.Name);
                }
            }

            if (_activeDevices.Count > 0)
            {
                if (disconnected.Count == 0)
                {
                    SetFooterStatus("Hệ thống sẵn sàng và hoạt động bình thường.", isError: false);
                }
                else
                {
                    string detail = disconnected.Count switch
                    {
                        1 => $"{disconnected[0]} mất kết nối",
                        2 => $"{disconnected[0]}, {disconnected[1]} mất kết nối",
                        _ => $"Có {disconnected.Count} thiết bị mất kết nối"
                    };
                    SetFooterStatus($"⚠️ {detail} (Nhấn để xem chi tiết)", isError: true);
                }
            }
            else
            {
                SetFooterStatus("Chưa có thiết bị nào được cấu hình trong hệ thống.", isError: false);
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

            SetHeaderStatus("⚠️ Đang cập nhật thiết bị thay đổi...");
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

        private void SetHeaderStatus(string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetHeaderStatus(message)));
                return;
            }

            lblSystemStatus.Text = message;
        }

        private void SetFooterStatus(string message, bool isError = false)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetFooterStatus(message, isError)));
                return;
            }

            lblFooterStatus.Text = $"[{DateTime.Now:HH:mm:ss}] {message}";
            lblFooterStatus.ForeColor = isError ? Color.FromArgb(220, 53, 69) : Color.FromArgb(40, 167, 69);
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
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
    }
}
