using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Devices.Controller
{
    /// <summary>
    /// Giao diện chuẩn cho Access Controller Service (Quản lý kết nối SDK, đọc Realtime Log, sự kiện cảm biến Radar/Vòng từ)
    /// </summary>
    public interface IControllerService : IDeviceAdapter, IDisposable
    {
        /// <summary>
        /// Sự kiện khi cảm biến Radar / Vòng từ (Aux In) thay đổi trạng thái
        /// </summary>
        event EventHandler<AuxTriggerEventArgs>? OnAuxInputTriggered;

        /// <summary>
        /// Kết nối tới Controller qua IP, Port và mật khẩu
        /// </summary>
        Task<bool> ConnectAsync(
            string ipAddress,
            int port,
            string? password,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Phân tích chuỗi RTLog và phát sự kiện OnAuxInputTriggered
        /// </summary>
        void ParseAndDispatchLog(string rawLog);
    }
}
