using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Services.Camera;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.DeviceHealth
{
    /// <summary>
    /// Adapter cho Camera Biển Số (NST SDK)
    /// </summary>
    public class PlateCameraAdapter : IDeviceAdapter
    {
        private readonly PlateCameraService _cameraService;

        public PlateCameraAdapter(PlateCameraService cameraService)
        {
            _cameraService = cameraService ?? throw new System.ArgumentNullException(nameof(cameraService));
        }

        /// <summary>
        /// Trạng thái kết nối SDK: IsLoggedIn && _handle > 0
        /// </summary>
        public bool IsConnected => _cameraService.IsLoggedIn;

        /// <summary>
        /// Thử kết nối/reconnect tới Camera Biển Số
        /// </summary>
        public async Task<bool> ConnectAsync(Device device, CancellationToken cancellationToken = default)
        {
            // Cập nhật config từ device entity
            if (_cameraService.Config != null)
            {
                _cameraService.Config.Ip = device.IpAddress;
                _cameraService.Config.Port = (ushort)device.Port;
                _cameraService.Config.UserName = device.UserName ?? string.Empty;
                _cameraService.Config.Password = device.Password ?? string.Empty;
            }

            return await _cameraService.LoginAsync(cancellationToken);
        }

        public Task DisconnectAsync()
        {
            _cameraService.Logout();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Khởi động lại: Logout → TaskDelay → Login
        /// </summary>
        public async Task<bool> RestartAsync(Device device, CancellationToken cancellationToken = default)
        {
            _cameraService.Logout();
            await Task.Delay(500, cancellationToken);

            if (_cameraService.Config != null)
            {
                _cameraService.Config.Ip = device.IpAddress;
                _cameraService.Config.Port = (ushort)device.Port;
                _cameraService.Config.UserName = device.UserName ?? string.Empty;
                _cameraService.Config.Password = device.Password ?? string.Empty;
            }

            return await _cameraService.LoginAsync(cancellationToken);
        }
    }
}
