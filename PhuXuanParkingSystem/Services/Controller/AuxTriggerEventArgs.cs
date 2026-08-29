using System;

namespace PhuXuanParkingSystem.Services.Controller
{
    /// <summary>
    /// Event arguments khi cảm biến Radar (Cổng Aux In trên Access Controller) thay đổi trạng thái
    /// </summary>
    public class AuxTriggerEventArgs(int auxPort, bool isActive, DateTime triggerTime, string? rawLog = null) : EventArgs
    {
        /// <summary>
        /// Cổng Aux In nhận tín hiệu (1 = Radar Làn Vào, 2 = Radar Làn Ra)
        /// </summary>
        public int AuxPort { get; } = auxPort;

        /// <summary>
        /// Trạng thái kích hoạt: true = Có xe / Đang kích hoạt (221), false = Hết xe / Đã ngắt (220)
        /// </summary>
        public bool IsActive { get; } = isActive;

        /// <summary>
        /// Thời điểm nhận tín hiệu
        /// </summary>
        public DateTime TriggerTime { get; } = triggerTime;

        /// <summary>
        /// Chuỗi log gốc từ thiết bị
        /// </summary>
        public string? RawLog { get; } = rawLog;

        public string LaneName => AuxPort == 1 ? "LÀN VÀO" : AuxPort == 2 ? "LÀN RA" : $"LÀN {AuxPort}";
    }
}
