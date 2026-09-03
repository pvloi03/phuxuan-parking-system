using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Services.Anpr;
using PhuXuanParkingSystem.Services.Devices.Controller;
using PhuXuanParkingSystem.Services.Logging;
using PhuXuanParkingSystem.Services.Parking;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhuXuanParkingSystem.Forms
{
    public partial class FrmMain
    {
        // ── 1 Access Controller dùng chung ───────────────────────────
        private readonly IControllerService _controller = new ControllerService();

        // ── Khóa chu kỳ xe & Chống rung Radar Debounce ────────────────────────
        private bool _isInLaneProcessing = false;
        private bool _isOutLaneProcessing = false;
        private DateTime _lastInRadarTriggerTime = DateTime.MinValue;
        private DateTime _lastOutRadarTriggerTime = DateTime.MinValue;
        private const int RADAR_DEBOUNCE_MS = 1500;
        private const int CYCLE_RESET_COOLDOWN_MS = 1500;

        #region Xử Lý Sự Kiện Radar AUX (Controller Realtime)

        private void Controller_OnAuxInputTriggered(object? sender, AuxTriggerEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => Controller_OnAuxInputTriggered(sender, e)));
                return;
            }

            if (e.AuxPort == 1)
            {
                // LÀN VÀO (Aux 1)
                if (e.IsActive)
                {
                    // Cạnh lên: Xe bắt đầu vào vùng cảm biến radar
                    bool shouldTrigger = false;
                    lock (_lockDebounce)
                    {
                        var elapsed = (DateTime.Now - _lastInRadarTriggerTime).TotalMilliseconds;
                        if (!_isInLaneProcessing && elapsed >= RADAR_DEBOUNCE_MS)
                        {
                            _isInLaneProcessing = true;
                            _lastInRadarTriggerTime = DateTime.Now;
                            shouldTrigger = true;
                        }
                        else
                        {
                            AppLogger.Debug($"[RADAR LÀN VÀO] Bỏ qua tín hiệu (Làn đang bận hoặc rung lặp: {elapsed:F0}ms).");
                        }
                    }

                    if (shouldTrigger)
                    {
                        lblInStatusVal.Text = "🟢 Phát hiện xe vào - Đang xử lý...";
                        lblInStatusVal.ForeColor = Color.SeaGreen;
                        lblInTimeVal.Text = e.TriggerTime.ToString("dd/MM/yyyy HH:mm:ss");

                        // Kích hoạt luồng chụp ảnh, ANPR và ghi nhận phiên vào ngầm
                        _ = Task.Run(async () => await HandleInLaneTriggerAsync("RADAR"));
                    }
                }
                else
                {
                    // Cạnh xuống: Xe đã đi qua khỏi cảm biến radar
                    lblInStatusVal.Text = "⚪ Xe đã qua làn vào";
                    lblInStatusVal.ForeColor = Color.FromArgb(100, 110, 120);

                    // Mở khóa chu kỳ xe sau khoảng trễ an toàn
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(CYCLE_RESET_COOLDOWN_MS);
                        lock (_lockDebounce)
                        {
                            _isInLaneProcessing = false;
                        }
                        AppLogger.Debug("[RADAR LÀN VÀO] Đã mở khóa sẵn sàng đón xe tiếp theo.");
                    });
                }
            }
            else if (e.AuxPort == 2)
            {
                // LÀN RA (Aux 2)
                if (e.IsActive)
                {
                    // Cạnh lên: Xe bắt đầu vào vùng cảm biến radar làn ra
                    bool shouldTrigger = false;
                    lock (_lockDebounce)
                    {
                        var elapsed = (DateTime.Now - _lastOutRadarTriggerTime).TotalMilliseconds;
                        if (!_isOutLaneProcessing && elapsed >= RADAR_DEBOUNCE_MS)
                        {
                            _isOutLaneProcessing = true;
                            _lastOutRadarTriggerTime = DateTime.Now;
                            shouldTrigger = true;
                        }
                        else
                        {
                            AppLogger.Debug($"[RADAR LÀN RA] Bỏ qua tín hiệu (Làn đang bận hoặc rung lặp: {elapsed:F0}ms).");
                        }
                    }

                    if (shouldTrigger)
                    {
                        lblOutStatusVal.Text = "🔴 Phát hiện xe ra - Đang xử lý...";
                        lblOutStatusVal.ForeColor = Color.SeaGreen;
                        lblOutTimeVal.Text = e.TriggerTime.ToString("dd/MM/yyyy HH:mm:ss");

                        // Kích hoạt luồng chụp ảnh, ANPR và ghi nhận phiên ra ngầm
                        _ = Task.Run(async () => await HandleOutLaneTriggerAsync("RADAR"));
                    }
                }
                else
                {
                    // Cạnh xuống: Xe đã đi qua khỏi cảm biến radar làn ra
                    lblOutStatusVal.Text = "⚪ Xe đã qua làn ra";
                    lblOutStatusVal.ForeColor = Color.FromArgb(100, 110, 120);

                    // Mở khóa chu kỳ xe sau khoảng trễ an toàn
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(CYCLE_RESET_COOLDOWN_MS);
                        lock (_lockDebounce)
                        {
                            _isOutLaneProcessing = false;
                        }
                        AppLogger.Debug("[RADAR LÀN RA] Đã mở khóa sẵn sàng đón xe tiếp theo.");
                    });
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

            SetFooterStatus($"[Access Controller] {message}");
        }

        #endregion

        #region Chụp Ảnh & Điều Phối Làn Vào / Ra

        public async Task HandleInLaneTriggerAsync(string triggerSource)
        {
            AppLogger.Information($"[LÀN VÀO] Bắt đầu kích hoạt chụp ảnh từ nguồn: {triggerSource}...", "LaneControl");

            try
            {
                var cfg = _deviceConfigService?.CurrentConfig;
                string? plateDevId = cfg?.InPlateCamera?.Id;
                string? ovwDevId = cfg?.InOverviewCamera?.Id;

                var res = await _laneService.ProcessInLaneAsync(
                    inLaneName: "Làn Vào",
                    plateCam: _inPlateCam,
                    overviewCam: _inOverviewCam,
                    plateDeviceId: plateDevId,
                    overviewDeviceId: ovwDevId,
                    triggerSource: triggerSource,
                    captureDir: _captureDir
                );

                void UpdateInUi()
                {
                    // 1. Cập nhật ảnh Toàn cảnh
                    if (!string.IsNullOrEmpty(res.OverviewImagePath) && File.Exists(res.OverviewImagePath))
                    {
                        DisplayCapturedImage(picInOverview, res.OverviewImagePath!);
                    }

                    // 2. Cập nhật ảnh Biển số (Ưu tiên hiển thị ảnh cắt biển số nhỏ zoom cận cảnh)
                    if (res.CroppedPlateImage != null)
                    {
                        DisplayCapturedBitmap(picInPlate, res.CroppedPlateImage);
                    }
                    else if (!string.IsNullOrEmpty(res.PlateCropImagePath) && File.Exists(res.PlateCropImagePath))
                    {
                        DisplayCapturedImage(picInPlate, res.PlateCropImagePath!);
                    }
                    else if (!string.IsNullOrEmpty(res.PlateImagePath) && File.Exists(res.PlateImagePath))
                    {
                        DisplayCapturedImage(picInPlate, res.PlateImagePath!);
                    }

                    // 3. Cập nhật thông tin nhận diện
                    txtInPlate.Text = res.PlateNumber;
                    lblInTimeVal.Text = res.ProcessedTime.ToString("dd/MM/yyyy HH:mm:ss");
                    lblInOwnerVal.Text = !string.IsNullOrEmpty(res.PersonName) ? res.PersonName : (res.IsRegisteredVehicle ? "Chưa gán chủ xe" : "Khách vãng lai");
                    lblInDeptVal.Text = !string.IsNullOrEmpty(res.DepartmentName) ? res.DepartmentName : "---";
                    lblInTypeVal.Text = res.IsRegisteredVehicle ? "Xe nội bộ" : "Xe vãng lai";

                    // 4. Trạng thái kết quả
                    if (res.IsCrossLaneIgnored)
                    {
                        lblInStatusVal.Text = "🟡 Bỏ qua góc quét chéo";
                        lblInStatusVal.ForeColor = Color.DarkOrange;
                    }
                    else if (res.Success)
                    {
                        lblInStatusVal.Text = res.PlateCamSuccess ? "🟢 Đã ghi nhận phiên vào" : "⚠️ Vào (Cam biển lỗi)";
                        lblInStatusVal.ForeColor = res.PlateCamSuccess ? Color.SeaGreen : Color.FromArgb(200, 120, 30);
                    }
                    else
                    {
                        lblInStatusVal.Text = "❌ Lỗi ghi nhận phiên vào";
                        lblInStatusVal.ForeColor = Color.Crimson;
                    }
                }

                if (InvokeRequired) BeginInvoke(new Action(UpdateInUi));
                else UpdateInUi();

                SetFooterStatus($"📸 LÀN VÀO ({triggerSource}): Biển [{res.PlateNumber}] - {res.PersonName ?? "Khách"} lúc {DateTime.Now:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Lỗi chụp ảnh Làn Vào: {ex.Message}", "LaneControl");
                SetFooterStatus($"Lỗi chụp ảnh Làn Vào: {ex.Message}", isError: true);
            }
        }

        public async Task HandleOutLaneTriggerAsync(string triggerSource)
        {
            AppLogger.Information($"[LÀN RA] Bắt đầu kích hoạt chụp ảnh từ nguồn: {triggerSource}...", "LaneControl");

            try
            {
                var cfg = _deviceConfigService?.CurrentConfig;
                string? plateDevId = cfg?.OutPlateCamera?.Id;
                string? ovwDevId = cfg?.OutOverviewCamera?.Id;

                var res = await _laneService.ProcessOutLaneAsync(
                    outLaneName: "Làn Ra",
                    plateCam: _outPlateCam,
                    overviewCam: _outOverviewCam,
                    plateDeviceId: plateDevId,
                    overviewDeviceId: ovwDevId,
                    triggerSource: triggerSource,
                    captureDir: _captureDir
                );

                void UpdateOutUi()
                {
                    // 1. Cập nhật ảnh Toàn cảnh
                    if (!string.IsNullOrEmpty(res.OverviewImagePath) && File.Exists(res.OverviewImagePath))
                    {
                        DisplayCapturedImage(picOutOverview, res.OverviewImagePath!);
                    }

                    // 2. Cập nhật ảnh Biển số (Ưu tiên hiển thị ảnh cắt biển số nhỏ zoom cận cảnh)
                    if (res.CroppedPlateImage != null)
                    {
                        DisplayCapturedBitmap(picOutPlate, res.CroppedPlateImage);
                    }
                    else if (!string.IsNullOrEmpty(res.PlateCropImagePath) && File.Exists(res.PlateCropImagePath))
                    {
                        DisplayCapturedImage(picOutPlate, res.PlateCropImagePath!);
                    }
                    else if (!string.IsNullOrEmpty(res.PlateImagePath) && File.Exists(res.PlateImagePath))
                    {
                        DisplayCapturedImage(picOutPlate, res.PlateImagePath!);
                    }

                    // 3. Cập nhật thông tin nhận diện
                    txtOutPlate.Text = res.PlateNumber;
                    lblOutTimeVal.Text = res.ProcessedTime.ToString("dd/MM/yyyy HH:mm:ss");
                    lblOutOwnerVal.Text = !string.IsNullOrEmpty(res.PersonName) ? res.PersonName : (res.IsRegisteredVehicle ? "Chưa gán chủ xe" : "Khách vãng lai");
                    lblOutDeptVal.Text = !string.IsNullOrEmpty(res.DepartmentName) ? res.DepartmentName : "---";
                    lblOutTypeVal.Text = res.IsRegisteredVehicle ? "Xe nội bộ" : "Xe vãng lai";

                    // 4. Trạng thái kết quả phiên xe ra
                    if (res.IsCrossLaneIgnored)
                    {
                        lblOutStatusVal.Text = "🟡 Bỏ qua góc quét chéo";
                        lblOutStatusVal.ForeColor = Color.DarkOrange;
                    }
                    else if (res.Session?.Status == ParkingSessionStatus.Completed)
                    {
                        var durationMin = res.Session.Duration?.TotalMinutes ?? 0;
                        lblOutStatusVal.Text = $"🔴 Hoàn tất xe ra ({durationMin:F0} phút)";
                        lblOutStatusVal.ForeColor = Color.SeaGreen;
                    }
                    else if (res.Session?.Status == ParkingSessionStatus.UnmatchedOut)
                    {
                        lblOutStatusVal.Text = "⚠️ Xe ra không có lượt vào!";
                        lblOutStatusVal.ForeColor = Color.Crimson;
                    }
                    else if (res.Success)
                    {
                        lblOutStatusVal.Text = "🟢 Đã xử lý xe ra";
                        lblOutStatusVal.ForeColor = Color.SeaGreen;
                    }
                    else
                    {
                        lblOutStatusVal.Text = "❌ Lỗi xử lý phiên xe ra";
                        lblOutStatusVal.ForeColor = Color.Crimson;
                    }
                }

                if (InvokeRequired) BeginInvoke(new Action(UpdateOutUi));
                else UpdateOutUi();

                SetFooterStatus($"📸 LÀN RA ({triggerSource}): Biển [{res.PlateNumber}] - {res.PersonName ?? "Khách"} lúc {DateTime.Now:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Lỗi chụp ảnh Làn Ra: {ex.Message}", "LaneControl");
                SetFooterStatus($"Lỗi chụp ảnh Làn Ra: {ex.Message}", isError: true);
            }
        }

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
            catch (Exception ex)
            {
                AppLogger.Warning($"Lỗi hiển thị ảnh {filePath}: {ex.Message}");
            }
        }

        private void DisplayCapturedBitmap(PictureBox picBox, Bitmap bitmap)
        {
            if (bitmap == null) return;
            try
            {
                var cloned = (Bitmap)bitmap.Clone();
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() =>
                    {
                        var oldImg = picBox.Image;
                        picBox.Image = cloned;
                        oldImg?.Dispose();
                    }));
                }
                else
                {
                    var oldImg = picBox.Image;
                    picBox.Image = cloned;
                    oldImg?.Dispose();
                }
            }
            catch (Exception e)
            {
                AppLogger.Error(e.Message);
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
            catch (Exception e)
            {
                AppLogger.Error(e.Message);
            }
        }

        #endregion
    }
}
