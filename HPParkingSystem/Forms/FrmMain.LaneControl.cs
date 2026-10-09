using HPParkingSystem.Models.Enums;
using HPParkingSystem.Models.ValueObjects;
using HPParkingSystem.Services.Anpr;
using HPParkingSystem.Services.Devices.Camera;
using HPParkingSystem.Services.Devices.Controller;
using HPParkingSystem.Services.Logging;
using HPParkingSystem.Services.Parking;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HPParkingSystem.Forms
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

            var cfg = _deviceConfigService?.CurrentConfig;

            if (e.AuxPort == 1)
            {
                // CỘT 1 (Aux 1) - Hướng do Lane1.Direction quyết định
                var lane1 = cfg?.Lane1;
                if (lane1 == null)
                {
                    AppLogger.Debug("[RADAR CỘT 1] Bỏ qua tín hiệu (Cột 1 chưa được gán làn xe).");
                    return;
                }
                var dir1 = lane1.Direction;
                string dirLabel = dir1 == LaneDirection.In ? "xe vào" : "xe ra";

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
                            AppLogger.Debug($"[RADAR CỘT 1] Bỏ qua tín hiệu (Làn đang bận hoặc rung lặp: {elapsed:F0}ms).");
                        }
                    }

                    if (shouldTrigger)
                    {
                        lblInStatusVal.Text = dir1 == LaneDirection.In ? "🟢 Phát hiện xe vào - Đang xử lý..." : "🔴 Phát hiện xe ra - Đang xử lý...";
                        lblInStatusVal.ForeColor = Color.SeaGreen;
                        lblInTimeVal.Text = e.TriggerTime.ToString("dd/MM/yyyy HH:mm:ss");

                        // Kích hoạt luồng chụp ảnh, ANPR và ghi nhận phiên ngầm theo Direction
                        _ = Task.Run(async () => await HandleLaneSlotAsync(1, dir1, "RADAR"));
                    }
                }
                else
                {
                    // Cạnh xuống: Xe đã đi qua khỏi cảm biến radar
                    lblInStatusVal.Text = $"⚪ Xe đã qua {dirLabel}";
                    lblInStatusVal.ForeColor = Color.FromArgb(100, 110, 120);

                    // Mở khóa chu kỳ xe sau khoảng trễ an toàn
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(CYCLE_RESET_COOLDOWN_MS);
                        lock (_lockDebounce)
                        {
                            _isInLaneProcessing = false;
                        }
                        AppLogger.Debug("[RADAR CỘT 1] Đã mở khóa sẵn sàng đón xe tiếp theo.");
                    });
                }
            }
            else if (e.AuxPort == 2)
            {
                // CỘT 2 (Aux 2) - Hướng do Lane2.Direction quyết định
                var lane2 = cfg?.Lane2;
                if (lane2 == null)
                {
                    AppLogger.Debug("[RADAR CỘT 2] Bỏ qua tín hiệu (Cột 2 chưa được gán làn xe).");
                    return;
                }
                var dir2 = lane2.Direction;
                string dirLabel = dir2 == LaneDirection.In ? "xe vào" : "xe ra";

                if (e.IsActive)
                {
                    // Cạnh lên: Xe bắt đầu vào vùng cảm biến radar
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
                            AppLogger.Debug($"[RADAR CỘT 2] Bỏ qua tín hiệu (Làn đang bận hoặc rung lặp: {elapsed:F0}ms).");
                        }
                    }

                    if (shouldTrigger)
                    {
                        lblOutStatusVal.Text = dir2 == LaneDirection.In ? "🟢 Phát hiện xe vào - Đang xử lý..." : "🔴 Phát hiện xe ra - Đang xử lý...";
                        lblOutStatusVal.ForeColor = Color.SeaGreen;
                        lblOutTimeVal.Text = e.TriggerTime.ToString("dd/MM/yyyy HH:mm:ss");

                        // Kích hoạt luồng chụp ảnh, ANPR và ghi nhận phiên ngầm theo Direction
                        _ = Task.Run(async () => await HandleLaneSlotAsync(2, dir2, "RADAR"));
                    }
                }
                else
                {
                    // Cạnh xuống: Xe đã đi qua khỏi cảm biến radar
                    lblOutStatusVal.Text = $"⚪ Xe đã qua {dirLabel}";
                    lblOutStatusVal.ForeColor = Color.FromArgb(100, 110, 120);

                    // Mở khóa chu kỳ xe sau khoảng trễ an toàn
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(CYCLE_RESET_COOLDOWN_MS);
                        lock (_lockDebounce)
                        {
                            _isOutLaneProcessing = false;
                        }
                        AppLogger.Debug("[RADAR CỘT 2] Đã mở khóa sẵn sàng đón xe tiếp theo.");
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

        #region Chụp Ảnh & Điều Phối Làn Theo Vị Trí Slot & Direction

        /// <summary>
        /// Xử lý kích hoạt làn theo Vị trí Slot (1 = Cột Trái / F1, 2 = Cột Phải / F2) và Chiều xe (In hoặc Out)
        /// </summary>
        public async Task HandleLaneSlotAsync(int slot, LaneDirection direction, string triggerSource)
        {
            var cfg = _deviceConfigService?.CurrentConfig;
            var lane = slot == 1 ? cfg?.Lane1 : cfg?.Lane2;
            if (lane == null)
            {
                AppLogger.Warning($"[CỘT {slot}] Chưa được gán làn xe. Bỏ qua xử lý.", "LaneControl");
                SetFooterStatus($"Cột {slot} chưa được gán làn xe.");
                return;
            }
            string laneName = lane.Name;

            ICameraService plateCam = slot == 1 ? _inPlateCam : _outPlateCam;
            ICameraService overviewCam = slot == 1 ? _inOverviewCam : _outOverviewCam;
            string? plateDevId = lane.PlateCamera?.Id ?? (slot == 1 ? cfg?.Lane1PlateCamera?.Id : cfg?.Lane2PlateCamera?.Id);
            string? ovwDevId = lane.OverviewCamera?.Id ?? (slot == 1 ? cfg?.Lane1OverviewCamera?.Id : cfg?.Lane2OverviewCamera?.Id);

            string dirLabel = direction == LaneDirection.In ? "LÀN VÀO" : "LÀN RA";
            AppLogger.Information($"[{dirLabel} - CỘT {slot}] Bắt đầu kích hoạt chụp ảnh ({laneName}) từ nguồn: {triggerSource}...", "LaneControl");

            try
            {
                LaneProcessResult res;
                if (direction == LaneDirection.In)
                {
                    res = await _laneService.ProcessInLaneAsync(
                        inLaneName: laneName,
                        plateCam: plateCam,
                        overviewCam: overviewCam,
                        plateDeviceId: plateDevId,
                        overviewDeviceId: ovwDevId,
                        triggerSource: triggerSource,
                        captureDir: _captureDir
                    );
                }
                else
                {
                    res = await _laneService.ProcessOutLaneAsync(
                        outLaneName: laneName,
                        plateCam: plateCam,
                        overviewCam: overviewCam,
                        plateDeviceId: plateDevId,
                        overviewDeviceId: ovwDevId,
                        triggerSource: triggerSource,
                        captureDir: _captureDir
                    );
                }

                // Cập nhật UI theo đúng Cột (Slot 1 = Cột Trái, Slot 2 = Cột Phải)
                void UpdateSlotUi()
                {
                    if (slot == 1)
                    {
                        UpdateSlot1Ui(res, direction);
                    }
                    else
                    {
                        UpdateSlot2Ui(res, direction);
                    }
                }

                if (InvokeRequired) BeginInvoke(new Action(UpdateSlotUi));
                else UpdateSlotUi();

                string plateDisp = PlateNumber.IsUnrecognized(res.PlateNumber) ? PlateNumber.UnrecognizedDisplay : res.PlateNumber;
                if (direction == LaneDirection.In && res.IsAlreadyInLot)
                {
                    SetFooterStatus($"⚠️ [CẢNH BÁO {laneName}] Xe [{plateDisp}] ĐANG Ở TRONG BÃI (Vào lúc {res.Session?.InTime:HH:mm:ss})!", isError: true);
                }
                else
                {
                    SetFooterStatus($"📸 {laneName} ({triggerSource}): Biển [{plateDisp}] - {res.PersonName ?? "Khách"} lúc {DateTime.Now:HH:mm:ss}");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Lỗi xử lý {laneName}: {ex.Message}", "LaneControl");
                SetFooterStatus($"Lỗi xử lý {laneName}: {ex.Message}", isError: true);
            }
        }

        public async Task HandleInLaneTriggerAsync(string triggerSource)
        {
            var cfg = _deviceConfigService?.CurrentConfig;
            var lane1 = cfg?.Lane1;
            if (lane1 != null)
            {
                await HandleLaneSlotAsync(1, lane1.Direction, triggerSource);
            }
        }

        public async Task HandleOutLaneTriggerAsync(string triggerSource)
        {
            var cfg = _deviceConfigService?.CurrentConfig;
            var lane2 = cfg?.Lane2;
            if (lane2 != null)
            {
                await HandleLaneSlotAsync(2, lane2.Direction, triggerSource);
            }
        }

        private void UpdateSlot1Ui(LaneProcessResult res, LaneDirection direction)
        {
            // 1. Ảnh toàn cảnh
            if (res.OverviewImageBytes != null && res.OverviewImageBytes.Length > 0)
                DisplayCapturedBytes(picInOverview, res.OverviewImageBytes);
            else if (!string.IsNullOrEmpty(res.OverviewImagePath) && File.Exists(res.OverviewImagePath))
                DisplayCapturedImage(picInOverview, res.OverviewImagePath!);

            // 2. Ảnh biển số
            if (res.CroppedPlateImage != null)
                DisplayCapturedBitmap(picInPlate, res.CroppedPlateImage);
            else if (!string.IsNullOrEmpty(res.PlateImagePath) && File.Exists(res.PlateImagePath))
                DisplayCapturedImage(picInPlate, res.PlateImagePath!);

            // 3. Thông tin nhận diện
            txtInPlate.Text = PlateNumber.IsUnrecognized(res.PlateNumber) ? PlateNumber.UnrecognizedDisplay : res.PlateNumber;
            lblInTimeVal.Text = res.ProcessedTime.ToString("dd/MM/yyyy HH:mm:ss");
            lblInOwnerVal.Text = !string.IsNullOrEmpty(res.PersonName) ? res.PersonName : (res.IsRegisteredVehicle ? "Chưa gán chủ xe" : "Khách vãng lai");
            lblInDeptVal.Text = !string.IsNullOrEmpty(res.DepartmentName) ? res.DepartmentName : "---";
            lblInTypeVal.Text = GetPersonTypeDisplay(res.PersonType, res.IsRegisteredVehicle);

            // 4. Trạng thái kết quả
            if (res.IsCrossLaneIgnored)
            {
                lblInStatusVal.Text = "🟡 Thao tác quá nhanh";
                lblInStatusVal.ForeColor = Color.DarkOrange;
            }
            else if (direction == LaneDirection.In)
            {
                if (res.IsAlreadyInLot)
                {
                    lblInStatusVal.Text = "⚠️ XE ĐANG TRONG BÃI";
                    lblInStatusVal.ForeColor = Color.Crimson;
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
            else // Xe Ra ở Cột 1
            {
                if (res.Session?.Status == ParkingSessionStatus.Completed)
                {
                    var durationMin = res.Session.Duration?.TotalMinutes;
                    lblInStatusVal.Text = durationMin.HasValue
                        ? $"🔴 Hoàn tất xe ra ({durationMin.Value:F0} phút)"
                        : "🟢 Hoàn tất xe ra";
                    lblInStatusVal.ForeColor = Color.SeaGreen;
                }
                else if (res.Success)
                {
                    lblInStatusVal.Text = "🟢 Đã xử lý xe ra";
                    lblInStatusVal.ForeColor = Color.SeaGreen;
                }
                else
                {
                    lblInStatusVal.Text = "❌ Lỗi xử lý phiên xe ra";
                    lblInStatusVal.ForeColor = Color.Crimson;
                }
            }
        }

        private void UpdateSlot2Ui(LaneProcessResult res, LaneDirection direction)
        {
            // 1. Ảnh toàn cảnh
            if (res.OverviewImageBytes != null && res.OverviewImageBytes.Length > 0)
                DisplayCapturedBytes(picOutOverview, res.OverviewImageBytes);
            else if (!string.IsNullOrEmpty(res.OverviewImagePath) && File.Exists(res.OverviewImagePath))
                DisplayCapturedImage(picOutOverview, res.OverviewImagePath!);

            // 2. Ảnh biển số
            if (res.CroppedPlateImage != null)
                DisplayCapturedBitmap(picOutPlate, res.CroppedPlateImage);
            else if (!string.IsNullOrEmpty(res.PlateImagePath) && File.Exists(res.PlateImagePath))
                DisplayCapturedImage(picOutPlate, res.PlateImagePath!);

            // 3. Thông tin nhận diện
            txtOutPlate.Text = PlateNumber.IsUnrecognized(res.PlateNumber) ? PlateNumber.UnrecognizedDisplay : res.PlateNumber;
            lblOutTimeVal.Text = res.ProcessedTime.ToString("dd/MM/yyyy HH:mm:ss");
            lblOutOwnerVal.Text = !string.IsNullOrEmpty(res.PersonName) ? res.PersonName : (res.IsRegisteredVehicle ? "Chưa gán chủ xe" : "Khách vãng lai");
            lblOutDeptVal.Text = !string.IsNullOrEmpty(res.DepartmentName) ? res.DepartmentName : "---";
            lblOutTypeVal.Text = GetPersonTypeDisplay(res.PersonType, res.IsRegisteredVehicle);

            // 4. Trạng thái kết quả
            if (res.IsCrossLaneIgnored)
            {
                lblOutStatusVal.Text = "🟡 Thao tác quá nhanh";
                lblOutStatusVal.ForeColor = Color.DarkOrange;
            }
            else if (direction == LaneDirection.In) // Xe Vào ở Cột 2
            {
                if (res.IsAlreadyInLot)
                {
                    lblOutStatusVal.Text = "⚠️ XE ĐANG TRONG BÃI";
                    lblOutStatusVal.ForeColor = Color.Crimson;
                }
                else if (res.Success)
                {
                    lblOutStatusVal.Text = res.PlateCamSuccess ? "🟢 Đã ghi nhận phiên vào" : "⚠️ Vào (Cam biển lỗi)";
                    lblOutStatusVal.ForeColor = res.PlateCamSuccess ? Color.SeaGreen : Color.FromArgb(200, 120, 30);
                }
                else
                {
                    lblOutStatusVal.Text = "❌ Lỗi ghi nhận phiên vào";
                    lblOutStatusVal.ForeColor = Color.Crimson;
                }
            }
            else // Xe Ra ở Cột 2
            {
                if (res.Session?.Status == ParkingSessionStatus.Completed)
                {
                    var durationMin = res.Session.Duration?.TotalMinutes;
                    lblOutStatusVal.Text = durationMin.HasValue
                        ? $"🔴 Hoàn tất xe ra ({durationMin.Value:F0} phút)"
                        : "🟢 Hoàn tất xe ra";
                    lblOutStatusVal.ForeColor = Color.SeaGreen;
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
        }

        private static string GetPersonTypeDisplay(PersonType personType, bool isRegistered)
        {
            return personType switch
            {
                PersonType.Employee => "Cán bộ / Nhân viên",
                PersonType.Contractor => "Đối tác / Nhà thầu",
                PersonType.VIP => "Khách VIP",
                PersonType.Visitor => isRegistered ? "Khách đã đăng ký" : "Khách vãng lai",
                _ => "Khách vãng lai"
            };
        }

        private void DisplayCapturedBytes(PictureBox picBox, byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return;
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetPictureBoxImage(picBox, bytes)));
            }
            else
            {
                SetPictureBoxImage(picBox, bytes);
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
