using HPParkingSystem.Services.Logging;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HPParkingSystem.Services.Devices.Camera
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

                byte[] dataToWrite = bytes;

                // Nếu ảnh nhận được là BMP, tự động nén sang JPEG trong bộ nhớ RAM (Quality = 75%)
                if (bytes.Length > 2 && bytes[0] == 0x42 && bytes[1] == 0x4D)
                {
                    try
                    {
                        using var inMs = new MemoryStream(bytes);
                        using var bmp = new Bitmap(inMs);
                        using var outMs = new MemoryStream();
                        var jgpEncoder = GetEncoder(ImageFormat.Jpeg);
                        if (jgpEncoder != null)
                        {
                            using var myEncoderParameters = new EncoderParameters(1);
                            myEncoderParameters.Param[0] = new EncoderParameter(Encoder.Quality, 75L);
                            bmp.Save(outMs, jgpEncoder, myEncoderParameters);
                        }
                        else
                        {
                            bmp.Save(outMs, ImageFormat.Jpeg);
                        }
                        dataToWrite = outMs.ToArray();
                    }
                    catch (Exception ex)
                    {
                        // Fallback ghi dữ liệu gốc nếu lỗi nén BMP trong RAM
                        AppLogger.Warning($"[{logTag}] Không thể nén BMP sang JPEG trong RAM ({ex.Message}), sẽ lưu dữ liệu gốc.", logCategory);
                    }
                }

                // Ghi dữ liệu ra file với cơ chế Retry chống tranh chấp khóa mạng SMB / Windows Defender quét file
                const int maxRetries = 3;
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete, 4096, true);
                        await fs.WriteAsync(dataToWrite, 0, dataToWrite.Length, cancellationToken).ConfigureAwait(false);
                        return true;
                    }
                    catch (IOException ex) when (attempt < maxRetries)
                    {
                        AppLogger.Warning($"[{logTag}] Tranh chấp file tạm thời ({filePath}), thử lại lần {attempt}/{maxRetries} sau 50ms: {ex.Message}", logCategory);
                        await Task.Delay(50, cancellationToken).ConfigureAwait(false);
                    }
                }

                return false;
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
