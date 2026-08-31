using PhuXuanParkingSystem.Services.Devices;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Devices.Camera
{
    /// <summary>
    /// Giao diện chuẩn cho Camera Service (Biển số / Toàn cảnh), tích hợp chuẩn IDeviceAdapter
    /// </summary>
    public interface ICameraService : IDeviceAdapter, IDisposable
    {
        CameraConfig Config { get; set; }
        bool IsLoggedIn { get; }
        Task<bool> LoginAsync(CancellationToken cancellationToken = default);
        bool StartPreview(IntPtr windowHandle);
        Task<byte[]?> CaptureSnapshotAsync(CancellationToken cancellationToken = default);
        Task<bool> CaptureToFileAsync(string filePath, CancellationToken cancellationToken = default);
        void StopPreview();
        void Logout();
    }
}
