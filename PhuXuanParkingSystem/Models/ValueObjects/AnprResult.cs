using System;

namespace PhuXuanParkingSystem.Models.ValueObjects
{
    /// <summary>
    /// Kết quả nhận dạng biển số xe từ Module ANPR AI.
    /// </summary>
    public class AnprResult
    {
        public string LicensePlate { get; set; }
        public double Confidence { get; set; }
        public bool IsSuccess { get; set; }
        public byte[]? PlateCropBytes { get; set; }
        public string RawOcrText { get; set; }
        public long ProcessTimeMs { get; set; }

        public AnprResult(
            string licensePlate,
            double confidence,
            bool isSuccess,
            byte[]? plateCropBytes = null,
            string rawOcrText = "",
            long processTimeMs = 0)
        {
            LicensePlate = licensePlate ?? string.Empty;
            Confidence = confidence;
            IsSuccess = isSuccess;
            PlateCropBytes = plateCropBytes;
            RawOcrText = rawOcrText ?? string.Empty;
            ProcessTimeMs = processTimeMs;
        }

        public static AnprResult Failed(string reason = "", long elapsedMs = 0)
        {
            return new AnprResult(string.Empty, 0.0, false, null, reason, elapsedMs);
        }
    }
}