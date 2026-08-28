using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Services.Camera;
using PhuXuanParkingSystem.Services.Controller;
using PhuXuanParkingSystem.Services.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.DeviceHealth
{
    /// <summary>
    /// Factory để lấy adapter phù hợp với loại thiết bị
    /// Cache adapter instances để tái sử dụng
    /// </summary>
    public interface IDeviceAdapterFactory
    {
        /// <summary>
        /// Lấy adapter cho một thiết bị cụ thể
        /// Dựa trên Device.Type để route đúng adapter
        /// </summary>
        IDeviceAdapter GetAdapter(Device device);

        /// <summary>
        /// Lấy tất cả adapters đang active
        /// </summary>
        IReadOnlyCollection<IDeviceAdapter> GetAllAdapters();
    }

    /// <summary>
    /// Implementation của DeviceAdapterFactory
    /// Quản lý lifecycle của các adapter instances
    /// </summary>
    public class DeviceAdapterFactory : IDeviceAdapterFactory, IDisposable
    {
        // Cache adapters theo Device ID
        private readonly ConcurrentDictionary<string, IDeviceAdapter> _adapters = new();

        // References tới các camera services đang được FrmMain sử dụng
        private PlateCameraService? _inPlateCam;
        private PlateCameraService? _outPlateCam;
        private OverviewCameraService? _inOverviewCam;
        private OverviewCameraService? _outOverviewCam;
        private ZKTecoDeviceAdapter? _controllerAdapter;

        // Device IDs để match khi query từ MongoDB
        private string? _inPlateCamDeviceId;
        private string? _outPlateCamDeviceId;
        private string? _inOverviewCamDeviceId;
        private string? _outOverviewCamDeviceId;
        private string? _controllerDeviceId;

        /// <summary>
        /// Đăng ký cameras với factory
        /// </summary>
        public void RegisterCameras(
            PlateCameraService? inPlateCam,
            PlateCameraService? outPlateCam,
            OverviewCameraService? inOverviewCam,
            OverviewCameraService? outOverviewCam,
            string? inPlateCamDeviceId = null,
            string? outPlateCamDeviceId = null,
            string? inOverviewCamDeviceId = null,
            string? outOverviewCamDeviceId = null)
        {
            _inPlateCam = inPlateCam;
            _outPlateCam = outPlateCam;
            _inOverviewCam = inOverviewCam;
            _outOverviewCam = outOverviewCam;

            // Lưu device IDs nếu được cung cấp
            _inPlateCamDeviceId = inPlateCamDeviceId;
            _outPlateCamDeviceId = outPlateCamDeviceId;
            _inOverviewCamDeviceId = inOverviewCamDeviceId;
            _outOverviewCamDeviceId = outOverviewCamDeviceId;

            // Pre-create adapters với device IDs
            if (inPlateCam != null && !string.IsNullOrEmpty(inPlateCamDeviceId))
            {
                string? deviceId = inPlateCamDeviceId;
                _adapters.AddOrUpdate(deviceId!,
                    new PlateCameraAdapter(inPlateCam),
                    (_, _) => new PlateCameraAdapter(inPlateCam));
            }

            if (outPlateCam != null && !string.IsNullOrEmpty(outPlateCamDeviceId))
            {
                string? deviceId = outPlateCamDeviceId;
                _adapters.AddOrUpdate(deviceId!,
                    new PlateCameraAdapter(outPlateCam),
                    (_, _) => new PlateCameraAdapter(outPlateCam));
            }

            if (inOverviewCam != null && !string.IsNullOrEmpty(inOverviewCamDeviceId))
            {
                string? deviceId = inOverviewCamDeviceId;
                _adapters.AddOrUpdate(deviceId!,
                    new OverviewCameraAdapter(inOverviewCam),
                    (_, _) => new OverviewCameraAdapter(inOverviewCam));
            }

            if (outOverviewCam != null && !string.IsNullOrEmpty(outOverviewCamDeviceId))
            {
                string? deviceId = outOverviewCamDeviceId;
                _adapters.AddOrUpdate(deviceId!,
                    new OverviewCameraAdapter(outOverviewCam),
                    (_, _) => new OverviewCameraAdapter(outOverviewCam));
            }
        }

        /// <summary>
        /// Đăng ký controller với factory
        /// </summary>
        public void RegisterController(ZKTecoDeviceAdapter controllerAdapter, string? deviceId = null)
        {
            _controllerAdapter = controllerAdapter;
            _controllerDeviceId = deviceId ?? string.Empty;

            if (controllerAdapter != null && !string.IsNullOrEmpty(deviceId))
            {
                string? devId = deviceId;
                _ = _adapters.AddOrUpdate(devId!,
                    new ZKTecoAdapter(controllerAdapter),
                    (_, _) => new ZKTecoAdapter(controllerAdapter));
            }
        }

        /// <summary>
        /// Lấy adapter cho một thiết bị dựa trên Device entity
        /// Route theo Device.Type và Device.Id
        /// </summary>
        public IDeviceAdapter GetAdapter(Device device)
        {
            if (device == null)
                return new DefaultDeviceAdapter("unknown");

            // Ưu tiên check theo Device ID đã được cache
            if (_adapters.TryGetValue(device.Id, out var cachedAdapter))
                return cachedAdapter;

            // Fallback: Check theo Device.Type và IP match
            return CreateAdapterForDevice(device);
        }

        /// <summary>
        /// Tạo adapter mới dựa trên device properties
        /// </summary>
        private IDeviceAdapter CreateAdapterForDevice(Device device)
        {
            switch (device.Type)
            {
                case DeviceType.PlateCamera:
                    {
                        // Match theo IP hoặc tạo adapter mới
                        var plateAdapter = GetPlateCameraAdapterByIp(device.IpAddress);
                        if (plateAdapter != null) return plateAdapter;
                        return new DefaultDeviceAdapter(device.Id);
                    }

                case DeviceType.OverviewCamera:
                    {
                        var overviewAdapter = GetOverviewCameraAdapterByIp(device.IpAddress);
                        if (overviewAdapter != null) return overviewAdapter;
                        return new DefaultDeviceAdapter(device.Id);
                    }

                case DeviceType.Controller:
                    if (_controllerAdapter != null) return new ZKTecoAdapter(_controllerAdapter);
                    return new DefaultDeviceAdapter(device.Id);

                default:
                    AppLogger.Warning($"[AdapterFactory] Unknown device type: {device.Type}");
                    return new DefaultDeviceAdapter(device.Id);
            }
        }

        private PlateCameraAdapter? GetPlateCameraAdapterByIp(string ip)
        {
            if (_inPlateCam?.Config.Ip == ip)
                return new PlateCameraAdapter(_inPlateCam);
            if (_outPlateCam?.Config.Ip == ip)
                return new PlateCameraAdapter(_outPlateCam);
            return null;
        }

        private OverviewCameraAdapter? GetOverviewCameraAdapterByIp(string ip)
        {
            if (_inOverviewCam?.Config.Ip == ip)
                return new OverviewCameraAdapter(_inOverviewCam);
            if (_outOverviewCam?.Config.Ip == ip)
                return new OverviewCameraAdapter(_outOverviewCam);
            return null;
        }

        public IReadOnlyCollection<IDeviceAdapter> GetAllAdapters()
        {
            return _adapters.Values.ToList();
        }

        /// <summary>
        /// Pre-register adapters với device IDs và types
        /// Gọi từ FrmMain khi khởi tạo thiết bị
        /// </summary>
        public void RegisterDeviceAdapter(string deviceId, DeviceType deviceType, IDeviceAdapter adapter)
        {
            _adapters.AddOrUpdate(deviceId, adapter, (_, _) => adapter);
        }

        public void Dispose()
        {
            _adapters.Clear();
        }
    }

    /// <summary>
    /// Default adapter - không có thiết bị thực
    /// </summary>
    internal class DefaultDeviceAdapter : IDeviceAdapter
    {
        private readonly string _deviceId;

        public DefaultDeviceAdapter(string deviceId)
        {
            _deviceId = deviceId;
        }

        public bool IsConnected => false;
        public bool IsStreaming => false;

        public Task<bool> PingAsync(int timeoutMs = 2000, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<bool> ConnectAsync(Device device, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task DisconnectAsync()
        {
            return Task.CompletedTask;
        }

        public Task<bool> RestartAsync(Device device, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

#pragma warning disable CS0067
        public event EventHandler<DeviceConnectionState>? OnConnectionStateChanged;
#pragma warning restore CS0067
    }
}
