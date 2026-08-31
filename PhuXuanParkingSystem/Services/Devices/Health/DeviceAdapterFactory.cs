using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Services.Devices;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Devices.Health
{
    /// <summary>
    /// Factory cung cấp adapter tương ứng cho từng thiết bị phần cứng
    /// </summary>
    public interface IDeviceAdapterFactory
    {
        IDeviceAdapter GetAdapter(Device device);
    }

    /// <summary>
    /// Implementation của DeviceAdapterFactory quản lý cache và route adapter
    /// </summary>
    public class DeviceAdapterFactory : IDeviceAdapterFactory, IDisposable
    {
        private readonly ConcurrentDictionary<string, IDeviceAdapter> _adapters = new();
        private readonly ConcurrentDictionary<string, IDeviceAdapter> _ipAdapters = new(StringComparer.OrdinalIgnoreCase);

        public void RegisterAdapter(string deviceId, IDeviceAdapter adapter, string? ipAddress = null)
        {
            if (adapter == null) return;

            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                _adapters[deviceId] = adapter;
            }

            if (!string.IsNullOrWhiteSpace(ipAddress))
            {
                _ipAdapters[ipAddress!] = adapter;
            }
        }

        public IDeviceAdapter GetAdapter(Device device)
        {
            if (device == null) return NoOpDeviceAdapter.Instance;

            if (!string.IsNullOrEmpty(device.Id) && _adapters.TryGetValue(device.Id, out var adapter))
                return adapter;

            if (!string.IsNullOrEmpty(device.IpAddress) && _ipAdapters.TryGetValue(device.IpAddress, out var ipAdapter))
                return ipAdapter;

            return NoOpDeviceAdapter.Instance;
        }

        public void Dispose()
        {
            _adapters.Clear();
            _ipAdapters.Clear();
        }
    }

    /// <summary>
    /// Adapter mặc định khi không tìm thấy thiết bị thực (Null Object Pattern)
    /// </summary>
    internal sealed class NoOpDeviceAdapter : IDeviceAdapter
    {
        public static readonly NoOpDeviceAdapter Instance = new();

        public bool IsConnected => false;
        public bool IsStreaming => false;
        public event EventHandler<DeviceStatus>? OnConnectionStateChanged { add { } remove { } }

        public Task<bool> PingAsync(int timeoutMs = 2000, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> ConnectAsync(Device device, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task DisconnectAsync() => Task.CompletedTask;
        public Task<bool> RestartAsync(Device device, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }
}
