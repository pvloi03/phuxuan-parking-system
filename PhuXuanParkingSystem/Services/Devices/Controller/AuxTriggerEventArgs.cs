using System;

namespace PhuXuanParkingSystem.Services.Devices.Controller
{
    /// <summary>
    /// Event arguments khi cảm biến Radar / Vòng từ (Cổng Aux In trên Access Controller) thay đổi trạng thái
    /// </summary>
    public class AuxTriggerEventArgs(int auxPort, bool isActive, DateTime triggerTime, string? rawLog = null) : EventArgs
    {
        public int AuxPort { get; } = auxPort;
        public bool IsActive { get; } = isActive;
        public DateTime TriggerTime { get; } = triggerTime;
        public string? RawLog { get; } = rawLog;
        public string LaneName => AuxPort == 1 ? "LÀN VÀO" : AuxPort == 2 ? "LÀN RA" : $"LÀN {AuxPort}";
    }
}
