using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Services.Controller;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.DeviceHealth
{
    /// <summary>
    /// Adapter cho Controller ZKTeco C3-200 (Pull SDK)
    /// </summary>
    public class ZKTecoAdapter : IDeviceAdapter
    {
        private readonly ZKTecoDeviceAdapter _controllerAdapter;

        public ZKTecoAdapter(ZKTecoDeviceAdapter controllerAdapter)
        {
            _controllerAdapter = controllerAdapter ?? throw new System.ArgumentNullException(nameof(controllerAdapter));
        }

        /// <summary>
        /// Trạng thái kết nối SDK: _handle != IntPtr.Zero
        /// </summary>
        public bool IsConnected => _controllerAdapter.IsConnected;

        /// <summary>
        /// Thử kết nối/reconnect tới Controller ZKTeco
        /// </summary>
        public async Task<bool> ConnectAsync(Device device, CancellationToken cancellationToken = default)
        {
            return await _controllerAdapter.ConnectAsync(
                ipAddress: device.IpAddress,
                port: device.Port,
                password: device.Password,
                cancellationToken: cancellationToken);
        }

        public Task DisconnectAsync()
        {
            return _controllerAdapter.DisconnectAsync();
        }
    }
}
