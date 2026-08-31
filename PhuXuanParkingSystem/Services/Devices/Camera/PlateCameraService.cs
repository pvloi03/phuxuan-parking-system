using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.SDK.NST;
using PhuXuanParkingSystem.Services.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Devices.Camera
{
    /// <summary>
    /// Service kết nối và điều khiển Camera Biển Số (NST SDK)
    /// </summary>
    public class PlateCameraService : CameraServiceBase
    {
        private static readonly object _sdkLock = new();
        private static volatile bool _sdkInitialized = false;
        private const int CaptureBufferSize = 3 * 1024 * 1024; // 3MB

        private int _handle = 0;
        private bool _isPreviewing = false;

        protected override string LogTag => "NST Camera";
        protected override string LogCategory => "NSTCamera";

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
                    throw new InvalidOperationException($"Khởi tạo HISDK (Camera biển số) thất bại. Mã lỗi: {ret}");
                }

                _sdkInitialized = true;
            }
        }

        public override async Task<bool> LoginAsync(CancellationToken cancellationToken = default)
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

                        AppLogger.Information($"[NST Camera {Config.Ip}:{Config.Port}] Kết nối thành công.", "NSTCamera");
                        RaiseConnectionStateChanged(DeviceStatus.Connected);
                        return true;
                    }

                    AppLogger.Error($"[NST Camera {Config.Ip}] Login thất bại. Handle={_handle}, Error={errCode}", "NSTCamera");
                    RaiseConnectionStateChanged(DeviceStatus.Disconnected);
                    return false;
                }
            }, cancellationToken);
        }

        public override bool StartPreview(IntPtr windowHandle)
        {
            lock (_lockObj)
            {
                if (!IsLoggedIn || _handle <= 0) return false;

                if (_isPreviewing)
                {
                    try { CHISDK.HI_SDK_StopRealPlay(_handle); } catch { }
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
                }
                else
                {
                    _isPreviewing = true;
                    IsStreaming = true;
                    RaiseConnectionStateChanged(DeviceStatus.Streaming);
                }

                return success;
            }
        }

        public override async Task<byte[]?> CaptureSnapshotAsync(CancellationToken cancellationToken = default)
        {
            if (!await _captureSemaphore.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
            {
                AppLogger.Warning($"[NST Camera {Config?.Ip}] Timeout chờ capture semaphore (5s).", "NSTCamera");
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
                            AppLogger.Warning($"[NST Camera {Config?.Ip}] Bỏ qua capture: Chưa kết nối hoặc Handle không hợp lệ.", "NSTCamera");
                            return null;
                        }

                        byte[] localBuffer = new byte[CaptureBufferSize];
                        int ret = CHISDK.HI_SDK_SnapJpeg(_handle, localBuffer, CaptureBufferSize, out int imageSize);

                        if (ret == CHISDK.HI_SUCCESS && imageSize > 0)
                        {
                            var bytes = new byte[imageSize];
                            Buffer.BlockCopy(localBuffer, 0, bytes, 0, imageSize);
                            return bytes;
                        }

                        // Fallback chụp qua file tạm
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
                            AppLogger.Error(ex, $"[NST Camera {Config?.Ip}] Lỗi fallback chụp file tạm: {ex.Message}", "NSTCamera");
                        }
                        finally
                        {
                            if (File.Exists(tempFile))
                            {
                                try { File.Delete(tempFile); } catch { }
                            }
                        }

                        AppLogger.Error($"[NST Camera {Config?.Ip}] Capture thất bại. Code={ret}, imageSize={imageSize}", "NSTCamera");
                        return null;
                    }
                }, cancellationToken);
            }
            finally
            {
                _captureSemaphore.Release();
            }
        }

        public override void StopPreview()
        {
            lock (_lockObj)
            {
                if (_handle > 0 && _isPreviewing)
                {
                    try { CHISDK.HI_SDK_StopRealPlay(_handle); } catch { }
                    _isPreviewing = false;
                    IsStreaming = false;
                    RaiseConnectionStateChanged(IsLoggedIn ? DeviceStatus.Connected : DeviceStatus.Disconnected);
                }
            }
        }

        public override void Logout()
        {
            lock (_lockObj)
            {
                if (_handle > 0)
                {
                    try { CHISDK.HI_SDK_StopRealPlay(_handle); } catch { }
                    try { CHISDK.HI_SDK_Logout(_handle); } catch { }

                    _handle = 0;
                    _isPreviewing = false;
                    IsLoggedIn = false;
                    IsStreaming = false;
                    RaiseConnectionStateChanged(DeviceStatus.Disconnected);
                }
            }
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
