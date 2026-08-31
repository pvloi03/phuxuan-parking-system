using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.SDK.NST;
using PhuXuanParkingSystem.Services.Logging;
using PhuXuanParkingSystem.Services.Notification;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Devices.Camera
{
    /// <summary>
    /// Service kết nối và điều khiển Camera Biển Số (NST SDK)
    /// Đã tối ưu hóa hiệu năng, xử lý không khóa file và chụp ảnh tốc độ cao
    /// </summary>
    public class PlateCameraService : ICameraService
    {
        private static readonly object _sdkLock = new();
        private static volatile bool _sdkInitialized = false;

        private const int CaptureBufferSize = 3 * 1024 * 1024; // 3MB

        private readonly object _lockObj = new();
        private readonly SemaphoreSlim _captureSemaphore = new(1, 1);

        private int _handle = 0;
        private bool _isPreviewing = false;

        public CameraConfig Config { get; set; } = new();

        public bool IsLoggedIn { get; private set; }

        /// <summary>
        /// TRUE = đang streaming video
        /// </summary>
        public bool IsStreaming { get; private set; }

        /// <summary>
        /// Event khi trạng thái kết nối thay đổi
        /// Dùng cho DeviceHealthManager sync với UI
        /// </summary>
        public event EventHandler<DeviceStatus>? OnConnectionStateChanged;


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

                    _handle = CHISDK.HI_SDK_LoginExt(
                        Config.Ip,
                        Config.UserName ?? "",
                        Config.Password ?? "",
                        Config.Port,
                        5000,
                        out int errCode);

                    IsLoggedIn = _handle > 0;

                    if (IsLoggedIn)
                    {
                        CHISDK.HI_SDK_SetConnectTime(_handle, 3000);
                        CHISDK.HI_SDK_SetReconnect(_handle, 5000);

                        AppNotificationService.NotifySuccess(NotificationCategory.Camera, "Camera Biển Số", $"Đã kết nối Camera Biển Số ({Config.Ip}:{Config.Port}) thành công.", Config.Ip);
                        OnConnectionStateChanged?.Invoke(this, DeviceStatus.Connected);
                        return true;
                    }

                    AppLogger.Error($"[NST Camera {Config.Ip}] Login thất bại. Handle={_handle}, Error={errCode}", "NSTCamera");
                    AppNotificationService.NotifyError(NotificationCategory.Camera, "Camera Biển Số", $"Kết nối Camera Biển Số ({Config.Ip}) thất bại. Mã lỗi: {errCode}", Config.Ip);
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

                if (_isPreviewing)
                {
                    try
                    {
                        CHISDK.HI_SDK_StopRealPlay(_handle);
                    }
                    catch { }
                    _isPreviewing = false;
                    IsStreaming = false;
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
                    AppNotificationService.NotifyError(NotificationCategory.Camera, "Camera Biển Số", $"Mở luồng Preview Camera Biển Số ({Config?.Ip}) thất bại. Mã lỗi: {ret}", Config?.Ip);
                }
                else
                {
                    _isPreviewing = true;
                    IsStreaming = true;
                    OnConnectionStateChanged?.Invoke(this, DeviceStatus.Streaming);
                }

                return success;
            }
        }

        /// <summary>
        /// Chụp ảnh Snapshot trả về mảng byte JPEG siêu nhanh từ Native SDK (không tạo Bitmap)
        /// Sử dụng SemaphoreSlim để serialize các yêu cầu chụp và ngăn ngừa xung đột SDK handle
        /// </summary>
        public async Task<byte[]?> CaptureSnapshotAsync(CancellationToken cancellationToken = default)
        {
            // Chờ semaphore với timeout 5 giây để tránh deadlock
            if (!await _captureSemaphore.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
            {
                AppLogger.Warning($"[DEBUG-nst] [NST Camera {Config?.Ip}] Timeout chờ capture semaphore (5s).", "NSTCamera");
                return null;
            }

            try
            {
                return await Task.Run<byte[]?>(() =>
                {
                    lock (_lockObj)
                    {
                        if (!IsLoggedIn || _handle <= 0)
                        {
                            AppLogger.Warning($"[DEBUG-nst] [NST Camera {Config?.Ip}] Bỏ qua capture: Camera chưa kết nối hoặc Handle không hợp lệ (IsLoggedIn={IsLoggedIn}, Handle={_handle}).", "NSTCamera");
                            return null;
                        }

                        // Local buffer riêng cho mỗi lần gọi chụp, tránh race condition / buffer corruption
                        byte[] localBuffer = new byte[CaptureBufferSize];

                        // Cách 1: Chụp trực tiếp vào buffer bộ nhớ
                        int ret = CHISDK.HI_SDK_SnapJpeg(_handle, localBuffer, CaptureBufferSize, out int imageSize);

                        if (ret == CHISDK.HI_SUCCESS && imageSize > 0)
                        {
                            var bytes = new byte[imageSize];
                            Buffer.BlockCopy(localBuffer, 0, bytes, 0, imageSize);
                            return bytes;
                        }

                        // Cách 2: Fallback chụp qua file tạm nếu thiết bị không hỗ trợ SnapJpeg
                        string tempFile = Path.Combine(Path.GetTempPath(), $"nst_snap_{Guid.NewGuid():N}.jpg");
                        try
                        {
                            int retCapture = CHISDK.HI_SDK_CaptureJPEGPicture(_handle, tempFile);
                            if ((retCapture != CHISDK.HI_SUCCESS || !File.Exists(tempFile)) && _handle > 0)
                            {
                                retCapture = CHISDK.HI_SDK_CapturePicture(_handle, tempFile);
                            }

                            if (File.Exists(tempFile))
                            {
                                return File.ReadAllBytes(tempFile);
                            }
                        }
                        catch (Exception ex)
                        {
                            AppLogger.Error(ex, $"[DEBUG-nst] [NST Camera {Config?.Ip}] Lỗi fallback chụp file tạm: {ex.Message}", "NSTCamera");
                        }
                        finally
                        {
                            if (File.Exists(tempFile))
                            {
                                try { File.Delete(tempFile); } catch { }
                            }
                        }

                        AppLogger.Error($"[DEBUG-nst] [NST Camera {Config?.Ip}] Capture thất bại. HI_SDK_SnapJpeg Code={ret}, imageSize={imageSize}", "NSTCamera");
                        return null;
                    }
                }, cancellationToken);
            }
            finally
            {
                _captureSemaphore.Release();
            }
        }

        /// <summary>
        /// Chụp ảnh Snapshot và lưu thẳng xuống file JPEG bất đồng bộ tốc độ cao
        /// </summary>
        public async Task<bool> CaptureToFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            var bytes = await CaptureSnapshotAsync(cancellationToken);
            return await CameraCaptureHelper.SaveBytesToFileAsync(bytes, filePath, "DEBUG-nst", "NSTCamera", cancellationToken);
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
                    IsStreaming = false;
                    OnConnectionStateChanged?.Invoke(this, IsLoggedIn ? DeviceStatus.Connected : DeviceStatus.Disconnected);
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
                        CHISDK.HI_SDK_StopRealPlay(_handle);
                    }
                    catch { }

                    try
                    {
                        CHISDK.HI_SDK_Logout(_handle);
                    }
                    catch { }
                    _handle = 0;
                    _isPreviewing = false;
                    IsLoggedIn = false;
                    IsStreaming = false;
                    OnConnectionStateChanged?.Invoke(this, DeviceStatus.Disconnected);
                }
            }
        }

        public void Dispose()
        {
            StopPreview();
            Logout();
            _captureSemaphore.Dispose();
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
