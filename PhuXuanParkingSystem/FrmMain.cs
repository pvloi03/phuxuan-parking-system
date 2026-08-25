using PhuXuanParkingSystem.Services.ANPR;
using PhuXuanParkingSystem.Services.Logging;
using PhuXuanParkingSystem.Services.Notification;
using PhuXuanParkingSystem.Services.Camera;
using PhuXuanParkingSystem.Services.Controller;
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

        #region Tự Động Kết Nối (Auto-Connect)

        private async Task AutoConnectAllAsync()
        {
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

                if (tPlate.Result && tOverview.Result)
                {
                    AppNotificationService.NotifySuccess(
                        NotificationCategory.LaneIn, "Chụp ảnh Làn Vào", $"Đã chụp và lưu ảnh xe vào thành công ({Path.GetFileName(filePlate)}, {Path.GetFileName(fileOverview)}).", triggerSource);
                }
                else
                {
                    AppNotificationService.NotifyWarning(NotificationCategory.LaneIn, "Chụp ảnh Làn Vào", "Chụp ảnh từ một trong các camera Làn Vào không thành công.", triggerSource);
                }

                SetFooterStatus($"📸 Đã chụp và lưu ảnh LÀN VÀO lúc {DateTime.Now:HH:mm:ss.fff}");

                // Tự động nhận diện biển số ANPR Làn Vào
                if (File.Exists(filePlate))
                {
                    _ = Task.Run(async () =>
                    {
                        AppLogger.Information($"[LÀN VÀO] Bắt đầu nhận diện biển số từ file: {Path.GetFileName(filePlate)}", "ANPR");
                        var anprResult = await AnprLaneCoordinator.Instance.ProcessLaneInFileAsync(filePlate);

                        BeginInvoke(new Action(() =>
                        {
                            if (anprResult.IsSuccess && !string.IsNullOrWhiteSpace(anprResult.LicensePlate))
                            {
                                txtInPlate.Text = anprResult.LicensePlate;
                                txtInPlate.ForeColor = Color.FromArgb(20, 30, 40);
                                SetFooterStatus($"🔍 [LÀN VÀO] Biển số: {anprResult.LicensePlate} ({anprResult.ProcessTimeMs}ms)");
                            }
                            else
                            {
                                txtInPlate.Text = "---";
                                txtInPlate.ForeColor = Color.FromArgb(120, 130, 140);
                                string rawPreview = string.IsNullOrWhiteSpace(anprResult.RawOcrText) ? "Không có ký tự" : anprResult.RawOcrText.Replace("\r", "").Replace("\n", " ");
                                SetFooterStatus($"🔍 [LÀN VÀO] OCR thô: '{rawPreview}'");
                            }
                        }));

                        if (anprResult.IsSuccess && !string.IsNullOrWhiteSpace(anprResult.LicensePlate))
                        {
                            AppNotificationService.NotifySuccess(
                                NotificationCategory.Vehicle,
                                "Nhận diện biển số Vào",
                                $"Biển số: {anprResult.LicensePlate} (Độ tin cậy: {anprResult.Confidence:P0}, Thời gian: {anprResult.ProcessTimeMs}ms)",
                                anprResult.LicensePlate);
                        }
                        else
                        {
                            AppNotificationService.NotifyWarning(
                                NotificationCategory.Vehicle,
                                "Nhận diện biển số Vào",
                                $"Ký tự thô: '{anprResult.RawOcrText}'. Chưa trích xuất được biển số hợp lệ.",
                                filePlate);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                SetFooterStatus($"Lỗi chụp ảnh Làn Vào: {ex.Message}");
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

                if (tPlate.Result && tOverview.Result)
                {
                    AppNotificationService.NotifySuccess(NotificationCategory.LaneOut, "Chụp ảnh Làn Ra", $"Đã chụp và lưu ảnh xe ra thành công ({Path.GetFileName(filePlate)}, {Path.GetFileName(fileOverview)}).", triggerSource);
                }
                else
                {
                    AppNotificationService.NotifyWarning(NotificationCategory.LaneOut, "Chụp ảnh Làn Ra", "Chụp ảnh từ một trong các camera Làn Ra không thành công.", triggerSource);
                }

                SetFooterStatus($"📸 Đã chụp và lưu ảnh LÀN RA lúc {DateTime.Now:HH:mm:ss.fff}");

                // Tự động nhận diện biển số ANPR Làn Ra
                if (File.Exists(filePlate))
                {
                    _ = Task.Run(async () =>
                    {
                        AppLogger.Information($"[LÀN RA] Bắt đầu nhận diện biển số từ file: {Path.GetFileName(filePlate)}", "ANPR");
                        var anprResult = await AnprLaneCoordinator.Instance.ProcessLaneOutFileAsync(filePlate);

                        BeginInvoke(new Action(() =>
                        {
                            if (anprResult.IsSuccess && !string.IsNullOrWhiteSpace(anprResult.LicensePlate))
                            {
                                txtOutPlate.Text = anprResult.LicensePlate;
                                txtOutPlate.ForeColor = Color.FromArgb(20, 30, 40);
                                SetFooterStatus($"🔍 [LÀN RA] Biển số: {anprResult.LicensePlate} ({anprResult.ProcessTimeMs}ms)");
                            }
                            else
                            {
                                txtOutPlate.Text = "---";
                                txtOutPlate.ForeColor = Color.FromArgb(120, 130, 140);
                                string rawPreview = string.IsNullOrWhiteSpace(anprResult.RawOcrText) ? "Không có ký tự" : anprResult.RawOcrText.Replace("\r", "").Replace("\n", " ");
                                SetFooterStatus($"🔍 [LÀN RA] OCR thô: '{rawPreview}'");
                            }
                        }));

                        if (anprResult.IsSuccess && !string.IsNullOrWhiteSpace(anprResult.LicensePlate))
                        {
                            AppNotificationService.NotifySuccess(
                                NotificationCategory.Vehicle,
                                "Nhận diện biển số Ra",
                                $"Biển số: {anprResult.LicensePlate} (Độ tin cậy: {anprResult.Confidence:P0}, Thời gian: {anprResult.ProcessTimeMs}ms)",
                                anprResult.LicensePlate);
                        }
                        else
                        {
                            AppNotificationService.NotifyWarning(
                                NotificationCategory.Vehicle,
                                "Nhận diện biển số Ra",
                                $"Ký tự thô: '{anprResult.RawOcrText}'. Chưa trích xuất được biển số hợp lệ.",
                                filePlate);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                SetFooterStatus($"Lỗi chụp ảnh Làn Ra: {ex.Message}");
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

                                // 2. Dừng và giải phóng ANPR Engine
                AnprLaneCoordinator.Instance.Dispose();

                // 3. Dừng và giải phóng Controller C3-200
                _controller.Dispose();

                // 3. Dọn dẹp SDK tĩnh
                PlateCameraService.CleanupSdk();
                OverviewCameraService.CleanupSdk();

                // 4. Giải phóng tài nguyên GDI+ Cache
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
