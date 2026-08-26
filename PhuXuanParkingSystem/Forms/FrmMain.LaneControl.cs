using PhuXuanParkingSystem.Services.Anpr;
using PhuXuanParkingSystem.Services.Controller;
using PhuXuanParkingSystem.Services.Logging;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhuXuanParkingSystem.Forms
{
    public partial class FrmMain
    {
        // ── 1 Controller ZKTeco C3-200 dùng chung ───────────────────────────
        private readonly ZKTecoDeviceAdapter _controller = new();

        // ── Chống rung Radar Debounce ─────────────────────────────────────────
        private DateTime _lastInRadarTriggerTime = DateTime.MinValue;
        private DateTime _lastOutRadarTriggerTime = DateTime.MinValue;
        private const int RADAR_DEBOUNCE_MS = 3000;

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
                    lock (_lockDebounce)
                    {
                        var elapsed = (DateTime.Now - _lastInRadarTriggerTime).TotalMilliseconds;
                        if (elapsed < RADAR_DEBOUNCE_MS)
                        {
                            AppLogger.Debug($"[RADAR LÀN VÀO] Bỏ qua tín hiệu radar rung/lặp (Debounce: {elapsed:F0}ms < {RADAR_DEBOUNCE_MS}ms).");
                            return;
                        }
                        _lastInRadarTriggerTime = DateTime.Now;
                    }

                    lblInStatusVal.Text = "🟢Xe đang đi qua";
                    lblInStatusVal.ForeColor = Color.SeaGreen;
                    lblInTimeVal.Text = e.TriggerTime.ToString("dd/MM/yyyy HH:mm:ss");

                    _ = CaptureInLaneAsync("RADAR_LAN_VAO");
                }
                else
                {
                    lblInStatusVal.Text = "⚪ Xe đã đi qua";
                    lblInStatusVal.ForeColor = Color.FromArgb(100, 110, 120);
                }
            }
            else if (e.AuxPort == 2)
            {
                // LÀN RA (Aux 2)
                if (e.IsActive)
                {
                    lock (_lockDebounce)
                    {
                        var elapsed = (DateTime.Now - _lastOutRadarTriggerTime).TotalMilliseconds;
                        if (elapsed < RADAR_DEBOUNCE_MS)
                        {
                            AppLogger.Debug($"[RADAR LÀN RA] Bỏ qua tín hiệu radar rung/lặp (Debounce: {elapsed:F0}ms < {RADAR_DEBOUNCE_MS}ms).");
                            return;
                        }
                        _lastOutRadarTriggerTime = DateTime.Now;
                    }

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

        #region Chụp Ảnh & Điều Phối Làn Vào / Ra

        private async Task CaptureInLaneAsync(string triggerSource)
        {
            AppLogger.Information($"[LÀN VÀO] Bắt đầu kích hoạt chụp ảnh từ nguồn: {triggerSource}...", "LaneControl");

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

                var tPlate = _inPlateCam.CaptureToFileAsync(filePlate);
                var tOverview = _inOverviewCam.CaptureToFileAsync(fileOverview);
                await Task.WhenAll(tPlate, tOverview);

                bool plateOk = File.Exists(filePlate);
                bool ovwOk = File.Exists(fileOverview);

                AppLogger.Information($"[LÀN VÀO] Kết quả chụp ảnh: Plate={plateOk} ({filePlate}), Overview={ovwOk} ({fileOverview})", "LaneControl");

                if (ovwOk) DisplayCapturedImage(picInOverview, fileOverview);

                PlateRecognitionResult? anprResult = null;
                if (plateOk)
                {
                    anprResult = await _anprService.RecognizeAsync(filePlate);
                    AppLogger.Information($"[LÀN VÀO ANPR] Nhận diện biển số: {anprResult?.FormattedPlate ?? "Không đọc được"} (Độ tin cậy: {anprResult?.Confidence:P1})", "ANPR");
                }
                else
                {
                    AppLogger.Warning($"[LÀN VÀO] Không thể chụp ảnh biển số (Camera Biển Số có thể đang Offline hoặc chưa kết nối).", "LaneControl");
                }

                if (anprResult?.CroppedPlateImage != null)
                {
                    DisplayCapturedBitmap(picInPlate, anprResult.CroppedPlateImage);
                }
                else if (plateOk)
                {
                    DisplayCapturedImage(picInPlate, filePlate);
                }

                var res = await _parkingLaneService.ProcessInLaneAsync(anprResult, triggerSource, filePlate, fileOverview);

                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => BindInLaneResultToUi(res)));
                }
                else
                {
                    BindInLaneResultToUi(res);
                }

                SetFooterStatus($"📸 Đã chụp và xử lý LÀN VÀO lúc {DateTime.Now:HH:mm:ss.fff}");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Lỗi chụp ảnh Làn Vào: {ex.Message}", "LaneControl");
                SetFooterStatus($"Lỗi chụp ảnh Làn Vào: {ex.Message}");
            }
        }

        private void BindInLaneResultToUi(Services.Parking.LaneProcessResult res)
        {
            lblInTimeVal.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            if (!string.IsNullOrEmpty(res.DisplayPlate))
            {
                txtInPlate.Text = res.DisplayPlate;
            }
            lblInStatusVal.Text = res.StatusText;
            lblInStatusVal.ForeColor = res.StatusColor;

            if (res.IsIgnored)
            {
                return;
            }

            lblInOwnerVal.Text = res.OwnerName;
            lblInDeptVal.Text = res.DepartmentName;
            lblInTypeVal.Text = res.VehicleType == Models.Enums.VehicleType.Car ? "Ô tô" : "Xe máy";
        }

        private async Task CaptureOutLaneAsync(string triggerSource)
        {
            AppLogger.Information($"[LÀN RA] Bắt đầu kích hoạt chụp ảnh từ nguồn: {triggerSource}...", "LaneControl");

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

                var tPlate = _outPlateCam.CaptureToFileAsync(filePlate);
                var tOverview = _outOverviewCam.CaptureToFileAsync(fileOverview);
                await Task.WhenAll(tPlate, tOverview);

                bool plateOk = File.Exists(filePlate);
                bool ovwOk = File.Exists(fileOverview);

                AppLogger.Information($"[LÀN RA] Kết quả chụp ảnh: Plate={plateOk} ({filePlate}), Overview={ovwOk} ({fileOverview})", "LaneControl");

                if (ovwOk) DisplayCapturedImage(picOutOverview, fileOverview);

                PlateRecognitionResult? anprResult = null;
                if (plateOk)
                {
                    anprResult = await _anprService.RecognizeAsync(filePlate);
                    AppLogger.Information($"[LÀN RA ANPR] Nhận diện biển số: {anprResult?.FormattedPlate ?? "Không đọc được"} (Độ tin cậy: {anprResult?.Confidence:P1})", "ANPR");
                }
                else
                {
                    AppLogger.Warning($"[LÀN RA] Không thể chụp ảnh biển số (Camera Biển Số có thể đang Offline hoặc chưa kết nối).", "LaneControl");
                }

                if (anprResult?.CroppedPlateImage != null)
                {
                    DisplayCapturedBitmap(picOutPlate, anprResult.CroppedPlateImage);
                }
                else if (plateOk)
                {
                    DisplayCapturedImage(picOutPlate, filePlate);
                }

                var res = await _parkingLaneService.ProcessOutLaneAsync(anprResult, triggerSource, filePlate, fileOverview);

                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => BindOutLaneResultToUi(res)));
                }
                else
                {
                    BindOutLaneResultToUi(res);
                }

                SetFooterStatus($"📸 Đã chụp và xử lý LÀN RA lúc {DateTime.Now:HH:mm:ss.fff}");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Lỗi chụp ảnh Làn Ra: {ex.Message}", "LaneControl");
                SetFooterStatus($"Lỗi chụp ảnh Làn Ra: {ex.Message}");
            }
        }

        private void BindOutLaneResultToUi(Services.Parking.LaneProcessResult res)
        {
            lblOutTimeVal.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            if (!string.IsNullOrEmpty(res.DisplayPlate))
            {
                txtOutPlate.Text = res.DisplayPlate;
            }
            lblOutStatusVal.Text = res.StatusText;
            lblOutStatusVal.ForeColor = res.StatusColor;

            if (res.IsIgnored)
            {
                return;
            }

            lblOutOwnerVal.Text = res.OwnerName;
            lblOutDeptVal.Text = res.DepartmentName;
            lblOutTypeVal.Text = res.VehicleType == Models.Enums.VehicleType.Car ? "Ô tô" : "Xe máy";
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
            catch
            {
                // Xử lý an toàn
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
