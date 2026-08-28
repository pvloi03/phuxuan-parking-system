using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Services.Camera;
using PhuXuanParkingSystem.Services.DeviceConfig;
using PhuXuanParkingSystem.Services.Logging;
using PhuXuanParkingSystem.Services.Notification;
using System;
using System.Configuration;
using System.Linq;
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
        private readonly Pen _penStreamingBorder = new(Color.FromArgb(40, 167, 69), 2); // Green
        private readonly SolidBrush _brushConnectingBg = new(Color.FromArgb(26, 30, 35));
        private readonly SolidBrush _brushConnectingText = new(Color.FromArgb(100, 180, 255));
        private readonly SolidBrush _brushErrorBg = new(Color.FromArgb(20, 22, 25));
        private readonly SolidBrush _brushErrorText = new(Color.FromArgb(235, 75, 75));
        private readonly SolidBrush _brushSubText = new(Color.FromArgb(180, 190, 200));
        private readonly SolidBrush _brushStreamingText = new(Color.FromArgb(40, 167, 69)); // Green

        private void LoadConfigurations()
        {
            try
            {
                // Thư mục lưu trữ ảnh chụp biển số & toàn cảnh
                string relativePath = ConfigurationManager.AppSettings["CaptureSavePath"] ?? "Captures";
                _captureDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
                if (!Directory.Exists(_captureDir))
                {
                    Directory.CreateDirectory(_captureDir);
                }

                SetFooterStatus("Khởi tạo hệ thống thành công. Đang tải cấu hình thiết bị từ CSDL MongoDB...");
            }
            catch (Exception ex)
            {
                SetFooterStatus($"Lỗi khởi tạo thư mục lưu ảnh: {ex.Message}");
            }
        }

        private async Task LoadConfigurationsFromDbAsync()
        {
            try
            {
                // Sử dụng DeviceConfigService - đã có Cache + Hash để detect thay đổi
                var result = await _deviceConfigService.LoadConfigAsync();

                if (!result.Success || result.InPlateCamera == null && result.OutPlateCamera == null)
                {
                    AppLogger.Warning("[Hardware Sync] Nạp cấu hình thất bại hoặc không có thiết bị.");
                    return;
                }

                // Áp dụng cấu hình vào Camera Services
                if (result.InPlateCamera != null)
                {
                    ApplyDeviceToConfig(_inPlateCam.Config, result.InPlateCamera);
                    _inPlateCamDeviceId = result.InPlateCamera.Id;
                }

                if (result.InOverviewCamera != null)
                {
                    ApplyDeviceToConfig(_inOverviewCam.Config, result.InOverviewCamera);
                    _inOverviewCamDeviceId = result.InOverviewCamera.Id;
                }

                if (result.OutPlateCamera != null)
                {
                    ApplyDeviceToConfig(_outPlateCam.Config, result.OutPlateCamera);
                    _outPlateCamDeviceId = result.OutPlateCamera.Id;
                }

                if (result.OutOverviewCamera != null)
                {
                    ApplyDeviceToConfig(_outOverviewCam.Config, result.OutOverviewCamera);
                    _outOverviewCamDeviceId = result.OutOverviewCamera.Id;
                }

                if (!string.IsNullOrEmpty(result.ControllerIp))
                {
                    _controllerIp = result.ControllerIp ?? _controllerIp;
                    _controllerPort = result.ControllerPort > 0 ? result.ControllerPort : _controllerPort;
                    _controllerDeviceId = result.Controller?.Id ?? string.Empty;
                }

                // Log chi tiết cấu hình
                AppLogger.Information(
                    $"[Hardware Sync] Nạp cấu hình thành công trong {result.LoadTime.TotalMilliseconds:F0}ms " +
                    $"(InPlate: {_inPlateCam.Config.Ip}, InOvw: {_inOverviewCam.Config.Ip}, " +
                    $"OutPlt: {_outPlateCam.Config.Ip}, OutOvw: {_outOverviewCam.Config.Ip}, " +
                    $"Ctrl: {_controllerIp}:{_controllerPort}).");

                // Log warnings nếu có
                foreach (var warning in result.Warnings)
                {
                    AppLogger.Warning($"[Hardware Sync] Cảnh báo: {warning}");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"[Hardware Sync] Lỗi nạp cấu hình: {ex.Message}");
            }
        }

        private static void ApplyDeviceToConfig(CameraConfig config, Device dev)
        {
            if (config == null || dev == null) return;
            config.Ip = dev.IpAddress;
            config.Port = (ushort)dev.Port;
            if (!string.IsNullOrEmpty(dev.UserName)) config.UserName = dev.UserName!;
            if (!string.IsNullOrEmpty(dev.Password)) config.Password = dev.Password!;
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

                _inPlateState = inPltTask.Result ? DeviceConnectionState.Streaming : DeviceConnectionState.Error;
                _inOverviewState = inOvwTask.Result ? DeviceConnectionState.Streaming : DeviceConnectionState.Error;
                _outPlateState = outPltTask.Result ? DeviceConnectionState.Streaming : DeviceConnectionState.Error;
                _outOverviewState = outOvwTask.Result ? DeviceConnectionState.Streaming : DeviceConnectionState.Error;
                bool ctrlOk = ctrlTask.Result;

                // Start preview cho cameras
                if (inPltTask.Result) _inPlateCam.StartPreview(pnlInPlateVideo.Handle);
                if (inOvwTask.Result) _inOverviewCam.StartPreview(pnlInOverviewVideo.Handle);
                if (outPltTask.Result) _outPlateCam.StartPreview(pnlOutPlateVideo.Handle);
                if (outOvwTask.Result) _outOverviewCam.StartPreview(pnlOutOverviewVideo.Handle);

                InvalidateCameraPanels();

                bool inAllOk = _inPlateState == DeviceConnectionState.Streaming && _inOverviewState == DeviceConnectionState.Streaming;
                bool outAllOk = _outPlateState == DeviceConnectionState.Streaming && _outOverviewState == DeviceConnectionState.Streaming;

                string inStatus = inAllOk ? "Làn Vào: Sẵn sàng" : "Làn Vào: Có Camera Mất Tín Hiệu";
                string outStatus = outAllOk ? "Làn Ra: Sẵn sàng" : "Làn Ra: Có Camera Mất Tín Hiệu";
                string ctrlStatus = ctrlOk ? "Radar C3-200: Đã kết nối" : "Radar C3-200: Mất tín hiệu";

                SetHeaderStatus($"{inStatus}  |  {outStatus}  |  {ctrlStatus}");
                SetFooterStatus("Hệ thống hoạt động bình thường. Tự động nhận diện xe khi qua vùng cảm biến Radar.");
            }
            catch (Exception ex)
            {
                _inPlateState = DeviceConnectionState.Error;
                _inOverviewState = DeviceConnectionState.Error;
                _outPlateState = DeviceConnectionState.Error;
                _outOverviewState = DeviceConnectionState.Error;
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
            string subtitle = $"{camTitle} ({ip ?? "IP trống"})";

            switch (state)
            {
                case DeviceConnectionState.Disconnected:
                    // Không hiển thị gì - panel trống
                    return;

                case DeviceConnectionState.Connecting:
                    DrawConnectingStatus(pnl, subtitle, e);
                    return;

                case DeviceConnectionState.Connected:
                    // Border nhẹ - đã kết nối nhưng chưa streaming
                    e.Graphics.DrawRectangle(_penNormalBorder, 0, 0, pnl.Width - 1, pnl.Height - 1);
                    return;

                case DeviceConnectionState.Streaming:
                    // Border xanh + chỉ báo đang streaming
                    e.Graphics.DrawRectangle(_penStreamingBorder, 0, 0, pnl.Width - 1, pnl.Height - 1);
                    DrawStreamingIndicator(pnl, subtitle, e);
                    return;

                case DeviceConnectionState.Error:
                    DrawErrorStatus(pnl, subtitle, e);
                    return;
            }
        }

        private void DrawConnectingStatus(Panel pnl, string subtitle, PaintEventArgs e)
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

        private void DrawStreamingIndicator(Panel pnl, string subtitle, PaintEventArgs e)
        {
            // Small green dot + "LIVE" text in corner
            string title = "● LIVE";
            var szTitle = e.Graphics.MeasureString(title, _fontSubStatus);
            e.Graphics.DrawString(title, _fontSubStatus, _brushStreamingText, 8, 8);

            // Small subtitle at bottom
            var szSub = e.Graphics.MeasureString(subtitle, _fontSubStatus);
            float xSub = (pnl.Width - szSub.Width) / 2;
            float ySub = pnl.Height - szSub.Height - 8;
            e.Graphics.DrawString(subtitle, _fontSubStatus, _brushSubText, xSub, ySub);
        }

        private void DrawErrorStatus(Panel pnl, string subtitle, PaintEventArgs e)
        {
            e.Graphics.FillRectangle(_brushErrorBg, 0, 0, pnl.Width, pnl.Height);
            e.Graphics.DrawRectangle(_penErrorBorder, 1, 1, pnl.Width - 3, pnl.Height - 3);

            string title = "❌ MẤT KẾT NỐI";
            var szTitle = e.Graphics.MeasureString(title, _fontBoldStatus);
            var szSub = e.Graphics.MeasureString(subtitle, _fontSubStatus);

            float startY = (pnl.Height - (szTitle.Height + szSub.Height + 6)) / 2;
            float xTitle = (pnl.Width - szTitle.Width) / 2;
            float xSub = (pnl.Width - szSub.Width) / 2;

            e.Graphics.DrawString(title, _fontBoldStatus, _brushErrorText, xTitle, startY);
            e.Graphics.DrawString(subtitle, _fontSubStatus, _brushSubText, xSub, startY + szTitle.Height + 6);
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
            _penStreamingBorder.Dispose();
            _brushConnectingBg.Dispose();
            _brushConnectingText.Dispose();
            _brushErrorBg.Dispose();
            _brushErrorText.Dispose();
            _brushSubText.Dispose();
            _brushStreamingText.Dispose();
        }
    }
}
