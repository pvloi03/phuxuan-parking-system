using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PhuXuanParkingSystem.Models.ValueObjects;
using PhuXuanParkingSystem.Services.Logging;
using Tesseract;

namespace PhuXuanParkingSystem.Services.ANPR
{
    /// <summary>
    /// Dịch vụ ANPR nhận diện biển số xe thực tế kèm Logging chi tiết từng bước:
    /// </summary>
    public class RapidOcrAnprService : IAnprService
    {
        private readonly TesseractEngine? _engine;
        private readonly object _lockObj = new object();
        private bool _disposed;

        public string LaneId { get; }
        public bool IsReady => !_disposed && _engine != null;

        public RapidOcrAnprService(string laneId)
        {
            LaneId = laneId ?? "LANE_DEFAULT";

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string tessDataPath = Path.Combine(baseDir, "tessdata");
                if (!Directory.Exists(tessDataPath))
                {
                    tessDataPath = Path.Combine(Directory.GetCurrentDirectory(), "tessdata");
                }

                AppLogger.Information($"[ANPR {LaneId}] Thư mục tessdata: '{tessDataPath}', Tồn tại: {Directory.Exists(tessDataPath)}", "ANPR");

                _engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
                _engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.- \n\r");
                AppLogger.Information($"[ANPR {LaneId}] Khởi tạo OCR Engine (Tesseract x86/x64) thành công.", "ANPR");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"[ANPR {LaneId}] Khởi tạo OCR Engine THẤT BẠI: {ex.Message}", "ANPR");
                _engine = null;
            }
        }

        public Task<AnprResult> RecognizeFileAsync(string imageFilePath, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(imageFilePath) || !File.Exists(imageFilePath))
            {
                AppLogger.Warning($"[ANPR {LaneId}] File ảnh không tồn tại: '{imageFilePath}'", "ANPR");
                return Task.FromResult(AnprResult.Failed("File ảnh không tồn tại."));
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(imageFilePath);
                AppLogger.Information($"[ANPR {LaneId}] Đã đọc file ảnh: {Path.GetFileName(imageFilePath)} ({bytes.Length} bytes)", "ANPR");
                return RecognizeAsync(bytes, ct);
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"[ANPR {LaneId}] Lỗi đọc file ảnh {imageFilePath}", "ANPR");
                return Task.FromResult(AnprResult.Failed($"Lỗi đọc file: {ex.Message}"));
            }
        }

        public Task<AnprResult> RecognizeAsync(byte[] imageBytes, CancellationToken ct = default)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                AppLogger.Warning($"[ANPR {LaneId}] Dữ liệu ảnh byte[] rỗng.", "ANPR");
                return Task.FromResult(AnprResult.Failed("Dữ liệu ảnh rỗng."));
            }

            if (_engine == null)
            {
                AppLogger.Error($"[ANPR {LaneId}] Mô hình AI OCR chưa sẵn sàng (_engine is null).", "ANPR");
                return Task.FromResult(AnprResult.Failed("Mô hình AI OCR chưa sẵn sàng."));
            }

            return Task.Run(() =>
            {
                var sw = Stopwatch.StartNew();

                try
                {
                    string rawText = string.Empty;
                    float meanConfidence = 0.0f;

                    lock (_lockObj)
                    {
                        using (var pix = Pix.LoadFromMemory(imageBytes))
                        using (var page = _engine.Process(pix, PageSegMode.Auto))
                        {
                            rawText = page.GetText() ?? string.Empty;
                            meanConfidence = page.GetMeanConfidence();
                        }
                    }

                    rawText = rawText.Trim();
                    AppLogger.Information(
                        $"[ANPR {LaneId}] OCR quét xong trong {sw.ElapsedMilliseconds}ms | Độ tin cậy: {meanConfidence:P1} | Ký tự thô phát hiện được: '{rawText.Replace("\r", "").Replace("\n", " [NL] ")}'",
                        "ANPR");

                    if (string.IsNullOrWhiteSpace(rawText))
                    {
                        sw.Stop();
                        AppLogger.Warning($"[ANPR {LaneId}] Không phát hiện thấy bất kỳ ký tự nào trong ảnh.", "ANPR");
                        return AnprResult.Failed("Không tìm thấy ký tự trong khung hình.", sw.ElapsedMilliseconds);
                    }

                    // Phân tích biển số xe Việt Nam kèm Positional Semantic Correction
                    var parsed = VietnamLicensePlateParser.ParseFromRawText(rawText, meanConfidence);

                    sw.Stop();

                    if (!parsed.IsSuccess || string.IsNullOrWhiteSpace(parsed.LicensePlate))
                    {
                        AppLogger.Warning(
                            $"[ANPR {LaneId}] Ký tự thô không khớp định dạng biển số VN. RawText: '{rawText.Replace("\r", "").Replace("\n", " ")}'",
                            "ANPR");

                        return new AnprResult(
                            string.Empty,
                            0.0,
                            false,
                            null,
                            rawText,
                            sw.ElapsedMilliseconds);
                    }

                    AppLogger.Information(
                        $"[ANPR {LaneId}] ĐÃ TRÍCH XUẤT BIỂN SỐ THÀNH CÔNG: '{parsed.LicensePlate}' (Confidence: {parsed.Confidence:P0}, Thời gian: {sw.ElapsedMilliseconds}ms)",
                        "ANPR");

                    return new AnprResult(
                        parsed.LicensePlate,
                        parsed.Confidence,
                        true,
                        null,
                        rawText,
                        sw.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    AppLogger.Error(ex, $"[ANPR {LaneId}] Lỗi trong quá trình nhận diện OCR: {ex.Message}", "ANPR");
                    return AnprResult.Failed($"Lỗi OCR: {ex.Message}", sw.ElapsedMilliseconds);
                }
            }, ct);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    _engine?.Dispose();
                }
                catch { }
                _disposed = true;
            }
        }
    }
}