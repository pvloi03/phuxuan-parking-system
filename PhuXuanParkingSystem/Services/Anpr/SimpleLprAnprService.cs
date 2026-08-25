using PhuXuanParkingSystem.Models.ValueObjects;
using PhuXuanParkingSystem.Services.Logging;
using SimpleLPR3;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace PhuXuanParkingSystem.Services.Anpr
{
    /// <summary>
    /// Triển khai dịch vụ nhận diện biển số xe tốc độ cao qua SimpleLPR v3.x Native x86 Engine
    /// </summary>
    public class SimpleLprAnprService : IPlateRecognitionService
    {
        private readonly object _lock = new();
        private ISimpleLPR? _lpr;
        private IProcessor? _processor;
        private bool _isInitialized;
        private bool _disposed;

        public bool IsInitialized => _isInitialized;

        public SimpleLprAnprService()
        {
            InitializeEngine();
        }

        private void InitializeEngine()
        {
            lock (_lock)
            {
                if (_isInitialized) return;

                try
                {
                    EngineSetupParms setupParms;
                    setupParms.cudaDeviceId = -1; // Chế độ CPU tối ưu
                    setupParms.enableImageProcessingWithGPU = false;
                    setupParms.enableClassificationWithGPU = false;
                    setupParms.maxConcurrentImageProcessingOps = 0;

                    _lpr = SimpleLPR.Setup(setupParms);

                    // Thiết lập trọng số nhận diện cho biển số Việt Nam (VN)
                    for (uint i = 0; i < _lpr.numSupportedCountries; ++i)
                    {
                        string country = _lpr.get_countryCode(i);
                        _lpr.set_countryWeight(country, string.Equals(country, "VN", StringComparison.OrdinalIgnoreCase) ? 1.0f : 0.0f);
                    }
                    _lpr.realizeCountryWeights();

                    // Nạp License Key bản quyền
                    string? keyXml = LoadLicenseKeyXml();
                    if (!string.IsNullOrWhiteSpace(keyXml))
                    {
                        var keyBytes = Encoding.UTF8.GetBytes(XDocument.Parse(keyXml).Document?.ToString() ?? keyXml);
                        _lpr.set_productKey(keyBytes);
                    }

                    _processor = _lpr.createProcessor();
                    _processor.plateRegionDetectionEnabled = true;
                    _processor.cropToPlateRegionEnabled = true;

                    _isInitialized = true;
                    AppLogger.Information("SimpleLPR3 x86 Engine đã được khởi tạo thành công (Trọng số: VN 100%).");
                }
                catch (Exception ex)
                {
                    _isInitialized = false;
                    AppLogger.Error(ex, "Lỗi khởi tạo SimpleLPR3 ANPR Engine.");
                }
            }
        }

        private static string? LoadLicenseKeyXml()
        {
            try
            {
                // 1. Thử đọc từ Embedded Resource
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream("SimpleLPR3_key.xml")
                                   ?? assembly.GetManifestResourceStream("PhuXuanParkingSystem.SimpleLPR3_key.xml")
                                   ?? assembly.GetManifestResourceStream("PhuXuanParkingSystem.SDK.Native.x86.SimpleLPR3.SimpleLPR3_key.xml");

                if (stream != null)
                {
                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    return reader.ReadToEnd();
                }

                // 2. Thử đọc từ file cục bộ trong AppDomain
                string[] searchPaths = {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SimpleLPR3_key.xml"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SDK", "Native", "x86", "SimpleLPR3", "SimpleLPR3_key.xml")
                };

                foreach (var path in searchPaths)
                {
                    if (File.Exists(path))
                    {
                        return File.ReadAllText(path, Encoding.UTF8);
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Warning($"Không thể nạp file license SimpleLPR3_key.xml: {ex.Message}");
            }

            return null;
        }

        public PlateRecognitionResult Recognize(string imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            {
                return PlateRecognitionResult.Failed("Đường dẫn file ảnh không tồn tại.");
            }

            var sw = Stopwatch.StartNew();
            try
            {
                // Đọc byte array để tránh khóa (lock) file trên ổ cứng
                byte[] bytes = File.ReadAllBytes(imagePath);
                using var ms = new MemoryStream(bytes);
                using var bmp = new Bitmap(ms);

                return Recognize(bmp, sw);
            }
            catch (Exception ex)
            {
                sw.Stop();
                AppLogger.Error(ex, $"Lỗi xử lý nhận diện file ảnh: {imagePath}");
                return PlateRecognitionResult.Failed($"Lỗi xử lý file ảnh: {ex.Message}", sw.ElapsedMilliseconds);
            }
        }

        public PlateRecognitionResult Recognize(Bitmap bitmap)
        {
            var sw = Stopwatch.StartNew();
            return Recognize(bitmap, sw);
        }

        /// <summary>
        /// Chấm điểm và lọc nhiễu cho kết quả nhận diện biển số
        /// Ưu tiên biển số Việt Nam (7-9 ký tự, đúng tiền tố tỉnh thành), loại bỏ rác ngắn như "111", "III"
        /// </summary>
        private static float CalculateMatchScore(string? text, float confidence)
        {
            if (string.IsNullOrWhiteSpace(text)) return -1f;
            string clean = PlateNumber.Clean(text);

            // Bỏ qua các chuỗi quá ngắn (< 4 ký tự như "111", "III", "A1") vì là nhiễu từ vân gỗ / khe hẹp
            if (clean.Length < 4) return -1f;

            float score = confidence;

            // Ưu tiên độ dài chuẩn của biển số xe Việt Nam (7-9 ký tự: ví dụ 35B263371, 29A12345)
            if (clean.Length >= 7 && clean.Length <= 9)
            {
                score += 2.0f; // Điểm cộng quyết định so với các chuỗi ký tự rác
            }
            else if (clean.Length >= 5 && clean.Length <= 10)
            {
                score += 0.5f;
            }
            else
            {
                score -= 0.5f;
            }

            // Bắt đầu bằng 2 số tỉnh thành Việt Nam hợp lệ (11-99) kèm chữ cái
            if (Regex.IsMatch(clean, @"^[1-9][0-9][A-Z]"))
            {
                score += 1.0f;
            }

            // Khớp chính xác định dạng biển số xe Việt Nam (4 số hoặc 5 số)
            if (Regex.IsMatch(clean, @"^([0-9]{2}[A-Z]{1,2}[0-9]?)([0-9]{4,5})$"))
            {
                score += 1.5f;
            }

            return score;
        }

        private PlateRecognitionResult Recognize(Bitmap bitmap, Stopwatch sw)
        {
            if (!_isInitialized || _processor == null)
            {
                // Thử khởi tạo lại
                InitializeEngine();
                if (!_isInitialized || _processor == null)
                {
                    sw.Stop();
                    return PlateRecognitionResult.Failed("SimpleLPR3 Engine chưa sẵn sàng.", sw.ElapsedMilliseconds);
                }
            }

            lock (_lock)
            {
                try
                {
                    // Chạy nhận diện qua SimpleLPR Processor
                    List<Candidate> candidates = _processor.analyze(bitmap);
                    sw.Stop();

                    if (candidates == null || candidates.Count == 0)
                    {
                        return PlateRecognitionResult.Failed("Không phát hiện được biển số xe.", sw.ElapsedMilliseconds);
                    }

                    // Tìm candidate và match có điểm số đánh giá chuẩn nhất (lọc bỏ nhiễu)
                    Candidate? bestCandidate = null;
                    CountryMatch? bestMatch = null;
                    float maxScore = 0.0f;

                    foreach (var cand in candidates)
                    {
                        if (cand.matches != null && cand.matches.Count > 0)
                        {
                            foreach (var m in cand.matches)
                            {
                                float score = CalculateMatchScore(m.text, m.confidence);
                                if (score > maxScore)
                                {
                                    maxScore = score;
                                    bestMatch = m;
                                    bestCandidate = cand;
                                }
                            }
                        }
                    }

                    if (bestMatch.HasValue && maxScore > 0 && !string.IsNullOrWhiteSpace(bestMatch.Value.text))
                    {
                        Rectangle bbox = default;
                        Bitmap? croppedPlate = null;

                        if (bestCandidate.HasValue)
                        {
                            var candBbox = bestCandidate.Value.bbox;
                            bbox = new Rectangle(candBbox.Left, candBbox.Top, candBbox.Width, candBbox.Height);

                            // Cắt ảnh vùng biển số phục vụ hiển thị UI
                            if (bbox.Width > 0 && bbox.Height > 0 &&
                                bbox.Right <= bitmap.Width && bbox.Bottom <= bitmap.Height)
                            {
                                try
                                {
                                    croppedPlate = new Bitmap(bbox.Width, bbox.Height);
                                    using var g = Graphics.FromImage(croppedPlate);
                                    g.DrawImage(bitmap, new Rectangle(0, 0, bbox.Width, bbox.Height), bbox, GraphicsUnit.Pixel);
                                }
                                catch
                                {
                                    // Bỏ qua nếu lỗi cắt ảnh
                                }
                            }
                        }

                        var result = PlateRecognitionResult.Success(
                            bestMatch.Value.text,
                            bestMatch.Value.confidence,
                            bbox,
                            croppedPlate,
                            sw.ElapsedMilliseconds);

                        AppLogger.Information($"Nhận diện thành công: '{result.FormattedPlate}' (Raw: '{result.RawText}', Conf: {result.Confidence:P1}, Time: {result.DurationMs}ms)");
                        return result;
                    }

                    return PlateRecognitionResult.Failed("Không phát hiện được biển số xe hợp lệ (Độ dài / Cấu trúc không khớp).", sw.ElapsedMilliseconds);
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    AppLogger.Error(ex, "Lỗi xảy ra trong quá trình nhận diện biển số.");
                    return PlateRecognitionResult.Failed($"Lỗi nhận diện: {ex.Message}", sw.ElapsedMilliseconds);
                }
            }
        }

        public Task<PlateRecognitionResult> RecognizeAsync(string imagePath)
        {
            return Task.Run(() => Recognize(imagePath));
        }

        public Task<PlateRecognitionResult> RecognizeAsync(Bitmap bitmap)
        {
            return Task.Run(() => Recognize(bitmap));
        }

        public void Dispose()
        {
            if (_disposed) return;

            lock (_lock)
            {
                try
                {
                    _processor?.Dispose();
                }
                catch { }

                try
                {
                    _lpr?.Dispose();
                }
                catch { }

                _processor = null;
                _lpr = null;
                _isInitialized = false;
                _disposed = true;
            }

            GC.SuppressFinalize(this);
        }
    }
}
