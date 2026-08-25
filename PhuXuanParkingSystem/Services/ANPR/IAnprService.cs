using System;
using System.Threading;
using System.Threading.Tasks;
using PhuXuanParkingSystem.Models.ValueObjects;

namespace PhuXuanParkingSystem.Services.ANPR
{
    /// <summary>
    /// Giao diện dịch vụ nhận dạng biển số xe tự động (ANPR / OCR).
    /// </summary>
    public interface IAnprService : IDisposable
    {
        string LaneId { get; }
        bool IsReady { get; }

        /// <summary>
        /// Phân tích ảnh từ mảng byte[] trong RAM và trả về kết quả biển số xe.
        /// </summary>
        Task<AnprResult> RecognizeAsync(byte[] imageBytes, CancellationToken ct = default);

        /// <summary>
        /// Phân tích ảnh từ file đĩa an toàn (không khóa file).
        /// </summary>
        Task<AnprResult> RecognizeFileAsync(string imageFilePath, CancellationToken ct = default);
    }
}