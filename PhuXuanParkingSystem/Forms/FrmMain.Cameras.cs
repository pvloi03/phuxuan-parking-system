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
                var lanes = await _laneRepo.FindAsync(l => !l.IsDeleted && l.IsActive);
                var devices = await _deviceRepo.FindAsync(d => !d.IsDeleted);

                if (devices == null || devices.Count == 0)
                {
                    AppLogger.Warning("Không tìm thấy thiết bị nào trong CSDL MongoDB (sử dụng cấu hình mặc định).");
                    return;
                }

                var devDict = new System.Collections.Generic.Dictionary<string, Device>(StringComparer.OrdinalIgnoreCase);
                foreach (var d in devices)
                {
                    if (!string.IsNullOrEmpty(d.Id))
                    {
                        devDict[d.Id] = d;
                    }
                }

                bool inPltMapped = false;
                bool inOvwMapped = false;
                bool outPltMapped = false;
                bool outOvwMapped = false;
                bool ctrlMapped = false;

                // 1. Ưu tiên nạp cấu hình theo liên kết trong LÀN KIỂM SOÁT (LANES)
                if (lanes != null && lanes.Count > 0)
                {
                    var inLane = System.Linq.Enumerable.FirstOrDefault(lanes, l => l.Direction == LaneDirection.In);
                    if (inLane != null)
                    {
                        if (!string.IsNullOrEmpty(inLane.PlateCameraDeviceId) && devDict.TryGetValue(inLane.PlateCameraDeviceId!, out var plDev))
                        {
                            ApplyDeviceToConfig(_inPlateCam.Config, plDev);
                            inPltMapped = true;
                        }
                        if (!string.IsNullOrEmpty(inLane.OverviewCameraDeviceId) && devDict.TryGetValue(inLane.OverviewCameraDeviceId!, out var ovDev))
                        {
                            ApplyDeviceToConfig(_inOverviewCam.Config, ovDev);
                            inOvwMapped = true;
                        }
                        if (!string.IsNullOrEmpty(inLane.ControllerDeviceId) && devDict.TryGetValue(inLane.ControllerDeviceId!, out var cDev))
                        {
                            _controllerIp = cDev.IpAddress;
                            _controllerPort = cDev.Port;
                            ctrlMapped = true;
                        }
                    }

                    var outLane = System.Linq.Enumerable.FirstOrDefault(lanes, l => l.Direction == LaneDirection.Out);
                    if (outLane != null)
                    {
                        if (!string.IsNullOrEmpty(outLane.PlateCameraDeviceId) && devDict.TryGetValue(outLane.PlateCameraDeviceId!, out var plDev))
                        {
                            ApplyDeviceToConfig(_outPlateCam.Config, plDev);
                            outPltMapped = true;
                        }
                        if (!string.IsNullOrEmpty(outLane.OverviewCameraDeviceId) && devDict.TryGetValue(outLane.OverviewCameraDeviceId!, out var ovDev))
                        {
                            ApplyDeviceToConfig(_outOverviewCam.Config, ovDev);
                            outOvwMapped = true;
                        }
                        if (!string.IsNullOrEmpty(outLane.ControllerDeviceId) && !ctrlMapped && devDict.TryGetValue(outLane.ControllerDeviceId!, out var cDev))
                        {
                            _controllerIp = cDev.IpAddress;
                            _controllerPort = cDev.Port;
                            ctrlMapped = true;
                        }
                    }
                }

                // 2. Fallback thông minh theo Phân loại thiết bị (DeviceType) và Tên/Mã nếu Làn chưa gán ID
                foreach (var dev in devices)
                {
                    if (!inPltMapped && (dev.Code == "CAM-IN-PLT" || dev.Code == "CAM_IN_PLATE" || (dev.Type == DeviceType.PlateCamera && dev.Name.IndexOf("Vào", StringComparison.OrdinalIgnoreCase) >= 0)))
                    {
                        ApplyDeviceToConfig(_inPlateCam.Config, dev);
                        inPltMapped = true;
                    }
                    else if (!inOvwMapped && (dev.Code == "CAM-IN-OVW" || dev.Code == "CAM_IN_OVW" || (dev.Type == DeviceType.OverviewCamera && dev.Name.IndexOf("Vào", StringComparison.OrdinalIgnoreCase) >= 0)))
                    {
                        ApplyDeviceToConfig(_inOverviewCam.Config, dev);
                        inOvwMapped = true;
                    }
                    else if (!outPltMapped && (dev.Code == "CAM-OUT-PLT" || dev.Code == "CAM_OUT_PLATE" || (dev.Type == DeviceType.PlateCamera && dev.Name.IndexOf("Ra", StringComparison.OrdinalIgnoreCase) >= 0)))
                    {
                        ApplyDeviceToConfig(_outPlateCam.Config, dev);
                        outPltMapped = true;
                    }
                    else if (!outOvwMapped && (dev.Code == "CAM-OUT-OVW" || dev.Code == "CAM_OUT_OVW" || (dev.Type == DeviceType.OverviewCamera && dev.Name.IndexOf("Ra", StringComparison.OrdinalIgnoreCase) >= 0)))
                    {
                        ApplyDeviceToConfig(_outOverviewCam.Config, dev);
                        outOvwMapped = true;
                    }
                    else if (!ctrlMapped && (dev.Code == "CTRL-C3-200" || dev.Type == DeviceType.Controller))
                    {
                        _controllerIp = dev.IpAddress;
                        _controllerPort = dev.Port;
                        ctrlMapped = true;
                    }
                }

                AppLogger.Information($"[Hardware Sync] Đã nạp cấu hình phần cứng từ CSDL MongoDB thành công (InPlate: {_inPlateCam.Config.Ip}, InOvw: {_inOverviewCam.Config.Ip}, OutPlt: {_outPlateCam.Config.Ip}, OutOvw: {_outOverviewCam.Config.Ip}, Ctrl: {_controllerIp}:{_controllerPort}).");
            }
            catch (Exception ex)
            {
                AppLogger.Warning($"Không thể nạp thiết bị từ CSDL (sử dụng cấu hình dự phòng App.config): {ex.Message}");
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
