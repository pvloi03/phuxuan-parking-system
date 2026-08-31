using CHCNetSDK_Library;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Services.Logging;
using PhuXuanParkingSystem.Services.Notification;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Devices.Camera
{
    /// <summary>
    /// Service kết nối và điều khiển Camera Toàn Cảnh (HikVision SDK)
    /// Đã tối ưu hóa hiệu năng, xử lý không khóa file và chụp ảnh tốc độ cao
    /// </summary>
    public class OverviewCameraService : ICameraService
    {
        private static readonly object _sdkLock = new();
        private static volatile bool _sdkInitialized = false;

        private const uint CaptureBufferSize = 3 * 1024 * 1024; // 3MB

        private readonly object _lockObj = new();
        private readonly SemaphoreSlim _captureSemaphore = new(1, 1);

        private int _userId = -1;
        private int _realHandle = -1;

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
                    throw new InvalidOperationException(
                        $"Khởi tạo HCNetSDK thất bại. Mã lỗi: {errorCode}");
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
                        AppNotificationService.NotifySuccess(NotificationCategory.Camera, "Camera Toàn Cảnh", $"Đã kết nối Camera Toàn Cảnh ({Config.Ip}:{Config.Port}) thành công.", Config.Ip);
                        OnConnectionStateChanged?.Invoke(this, DeviceStatus.Connected);
                        return true;
                    }

                    uint errCode = CHCNetSDK.NET_DVR_GetLastError();
                    AppLogger.Error($"[Hikvision {Config.Ip}] Login thất bại. Error={errCode}", "Hikvision");
                    AppNotificationService.NotifyError(NotificationCategory.Camera, "Camera Toàn Cảnh", $"Kết nối Camera Toàn Cảnh ({Config.Ip}) thất bại. Mã lỗi: {errCode}", Config.Ip);
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
                AppNotificationService.NotifyError(NotificationCategory.Camera, "Cấu hình Camera Toàn Cảnh", msg, Config?.Ip);
                return false;
            }

            Encoding.ASCII.GetBytes(value, 0, value.Length, destination, 0);
            return true;
        }

        public bool StartPreview(IntPtr windowHandle)
        {
            lock (_lockObj)
            {
                if (!IsLoggedIn || _userId < 0)
                {
                    return false;
                }

                if (_realHandle >= 0)
                {
                    try
                    {
                        CHCNetSDK.NET_DVR_StopRealPlay(_realHandle);
                    }
                    catch { }
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
                    AppNotificationService.NotifyError(NotificationCategory.Camera, "Camera Toàn Cảnh", $"Mở luồng Preview Camera Toàn Cảnh ({Config?.Ip}) thất bại. Mã lỗi: {errorCode}", Config?.Ip);
                }
                else
                {
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
                AppLogger.Warning($"[DEBUG-hik] [Hikvision {Config?.Ip}] Timeout chờ capture semaphore (5s).", "Hikvision");
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
                            AppLogger.Warning($"[DEBUG-hik] [Hikvision {Config?.Ip}] Bỏ qua capture: Camera chưa login hoặc UserId không hợp lệ (IsLoggedIn={IsLoggedIn}, UserId={_userId}).", "Hikvision");
                            return null;
                        }

                        // Local buffer riêng cho mỗi lần gọi chụp, tránh race condition / buffer corruption
                        byte[] localBuffer = new byte[CaptureBufferSize];

                        var jpegPara = new CHCNetSDK.NET_DVR_JPEGPARA
                        {
                            wPicQuality = 2, // Chất lượng cao nhất
                            wPicSize = 0xff  // Giữ nguyên độ phân giải
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

                        // Fallback: Chụp trực tiếp từ RealPlay handle nếu cần
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
                                AppLogger.Error(ex, $"[DEBUG-hik] [Hikvision {Config?.Ip}] Lỗi fallback chụp RealPlay handle: {ex.Message}", "Hikvision");
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
                        AppLogger.Error($"[DEBUG-hik] [Hikvision {Config?.Ip}] Capture thất bại. ErrorCode={errorCode}, isSuccess={isSuccess}, imageSizeRet={imageSizeRet}", "Hikvision");
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
            return await CameraCaptureHelper.SaveBytesToFileAsync(bytes, filePath, "DEBUG-hik", "Hikvision", cancellationToken);
        }

        public void StopPreview()
        {
            lock (_lockObj)
            {
                if (_realHandle >= 0)
                {
                    CHCNetSDK.NET_DVR_StopRealPlay(_realHandle);
                    _realHandle = -1;
                    IsStreaming = false;
                    OnConnectionStateChanged?.Invoke(this, IsLoggedIn ? DeviceStatus.Connected : DeviceStatus.Disconnected);
                }
            }
        }

        public void Logout()
        {
            lock (_lockObj)
            {
                if (_userId >= 0)
                {
                    // Stop preview first if running
                    if (_realHandle >= 0)
                    {
                        CHCNetSDK.NET_DVR_StopRealPlay(_realHandle);
                        _realHandle = -1;
                    }

                    try
                    {
                        CHCNetSDK.NET_DVR_Logout(_userId);
                    }
                    catch { }
                    _userId = -1;
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

                CHCNetSDK.NET_DVR_Cleanup();
                _sdkInitialized = false;
            }
        }
    }
}
