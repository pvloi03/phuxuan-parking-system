using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Services.Camera;
using PhuXuanParkingSystem.Services.Logging;
using PhuXuanParkingSystem.Services.Notification;
using System;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhuXuanParkingSystem.Forms
{
    public partial class FrmMain
    {
        // ── 4 Camera (2 Làn Vào, 2 Làn Ra) ──────────────────────────────────
        private readonly PlateCameraService _inPlateCam = new();
        private readonly OverviewCameraService _inOverviewCam = new();
        private readonly PlateCameraService _outPlateCam = new();
        private readonly OverviewCameraService _outOverviewCam = new();

        // ── Trạng thái kết nối của từng Camera ──────────────────────────────
        private DeviceConnectionState _inPlateState = DeviceConnectionState.Connecting;
        private DeviceConnectionState _inOverviewState = DeviceConnectionState.Connecting;
        private DeviceConnectionState _outPlateState = DeviceConnectionState.Connecting;
        private DeviceConnectionState _outOverviewState = DeviceConnectionState.Connecting;

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

        private async Task AutoConnectAllAsync()
        {
            await LoadConfigurationsFromDbAsync();

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

                if (_inPlateState == DeviceConnectionState.Connected) _inPlateCam.StartPreview(pnlInPlateVideo.Handle);
                if (_inOverviewState == DeviceConnectionState.Connected) _inOverviewCam.StartPreview(pnlInOverviewVideo.Handle);
                if (_outPlateState == DeviceConnectionState.Connected) _outPlateCam.StartPreview(pnlOutPlateVideo.Handle);
                if (_outOverviewState == DeviceConnectionState.Connected) _outOverviewCam.StartPreview(pnlOutOverviewVideo.Handle);

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
                e.Graphics.DrawRectangle(_penNormalBorder, 0, 0, pnl.Width - 1, pnl.Height - 1);
                return;
            }

            string subtitle = $"{camTitle} ({ip ?? "IP trống"})";

            if (state == DeviceConnectionState.Connecting)
            {
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
            else
            {
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

        private void CleanupCameras()
        {
            _inPlateCam.Dispose();
            _inOverviewCam.Dispose();
            _outPlateCam.Dispose();
            _outOverviewCam.Dispose();
            PlateCameraService.CleanupSdk();
            OverviewCameraService.CleanupSdk();

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
    }
}
