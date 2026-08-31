using PhuXuanParkingSystem.Models.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Devices.Config
{
    /// <summary>
    /// Kết quả nạp cấu hình thiết bị từ MongoDB
    /// </summary>
    public class DeviceConfigResult
    {
        public bool Success { get; set; }
        public Device? InPlateCamera { get; set; }
        public Device? InOverviewCamera { get; set; }
        public Device? OutPlateCamera { get; set; }
        public Device? OutOverviewCamera { get; set; }
        public Device? Controller { get; set; }
        public string? ControllerIp { get; set; }
        public int ControllerPort { get; set; }
        public List<string> Warnings { get; set; } = new();
        public TimeSpan LoadTime { get; set; }
    }

    /// <summary>
    /// Thay đổi cấu hình được phát hiện
    /// </summary>
    public class ConfigChangeEventArgs : EventArgs
    {
        public DeviceConfigResult OldConfig { get; set; } = new();
        public DeviceConfigResult NewConfig { get; set; } = new();
        public List<string> ChangedDevices { get; set; } = new();
    }

    /// <summary>
    /// Giao diện dịch vụ quản lý cấu hình thiết bị - Nạp, Cache, Reload động
    /// </summary>
    public interface IDeviceConfigService
    {
        /// <summary>
        /// Sự kiện phát ra khi cấu hình thay đổi (Web Admin sửa IP, etc.)
        /// </summary>
        event EventHandler<ConfigChangeEventArgs>? OnConfigChanged;

        /// <summary>
        /// Lấy cấu hình hiện tại (từ cache)
        /// </summary>
        DeviceConfigResult CurrentConfig { get; }

        /// <summary>
        /// Nạp cấu hình từ MongoDB lần đầu hoặc reload
        /// </summary>
        Task<DeviceConfigResult> LoadConfigAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Reload cấu hình nếu có thay đổi (dùng ETag/Hash)
        /// </summary>
        Task<(bool hasChanged, DeviceConfigResult? newConfig)> CheckAndReloadIfChangedAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Bắt đầu giám sát thay đổi cấu hình định kỳ
        /// </summary>
        void StartMonitoring(TimeSpan interval);

        /// <summary>
        /// Dừng giám sát
        /// </summary>
        void StopMonitoring();
    }
}
