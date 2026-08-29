using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Services.Camera;
using PhuXuanParkingSystem.Services.DeviceConfig;
using PhuXuanParkingSystem.Services.Logging;
using PhuXuanParkingSystem.Services.Notification;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhuXuanParkingSystem.Forms
{
    /// <summary>
    /// Vị trí / Vai trò Camera trên màn hình giám sát WinForms
    /// </summary>
    public enum CameraSlot
    {
        InPlate,
        InOverview,
        OutPlate,
        OutOverview
    }

    public partial class FrmMain
    {
        // ── 4 Camera Services (2 Làn Vào, 2 Làn Ra) ──────────────────────────
        private readonly PlateCameraService _inPlateCam = new();
        private readonly OverviewCameraService _inOverviewCam = new();
        private readonly PlateCameraService _outPlateCam = new();
        private readonly OverviewCameraService _outOverviewCam = new();

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

        private ICameraService GetCameraService(CameraSlot slot) => slot switch
        {
            CameraSlot.InPlate => _inPlateCam,
            CameraSlot.InOverview => _inOverviewCam,
            CameraSlot.OutPlate => _outPlateCam,
            CameraSlot.OutOverview => _outOverviewCam,
            _ => throw new ArgumentOutOfRangeException(nameof(slot))
        };

        private Panel GetCameraPanel(CameraSlot slot) => slot switch
        {
            CameraSlot.InPlate => pnlInPlateVideo,
            CameraSlot.InOverview => pnlInOverviewVideo,
            CameraSlot.OutPlate => pnlOutPlateVideo,
            CameraSlot.OutOverview => pnlOutOverviewVideo,
            _ => throw new ArgumentOutOfRangeException(nameof(slot))
        };

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

                if (!result.Success || (result.InPlateCamera == null && result.OutPlateCamera == null))
                {
                    AppLogger.Warning("[Hardware Sync] Nạp cấu hình thất bại hoặc không có thiết bị.");
                    return;
                }

                _activeDevices.Clear();
                _deviceIdToSlotMap.Clear();

                void BindSlot(CameraSlot slot, Device? dev, ICameraService cam)
                {
                    if (dev != null && dev.IsActive)
                    {
                        ApplyDeviceToConfig(cam.Config, dev);
                        _activeDevices[dev.Id] = dev;
                        _deviceIdToSlotMap[dev.Id] = slot;
                    }
                    else
                    {
                        cam.Config.Ip = string.Empty;
                        _slotStates[slot] = DeviceStatus.Disconnected;
                    }
                }

                BindSlot(CameraSlot.InPlate, result.InPlateCamera, _inPlateCam);
                BindSlot(CameraSlot.InOverview, result.InOverviewCamera, _inOverviewCam);
                BindSlot(CameraSlot.OutPlate, result.OutPlateCamera, _outPlateCam);
                BindSlot(CameraSlot.OutOverview, result.OutOverviewCamera, _outOverviewCam);

                if (result.Controller != null && result.Controller.IsActive && !string.IsNullOrEmpty(result.ControllerIp))
                {
                    _controllerIp = result.ControllerIp ?? _controllerIp;
                    _controllerPort = result.ControllerPort > 0 ? result.ControllerPort : _controllerPort;
                    _activeDevices[result.Controller.Id] = result.Controller;
                }

                // Log chi tiết cấu hình
                AppLogger.Information(
                    $"[Hardware Sync] Nạp cấu hình thành công {_activeDevices.Count} thiết bị trong {result.LoadTime.TotalMilliseconds:F0}ms " +
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

        private void DisconnectAllCamerasAndClearPanels()
        {
            foreach (var slot in (CameraSlot[])Enum.GetValues(typeof(CameraSlot)))
            {
                var cam = GetCameraService(slot);
                try
                {
                    cam.StopPreview();
                }
                catch (Exception ex)
                {
                    AppLogger.Warning($"[FrmMain] Lỗi StopPreview cho slot {slot}: {ex.Message}");
                }

                try
                {
                    cam.Logout();
                }
                catch (Exception ex)
                {
                    AppLogger.Warning($"[FrmMain] Lỗi Logout cho slot {slot}: {ex.Message}");
                }

                _slotStates[slot] = DeviceStatus.Disconnected;
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action(() =>
                {
                    pnlInPlateVideo.Invalidate();
                    pnlInPlateVideo.Update();
                    pnlInOverviewVideo.Invalidate();
                    pnlInOverviewVideo.Update();
                    pnlOutPlateVideo.Invalidate();
                    pnlOutPlateVideo.Update();
                    pnlOutOverviewVideo.Invalidate();
                    pnlOutOverviewVideo.Update();
                }));
            }
            else
            {
                pnlInPlateVideo.Invalidate();
                pnlInPlateVideo.Update();
                pnlInOverviewVideo.Invalidate();
                pnlInOverviewVideo.Update();
                pnlOutPlateVideo.Invalidate();
                pnlOutPlateVideo.Update();
                pnlOutOverviewVideo.Invalidate();
                pnlOutOverviewVideo.Update();
            }
        }

        private async Task AutoConnectAllAsync()
        {
            DisconnectAllCamerasAndClearPanels();

            await LoadConfigurationsFromDbAsync();

            RegisterDevicesWithHealthManager();
            RegisterDeviceAdapters();

            foreach (var slot in (CameraSlot[])Enum.GetValues(typeof(CameraSlot)))
            {
                var cam = GetCameraService(slot);
                _slotStates[slot] = !string.IsNullOrEmpty(cam.Config.Ip) ? DeviceStatus.Connecting : DeviceStatus.Disconnected;
            }

            InvalidateCameraPanels();

            AppNotificationService.NotifyInfo(NotificationCategory.System, "Khởi động hệ thống", "Đang tự động kết nối Camera và Controller...");
            SetHeaderStatus("Đang tự động kết nối Camera và Controller...");
            SetFooterStatus("Đang khởi chạy luồng kết nối song song...");

            try
            {
                var inPltTask = !string.IsNullOrEmpty(_inPlateCam.Config.Ip) ? _inPlateCam.LoginAsync() : Task.FromResult(false);
                var inOvwTask = !string.IsNullOrEmpty(_inOverviewCam.Config.Ip) ? _inOverviewCam.LoginAsync() : Task.FromResult(false);
                var outPltTask = !string.IsNullOrEmpty(_outPlateCam.Config.Ip) ? _outPlateCam.LoginAsync() : Task.FromResult(false);
                var outOvwTask = !string.IsNullOrEmpty(_outOverviewCam.Config.Ip) ? _outOverviewCam.LoginAsync() : Task.FromResult(false);
                var ctrlTask = !string.IsNullOrEmpty(_controllerIp) ? _controller.ConnectAsync(_controllerIp, _controllerPort) : Task.FromResult(false);

                await Task.WhenAll(inPltTask, inOvwTask, outPltTask, outOvwTask, ctrlTask);

                void ApplySlotResult(CameraSlot slot, Task<bool> task, Panel pnl, ICameraService cam)
                {
                    if (string.IsNullOrEmpty(cam.Config.Ip))
                    {
                        _slotStates[slot] = DeviceStatus.Disconnected;
                    }
                    else if (task.Result)
                    {
                        _slotStates[slot] = DeviceStatus.Streaming;
                        cam.StartPreview(pnl.Handle);
                    }
                    else
                    {
                        _slotStates[slot] = DeviceStatus.Error;
                    }
                }

                ApplySlotResult(CameraSlot.InPlate, inPltTask, pnlInPlateVideo, _inPlateCam);
                ApplySlotResult(CameraSlot.InOverview, inOvwTask, pnlInOverviewVideo, _inOverviewCam);
                ApplySlotResult(CameraSlot.OutPlate, outPltTask, pnlOutPlateVideo, _outPlateCam);
                ApplySlotResult(CameraSlot.OutOverview, outOvwTask, pnlOutOverviewVideo, _outOverviewCam);

                InvalidateCameraPanels();
                UpdateHeaderStatusFromAllStates();
            }
            catch (Exception ex)
            {
                foreach (var slot in (CameraSlot[])Enum.GetValues(typeof(CameraSlot)))
                {
                    _slotStates[slot] = DeviceStatus.Error;
                }
                InvalidateCameraPanels();

                SetHeaderStatus($"Lỗi kết nối: {ex.Message}");
                SetFooterStatus($"⚠️ Lỗi kết nối thiết bị: {ex.Message} (Nhấn để kiểm tra)", isError: true);
            }
        }

        /// <summary>
        /// Nạp lại cấu hình phân sai (Differential Hot-Reload): Chỉ ngắt và kết nối lại đúng thiết bị thay đổi, giữ nguyên các thiết bị khác đang hoạt động.
        /// </summary>
        private async Task ApplyDifferentialConfigAsync(DeviceConfigResult oldConfig, DeviceConfigResult newConfig)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(async () => await ApplyDifferentialConfigAsync(oldConfig, newConfig)));
                return;
            }

            var changedSlots = new List<CameraSlot>();

            if (!IsSameDevice(oldConfig.InPlateCamera, newConfig.InPlateCamera))
                changedSlots.Add(CameraSlot.InPlate);
            if (!IsSameDevice(oldConfig.InOverviewCamera, newConfig.InOverviewCamera))
                changedSlots.Add(CameraSlot.InOverview);
            if (!IsSameDevice(oldConfig.OutPlateCamera, newConfig.OutPlateCamera))
                changedSlots.Add(CameraSlot.OutPlate);
            if (!IsSameDevice(oldConfig.OutOverviewCamera, newConfig.OutOverviewCamera))
                changedSlots.Add(CameraSlot.OutOverview);

            bool controllerChanged = !IsSameDevice(oldConfig.Controller, newConfig.Controller)
                || oldConfig.ControllerIp != newConfig.ControllerIp
                || oldConfig.ControllerPort != newConfig.ControllerPort;

            if (changedSlots.Count == 0 && !controllerChanged)
            {
                AppLogger.Information("[Hardware Sync] Cấu hình không có thay đổi thực tế đối với thiết bị.");
                return;
            }

            AppLogger.Information($"[Hardware Sync] Hot-Reload: Chỉ thay đổi {changedSlots.Count} camera [{string.Join(", ", changedSlots)}], Controller: {controllerChanged}");

            // Dừng và xóa panel CHỈ cho các camera bị thay đổi
            foreach (var slot in changedSlots)
            {
                var cam = GetCameraService(slot);
                var pnl = GetCameraPanel(slot);
                try
                {
                    cam.StopPreview();
                }
                catch (Exception ex)
                {
                    AppLogger.Warning($"[FrmMain] Lỗi StopPreview {slot}: {ex.Message}");
                }

                try
                {
                    cam.Logout();
                }
                catch (Exception ex)
                {
                    AppLogger.Warning($"[FrmMain] Lỗi Logout {slot}: {ex.Message}");
                }

                _slotStates[slot] = DeviceStatus.Disconnected;
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => { pnl.Invalidate(); pnl.Update(); }));
                }
                else
                {
                    pnl.Invalidate();
                    pnl.Update();
                }
            }

            // Cập nhật lại _activeDevices và _deviceIdToSlotMap
            _activeDevices.Clear();
            _deviceIdToSlotMap.Clear();

            void BindSlot(CameraSlot slot, Device? dev, ICameraService cam)
            {
                if (dev != null && dev.IsActive)
                {
                    ApplyDeviceToConfig(cam.Config, dev);
                    _activeDevices[dev.Id] = dev;
                    _deviceIdToSlotMap[dev.Id] = slot;
                }
                else
                {
                    cam.Config.Ip = string.Empty;
                    _slotStates[slot] = DeviceStatus.Disconnected;
                }
            }

            BindSlot(CameraSlot.InPlate, newConfig.InPlateCamera, _inPlateCam);
            BindSlot(CameraSlot.InOverview, newConfig.InOverviewCamera, _inOverviewCam);
            BindSlot(CameraSlot.OutPlate, newConfig.OutPlateCamera, _outPlateCam);
            BindSlot(CameraSlot.OutOverview, newConfig.OutOverviewCamera, _outOverviewCam);

            if (newConfig.Controller != null && newConfig.Controller.IsActive && !string.IsNullOrEmpty(newConfig.ControllerIp))
            {
                _controllerIp = newConfig.ControllerIp ?? _controllerIp;
                _controllerPort = newConfig.ControllerPort > 0 ? newConfig.ControllerPort : _controllerPort;
                _activeDevices[newConfig.Controller.Id] = newConfig.Controller;
            }

            // Đồng bộ DeviceHealthManager và Adapters
            RegisterDevicesWithHealthManager();
            RegisterDeviceAdapters();

            // Kết nối lại CHỈ các camera bị thay đổi
            var tasks = new List<Task>();
            foreach (var slot in changedSlots)
            {
                var slotCapture = slot;
                var cam = GetCameraService(slotCapture);
                var pnl = GetCameraPanel(slotCapture);
                IntPtr pnlHandle = pnl.Handle;

                if (!string.IsNullOrEmpty(cam.Config.Ip))
                {
                    _slotStates[slotCapture] = DeviceStatus.Connecting;
                    pnl.Invalidate();

                    tasks.Add(Task.Run(async () =>
                    {
                        bool loginOk = await cam.LoginAsync();
                        if (loginOk)
                        {
                            _slotStates[slotCapture] = DeviceStatus.Streaming;
                            if (pnlHandle != IntPtr.Zero)
                            {
                                cam.StartPreview(pnlHandle);
                            }
                        }
                        else
                        {
                            _slotStates[slotCapture] = DeviceStatus.Error;
                        }

                        if (InvokeRequired)
                        {
                            BeginInvoke(new Action(() => pnl.Invalidate()));
                        }
                        else
                        {
                            pnl.Invalidate();
                        }
                    }));
                }
                else
                {
                    _slotStates[slotCapture] = DeviceStatus.Disconnected;
                    pnl.Invalidate();
                }
            }

            if (controllerChanged && !string.IsNullOrEmpty(_controllerIp))
            {
                tasks.Add(_controller.ConnectAsync(_controllerIp, _controllerPort));
            }

            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }

            InvalidateCameraPanels();
            UpdateHeaderStatusFromAllStates();
        }

        private static bool IsSameDevice(Device? dev1, Device? dev2)
        {
            if (dev1 == null && dev2 == null) return true;
            if (dev1 == null || dev2 == null) return false;
            return dev1.Id == dev2.Id
                && dev1.IpAddress == dev2.IpAddress
                && dev1.Port == dev2.Port
                && dev1.UserName == dev2.UserName
                && dev1.Password == dev2.Password
                && dev1.IsActive == dev2.IsActive;
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
            DrawCameraSlot(pnlInPlateVideo, CameraSlot.InPlate, "Camera Biển Số Vào", e);
        }

        private void PnlInOverviewVideo_Paint(object sender, PaintEventArgs e)
        {
            DrawCameraSlot(pnlInOverviewVideo, CameraSlot.InOverview, "Camera Toàn Cảnh Vào", e);
        }

        private void PnlOutPlateVideo_Paint(object sender, PaintEventArgs e)
        {
            DrawCameraSlot(pnlOutPlateVideo, CameraSlot.OutPlate, "Camera Biển Số Ra", e);
        }

        private void PnlOutOverviewVideo_Paint(object sender, PaintEventArgs e)
        {
            DrawCameraSlot(pnlOutOverviewVideo, CameraSlot.OutOverview, "Camera Toàn Cảnh Ra", e);
        }

        private void DrawCameraSlot(Panel pnl, CameraSlot slot, string defaultTitle, PaintEventArgs e)
        {
            var state = _slotStates.TryGetValue(slot, out var s) ? s : DeviceStatus.Disconnected;
            var cam = GetCameraService(slot);
            DrawVideoPanelStatus(pnl, state, defaultTitle, cam.Config.Ip, e);
        }

        private void DrawVideoPanelStatus(Panel pnl, DeviceStatus state, string camTitle, string? ip, PaintEventArgs e)
        {
            string subtitle = $"{camTitle} ({ip ?? "IP trống"})";

            switch (state)
            {
                case DeviceStatus.Disconnected:
                    // Clear panel - vẽ xóa nền sạch sẽ
                    e.Graphics.FillRectangle(_brushConnectingBg, 0, 0, pnl.Width, pnl.Height);
                    e.Graphics.DrawRectangle(_penNormalBorder, 0, 0, pnl.Width - 1, pnl.Height - 1);
                    return;

                case DeviceStatus.Connecting:
                    DrawConnectingStatus(pnl, subtitle, e);
                    return;

                case DeviceStatus.Connected:
                    // Border nhẹ - đã kết nối nhưng chưa streaming
                    e.Graphics.DrawRectangle(_penNormalBorder, 0, 0, pnl.Width - 1, pnl.Height - 1);
                    return;

                case DeviceStatus.Streaming:
                    // Border xanh + chỉ báo đang streaming
                    e.Graphics.DrawRectangle(_penStreamingBorder, 0, 0, pnl.Width - 1, pnl.Height - 1);
                    DrawStreamingIndicator(pnl, subtitle, e);
                    return;

                case DeviceStatus.Error:
                case DeviceStatus.Maintenance:
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
