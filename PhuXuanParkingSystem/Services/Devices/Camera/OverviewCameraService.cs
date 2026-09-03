using CHCNetSDK_Library;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Services.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Devices.Camera
{
    /// <summary>
    /// Service kết nối và điều khiển Camera Toàn Cảnh (HikVision SDK)
    /// </summary>
    public class OverviewCameraService : CameraServiceBase
    {
        private static readonly object _sdkLock = new();
        private static volatile bool _sdkInitialized = false;
        private const uint CaptureBufferSize = 3 * 1024 * 1024; // 3MB

        private int _userId = -1;
        private int _realHandle = -1;

        protected override string LogTag => "Hikvision";
        protected override string LogCategory => "Hikvision";

        public OverviewCameraService()
        {
            InitializeSdk();
        }

        public static void InitializeSdk()
        {
            if (_sdkInitialized) return;

            lock (_sdkLock)
            {
                if (_sdkInitialized) return;

                bool isInitSuccess = CHCNetSDK.NET_DVR_Init();
                if (!isInitSuccess)
                {
                    uint errorCode = CHCNetSDK.NET_DVR_GetLastError();
                    throw new InvalidOperationException($"Khởi tạo HCNetSDK thất bại. Mã lỗi: {errorCode}");
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
                    if (IsLoggedIn && _userId >= 0) return true;

                    if (Config == null || string.IsNullOrEmpty(Config.Ip))
                    {
                        AppLogger.Warning("[Hikvision] Login thất bại: Config hoặc IP trống.", "Hikvision");
                        return false;
                    }

                    var loginInfo = new CHCNetSDK.NET_DVR_USER_LOGIN_INFO
                    {
                        sDeviceAddress = new byte[CHCNetSDK.NET_DVR_DEV_ADDRESS_MAX_LEN],
                        sUserName = new byte[CHCNetSDK.NET_DVR_LOGIN_USERNAME_MAX_LEN],
                        sPassword = new byte[CHCNetSDK.NET_DVR_LOGIN_PASSWD_MAX_LEN],
                        wPort = Config.Port,
                        bUseAsynLogin = false
                    };

                    if (!TryWriteAscii(Config.Ip, loginInfo.sDeviceAddress, CHCNetSDK.NET_DVR_DEV_ADDRESS_MAX_LEN, "IP"))
                        return false;

                    if (!string.IsNullOrEmpty(Config.UserName) &&
                        !TryWriteAscii(Config.UserName, loginInfo.sUserName, CHCNetSDK.NET_DVR_LOGIN_USERNAME_MAX_LEN, "UserName"))
                        return false;

                    if (!string.IsNullOrEmpty(Config.Password) &&
                        !TryWriteAscii(Config.Password, loginInfo.sPassword, CHCNetSDK.NET_DVR_LOGIN_PASSWD_MAX_LEN, "Password"))
                        return false;

                    var deviceInfo = new CHCNetSDK.NET_DVR_DEVICEINFO_V40();
                    _userId = CHCNetSDK.NET_DVR_Login_V40(ref loginInfo, ref deviceInfo);
                    IsLoggedIn = _userId >= 0;

                    if (IsLoggedIn)
                    {
                        AppLogger.Information($"[Hikvision {Config.Ip}:{Config.Port}] Kết nối thành công.", "Hikvision");
                        RaiseConnectionStateChanged(DeviceStatus.Connected);
                        return true;
                    }

                    uint errCode = CHCNetSDK.NET_DVR_GetLastError();
                    AppLogger.Error($"[Hikvision {Config.Ip}] Login thất bại. Error={errCode}", "Hikvision");
                    RaiseConnectionStateChanged(DeviceStatus.Disconnected);
                    return false;
                }
            }, cancellationToken);
        }

        private bool TryWriteAscii(string value, byte[] destination, int maxLen, string fieldName)
        {
            if (value.Length > maxLen - 1)
            {
                string msg = $"[Hikvision {Config?.Ip}] Tham số '{fieldName}' vượt quá độ dài cho phép ({value.Length} > {maxLen - 1} ký tự).";
                AppLogger.Error(msg, "Hikvision");
                return false;
            }

            Encoding.ASCII.GetBytes(value, 0, value.Length, destination, 0);
            return true;
        }

        public override bool StartPreview(IntPtr windowHandle)
        {
            lock (_lockObj)
            {
                if (!IsLoggedIn || _userId < 0) return false;

                if (_realHandle >= 0)
                {
                    try { CHCNetSDK.NET_DVR_StopRealPlay(_realHandle); } catch { }
                    _realHandle = -1;
                    IsStreaming = false;
                }

                var previewInfo = new CHCNetSDK.NET_DVR_PREVIEWINFO
                {
                    hPlayWnd = windowHandle,
                    lChannel = 1,
                    dwStreamType = 0, // 0: Main stream HD
                    dwLinkMode = 0,   // TCP
                    bBlocked = true
                };

                _realHandle = CHCNetSDK.NET_DVR_RealPlay_V40(_userId, ref previewInfo, null!, IntPtr.Zero);
                bool success = _realHandle >= 0;

                if (!success)
                {
                    uint errorCode = CHCNetSDK.NET_DVR_GetLastError();
                    AppLogger.Error($"[Hikvision {Config?.Ip}] Preview thất bại. Error={errorCode}", "Hikvision");
                }
                else
                {
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
                AppLogger.Warning($"[Hikvision {Config?.Ip}] Timeout chờ capture semaphore (5s).", "Hikvision");
                return null;
            }

            try
            {
                return await Task.Run<byte[]?>(() =>
                {
                    lock (_lockObj)
                    {
                        if (!IsLoggedIn || _userId < 0)
                        {
                            AppLogger.Warning($"[Hikvision {Config?.Ip}] Bỏ qua capture: Camera chưa login hoặc UserId không hợp lệ.", "Hikvision");
                            return null;
                        }

                        byte[] localBuffer = new byte[CaptureBufferSize];
                        var jpegPara = new CHCNetSDK.NET_DVR_JPEGPARA
                        {
                            wPicQuality = 2, // 2: Mức chất lượng trung bình (Normal/Average - chuẩn SDK Hikvision tối ưu tốc độ & dung lượng)
                            wPicSize = 0xff  // 0xff: Kích thước phân giải tiêu chuẩn của channel
                        };

                        uint imageSizeRet = 0;
                        bool isSuccess = CHCNetSDK.NET_DVR_CaptureJPEGPicture_NEW(
                            _userId,
                            1,
                            ref jpegPara,
                            localBuffer,
                            CaptureBufferSize,
                            ref imageSizeRet);

                        if (isSuccess && imageSizeRet > 0)
                        {
                            var result = new byte[imageSizeRet];
                            Buffer.BlockCopy(localBuffer, 0, result, 0, (int)imageSizeRet);
                            return result;
                        }

                        // Fallback chụp RealPlay handle
                        if (_realHandle >= 0)
                        {
                            string tempFile = Path.Combine(Path.GetTempPath(), $"hik_snap_{Guid.NewGuid():N}.bmp");
                            try
                            {
                                if (CHCNetSDK.NET_DVR_CapturePicture(_realHandle, tempFile) && File.Exists(tempFile))
                                {
                                    return File.ReadAllBytes(tempFile);
                                }
                            }
                            catch (Exception ex)
                            {
                                AppLogger.Error(ex, $"[Hikvision {Config?.Ip}] Lỗi fallback chụp RealPlay handle: {ex.Message}", "Hikvision");
                            }
                            finally
                            {
                                if (File.Exists(tempFile))
                                {
                                    try { File.Delete(tempFile); } catch { }
                                }
                            }
                        }

                        uint errorCode = CHCNetSDK.NET_DVR_GetLastError();
                        AppLogger.Error($"[Hikvision {Config?.Ip}] Capture thất bại. ErrorCode={errorCode}", "Hikvision");
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
                if (_realHandle >= 0)
                {
                    try { CHCNetSDK.NET_DVR_StopRealPlay(_realHandle); } catch { }
                    _realHandle = -1;
                    IsStreaming = false;
                    RaiseConnectionStateChanged(IsLoggedIn ? DeviceStatus.Connected : DeviceStatus.Disconnected);
                }
            }
        }

        public override void Logout()
        {
            lock (_lockObj)
            {
                if (_userId >= 0)
                {
                    if (_realHandle >= 0)
                    {
                        try { CHCNetSDK.NET_DVR_StopRealPlay(_realHandle); } catch { }
                        _realHandle = -1;
                    }

                    try { CHCNetSDK.NET_DVR_Logout(_userId); } catch { }
                    _userId = -1;
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
                CHCNetSDK.NET_DVR_Cleanup();
                _sdkInitialized = false;
            }
        }
    }
}
