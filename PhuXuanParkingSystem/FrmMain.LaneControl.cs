using PhuXuanParkingSystem.Services.Anpr;
using PhuXuanParkingSystem.Services.Controller;
using PhuXuanParkingSystem.Services.Logging;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhuXuanParkingSystem
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

                if (File.Exists(fileOverview)) DisplayCapturedImage(picInOverview, fileOverview);

                PlateRecognitionResult? anprResult = null;
                if (File.Exists(filePlate))
                {
                    anprResult = await _anprService.RecognizeAsync(filePlate);
                }

                if (anprResult?.CroppedPlateImage != null)
                {
                    DisplayCapturedBitmap(picInPlate, anprResult.CroppedPlateImage);
                }
                else if (File.Exists(filePlate))
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
                AppLogger.Error(ex, $"Lỗi chụp ảnh Làn Vào: {ex.Message}");
                SetFooterStatus($"Lỗi chụp ảnh Làn Vào: {ex.Message}");
            }
        }

        private void BindInLaneResultToUi(Services.Parking.LaneProcessResult res)
        {
            lblInTimeVal.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            if (res.IsIgnored)
            {
                lblInStatusVal.Text = res.StatusText;
                lblInStatusVal.ForeColor = res.StatusColor;
                return;
            }

            txtInPlate.Text = res.DisplayPlate;
            lblInOwnerVal.Text = res.OwnerName;
            lblInDeptVal.Text = res.DepartmentName;
            lblInTypeVal.Text = res.VehicleType == Models.Enums.VehicleType.Car ? "Ô tô" : "Xe máy";
            lblInStatusVal.Text = res.StatusText;
            lblInStatusVal.ForeColor = res.StatusColor;
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

                var tPlate = _outPlateCam.CaptureToFileAsync(filePlate);
                var tOverview = _outOverviewCam.CaptureToFileAsync(fileOverview);
                await Task.WhenAll(tPlate, tOverview);

                if (File.Exists(fileOverview)) DisplayCapturedImage(picOutOverview, fileOverview);

                PlateRecognitionResult? anprResult = null;
                if (File.Exists(filePlate))
                {
                    anprResult = await _anprService.RecognizeAsync(filePlate);
                }

                if (anprResult?.CroppedPlateImage != null)
                {
                    DisplayCapturedBitmap(picOutPlate, anprResult.CroppedPlateImage);
                }
                else if (File.Exists(filePlate))
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
                AppLogger.Error(ex, $"Lỗi chụp ảnh Làn Ra: {ex.Message}");
                SetFooterStatus($"Lỗi chụp ảnh Làn Ra: {ex.Message}");
            }
        }

        private void BindOutLaneResultToUi(Services.Parking.LaneProcessResult res)
        {
            lblOutTimeVal.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
            if (res.IsIgnored)
            {
                lblOutStatusVal.Text = res.StatusText;
                lblOutStatusVal.ForeColor = res.StatusColor;
                return;
            }

            txtOutPlate.Text = res.DisplayPlate;
            lblOutOwnerVal.Text = res.OwnerName;
            lblOutDeptVal.Text = res.DepartmentName;
            lblOutTypeVal.Text = res.VehicleType == Models.Enums.VehicleType.Car ? "Ô tô" : "Xe máy";
            lblOutStatusVal.Text = res.StatusText;
            lblOutStatusVal.ForeColor = res.StatusColor;
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
            catch
            {
                // Xử lý an toàn
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
    }
}
