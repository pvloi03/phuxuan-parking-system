using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using PhuXuanParkingSystem.Models.ValueObjects;

namespace PhuXuanParkingSystem.Services.ANPR
{
    public class ParsedPlateResult
    {
        public string LicensePlate { get; }
        public double Confidence { get; }
        public RectangleF PlateBox { get; }
        public bool IsSuccess { get; }

        public ParsedPlateResult(string licensePlate, double confidence, RectangleF plateBox, bool isSuccess)
        {
            LicensePlate = licensePlate ?? string.Empty;
            Confidence = confidence;
            PlateBox = plateBox;
            IsSuccess = isSuccess;
        }
    }

    /// <summary>
    /// Bộ phân tích và chuẩn hóa biển số xe Việt Nam chuyên sâu:
    /// - Hỗ trợ biển 1 dòng (Ô tô dài, xe tải) và biển 2 dòng (Xe máy, Ô tô vuông).
    /// - Tích hợp Positional Semantic Correction (Sửa lỗi nhầm lẫn ký tự OCR: 8<->B, 0<->D/O, 5<->S, 1<->I/L).
    /// - Bộ lọc Blacklist loại bỏ chữ thương hiệu xe (HONDA, YAMAHA, TAXI, VISION, SH...) và số điện thoại quảng cáo.
    /// </summary>
    public static class VietnamLicensePlateParser
    {
        private static readonly HashSet<string> VehicleBrandBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "HONDA", "YAMAHA", "SUZUKI", "SYM", "PIAGGIO", "VESPA", "DUCATI", "KAWASAKI",
            "TOYOTA", "HYUNDAI", "KIA", "MAZDA", "FORD", "MITSUBISHI", "CHEVROLET",
            "NISSAN", "MERCEDES", "BMW", "AUDI", "LEXUS", "VINFAST", "ISUZU", "HINO",
            "TAXI", "MAILINH", "MAI LINH", "GRAB", "BE", "GOJEK", "VIETSOVPETRO",
            "AIRBLADE", "VISION", "LEAD", "SH", "WAVE", "SIRIUS", "EXCITER", "WINNER",
            "INNOVA", "VIOS", "FORTUNER", "ACCENT", "RVR", "RANGER", "SANTAFE", "SORENTO"
        };

        private static readonly HashSet<string> TwoLetterCarSeries = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "LD", "NN", "NG", "DA", "KT", "MD"
        };

        // Regex biển 1 dòng: 51F-123.45, 30A-999.99, 17B-123.45, 80A-123.45, 51LD-123.45, 29A-1234
        private static readonly Regex SingleLinePlateRegex = new Regex(
            @"^(?<prov>\d{2})(?<series>[A-Z]{1,2}|[A-Z]\d)[-. ]?(?<num1>\d{3})[-. ]?(?<num2>\d{2})$|" +
            @"^(?<prov>\d{2})(?<series>[A-Z]{1,2}|[A-Z]\d)[-. ]?(?<num4>\d{4})$|" +
            @"^(?<prov>\d{2})(?<series>[A-Z]{1,2}|[A-Z]\d)[-. ]?(?<num5>\d{5})$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Regex dòng trên của biển 2 dòng: 17-B1, 29-H1, 59-X1, 51F, 30A, 80-B1
        private static readonly Regex TopLineRegex = new Regex(
            @"^(?<prov>\d{2})[-. ]?(?<series>[A-Z\d]{1,2}[A-Z\d]?)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Regex dòng dưới của biển 2 dòng: 123.45, 678.90, 1234, 12345
        private static readonly Regex BottomLineRegex = new Regex(
            @"^(?<num1>\d{3})[-. ]?(?<num2>\d{2})$|^(\d{4,5})$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

                /// <summary>
        /// Phân tích trực tiếp từ chuỗi OCR thô (VD: từ Tesseract hoặc OCR engine)
        /// </summary>
        public static ParsedPlateResult ParseFromRawText(string rawOcrText, double confidence = 0.9)
        {
            if (string.IsNullOrWhiteSpace(rawOcrText))
            {
                return new ParsedPlateResult(string.Empty, 0.0, RectangleF.Empty, false);
            }

            var lines = rawOcrText.Split(new[] { '\r', '\n', '|' }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(l => l.Trim())
                                  .Where(l => !string.IsNullOrWhiteSpace(l) && l.Length >= 3)
                                  .ToList();

            var blocks = new List<OcrTextBlock>();
            float y = 50;
            foreach (var line in lines)
            {
                blocks.Add(new OcrTextBlock(line, (float)confidence, new PointF[0], new RectangleF(50, y, 200, 40)));
                y += 45;
            }

            return Parse(blocks);
        }
        public static ParsedPlateResult Parse(IReadOnlyList<OcrTextBlock> blocks, int imageWidth = 1920, int imageHeight = 1080)
        {
            if (blocks == null || blocks.Count == 0)
            {
                return new ParsedPlateResult(string.Empty, 0.0, RectangleF.Empty, false);
            }

            // 1. Lọc và chuẩn hóa các block chữ
            var cleanedBlocks = new List<OcrTextBlock>();
            foreach (var block in blocks)
            {
                var cleanText = NormalizeOcrText(block.Text);
                if (string.IsNullOrWhiteSpace(cleanText) || cleanText.Length < 3)
                    continue;

                if (IsBlacklistedBrandOrNoise(cleanText))
                    continue;

                cleanedBlocks.Add(new OcrTextBlock(cleanText, block.Score, block.Points, block.BoundingBox));
            }

            if (cleanedBlocks.Count == 0)
            {
                return new ParsedPlateResult(string.Empty, 0.0, RectangleF.Empty, false);
            }

            var candidates = new List<(string Plate, double Score, RectangleF Box)>();

            // 2. Thử tìm biển 1 dòng hoàn chỉnh trong từng block (Kèm Positional Semantic Correction)
            foreach (var block in cleanedBlocks)
            {
                var match = SingleLinePlateRegex.Match(block.Text);
                if (match.Success)
                {
                    var formatted = FormatSingleLine(match);
                    candidates.Add((formatted, block.Score, block.BoundingBox));
                    continue;
                }

                string correctedText = ApplyPositionalCorrectionSingleLine(block.Text);
                if (!string.IsNullOrEmpty(correctedText))
                {
                    var correctedMatch = SingleLinePlateRegex.Match(correctedText);
                    if (correctedMatch.Success)
                    {
                        var formatted = FormatSingleLine(correctedMatch);
                        candidates.Add((formatted, block.Score * 0.95, block.BoundingBox));
                    }
                }
            }

            // 3. Nếu chưa thấy biển 1 dòng, thử ghép cặp 2 dòng (Top Line + Bottom Line)
            if (candidates.Count == 0 && cleanedBlocks.Count >= 2)
            {
                var sortedBlocks = cleanedBlocks.OrderBy(b => b.BoundingBox.Top).ToList();

                for (int i = 0; i < sortedBlocks.Count; i++)
                {
                    var topBlock = sortedBlocks[i];
                    string topCandidateText = topBlock.Text;

                    var topMatch = TopLineRegex.Match(topCandidateText);
                    if (!topMatch.Success)
                    {
                        topCandidateText = ApplyPositionalCorrectionTopLine(topBlock.Text);
                        topMatch = TopLineRegex.Match(topCandidateText);
                    }

                    if (!topMatch.Success) continue;

                    for (int j = i + 1; j < sortedBlocks.Count; j++)
                    {
                        var bottomBlock = sortedBlocks[j];
                        string bottomCandidateText = bottomBlock.Text;

                        float xOverlap = Math.Max(0, Math.Min(topBlock.BoundingBox.Right, bottomBlock.BoundingBox.Right) -
                                                     Math.Max(topBlock.BoundingBox.Left, bottomBlock.BoundingBox.Left));
                        float yDistance = bottomBlock.BoundingBox.Top - topBlock.BoundingBox.Bottom;

                        if (xOverlap <= 0 || yDistance < -15 || yDistance > topBlock.BoundingBox.Height * 3.5f)
                            continue;

                        var bottomMatch = BottomLineRegex.Match(bottomCandidateText);
                        if (!bottomMatch.Success)
                        {
                            bottomCandidateText = ApplyPositionalCorrectionBottomLine(bottomBlock.Text);
                            bottomMatch = BottomLineRegex.Match(bottomCandidateText);
                        }

                        if (bottomMatch.Success)
                        {
                            var combinedPlate = FormatTwoLines(topMatch, bottomMatch);
                            double avgScore = (topBlock.Score + bottomBlock.Score) / 2.0;
                            var unionBox = RectangleF.Union(topBlock.BoundingBox, bottomBlock.BoundingBox);
                            candidates.Add((combinedPlate, avgScore, unionBox));
                        }
                    }
                }
            }

            // 4. Fallback thông minh: Tìm chuỗi có cấu trúc biển số gần đúng nhất
            if (candidates.Count == 0)
            {
                foreach (var block in cleanedBlocks)
                {
                    var raw = block.Text.Replace("-", "").Replace(".", "").Replace(" ", "").ToUpperInvariant();
                    string correctedRaw = ApplyPositionalCorrectionSingleLine(raw);

                    if (correctedRaw.Length >= 7 && correctedRaw.Length <= 9 &&
                        char.IsDigit(correctedRaw[0]) && char.IsDigit(correctedRaw[1]))
                    {
                        var formatted = FormatRawPlate(correctedRaw);
                        candidates.Add((formatted, block.Score * 0.85, block.BoundingBox));
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return new ParsedPlateResult(string.Empty, 0.0, RectangleF.Empty, false);
            }

            var best = candidates.OrderByDescending(c => c.Score).First();
            return new ParsedPlateResult(best.Plate, best.Score, best.Box, true);
        }

        #region Positional Semantic Correction (Sửa Lỗi Ngữ Nghĩa Vị Trí)

        /// <summary>
        /// Sửa lỗi ký tự OCR cho biển số 1 dòng (VD: 5IF-I23.4S -> 51F-123.45)
        /// </summary>
        public static string ApplyPositionalCorrectionSingleLine(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;
            string clean = Regex.Replace(rawText, @"[^\w]", "").ToUpperInvariant();
            if (clean.Length < 7 || clean.Length > 9) return string.Empty;

            char[] chars = clean.ToCharArray();

            // Vị trí 0, 1 (Mã tỉnh) -> Bắt buộc là SỐ
            chars[0] = FixLetterToDigit(chars[0]);
            chars[1] = FixLetterToDigit(chars[1]);

            // Vị trí 2 (Ký tự Seri đầu) -> Bắt buộc là CHỮ CÁI
            chars[2] = FixDigitToLetter(chars[2]);

            int numStartIndex = 3;
            if (clean.Length == 8)
            {
                // Độ dài 8: 51F12345 (ô tô 5 số) hoặc 29H11234 (xe máy 4 số)
                chars[3] = FixLetterToDigit(chars[3]);
                numStartIndex = 4;
            }
            else if (clean.Length == 9)
            {
                // Độ dài 9: 51LD12345 (ô tô 2 chữ) hoặc 29H112345 (xe máy 5 số)
                if (IsKnownTwoLetterCarSeries(chars[2], chars[3]))
                {
                    chars[3] = FixDigitToLetter(chars[3]);
                }
                else
                {
                    chars[3] = FixLetterToDigit(chars[3]);
                }
                numStartIndex = 4;
            }
            else if (clean.Length == 7) // 29A1234 (ô tô 4 số)
            {
                chars[3] = FixLetterToDigit(chars[3]);
                numStartIndex = 4;
            }

            // Toàn bộ các vị trí còn lại ở đuôi -> Bắt buộc là SỐ
            for (int i = numStartIndex; i < chars.Length; i++)
            {
                chars[i] = FixLetterToDigit(chars[i]);
            }

            return new string(chars);
        }

        private static bool IsKnownTwoLetterCarSeries(char c1, char c2)
        {
            string candidate = $"{c1}{c2}".ToUpperInvariant();
            return TwoLetterCarSeries.Contains(candidate);
        }

        /// <summary>
        /// Sửa lỗi dòng trên biển 2 dòng (VD: I7-BI -> 17-B1, OI-H1 -> 01-H1)
        /// </summary>
        public static string ApplyPositionalCorrectionTopLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            string clean = Regex.Replace(text, @"[^\w]", "").ToUpperInvariant();
            if (clean.Length < 3 || clean.Length > 5) return text;

            char[] chars = clean.ToCharArray();
            chars[0] = FixLetterToDigit(chars[0]);
            chars[1] = FixLetterToDigit(chars[1]);
            chars[2] = FixDigitToLetter(chars[2]);

            if (chars.Length >= 4)
            {
                // Vị trí 3 của xe máy là số (17B1)
                chars[3] = FixLetterToDigit(chars[3]);
            }

            return new string(chars);
        }

        /// <summary>
        /// Sửa lỗi dòng dưới biển 2 dòng (VD: I23.4S -> 123.45)
        /// </summary>
        public static string ApplyPositionalCorrectionBottomLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            string clean = Regex.Replace(text, @"[^\w]", "").ToUpperInvariant();
            if (clean.Length < 4 || clean.Length > 5) return text;

            char[] chars = clean.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                chars[i] = FixLetterToDigit(chars[i]);
            }

            return new string(chars);
        }

        public static char FixLetterToDigit(char c)
        {
            switch (c)
            {
                case 'O':
                case 'D':
                case 'Q':
                case 'U': return '0';
                case 'I':
                case 'L':
                case 'T':
                case 'J': return '1';
                case 'Z': return '2';
                case 'E': return '3';
                case 'A': return '4';
                case 'S': return '5';
                case 'G': return '6';
                case 'B': return '8';
                case 'P':
                case 'q': return '9';
                default: return c;
            }
        }

        public static char FixDigitToLetter(char c)
        {
            switch (c)
            {
                case '8': return 'B';
                case '0': return 'D';
                case '1': return 'L';
                case '5': return 'S';
                case '2': return 'Z';
                case '6': return 'G';
                default: return c;
            }
        }

        #endregion

        #region Helpers & Formatters

        private static string NormalizeOcrText(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            var cleaned = input.Trim().ToUpperInvariant();
            cleaned = cleaned.Replace("–", "-").Replace("—", "-").Replace("—", "-");
            cleaned = Regex.Replace(cleaned, @"[^\w\-\. ]", "");
            return cleaned;
        }

        private static bool IsBlacklistedBrandOrNoise(string text)
        {
            var noSpaces = text.Replace(" ", "").Replace("-", "").Replace(".", "");
            if (VehicleBrandBlacklist.Contains(text) || VehicleBrandBlacklist.Contains(noSpaces))
                return true;

            if (Regex.IsMatch(noSpaces, @"\d{10,}"))
                return true;

            return false;
        }

        private static string FormatSingleLine(Match match)
        {
            var prov = match.Groups["prov"].Value.ToUpperInvariant();
            var series = match.Groups["series"].Value.ToUpperInvariant();

            if (match.Groups["num1"].Success && match.Groups["num2"].Success)
            {
                return $"{prov}{series}-{match.Groups["num1"].Value}.{match.Groups["num2"].Value}";
            }

            if (match.Groups["num5"].Success)
            {
                var num5 = match.Groups["num5"].Value;
                if (num5.Length == 5)
                    return $"{prov}{series}-{num5.Substring(0, 3)}.{num5.Substring(3)}";
                return $"{prov}{series}-{num5}";
            }

            if (match.Groups["num4"].Success)
            {
                return $"{prov}{series}-{match.Groups["num4"].Value}";
            }

            return match.Value.ToUpperInvariant();
        }

        private static string FormatTwoLines(Match topMatch, Match bottomMatch)
        {
            var prov = topMatch.Groups["prov"].Value.ToUpperInvariant();
            var series = topMatch.Groups["series"].Value.ToUpperInvariant();

            string bottomText = bottomMatch.Value.Replace(" ", "").Replace("-", "").Replace(".", "");
            if (bottomText.Length == 5)
            {
                return $"{prov}{series}-{bottomText.Substring(0, 3)}.{bottomText.Substring(3)}";
            }

            return $"{prov}{series}-{bottomText}";
        }

        private static string FormatRawPlate(string raw)
        {
            if (raw.Length == 7) // 29A1234
            {
                return $"{raw.Substring(0, 3)}-{raw.Substring(3)}";
            }
            if (raw.Length == 8)
            {
                if (char.IsDigit(raw[3]))
                    return $"{raw.Substring(0, 4)}-{raw.Substring(4)}";
                else
                    return $"{raw.Substring(0, 3)}-{raw.Substring(3, 3)}.{raw.Substring(6)}";
            }
            if (raw.Length == 9)
            {
                return $"{raw.Substring(0, 4)}-{raw.Substring(4, 3)}.{raw.Substring(7)}";
            }

            return raw;
        }

        #endregion
    }
}