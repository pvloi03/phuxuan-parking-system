using PhuXuanParkingSystem.Models.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Camera
{
    /// <summary>
    /// Giao diện chuẩn cho Camera Service (Biển số / Toàn cảnh)
    /// </summary>
    public interface ICameraService : IDisposable
    {
        CameraConfig Config { get; set; }
        bool IsLoggedIn { get; }
        bool IsStreaming { get; }
        event EventHandler<DeviceStatus>? OnConnectionStateChanged;
        Task<bool> LoginAsync(CancellationToken cancellationToken = default);
        bool StartPreview(IntPtr windowHandle);
        Task<byte[]?> CaptureSnapshotAsync(CancellationToken cancellationToken = default);
        Task<bool> CaptureToFileAsync(string filePath, CancellationToken cancellationToken = default);
        void StopPreview();
        void Logout();
    }
}
