using System;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Storage
{
    /// <summary>
    /// Kết quả lưu trữ ảnh chụp làn xe
    /// </summary>
    public class ImageSaveResult
    {
        public bool Success { get; set; }
        public string FinalPath { get; set; } = string.Empty;
        public bool IsSavedLocally { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Dịch vụ lưu trữ và đồng bộ file ảnh với cơ chế chống chịu lỗi khi mất kết nối mạng máy chủ
    /// </summary>
    public interface IImageStorageService
    {
        /// <summary>
        /// Đường dẫn lưu trữ chính trên máy chủ (hỗ trợ đường dẫn mạng UNC ví dụ \\192.168.1.254\Captures)
        /// </summary>
        string PrimaryPath { get; }

        /// <summary>
        /// Đường dẫn lưu trữ cục bộ dự phòng trên máy bốt
        /// </summary>
        string OfflinePath { get; }

        /// <summary>
        /// Lưu mảng byte ảnh vào nơi lưu trữ thích hợp (Primary hoặc Offline)
        /// </summary>
        Task<ImageSaveResult> SaveImageBytesAsync(byte[] imageBytes, string subFolder, string fileName, bool isServerOnline);

        /// <summary>
        /// Sao chép một file ảnh từ đường dẫn tạm vào nơi lưu trữ thích hợp
        /// </summary>
        Task<ImageSaveResult> SaveImageFileAsync(string sourceFilePath, string subFolder, string fileName, bool isServerOnline);

        /// <summary>
        /// Đồng bộ các file ảnh đang lưu tạm ở máy bốt lên máy chủ
        /// </summary>
        Task<int> SyncPendingImagesAsync();
    }
}
