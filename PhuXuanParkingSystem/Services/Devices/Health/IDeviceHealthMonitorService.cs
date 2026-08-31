using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Devices.Health
{
    /// <summary>
    /// Giao diện dịch vụ kiểm tra sức khỏe và đồng bộ trạng thái thiết bị
    /// </summary>
    public interface IDeviceHealthMonitorService : IDisposable
    {
        /// <summary>
        /// Sự kiện phát ra khi một thiết bị hoàn thành kiểm tra kết nối
        /// </summary>
        event EventHandler<DevicePingResult>? OnDeviceChecked;

        /// <summary>
        /// Sự kiện phát ra khi trạng thái thiết bị thay đổi (Online, Offline, Error,...)
        /// </summary>
        event EventHandler<DeviceStateChangedEventArgs>? OnStateChanged;

        /// <summary>
        /// Lấy trạng thái hiện tại của một thiết bị theo Id
        /// </summary>
        DeviceStatus GetState(string deviceId);

        /// <summary>
        /// Bắt đầu chu kỳ tự động kiểm tra sức khỏe định kỳ
        /// </summary>
        void StartHealthCheck(TimeSpan interval);

        /// <summary>
        /// Dừng chu kỳ kiểm tra sức khỏe
        /// </summary>
        void StopHealthCheck();

        /// <summary>
        /// Kiểm tra kết nối tới 1 thiết bị cụ thể (TCP Socket Connect + ICMP Ping)
        /// </summary>
        Task<DevicePingResult> PingDeviceAsync(Device device, int timeoutMs = 2000, CancellationToken cancellationToken = default);

        /// <summary>
        /// Kiểm tra toàn bộ danh sách thiết bị và tự động đồng bộ vào CSDL MongoDB
        /// </summary>
        Task<IReadOnlyList<DevicePingResult>> CheckAllAndSyncAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Đồng bộ trạng thái kết quả kiểm tra vào collection Devices trong MongoDB
        /// </summary>
        Task SyncStatusToDbAsync(DevicePingResult result, CancellationToken cancellationToken = default);
    }
}
