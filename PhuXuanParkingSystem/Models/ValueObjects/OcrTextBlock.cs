using System.Drawing;

namespace PhuXuanParkingSystem.Models.ValueObjects
{
    /// <summary>
    /// Đại diện cho một khối chữ được phát hiện bởi RapidOCR.
    /// </summary>
    public class OcrTextBlock
    {
        public string Text { get; set; }
        public float Score { get; set; }
        public PointF[] Points { get; set; }
        public RectangleF BoundingBox { get; set; }

        public OcrTextBlock(string text, float score, PointF[] points, RectangleF boundingBox)
        {
            Text = text ?? string.Empty;
            Score = score;
            Points = points;
            BoundingBox = boundingBox;
        }
    }
}