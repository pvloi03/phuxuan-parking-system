using System;
using System.Drawing;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Anpr
{
    /// <summary>
    /// Cổng giao tiếp cho dịch vụ nhận diện biển số xe (ANPR / LPR)
    /// </summary>
    public interface IPlateRecognitionService : IDisposable
    {
        /// <summary>
        /// Nhận diện biển số từ đường dẫn file ảnh
        /// </summary>
        PlateRecognitionResult Recognize(string imagePath);

        /// <summary>
        /// Nhận diện biển số từ đối tượng Bitmap
        /// </summary>
        PlateRecognitionResult Recognize(Bitmap bitmap);

        /// <summary>
        /// Nhận diện biển số bất đồng bộ từ đường dẫn file ảnh
        /// </summary>
        Task<PlateRecognitionResult> RecognizeAsync(string imagePath);

        /// <summary>
        /// Nhận diện biển số bất đồng bộ từ đối tượng Bitmap
        /// </summary>
        Task<PlateRecognitionResult> RecognizeAsync(Bitmap bitmap);
    }
}
