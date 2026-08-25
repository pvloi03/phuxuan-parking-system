using CHCNetSDK_Library;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HPParkingThaiThuy.Services.Camera
{
    /// <summary>
    /// Service kết nối và điều khiển Camera Toàn Cảnh (HikVision SDK)
    /// Đã tối ưu hóa hiệu năng, xử lý không khóa file và chụp ảnh tốc độ cao
    /// </summary>
    public class OverviewCameraService : IDisposable
    {
        private static readonly object _sdkLock = new();
        private static volatile bool _sdkInitialized = false;

        private const uint CaptureBufferSize = 3 * 1024 * 1024; // 3MB

        private readonly object _lockObj = new();
        private readonly byte[] _captureBuffer = new byte[CaptureBufferSize];

        private int _userId = -1;
        private int _realHandle = -1;

        public CameraConfig Config { get; set; } = new();

        public bool IsLoggedIn { get; private set; }

        public event Action<bool, string> OnStatusChanged = delegate { };

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
                        Debug.WriteLine("[Hikvision] Login thất bại: Config hoặc IP trống.");
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
                        OnStatusChanged?.Invoke(true, "Đã kết nối Camera toàn cảnh thành công.");
                        return true;
                    }

                    uint errCode = CHCNetSDK.NET_DVR_GetLastError();
                    Debug.WriteLine($"[Hikvision {Config.Ip}] Login thất bại. Error={errCode}");
                    OnStatusChanged?.Invoke(false, $"Kết nối Camera toàn cảnh thất bại. Mã lỗi: {errCode}");
                    return false;
                }
            }, cancellationToken);
        }

        private bool TryWriteAscii(string value, byte[] destination, int maxLen, string fieldName)
        {
            if (value.Length > maxLen - 1)
            {
                Debug.WriteLine(
                    $"[Hikvision {Config?.Ip}] {fieldName} vượt quá độ dài cho phép " +
                    $"({value.Length} > {maxLen - 1} ký tự).");
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
                    Debug.WriteLine($"[Hikvision {Config?.Ip}] Preview thất bại. Error={errorCode}");
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
                    if (!IsLoggedIn || _userId < 0) return null;

                    var jpegPara = new CHCNetSDK.NET_DVR_JPEGPARA
                    {
                        wPicQuality = 0, // Chất lượng cao nhất
                        wPicSize = 0xff  // Giữ nguyên độ phân giải
                    };

                    uint imageSizeRet = 0;
                    bool isSuccess = CHCNetSDK.NET_DVR_CaptureJPEGPicture_NEW(
                        _userId,
                        1,
                        ref jpegPara,
                        _captureBuffer,
                        CaptureBufferSize,
                        ref imageSizeRet);

                    if (isSuccess && imageSizeRet > 0)
                    {
                        var result = new byte[imageSizeRet];
                        Buffer.BlockCopy(_captureBuffer, 0, result, 0, (int)imageSizeRet);
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
                        catch { }
                        finally
                        {
                            if (File.Exists(tempFile))
                            {
                                try { File.Delete(tempFile); } catch { }
                            }
                        }
                    }

                    uint errorCode = CHCNetSDK.NET_DVR_GetLastError();
                    Debug.WriteLine($"[Hikvision {Config?.Ip}] Capture thất bại. Error={errorCode}");
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
                Debug.WriteLine($"[Hikvision CaptureToFileAsync Error] {ex.Message}");
            }

            return false;
        }

        public void StopPreview()
        {
            lock (_lockObj)
            {
                if (_realHandle >= 0)
                {
                    CHCNetSDK.NET_DVR_StopRealPlay(_realHandle);
                    _realHandle = -1;
                }
            }
        }

        public void Logout()
        {
            lock (_lockObj)
            {
                if (_userId >= 0)
                {
                    try
                    {
                        CHCNetSDK.NET_DVR_Logout(_userId);
                    }
                    catch { }
                    _userId = -1;
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

                CHCNetSDK.NET_DVR_Cleanup();
                _sdkInitialized = false;
            }
        }
    }
}