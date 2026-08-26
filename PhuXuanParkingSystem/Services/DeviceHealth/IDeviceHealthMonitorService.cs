using PhuXuanParkingSystem.Models.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.DeviceHealth
{
    /// <summary>
    /// Giao diện dịch vụ kiểm tra sức khỏe và đồng bộ trạng thái thiết bị Clean Architecture
    /// </summary>
    public interface IDeviceHealthMonitorService
    {
        /// <summary>
        /// Sự kiện phát ra khi một thiết bị hoàn thành kiểm tra kết nối
        /// </summary>
        event EventHandler<DevicePingResult>? OnDeviceChecked;

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
