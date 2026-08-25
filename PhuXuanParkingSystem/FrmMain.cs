using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using PhuXuanParkingSystem.Services.Anpr;
using PhuXuanParkingSystem.Services.Camera;
using PhuXuanParkingSystem.Services.Controller;
using PhuXuanParkingSystem.Services.Logging;
using PhuXuanParkingSystem.Services.Notification;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhuXuanParkingSystem
{
    public enum DeviceConnectionState
    {
        Connecting,
        Connected,
        Failed
    }

    public partial class FrmMain : Form
    {
        // ── 4 Camera (2 Làn Vào, 2 Làn Ra) ──────────────────────────────────
        private readonly PlateCameraService _inPlateCam = new();
        private readonly OverviewCameraService _inOverviewCam = new();
        private readonly PlateCameraService _outPlateCam = new();
        private readonly OverviewCameraService _outOverviewCam = new();

        // ── Trạng thái kết nối của từng Camera (Connecting, Connected, Failed)
        private DeviceConnectionState _inPlateState = DeviceConnectionState.Connecting;
        private DeviceConnectionState _inOverviewState = DeviceConnectionState.Connecting;
        private DeviceConnectionState _outPlateState = DeviceConnectionState.Connecting;
        private DeviceConnectionState _outOverviewState = DeviceConnectionState.Connecting;

        // ── 1 Controller ZKTeco C3-200 dùng chung ───────────────────────────
        private readonly ZKTecoDeviceAdapter _controller = new();

        // ── Dịch vụ Nhận diện biển số & CSDL MongoDB ────────────────────────
        private readonly IPlateRecognitionService _anprService;
        private readonly IRepository<ParkingSession> _sessionRepo;
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IRepository<Person> _personRepo;
        private readonly IRepository<Department> _departmentRepo;
        private readonly IRepository<Company> _companyRepo;
        private readonly IRepository<Contractor> _contractorRepo;
        private readonly IRepository<Lane> _laneRepo;
        private readonly IRepository<Device> _deviceRepo;

        // ── Bộ nhớ Cache GDI+ (Tránh cấp phát lại liên tục trong sự kiện Paint) ──
        private readonly Font _fontBoldStatus = new("Segoe UI", 10.5F, FontStyle.Bold);
        private readonly Font _fontSubStatus = new("Segoe UI", 9F, FontStyle.Regular);
        private readonly Pen _penNormalBorder = new(Color.FromArgb(50, 55, 60), 1);
        private readonly Pen _penConnectingBorder = new(Color.FromArgb(0, 123, 255), 2);
        private readonly Pen _penErrorBorder = new(Color.FromArgb(220, 53, 69), 3);
        private readonly SolidBrush _brushConnectingBg = new(Color.FromArgb(26, 30, 35));
        private readonly SolidBrush _brushConnectingText = new(Color.FromArgb(100, 180, 255));
        private readonly SolidBrush _brushErrorBg = new(Color.FromArgb(20, 22, 25));
        private readonly SolidBrush _brushErrorText = new(Color.FromArgb(235, 75, 75));
        private readonly SolidBrush _brushSubText = new(Color.FromArgb(180, 190, 200));

        private string _captureDir = "";
        private string _controllerIp = "192.168.1.202";
        private int _controllerPort = 4370;

        public FrmMain()
        {
            InitializeComponent();

            if (Program.ServiceProvider != null)
            {
                _anprService = Program.ServiceProvider.GetService<IPlateRecognitionService>() ?? new SimpleLprAnprService();
                _sessionRepo = Program.ServiceProvider.GetService<IRepository<ParkingSession>>() ?? new MongoRepository<ParkingSession>();
                _vehicleRepo = Program.ServiceProvider.GetService<IRepository<Vehicle>>() ?? new MongoRepository<Vehicle>();
                _personRepo = Program.ServiceProvider.GetService<IRepository<Person>>() ?? new MongoRepository<Person>();
                _departmentRepo = Program.ServiceProvider.GetService<IRepository<Department>>() ?? new MongoRepository<Department>();
                _companyRepo = Program.ServiceProvider.GetService<IRepository<Company>>() ?? new MongoRepository<Company>();
                _contractorRepo = Program.ServiceProvider.GetService<IRepository<Contractor>>() ?? new MongoRepository<Contractor>();
                _laneRepo = Program.ServiceProvider.GetService<IRepository<Lane>>() ?? new MongoRepository<Lane>();
                _deviceRepo = Program.ServiceProvider.GetService<IRepository<Device>>() ?? new MongoRepository<Device>();
            }
            else
            {
                _anprService = new SimpleLprAnprService();
                _sessionRepo = new MongoRepository<ParkingSession>();
                _vehicleRepo = new MongoRepository<Vehicle>();
                _personRepo = new MongoRepository<Person>();
                _departmentRepo = new MongoRepository<Department>();
                _companyRepo = new MongoRepository<Company>();
                _contractorRepo = new MongoRepository<Contractor>();
                _laneRepo = new MongoRepository<Lane>();
                _deviceRepo = new MongoRepository<Device>();
            }

            // Đăng ký sự kiện Controller ZKTeco
            _controller.OnAuxInputTriggered += Controller_OnAuxInputTriggered;

            // Hỗ trợ phím tắt tiện lợi cho vận hành
            KeyPreview = true;
            KeyDown += FrmMain_KeyDown;
        }

        public FrmMain(
            IPlateRecognitionService anprService,
            IRepository<ParkingSession> sessionRepo,
            IRepository<Vehicle> vehicleRepo,
            IRepository<Person> personRepo,
            IRepository<Department> departmentRepo,
            IRepository<Company> companyRepo,
            IRepository<Contractor> contractorRepo,
            IRepository<Lane> laneRepo,
            IRepository<Device> deviceRepo)
        {
            InitializeComponent();

            _anprService = anprService ?? new SimpleLprAnprService();
            _sessionRepo = sessionRepo ?? new MongoRepository<ParkingSession>();
            _vehicleRepo = vehicleRepo ?? new MongoRepository<Vehicle>();
            _personRepo = personRepo ?? new MongoRepository<Person>();
            _departmentRepo = departmentRepo ?? new MongoRepository<Department>();
            _companyRepo = companyRepo ?? new MongoRepository<Company>();
            _contractorRepo = contractorRepo ?? new MongoRepository<Contractor>();
            _laneRepo = laneRepo ?? new MongoRepository<Lane>();
            _deviceRepo = deviceRepo ?? new MongoRepository<Device>();

            // Đăng ký sự kiện Controller ZKTeco
            _controller.OnAuxInputTriggered += Controller_OnAuxInputTriggered;

            // Hỗ trợ phím tắt tiện lợi cho vận hành
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
            // Tự động kết nối toàn bộ hệ thống khi khởi động
            await AutoConnectAllAsync();
        }

        private void TimerClock_Tick(object sender, EventArgs e)
        {
            UpdateClock();
        }

        private void UpdateClock()
        {
            lblClock.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        }

        private void LoadConfigurations()
        {
            try
            {
                // 1. Cấu hình LÀN VÀO (IN-LANE)
                _inPlateCam.Config.Ip = ConfigurationManager.AppSettings["In_PlateCam_Ip"] ?? "192.168.1.200";
                _inPlateCam.Config.Port = ushort.TryParse(ConfigurationManager.AppSettings["In_PlateCam_Port"], out ushort inPltPort) ? inPltPort : (ushort)3000;
                _inPlateCam.Config.UserName = ConfigurationManager.AppSettings["In_PlateCam_User"] ?? "admin";
                _inPlateCam.Config.Password = ConfigurationManager.AppSettings["In_PlateCam_Password"] ?? "admin";

                _inOverviewCam.Config.Ip = ConfigurationManager.AppSettings["In_OverviewCam_Ip"] ?? "192.168.1.61";
                _inOverviewCam.Config.Port = ushort.TryParse(ConfigurationManager.AppSettings["In_OverviewCam_Port"], out ushort inOvwPort) ? inOvwPort : (ushort)8000;
                _inOverviewCam.Config.UserName = ConfigurationManager.AppSettings["In_OverviewCam_User"] ?? "admin";
                _inOverviewCam.Config.Password = ConfigurationManager.AppSettings["In_OverviewCam_Password"] ?? "Hoangphat130225";

                // 2. Cấu hình LÀN RA (OUT-LANE)
                _outPlateCam.Config.Ip = ConfigurationManager.AppSettings["Out_PlateCam_Ip"] ?? "192.168.1.203";
                _outPlateCam.Config.Port = ushort.TryParse(ConfigurationManager.AppSettings["Out_PlateCam_Port"], out ushort outPltPort) ? outPltPort : (ushort)3000;
                _outPlateCam.Config.UserName = ConfigurationManager.AppSettings["Out_PlateCam_User"] ?? "admin";
                _outPlateCam.Config.Password = ConfigurationManager.AppSettings["Out_PlateCam_Password"] ?? "admin";

                _outOverviewCam.Config.Ip = ConfigurationManager.AppSettings["Out_OverviewCam_Ip"] ?? "192.168.1.62";
                _outOverviewCam.Config.Port = ushort.TryParse(ConfigurationManager.AppSettings["Out_OverviewCam_Port"], out ushort outOvwPort) ? outOvwPort : (ushort)8000;
                _outOverviewCam.Config.UserName = ConfigurationManager.AppSettings["Out_OverviewCam_User"] ?? "admin";
                _outOverviewCam.Config.Password = ConfigurationManager.AppSettings["Out_OverviewCam_Password"] ?? "Hoangphat130225";

                // 3. Cấu hình Controller ZKTeco C3-200
                _controllerIp = ConfigurationManager.AppSettings["Controller_Ip"] ?? "192.168.1.202";
                _controllerPort = int.TryParse(ConfigurationManager.AppSettings["Controller_Port"], out int ctrlPort) ? ctrlPort : 4370;

                // Thư mục lưu ảnh chụp
                string relativePath = ConfigurationManager.AppSettings["CaptureSavePath"] ?? "Captures";
                _captureDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
                if (!Directory.Exists(_captureDir))
                {
                    Directory.CreateDirectory(_captureDir);
                }

                SetFooterStatus("Đã tải cấu hình hệ thống. Đang tự động kết nối...");
            }
            catch (Exception ex)
            {
                SetFooterStatus($"Lỗi tải cấu hình: {ex.Message}");
            }
        }

        private async Task LoadConfigurationsFromDbAsync()
        {
            try
            {
                var devices = await _deviceRepo.FindAsync(d => !d.IsDeleted);
                if (devices != null && devices.Count > 0)
                {
                    foreach (var dev in devices)
                    {
                        if (dev.Code == "CAM-IN-PLT" || dev.Code == "CAM_IN_PLATE" || (dev.Type == DeviceType.PlateCamera && dev.Name.Contains("Vào")))
                        {
                            _inPlateCam.Config.Ip = dev.IpAddress;
                            _inPlateCam.Config.Port = (ushort)dev.Port;
                            if (!string.IsNullOrEmpty(dev.UserName)) _inPlateCam.Config.UserName = dev.UserName!;
                            if (!string.IsNullOrEmpty(dev.Password)) _inPlateCam.Config.Password = dev.Password!;
                        }
                        else if (dev.Code == "CAM-IN-OVW" || dev.Code == "CAM_IN_OVW" || (dev.Type == DeviceType.OverviewCamera && dev.Name.Contains("Vào")))
                        {
                            _inOverviewCam.Config.Ip = dev.IpAddress;
                            _inOverviewCam.Config.Port = (ushort)dev.Port;
                            if (!string.IsNullOrEmpty(dev.UserName)) _inOverviewCam.Config.UserName = dev.UserName!;
                            if (!string.IsNullOrEmpty(dev.Password)) _inOverviewCam.Config.Password = dev.Password!;
                        }
                        else if (dev.Code == "CAM-OUT-PLT" || dev.Code == "CAM_OUT_PLATE" || (dev.Type == DeviceType.PlateCamera && dev.Name.Contains("Ra")))
                        {
                            _outPlateCam.Config.Ip = dev.IpAddress;
                            _outPlateCam.Config.Port = (ushort)dev.Port;
                            if (!string.IsNullOrEmpty(dev.UserName)) _outPlateCam.Config.UserName = dev.UserName!;
                            if (!string.IsNullOrEmpty(dev.Password)) _outPlateCam.Config.Password = dev.Password!;
                        }
                        else if (dev.Code == "CAM-OUT-OVW" || dev.Code == "CAM_OUT_OVW" || (dev.Type == DeviceType.OverviewCamera && dev.Name.Contains("Ra")))
                        {
                            _outOverviewCam.Config.Ip = dev.IpAddress;
                            _outOverviewCam.Config.Port = (ushort)dev.Port;
                            if (!string.IsNullOrEmpty(dev.UserName)) _outOverviewCam.Config.UserName = dev.UserName!;
                            if (!string.IsNullOrEmpty(dev.Password)) _outOverviewCam.Config.Password = dev.Password!;
                        }
                        else if (dev.Code == "CTRL-C3-200" || dev.Type == DeviceType.Controller)
                        {
                            _controllerIp = dev.IpAddress;
                            _controllerPort = dev.Port;
                        }
                    }
                    AppLogger.Information($"Đã tải cấu hình {devices.Count} thiết bị từ CSDL MongoDB.");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warning($"Không thể nạp thiết bị từ CSDL (sử dụng cấu hình dự phòng): {ex.Message}");
            }
        }

        #region Tự Động Kết Nối (Auto-Connect)

        private async Task AutoConnectAllAsync()
        {
            // 1. Tải cấu hình mới nhất từ CSDL MongoDB
            await LoadConfigurationsFromDbAsync();

            // Thiết lập trạng thái Đang Kết Nối cho cả 4 Camera
            _inPlateState = DeviceConnectionState.Connecting;
            _inOverviewState = DeviceConnectionState.Connecting;
            _outPlateState = DeviceConnectionState.Connecting;
            _outOverviewState = DeviceConnectionState.Connecting;

            InvalidateCameraPanels();

            AppNotificationService.NotifyInfo(NotificationCategory.System, "Khởi động hệ thống", "Đang tự động kết nối 4 Camera và Controller C3-200...");
            SetHeaderStatus("Đang tự động kết nối 4 Camera và Controller C3-200...");
            SetFooterStatus("Đang khởi chạy luồng kết nối song song...");

            try
            {
                var inPltTask = _inPlateCam.LoginAsync();
                var inOvwTask = _inOverviewCam.LoginAsync();
                var outPltTask = _outPlateCam.LoginAsync();
                var outOvwTask = _outOverviewCam.LoginAsync();
                var ctrlTask = _controller.ConnectAsync(_controllerIp, _controllerPort);

                await Task.WhenAll(inPltTask, inOvwTask, outPltTask, outOvwTask, ctrlTask);

                _inPlateState = inPltTask.Result ? DeviceConnectionState.Connected : DeviceConnectionState.Failed;
                _inOverviewState = inOvwTask.Result ? DeviceConnectionState.Connected : DeviceConnectionState.Failed;
                _outPlateState = outPltTask.Result ? DeviceConnectionState.Connected : DeviceConnectionState.Failed;
                _outOverviewState = outOvwTask.Result ? DeviceConnectionState.Connected : DeviceConnectionState.Failed;
                bool ctrlOk = ctrlTask.Result;

                // Mở luồng xem trực tiếp lên các Panel nếu kết nối thành công
                if (_inPlateState == DeviceConnectionState.Connected) _inPlateCam.StartPreview(pnlInPlateVideo.Handle);
                if (_inOverviewState == DeviceConnectionState.Connected) _inOverviewCam.StartPreview(pnlInOverviewVideo.Handle);
                if (_outPlateState == DeviceConnectionState.Connected) _outPlateCam.StartPreview(pnlOutPlateVideo.Handle);
                if (_outOverviewState == DeviceConnectionState.Connected) _outOverviewCam.StartPreview(pnlOutOverviewVideo.Handle);

                // Cập nhật lại giao diện hiển thị viền trạng thái
                InvalidateCameraPanels();

                bool inAllOk = _inPlateState == DeviceConnectionState.Connected && _inOverviewState == DeviceConnectionState.Connected;
                bool outAllOk = _outPlateState == DeviceConnectionState.Connected && _outOverviewState == DeviceConnectionState.Connected;

                string inStatus = inAllOk ? "Làn Vào: Sẵn sàng" : "Làn Vào: Có Camera Mất Tín Hiệu";
                string outStatus = outAllOk ? "Làn Ra: Sẵn sàng" : "Làn Ra: Có Camera Mất Tín Hiệu";
                string ctrlStatus = ctrlOk ? "Radar C3-200: Đã kết nối" : "Radar C3-200: Mất tín hiệu";

                SetHeaderStatus($"{inStatus}  |  {outStatus}  |  {ctrlStatus}");
                SetFooterStatus("Hệ thống hoạt động bình thường. Tự động nhận diện xe khi qua vùng cảm biến Radar.");
            }
            catch (Exception ex)
            {
                _inPlateState = DeviceConnectionState.Failed;
                _inOverviewState = DeviceConnectionState.Failed;
                _outPlateState = DeviceConnectionState.Failed;
                _outOverviewState = DeviceConnectionState.Failed;
                InvalidateCameraPanels();

                SetHeaderStatus($"Lỗi kết nối: {ex.Message}");
                SetFooterStatus($"Chi tiết lỗi: {ex.Message}");
            }
        }

        private void InvalidateCameraPanels()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(InvalidateCameraPanels));
                return;
            }

            pnlInPlateVideo.Invalidate();
            pnlInOverviewVideo.Invalidate();
            pnlOutPlateVideo.Invalidate();
            pnlOutOverviewVideo.Invalidate();
        }

        #endregion

        #region Vẽ Trạng Thái Video Panel (Đang kết nối / Đã kết nối / Viền đỏ lỗi)

        private void PnlInPlateVideo_Paint(object sender, PaintEventArgs e)
        {
            DrawVideoPanelStatus(pnlInPlateVideo, _inPlateState, "Camera Biển Số Vào", _inPlateCam.Config.Ip, e);
        }

        private void PnlInOverviewVideo_Paint(object sender, PaintEventArgs e)
        {
            DrawVideoPanelStatus(pnlInOverviewVideo, _inOverviewState, "Camera Toàn Cảnh Vào", _inOverviewCam.Config.Ip, e);
        }

        private void PnlOutPlateVideo_Paint(object sender, PaintEventArgs e)
        {
            DrawVideoPanelStatus(pnlOutPlateVideo, _outPlateState, "Camera Biển Số Ra", _outPlateCam.Config.Ip, e);
        }

        private void PnlOutOverviewVideo_Paint(object sender, PaintEventArgs e)
        {
            DrawVideoPanelStatus(pnlOutOverviewVideo, _outOverviewState, "Camera Toàn Cảnh Ra", _outOverviewCam.Config.Ip, e);
        }

        private void DrawVideoPanelStatus(Panel pnl, DeviceConnectionState state, string camTitle, string? ip, PaintEventArgs e)
        {
            if (state == DeviceConnectionState.Connected)
            {
                // Khi đã kết nối thành công: Viền mỏng trung tính, không vẽ đè lên video stream
                e.Graphics.DrawRectangle(_penNormalBorder, 0, 0, pnl.Width - 1, pnl.Height - 1);
                return;
            }

            string subtitle = $"{camTitle} ({ip ?? "IP trống"})";

            if (state == DeviceConnectionState.Connecting)
            {
                // 1. Trạng thái ĐANG KẾT NỐI
                e.Graphics.FillRectangle(_brushConnectingBg, 0, 0, pnl.Width, pnl.Height);
                e.Graphics.DrawRectangle(_penConnectingBorder, 1, 1, pnl.Width - 3, pnl.Height - 3);

                string title = "🔄 ĐANG KẾT NỐI...";
                var szTitle = e.Graphics.MeasureString(title, _fontBoldStatus);
                var szSub = e.Graphics.MeasureString(subtitle, _fontSubStatus);

                float startY = (pnl.Height - (szTitle.Height + szSub.Height + 6)) / 2;
                float xTitle = (pnl.Width - szTitle.Width) / 2;
                float xSub = (pnl.Width - szSub.Width) / 2;

                e.Graphics.DrawString(title, _fontBoldStatus, _brushConnectingText, xTitle, startY);
                e.Graphics.DrawString(subtitle, _fontSubStatus, _brushSubText, xSub, startY + szTitle.Height + 6);
            }
            else // DeviceConnectionState.Failed
            {
                // 2. Trạng thái LỖI / MẤT KẾT NỐI: Nền xám đậm + Viền Đỏ 3px nổi bật
                e.Graphics.FillRectangle(_brushErrorBg, 0, 0, pnl.Width, pnl.Height);
                e.Graphics.DrawRectangle(_penErrorBorder, 1, 1, pnl.Width - 3, pnl.Height - 3);

                string title = "❌ KHÔNG KẾT NỐI ĐƯỢC";
                var szTitle = e.Graphics.MeasureString(title, _fontBoldStatus);
                var szSub = e.Graphics.MeasureString(subtitle, _fontSubStatus);

                float startY = (pnl.Height - (szTitle.Height + szSub.Height + 6)) / 2;
                float xTitle = (pnl.Width - szTitle.Width) / 2;
                float xSub = (pnl.Width - szSub.Width) / 2;

                e.Graphics.DrawString(title, _fontBoldStatus, _brushErrorText, xTitle, startY);
                e.Graphics.DrawString(subtitle, _fontSubStatus, _brushSubText, xSub, startY + szTitle.Height + 6);
            }
        }

        #endregion

        #region Xử Lý Sự Kiện Radar AUX (Controller Realtime)

        /// <summary>
        /// Sự kiện khi Cảm biến Radar (AUX In) thay đổi trạng thái:
        /// Aux 1 = LÀN VÀO (Event 221: Có xe, Event 220: Hết xe)
        /// Aux 2 = LÀN RA  (Event 221: Có xe, Event 220: Hết xe)
        /// </summary>
        private void Controller_OnAuxInputTriggered(object? sender, AuxTriggerEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Controller_OnAuxInputTriggered(sender, e)));
                return;
            }

            if (e.AuxPort == 1)
            {
                // LÀN VÀO
                if (e.IsActive)
                {
                    lblInStatusVal.Text = "🟢 Phát hiện xe vào (Radar kích hoạt)";
                    lblInStatusVal.ForeColor = Color.SeaGreen;
                    lblInTimeVal.Text = e.TriggerTime.ToString("dd/MM/yyyy HH:mm:ss");

                    _ = CaptureInLaneAsync("RADAR_LAN_VAO");
                }
                else
                {
                    lblInStatusVal.Text = "⚪ Xe đã qua làn vào";
                    lblInStatusVal.ForeColor = Color.FromArgb(100, 110, 120);
                }
            }
            else if (e.AuxPort == 2)
            {
                // LÀN RA
                if (e.IsActive)
                {
                    lblOutStatusVal.Text = "🔴 Phát hiện xe ra (Radar kích hoạt)";
                    lblOutStatusVal.ForeColor = Color.SeaGreen;
                    lblOutTimeVal.Text = e.TriggerTime.ToString("dd/MM/yyyy HH:mm:ss");

                    _ = CaptureOutLaneAsync("RADAR_LAN_RA");
                }
                else
                {
                    lblOutStatusVal.Text = "⚪ Xe đã qua làn ra";
                    lblOutStatusVal.ForeColor = Color.FromArgb(100, 110, 120);
                }
            }

            SetFooterStatus($"[RADAR] 📡 {e.LaneName} (Aux {e.AuxPort}): {(e.IsActive ? "CÓ XE VÀO VÙNG QUÉT" : "HẾT XE")} lúc {e.TriggerTime:HH:mm:ss}");
        }

        private void Controller_OnStatusChanged(bool success, string message)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Controller_OnStatusChanged(success, message)));
                return;
            }

            SetFooterStatus($"[Controller C3-200] {message}");
        }

        #endregion

        #region Chụp Ảnh Siêu Nhanh & Hiển Thị An Toàn Tuyệt Đối (0 Leak, 0 File Lock)

        private async Task CaptureInLaneAsync(string triggerSource)
        {
            try
            {
                string todayFolder = Path.Combine(_captureDir, DateTime.Now.ToString("yyyy-MM-dd"));
                if (!Directory.Exists(todayFolder))
                {
                    Directory.CreateDirectory(todayFolder);
                }

                string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                string filePlate = Path.Combine(todayFolder, $"{timeStamp}_{triggerSource}_plate.jpg");
                string fileOverview = Path.Combine(todayFolder, $"{timeStamp}_{triggerSource}_panoramic.jpg");

                // Chụp song song trực tiếp ra file bằng Native SDK tốc độ cao
                var tPlate = _inPlateCam.CaptureToFileAsync(filePlate);
                var tOverview = _inOverviewCam.CaptureToFileAsync(fileOverview);

                await Task.WhenAll(tPlate, tOverview);

                // Hiển thị ảnh an toàn lên UI
                if (File.Exists(filePlate)) DisplayCapturedImage(picInPlate, filePlate);
                if (File.Exists(fileOverview)) DisplayCapturedImage(picInOverview, fileOverview);

                // Nhận diện biển số xe bằng SimpleLPR3 Engine
                PlateRecognitionResult? anprResult = null;
                if (File.Exists(filePlate))
                {
                    anprResult = await _anprService.RecognizeAsync(filePlate);
                }

                // Cập nhật thông tin giao diện và lưu trữ CSDL
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(async () => await UpdateInLaneUiAsync(anprResult, triggerSource, filePlate, fileOverview)));
                }
                else
                {
                    await UpdateInLaneUiAsync(anprResult, triggerSource, filePlate, fileOverview);
                }

                SetFooterStatus($"📸 Đã chụp và xử lý LÀN VÀO lúc {DateTime.Now:HH:mm:ss.fff}");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Lỗi chụp ảnh Làn Vào: {ex.Message}");
                SetFooterStatus($"Lỗi chụp ảnh Làn Vào: {ex.Message}");
            }
        }

        private async Task UpdateInLaneUiAsync(PlateRecognitionResult? anprResult, string triggerSource, string filePlate, string fileOverview)
        {
            lblInTimeVal.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            if (anprResult != null && anprResult.IsSuccess)
            {
                txtInPlate.Text = anprResult.FormattedPlate;
                string cleanPlate = anprResult.CleanPlate;

                Vehicle? vehicle = null;
                Person? person = null;
                Department? department = null;
                Company? company = null;
                Contractor? contractor = null;

                try
                {
                    vehicle = await _vehicleRepo.FindOneAsync(v => v.PlateNumber == cleanPlate && !v.IsDeleted);
                    if (vehicle != null && !string.IsNullOrEmpty(vehicle.OwnerPersonId))
                    {
                        person = await _personRepo.GetByIdAsync(vehicle.OwnerPersonId!);
                        if (person != null)
                        {
                            if (!string.IsNullOrEmpty(person.DepartmentId))
                            {
                                department = await _departmentRepo.GetByIdAsync(person.DepartmentId!);
                            }
                            if (!string.IsNullOrEmpty(person.CompanyId))
                            {
                                company = await _companyRepo.GetByIdAsync(person.CompanyId!);
                            }
                            if (!string.IsNullOrEmpty(person.ContractorId))
                            {
                                contractor = await _contractorRepo.GetByIdAsync(person.ContractorId!);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Warning($"Lỗi truy vấn CSDL xe vào: {ex.Message}");
                }

                bool isRegistered = person != null || vehicle != null;
                string ownerName = person?.FullName ?? "Xe lạ";
                string deptName = department?.Name ?? company?.Name ?? contractor?.Name ?? (isRegistered ? "Đơn vị nội bộ" : "Khách vãng lai");
                VehicleType vType = vehicle?.Type ?? VehicleType.Car;

                lblInOwnerVal.Text = ownerName;
                lblInDeptVal.Text = deptName;
                lblInTypeVal.Text = vType == VehicleType.Car ? "Ô tô" : "Xe máy";

                if (isRegistered)
                {
                    lblInStatusVal.Text = "Cho phép vào - Xe nội bộ / Đã đăng ký";
                    lblInStatusVal.ForeColor = Color.FromArgb(40, 140, 70); // Green
                }
                else
                {
                    lblInStatusVal.Text = "Cho phép vào - Xe lạ / Khách vãng lai";
                    lblInStatusVal.ForeColor = Color.FromArgb(0, 120, 215); // Friendly Blue - không chặn ai!
                }

                try
                {
                    var session = ParkingSession.CheckIn(
                        "LANE-IN-01",
                        cleanPlate,
                        fileOverview,
                        filePlate,
                        isRegistered ? ownerName : null,
                        vType,
                        $"Nguồn: {triggerSource}, Conf: {anprResult.Confidence:P0}, Time: {anprResult.DurationMs}ms{(isRegistered ? " [Nội bộ]" : " [Xe lạ]")}");

                    await _sessionRepo.AddAsync(session);
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, "Lỗi lưu ParkingSession vào MongoDB");
                }

                AppNotificationService.NotifySuccess(
                    NotificationCategory.LaneIn,
                    "Nhận diện xe vào",
                    $"Biển số: {anprResult.FormattedPlate} - {ownerName} ({(isRegistered ? deptName : "Xe lạ")}) (Độ tin cậy: {anprResult.Confidence:P0}, {anprResult.DurationMs}ms)",
                    anprResult.FormattedPlate);
            }
            else
            {
                // Khi không nhận dạng được biển số, vẫn ghi nhận và cho phép vào như xe lạ
                string unknownPlate = "UNKNOWN_" + DateTime.Now.ToString("HHmmss");
                txtInPlate.Text = "Không nhận dạng được";
                lblInOwnerVal.Text = "Xe lạ";
                lblInDeptVal.Text = "Khách vãng lai";
                lblInTypeVal.Text = "Ô tô";
                lblInStatusVal.Text = "Cho phép vào - Ghi nhận hình ảnh (Không đọc được biển)";
                lblInStatusVal.ForeColor = Color.FromArgb(200, 120, 30);

                try
                {
                    var session = ParkingSession.CheckIn(
                        "LANE-IN-01",
                        unknownPlate,
                        fileOverview,
                        filePlate,
                        null,
                        VehicleType.Car,
                        $"Nguồn: {triggerSource}, Không nhận dạng được biển số lúc vào");

                    await _sessionRepo.AddAsync(session);
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, "Lỗi lưu ParkingSession không biển số vào MongoDB");
                }

                AppNotificationService.NotifyWarning(
                    NotificationCategory.LaneIn,
                    "Nhận diện biển số",
                    "Chụp ảnh thành công và cho phép vào. Không nhận dạng được biển số xe.",
                    triggerSource);
            }
        }

        private async Task CaptureOutLaneAsync(string triggerSource)
        {
            try
            {
                string todayFolder = Path.Combine(_captureDir, DateTime.Now.ToString("yyyy-MM-dd"));
                if (!Directory.Exists(todayFolder))
                {
                    Directory.CreateDirectory(todayFolder);
                }

                string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
                string filePlate = Path.Combine(todayFolder, $"{timeStamp}_{triggerSource}_plate.jpg");
                string fileOverview = Path.Combine(todayFolder, $"{timeStamp}_{triggerSource}_panoramic.jpg");

                // Chụp song song trực tiếp ra file bằng Native SDK tốc độ cao
                var tPlate = _outPlateCam.CaptureToFileAsync(filePlate);
                var tOverview = _outOverviewCam.CaptureToFileAsync(fileOverview);

                await Task.WhenAll(tPlate, tOverview);

                // Hiển thị ảnh an toàn lên UI
                if (File.Exists(filePlate)) DisplayCapturedImage(picOutPlate, filePlate);
                if (File.Exists(fileOverview)) DisplayCapturedImage(picOutOverview, fileOverview);

                // Nhận diện biển số xe bằng SimpleLPR3 Engine
                PlateRecognitionResult? anprResult = null;
                if (File.Exists(filePlate))
                {
                    anprResult = await _anprService.RecognizeAsync(filePlate);
                }

                // Cập nhật thông tin giao diện và kiểm tra lượt ra
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(async () => await UpdateOutLaneUiAsync(anprResult, triggerSource, filePlate, fileOverview)));
                }
                else
                {
                    await UpdateOutLaneUiAsync(anprResult, triggerSource, filePlate, fileOverview);
                }

                SetFooterStatus($"📸 Đã chụp và xử lý LÀN RA lúc {DateTime.Now:HH:mm:ss.fff}");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Lỗi chụp ảnh Làn Ra: {ex.Message}");
                SetFooterStatus($"Lỗi chụp ảnh Làn Ra: {ex.Message}");
            }
        }

        private async Task UpdateOutLaneUiAsync(PlateRecognitionResult? anprResult, string triggerSource, string filePlate, string fileOverview)
        {
            lblOutTimeVal.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");

            if (anprResult != null && anprResult.IsSuccess)
            {
                txtOutPlate.Text = anprResult.FormattedPlate;
                string cleanPlate = anprResult.CleanPlate;

                ParkingSession? activeSession = null;
                Vehicle? vehicle = null;
                Person? person = null;
                Department? department = null;
                Company? company = null;
                Contractor? contractor = null;

                try
                {
                    activeSession = await _sessionRepo.FindOneAsync(s => s.PlateNumber == cleanPlate && s.Status == ParkingSessionStatus.Active && !s.IsDeleted);
                    vehicle = await _vehicleRepo.FindOneAsync(v => v.PlateNumber == cleanPlate && !v.IsDeleted);
                    if (vehicle != null && !string.IsNullOrEmpty(vehicle.OwnerPersonId))
                    {
                        person = await _personRepo.GetByIdAsync(vehicle.OwnerPersonId!);
                        if (person != null)
                        {
                            if (!string.IsNullOrEmpty(person.DepartmentId))
                            {
                                department = await _departmentRepo.GetByIdAsync(person.DepartmentId!);
                            }
                            if (!string.IsNullOrEmpty(person.CompanyId))
                            {
                                company = await _companyRepo.GetByIdAsync(person.CompanyId!);
                            }
                            if (!string.IsNullOrEmpty(person.ContractorId))
                            {
                                contractor = await _contractorRepo.GetByIdAsync(person.ContractorId!);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Warning($"Lỗi truy vấn CSDL xe ra: {ex.Message}");
                }

                bool isRegistered = person != null || vehicle != null;
                string ownerName = person?.FullName ?? activeSession?.PersonName ?? (isRegistered ? "Cán bộ / Nhân viên" : "Xe lạ");
                string deptName = department?.Name ?? company?.Name ?? contractor?.Name ?? (isRegistered ? "Đơn vị nội bộ" : "Khách vãng lai");
                VehicleType vType = activeSession?.VehicleType ?? vehicle?.Type ?? VehicleType.Car;

                lblOutOwnerVal.Text = ownerName;
                lblOutDeptVal.Text = deptName;
                lblOutTypeVal.Text = vType == VehicleType.Car ? "Ô tô" : "Xe máy";

                if (activeSession != null)
                {
                    activeSession.CheckOut("LANE-OUT-01", fileOverview, filePlate, $"Nguồn: {triggerSource}, Conf: {anprResult.Confidence:P0}, Time: {anprResult.DurationMs}ms");
                    try
                    {
                        await _sessionRepo.UpdateAsync(activeSession);
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error(ex, "Lỗi cập nhật Checkout ParkingSession");
                    }

                    string durationText = activeSession.Duration.HasValue
                        ? $"{activeSession.Duration.Value.Hours}h {activeSession.Duration.Value.Minutes}m"
                        : "";

                    lblOutStatusVal.Text = $"Cho phép ra ({durationText}) - {(isRegistered ? "Nội bộ" : "Xe lạ")}";
                    lblOutStatusVal.ForeColor = Color.FromArgb(40, 140, 70); // Green

                    AppNotificationService.NotifySuccess(
                        NotificationCategory.LaneOut,
                        "Nhận diện xe ra",
                        $"Biển số: {anprResult.FormattedPlate} - {ownerName} (Khớp lượt vào {activeSession.InTime:HH:mm:ss}, đỗ {durationText})",
                        anprResult.FormattedPlate);
                }
                else
                {
                    // Xe ra không có lượt vào trước đó -> VẪN CHO PHÉP RA, tạo UnmatchedOut Session
                    var unmatchedSession = ParkingSession.CreateUnmatchedOut(
                        "LANE-OUT-01",
                        cleanPlate,
                        fileOverview,
                        filePlate,
                        isRegistered ? ownerName : null,
                        vType,
                        $"Nguồn: {triggerSource}, Conf: {anprResult.Confidence:P0} (Ghi nhận xe ra không có lượt vào)"
                    );

                    try
                    {
                        await _sessionRepo.AddAsync(unmatchedSession);
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error(ex, "Lỗi lưu Unmatched ParkingSession vào MongoDB");
                    }

                    lblOutStatusVal.Text = "Cho phép ra (Ghi nhận xe ra không có lượt vào)";
                    lblOutStatusVal.ForeColor = Color.FromArgb(0, 120, 215); // Friendly Blue - không chặn ai!

                    AppNotificationService.NotifyInfo(
                        NotificationCategory.LaneOut,
                        "Xe ra ghi nhận mới",
                        $"Biển số {anprResult.FormattedPlate} - {ownerName} (Không tìm thấy lượt vào trước đó - đã tự động ghi nhận)",
                        anprResult.FormattedPlate);
                }
            }
            else
            {
                string unknownPlate = "UNKNOWN_OUT_" + DateTime.Now.ToString("HHmmss");
                txtOutPlate.Text = "Không nhận dạng được";
                lblOutOwnerVal.Text = "Xe lạ";
                lblOutDeptVal.Text = "Khách vãng lai";
                lblOutTypeVal.Text = "Ô tô";
                lblOutStatusVal.Text = "Cho phép ra - Ghi nhận hình ảnh (Không đọc được biển)";
                lblOutStatusVal.ForeColor = Color.FromArgb(200, 120, 30);

                try
                {
                    var unmatchedSession = ParkingSession.CreateUnmatchedOut(
                        "LANE-OUT-01",
                        unknownPlate,
                        fileOverview,
                        filePlate,
                        null,
                        VehicleType.Car,
                        $"Nguồn: {triggerSource}, Không nhận dạng được biển số lúc ra"
                    );
                    await _sessionRepo.AddAsync(unmatchedSession);
                }
                catch (Exception ex)
                {
                    AppLogger.Error(ex, "Lỗi lưu Unmatched ParkingSession không biển số vào MongoDB");
                }

                AppNotificationService.NotifyWarning(
                    NotificationCategory.LaneOut,
                    "Xe ra",
                    "Chụp ảnh thành công và cho phép ra. Không nhận dạng được biển số rõ ràng.",
                    triggerSource);
            }
        }

        /// <summary>
        /// Nạp ảnh từ file lên PictureBox an toàn tuyệt đối:
        /// 1. Đọc byte[] để không khóa (lock) file trên ổ đĩa.
        /// 2. Chuyển đổi và gán ảnh trên UI Thread.
        /// 3. Gọi Dispose() ngay lập tức cho ảnh cũ, giải phóng GDI+ Handle triệt để.
        /// </summary>
        private void DisplayCapturedImage(PictureBox picBox, string filePath)
        {
            if (!File.Exists(filePath)) return;

            try
            {
                byte[] bytes = File.ReadAllBytes(filePath);

                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => SetPictureBoxImage(picBox, bytes)));
                }
                else
                {
                    SetPictureBoxImage(picBox, bytes);
                }
            }
            catch
            {
                // Bỏ qua lỗi render nếu file chưa sẵn sàng
            }
        }

        private void SetPictureBoxImage(PictureBox picBox, byte[] bytes)
        {
            try
            {
                using var ms = new MemoryStream(bytes);
                using var tempImg = Image.FromStream(ms);
                var newImg = new Bitmap(tempImg);

                var oldImg = picBox.Image;
                picBox.Image = newImg;
                oldImg?.Dispose();
            }
            catch
            {
                // Xử lý an toàn
            }
        }

        #endregion

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
                // 1. Dừng và giải phóng 4 Camera
                _inPlateCam.Dispose();
                _inOverviewCam.Dispose();
                _outPlateCam.Dispose();
                _outOverviewCam.Dispose();

                // 2. Giải phóng dịch vụ ANPR
                _anprService.Dispose();

                // 3. Dừng và giải phóng Controller C3-200
                _controller.Dispose();

                // 4. Dọn dẹp SDK tĩnh
                PlateCameraService.CleanupSdk();
                OverviewCameraService.CleanupSdk();

                // 5. Giải phóng tài nguyên GDI+ Cache
                _fontBoldStatus.Dispose();
                _fontSubStatus.Dispose();
                _penNormalBorder.Dispose();
                _penConnectingBorder.Dispose();
                _penErrorBorder.Dispose();
                _brushConnectingBg.Dispose();
                _brushConnectingText.Dispose();
                _brushErrorBg.Dispose();
                _brushErrorText.Dispose();
                _brushSubText.Dispose();
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"[FormClosing Error] {ex.Message}", "FrmMain");
            }
        }
    }
}
