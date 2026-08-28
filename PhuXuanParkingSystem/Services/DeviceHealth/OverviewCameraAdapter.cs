using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Services.Camera;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.DeviceHealth
{
    /// <summary>
    /// Adapter cho Camera Toàn Cảnh (Hikvision SDK)
    /// </summary>
    public class OverviewCameraAdapter : IDeviceAdapter
    {
        private readonly OverviewCameraService _cameraService;

        public OverviewCameraAdapter(OverviewCameraService cameraService)
        {
            _cameraService = cameraService ?? throw new System.ArgumentNullException(nameof(cameraService));
        }

        /// <summary>
        /// Trạng thái kết nối SDK: IsLoggedIn && _userId >= 0
        /// </summary>
        public bool IsConnected => _cameraService.IsLoggedIn;

        /// <summary>
        /// Thử kết nối/reconnect tới Camera Toàn Cảnh
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
    }
}
