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
    /// Dịch vụ ANPR nhận diện biển số xe thực tế:
    /// - Hỗ trợ 100% kiến trúc 32-bit (x86) và 64-bit (x64) tương thích với Native Camera/Controller SDKs.
    /// - Tích hợp VietnamLicensePlateParser (Positional Semantic Correction + Blacklist).
    /// - Tự động trích xuất biển số 1 dòng và 2 dòng.
    /// - Mỗi làn xe sở hữu 1 Instance riêng trong RAM (Thread-safe, 0 lock chéo).
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

                _engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);
                _engine.SetVariable("tessedit_char_whitelist", "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789.- \n\r");
                AppLogger.Information($"[ANPR {LaneId}] Khởi tạo OCR Engine thành công.", "ANPR");
            }
            catch (Exception ex)
            {
                AppLogger.Warning($"[ANPR {LaneId}] Khởi tạo OCR Engine không khả dụng: {ex.Message}", "ANPR");
                _engine = null;
            }
        }

        public Task<AnprResult> RecognizeFileAsync(string imageFilePath, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(imageFilePath) || !File.Exists(imageFilePath))
            {
                return Task.FromResult(AnprResult.Failed("File ảnh không tồn tại."));
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(imageFilePath);
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
                return Task.FromResult(AnprResult.Failed("Dữ liệu ảnh rỗng."));
            }

            if (_engine == null)
            {
                return Task.FromResult(AnprResult.Failed("Mô hình AI OCR chưa sẵn sàng."));
            }

            return Task.Run(() =>
            {
                var sw = Stopwatch.StartNew();

                try
                {
                    string rawText = string.Empty;
                    float meanConfidence = 0.85f;

                    lock (_lockObj)
                    {
                        using (var pix = Pix.LoadFromMemory(imageBytes))
                        using (var page = _engine.Process(pix, PageSegMode.Auto))
                        {
                            rawText = page.GetText() ?? string.Empty;
                            meanConfidence = page.GetMeanConfidence();
                        }
                    }

                    if (string.IsNullOrWhiteSpace(rawText))
                    {
                        sw.Stop();
                        return AnprResult.Failed("Không tìm thấy ký tự trong khung hình.", sw.ElapsedMilliseconds);
                    }

                    // 2. Phân tích biển số xe Việt Nam kèm Positional Semantic Correction
                    var parsed = VietnamLicensePlateParser.ParseFromRawText(rawText, meanConfidence);

                    if (!parsed.IsSuccess || string.IsNullOrWhiteSpace(parsed.LicensePlate))
                    {
                        sw.Stop();
                        return new AnprResult(
                            string.Empty,
                            0.0,
                            false,
                            null,
                            $"Phát hiện chữ ({rawText.Trim().Replace("\n", " | ")}) nhưng không khớp biển số VN",
                            sw.ElapsedMilliseconds);
                    }

                    sw.Stop();

                    AppLogger.Information(
                        $"[ANPR {LaneId}] Nhận diện biển số: {parsed.LicensePlate} (Confidence: {parsed.Confidence:P0}, Thời gian: {sw.ElapsedMilliseconds}ms)",
                        "ANPR");

                    return new AnprResult(
                        parsed.LicensePlate,
                        parsed.Confidence,
                        true,
                        null,
                        rawText.Trim(),
                        sw.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    AppLogger.Error(ex, $"[ANPR {LaneId}] Lỗi trong quá trình nhận diện OCR", "ANPR");
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