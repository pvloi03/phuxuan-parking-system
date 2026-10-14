using HPParkingSystem.Models.ValueObjects;
using System;
using System.Drawing;

namespace HPParkingSystem.Services.Anpr
{
    /// <summary>
    /// Kết quả nhận diện biển số xe từ động cơ ANPR
    /// </summary>
    public class PlateRecognitionResult
    {
        public bool IsSuccess { get; set; }
        public string RawText { get; set; } = string.Empty;
        public string CleanPlate { get; set; } = string.Empty;
        public string FormattedPlate { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public Rectangle BoundingBox { get; set; }
        public Bitmap? CroppedPlateImage { get; set; }
        public long DurationMs { get; set; }
        public string? ErrorMessage { get; set; }

        public static PlateRecognitionResult Failed(string errorMessage, long durationMs = 0)
        {
            return new PlateRecognitionResult
            {
                IsSuccess = false,
                CleanPlate = PlateNumber.Unrecognized,
                FormattedPlate = PlateNumber.UnrecognizedDisplay,
                ErrorMessage = errorMessage,
                DurationMs = durationMs
            };
        }

        public static PlateRecognitionResult Success(string rawText, float confidence, Rectangle bbox = default, Bitmap? cropped = null, long durationMs = 0)
        {
            string clean = PlateNumber.Clean(rawText);
            bool isValid = PlateNumber.IsValid(clean);
            string formatted = isValid ? PlateNumber.FormatDisplay(clean) : PlateNumber.UnrecognizedDisplay;

            return new PlateRecognitionResult
            {
                IsSuccess = isValid,
                RawText = rawText,
                CleanPlate = isValid ? clean : PlateNumber.Unrecognized,
                FormattedPlate = formatted,
                Confidence = confidence,
                BoundingBox = bbox,
                CroppedPlateImage = cropped,
                DurationMs = durationMs
            };
        }
    }
}
