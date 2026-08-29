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
        // ── 1 Access Controller dùng chung ───────────────────────────
        private readonly ZKTecoDeviceAdapter _controller = new();

        // ── Chống rung Radar Debounce ─────────────────────────────────────────
        private DateTime _lastInRadarTriggerTime = DateTime.MinValue;
        private DateTime _lastOutRadarTriggerTime = DateTime.MinValue;
        private const int RADAR_DEBOUNCE_MS = 1500;

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

            SetFooterStatus($"[Access Controller] {message}");
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

                if (ovwOk)
                {
                    try
                    {
                        byte[] ovwBytes = File.ReadAllBytes(fileOverview);
                        if (InvokeRequired) BeginInvoke(new Action(() => SetPictureBoxImage(picInOverview, ovwBytes)));
                        else SetPictureBoxImage(picInOverview, ovwBytes);
                    }
                    catch { }
                }

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
                    try
                    {
                        byte[] pltBytes = File.ReadAllBytes(filePlate);
                        if (InvokeRequired) BeginInvoke(new Action(() => SetPictureBoxImage(picInPlate, pltBytes)));
                        else SetPictureBoxImage(picInPlate, pltBytes);
                    }
                    catch { }
                }

                void UpdateInUi()
                {
                    lblInTimeVal.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                    if (anprResult != null && anprResult.IsSuccess)
                    {
                        txtInPlate.Text = anprResult.FormattedPlate;
                        lblInStatusVal.Text = "🟢 Đã nhận diện biển số";
                        lblInStatusVal.ForeColor = Color.SeaGreen;
                    }
                    else
                    {
                        txtInPlate.Text = plateOk ? "Không đọc được" : "";
                        lblInStatusVal.Text = plateOk ? "⚪ Không đọc được biển" : "❌ Camera biển số lỗi";
                        lblInStatusVal.ForeColor = Color.FromArgb(200, 120, 30);
                    }
                }

                if (InvokeRequired) BeginInvoke(new Action(UpdateInUi));
                else UpdateInUi();

                SetFooterStatus($"📸 Đã chụp và xử lý LÀN VÀO lúc {DateTime.Now:HH:mm:ss.fff}");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Lỗi chụp ảnh Làn Vào: {ex.Message}", "LaneControl");
                SetFooterStatus($"Lỗi chụp ảnh Làn Vào: {ex.Message}");
            }
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

                if (ovwOk)
                {
                    try
                    {
                        byte[] ovwBytes = File.ReadAllBytes(fileOverview);
                        if (InvokeRequired) BeginInvoke(new Action(() => SetPictureBoxImage(picOutOverview, ovwBytes)));
                        else SetPictureBoxImage(picOutOverview, ovwBytes);
                    }
                    catch { }
                }

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
                    try
                    {
                        byte[] pltBytes = File.ReadAllBytes(filePlate);
                        if (InvokeRequired) BeginInvoke(new Action(() => SetPictureBoxImage(picOutPlate, pltBytes)));
                        else SetPictureBoxImage(picOutPlate, pltBytes);
                    }
                    catch { }
                }

                void UpdateOutUi()
                {
                    lblOutTimeVal.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                    if (anprResult != null && anprResult.IsSuccess)
                    {
                        txtOutPlate.Text = anprResult.FormattedPlate;
                        lblOutStatusVal.Text = "🔴 Đã nhận diện biển số ra";
                        lblOutStatusVal.ForeColor = Color.SeaGreen;
                    }
                    else
                    {
                        txtOutPlate.Text = plateOk ? "Không đọc được" : "";
                        lblOutStatusVal.Text = plateOk ? "⚪ Không đọc được biển" : "❌ Camera biển số lỗi";
                        lblOutStatusVal.ForeColor = Color.FromArgb(200, 120, 30);
                    }
                }

                if (InvokeRequired) BeginInvoke(new Action(UpdateOutUi));
                else UpdateOutUi();

                SetFooterStatus($"📸 Đã chụp và xử lý LÀN RA lúc {DateTime.Now:HH:mm:ss.fff}");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"Lỗi chụp ảnh Làn Ra: {ex.Message}", "LaneControl");
                SetFooterStatus($"Lỗi chụp ảnh Làn Ra: {ex.Message}");
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
