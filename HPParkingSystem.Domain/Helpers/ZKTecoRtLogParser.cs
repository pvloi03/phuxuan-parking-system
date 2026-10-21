using System;

namespace HPParkingSystem.Helpers
{
    public enum ZKTecoRtLogParseStatus
    {
        Success = 0,
        IgnoredMalformed = 1,
        IgnoredStatusPacket = 2,
        IgnoredInvalidPort = 3,
        IgnoredStaleEvent = 4
    }

    public class ZKTecoRtLogParseResult
    {
        public ZKTecoRtLogParseStatus Status { get; set; }
        public int AuxPort { get; set; }
        public bool IsActive { get; set; }
        public int EventType { get; set; }
        public DateTime TriggerTime { get; set; }
        public double AgeSeconds { get; set; }
        public string? RawLog { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Bộ phân tích và sàng lọc chuỗi Realtime Log (RTLog) từ bộ điều khiển ZKTeco C3
    /// </summary>
    public static class ZKTecoRtLogParser
    {
        public const double DefaultMaxAgeSeconds = 5.0;

        /// <summary>
        /// Phân tích cú pháp chuỗi RTLog và kiểm tra tính hợp lệ
        /// </summary>
        /// <param name="rawLog">Chuỗi log định dạng CSV (ví dụ: '2026-10-21 15:00:00,0,0,1,221,2,200')</param>
        /// <param name="referenceTime">Thời điểm tham chiếu (mặc định DateTime.Now)</param>
        /// <param name="maxAgeSeconds">Thời gian tối đa tính từ sự kiện xảy ra đến hiện tại (mặc định 5s)</param>
        public static ZKTecoRtLogParseResult Parse(string? rawLog, DateTime? referenceTime = null, double maxAgeSeconds = DefaultMaxAgeSeconds)
        {
            if (string.IsNullOrWhiteSpace(rawLog))
            {
                return new ZKTecoRtLogParseResult
                {
                    Status = ZKTecoRtLogParseStatus.IgnoredMalformed,
                    Reason = "Chuỗi log rỗng"
                };
            }

            var parts = rawLog!.Split(',');
            if (parts.Length < 5)
            {
                return new ZKTecoRtLogParseResult
                {
                    Status = ZKTecoRtLogParseStatus.IgnoredMalformed,
                    RawLog = rawLog,
                    Reason = "Log không đủ 5 trường"
                };
            }

            // Bit 4 là 255 -> Bản ghi trạng thái Door/Alarm status định kỳ, không phải realtime event
            var eventTypeStr = parts[4]?.Trim() ?? string.Empty;
            if (eventTypeStr == "255")
            {
                return new ZKTecoRtLogParseResult
                {
                    Status = ZKTecoRtLogParseStatus.IgnoredStatusPacket,
                    RawLog = rawLog,
                    Reason = "Gói tin trạng thái định kỳ Door/Alarm (Bit4=255)"
                };
            }

            // Cột DoorID / AuxID (index 3): 1 = Làn Vào / Cột 1, 2 = Làn Ra / Cột 2
            var portStr = parts[3]?.Trim() ?? string.Empty;
            if (!int.TryParse(portStr, out var portIndex) || (portIndex != 1 && portIndex != 2))
            {
                return new ZKTecoRtLogParseResult
                {
                    Status = ZKTecoRtLogParseStatus.IgnoredInvalidPort,
                    RawLog = rawLog,
                    Reason = $"Cổng không thuộc Làn 1 hoặc 2 (Port={portStr})"
                };
            }

            // Cột EventType (index 4):
            // 221 = CÓ XE / Đang kích hoạt cảm biến Radar / Vòng từ (Aux In Closed)
            // 220 = HẾT XE / Đã ngắt cảm biến (Aux In Opened / Restored)
            // 25  = Báo động Aux Input
            // 1   = Kích hoạt cảm biến
            _ = int.TryParse(eventTypeStr, out var eventType);
            bool isActive = eventType == 221 || eventType == 25 || eventType == 1;

            var now = referenceTime ?? DateTime.Now;
            var triggerTime = now;
            var dateStr = parts[0]?.Trim() ?? string.Empty;
            if (DateTime.TryParse(dateStr, out var parsedTime))
            {
                triggerTime = parsedTime;
            }

            var age = (now - triggerTime).TotalSeconds;
            // Nếu sự kiện trong quá khứ cũ hơn maxAgeSeconds (hoặc âm quá 60s do sai lệch đồng hồ lớn)
            if (age > maxAgeSeconds)
            {
                return new ZKTecoRtLogParseResult
                {
                    Status = ZKTecoRtLogParseStatus.IgnoredStaleEvent,
                    AuxPort = portIndex,
                    IsActive = isActive,
                    EventType = eventType,
                    TriggerTime = triggerTime,
                    AgeSeconds = age,
                    RawLog = rawLog,
                    Reason = $"Sự kiện tồn đọng cũ ({age:F0}s trước lúc {triggerTime:HH:mm:ss})"
                };
            }

            return new ZKTecoRtLogParseResult
            {
                Status = ZKTecoRtLogParseStatus.Success,
                AuxPort = portIndex,
                IsActive = isActive,
                EventType = eventType,
                TriggerTime = triggerTime,
                AgeSeconds = age,
                RawLog = rawLog
            };
        }
    }
}
