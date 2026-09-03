using PhuXuanParkingSystem.Services.Logging;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Devices.Camera
{
    /// <summary>
    /// Helper dùng chung cho việc lưu file ảnh snapshot bất đồng bộ với mức chất lượng chuẩn hóa
    /// </summary>
    public static class CameraCaptureHelper
    {
        public static async Task<bool> SaveBytesToFileAsync(
            byte[]? bytes,
            string filePath,
            string logTag,
            string logCategory,
            CancellationToken cancellationToken = default)
        {
            if (bytes == null || bytes.Length == 0)
            {
                AppLogger.Warning($"[{logTag}] Không nhận được dữ liệu ảnh để ghi ra file: {filePath}", logCategory);
                return false;
            }

            try
            {
                string? dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // Nếu ảnh nhận được là BMP, tự động nén sang JPEG mức chất lượng trung bình (Quality = 75%)
                if (bytes.Length > 2 && bytes[0] == 0x42 && bytes[1] == 0x4D)
                {
                    try
                    {
                        using var ms = new MemoryStream(bytes);
                        using var bmp = new Bitmap(ms);
                        SaveBitmapAsMediumJpeg(bmp, filePath, 75L);
                        return true;
                    }
                    catch
                    {
                        // Fallback ghi file nhị phân trực tiếp nếu lỗi decode BMP
                    }
                }

                using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, true);
                await fs.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                return true;
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"[{logTag}] Lỗi ghi file ảnh snapshot ({filePath}): {ex.Message}", logCategory);
                return false;
            }
        }

        /// <summary>
        /// Lưu Bitmap ra định dạng JPEG với mức chất lượng chỉ định (Mặc định: 75% - Mức trung bình)
        /// </summary>
        public static void SaveBitmapAsMediumJpeg(Bitmap bitmap, string filePath, long quality = 75L)
        {
            var jgpEncoder = GetEncoder(ImageFormat.Jpeg);
            if (jgpEncoder != null)
            {
                using var myEncoderParameters = new EncoderParameters(1);
                myEncoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, quality);
                bitmap.Save(filePath, jgpEncoder, myEncoderParameters);
            }
            else
            {
                bitmap.Save(filePath, ImageFormat.Jpeg);
            }
        }

        private static ImageCodecInfo? GetEncoder(ImageFormat format)
        {
            return ImageCodecInfo.GetImageDecoders().FirstOrDefault(codec => codec.FormatID == format.Guid);
        }
    }
}
