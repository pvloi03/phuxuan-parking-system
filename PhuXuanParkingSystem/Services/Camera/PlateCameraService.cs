using PhuXuanParkingSystem.Services.Logging;
using PhuXuanParkingSystem.SDK.NST;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Camera
{
    /// <summary>
    /// Service kết nối và điều khiển Camera Biển Số (NST SDK)
    /// Đã tối ưu hóa hiệu năng, xử lý không khóa file và chụp ảnh tốc độ cao
    /// </summary>
    public class PlateCameraService : IDisposable
    {
        private static readonly object _sdkLock = new();
        private static volatile bool _sdkInitialized = false;

        private const int CaptureBufferSize = 3 * 1024 * 1024; // 3MB

        private readonly object _lockObj = new();
        private readonly byte[] _captureBuffer = new byte[CaptureBufferSize];

        private int _handle = 0;
        private bool _isPreviewing = false;

        public CameraConfig Config { get; set; } = new();

        public bool IsLoggedIn { get; private set; }

        public event Action<bool, string> OnStatusChanged = delegate { };

        public PlateCameraService()
        {
            InitializeSdk();
        }

        public static void InitializeSdk()
        {
            if (_sdkInitialized) return;

            lock (_sdkLock)
            {
                if (_sdkInitialized) return;

                int ret = CHISDK.HI_SDK_Init();

                if (ret != CHISDK.HI_SUCCESS)
                {
                    throw new InvalidOperationException(
                        $"Khởi tạo HISDK (Camera biển số) thất bại. Mã lỗi: {ret}");
                }

                _sdkInitialized = true;
            }
        }

        public async Task<bool> LoginAsync(CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                lock (_lockObj)
                {
                    if (IsLoggedIn && _handle > 0) return true;

                    if (Config == null || string.IsNullOrEmpty(Config.Ip))
                    {
                        AppLogger.Warning("[NST Camera] Login thất bại: Config hoặc IP trống.", "NSTCamera");
                        return false;
                    }

                    int errCode = 0;
                    _handle = CHISDK.HI_SDK_LoginExt(
                        Config.Ip,
                        Config.UserName ?? "",
                        Config.Password ?? "",
                        Config.Port,
                        5000,
                        out errCode);

                    IsLoggedIn = _handle > 0;

                    if (IsLoggedIn)
                    {
                        CHISDK.HI_SDK_SetConnectTime(_handle, 3000);
                        CHISDK.HI_SDK_SetReconnect(_handle, 5000);

                        OnStatusChanged?.Invoke(true, "Đã kết nối Camera biển số thành công.");
                        return true;
                    }

                    AppLogger.Error($"[NST Camera {Config.Ip}] Login thất bại. Handle={_handle}, Error={errCode}", "NSTCamera");
                    OnStatusChanged?.Invoke(false, $"Kết nối Camera biển số thất bại. Mã lỗi: {errCode}");
                    return false;
                }
            }, cancellationToken);
        }

        public bool StartPreview(IntPtr windowHandle)
        {
            lock (_lockObj)
            {
                if (!IsLoggedIn || _handle <= 0)
                {
                    return false;
                }

                var streamInfo = new CHISDK.HI_S_STREAM_INFO_EXT
                {
                    u32Channel = CHISDK.HI_CHANNEL_1,
                    u32Stream = CHISDK.HI_STREAM_1, // Main stream (HD)
                    u32Mode = CHISDK.HI_STREAM_MODE_TCP,
                    u8Type = CHISDK.HI_STREAM_ALL
                };

                CHISDK.HI_SDK_SetDisplayCallback(_handle, 1);

                int ret = CHISDK.HI_SDK_RealPlayExt(_handle, windowHandle, ref streamInfo);
                bool success = ret == CHISDK.HI_SUCCESS;

                if (!success)
                {
                    AppLogger.Error($"[NST Camera {Config?.Ip}] Preview thất bại. Mã lỗi: {ret}", "NSTCamera");
                }
                else
                {
                    _isPreviewing = true;
                }

                return success;
            }
        }

        /// <summary>
        /// Chụp ảnh Snapshot trả về mảng byte JPEG siêu nhanh từ Native SDK (không tạo Bitmap)
        /// </summary>
        public Task<byte[]?> CaptureSnapshotAsync(CancellationToken cancellationToken = default)
        {
            return Task.Run<byte[]?>(() =>
            {
                lock (_lockObj)
                {
                    if (!IsLoggedIn || _handle <= 0) return null;

                    // Cách 1: Chụp trực tiếp vào buffer bộ nhớ
                    int imageSize = 0;
                    int ret = CHISDK.HI_SDK_SnapJpeg(_handle, _captureBuffer, CaptureBufferSize, out imageSize);

                    if (ret == CHISDK.HI_SUCCESS && imageSize > 0)
                    {
                        var bytes = new byte[imageSize];
                        Buffer.BlockCopy(_captureBuffer, 0, bytes, 0, imageSize);
                        return bytes;
                    }

                    // Cách 2: Fallback chụp qua file tạm nếu thiết bị không hỗ trợ SnapJpeg
                    string tempFile = Path.Combine(Path.GetTempPath(), $"nst_snap_{Guid.NewGuid():N}.jpg");
                    try
                    {
                        int retCapture = CHISDK.HI_SDK_CaptureJPEGPicture(_handle, tempFile);
                        if (retCapture == CHISDK.HI_SUCCESS && File.Exists(tempFile))
                        {
                            return File.ReadAllBytes(tempFile);
                        }
                    }
                    catch { }
                    finally
                    {
                        if (File.Exists(tempFile))
                        {
                            try { File.Delete(tempFile); } catch { }
                        }
                    }

                    AppLogger.Error($"[NST Camera {Config?.Ip}] Capture thất bại. Code={ret}", "NSTCamera");
                    return null;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Chụp ảnh Snapshot và lưu thẳng xuống file JPEG bất đồng bộ tốc độ cao
        /// </summary>
        public async Task<bool> CaptureToFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                string? dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var bytes = await CaptureSnapshotAsync(cancellationToken);
                if (bytes != null && bytes.Length > 0)
                {
                    using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, true);
                    await fs.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                    return true;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, $"[NST CaptureToFileAsync Error] {ex.Message}", "NSTCamera");
            }

            return false;
        }

        public void StopPreview()
        {
            lock (_lockObj)
            {
                if (_handle > 0 && _isPreviewing)
                {
                    try
                    {
                        CHISDK.HI_SDK_StopRealPlay(_handle);
                    }
                    catch { }
                    _isPreviewing = false;
                }
            }
        }

        public void Logout()
        {
            lock (_lockObj)
            {
                if (_handle > 0)
                {
                    try
                    {
                        CHISDK.HI_SDK_Logout(_handle);
                    }
                    catch { }
                    _handle = 0;
                    IsLoggedIn = false;
                }
            }
        }

        public void Dispose()
        {
            StopPreview();
            Logout();
            GC.SuppressFinalize(this);
        }

        public static void CleanupSdk()
        {
            lock (_sdkLock)
            {
                if (!_sdkInitialized) return;

                CHISDK.HI_SDK_Cleanup();
                _sdkInitialized = false;
            }
        }
    }
}
