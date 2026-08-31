using PhuXuanParkingSystem.Services.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Devices.Camera
{
    /// <summary>
    /// Helper dùng chung cho việc lưu file ảnh snapshot bất đồng bộ
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
    }
}
