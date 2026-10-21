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
        /// Mã hóa DateTime theo công thức chuẩn ZKTeco Pull SDK (Packed integer tính từ năm 2000)
        /// val = (((Year - 2000) * 12 * 31 + (Month - 1) * 31 + (Day - 1)) * 86400) + (Hour * 3600) + (Minute * 60) + Second
        /// </summary>
        public static long EncodeDateTime(DateTime dt)
        {
            if (dt.Year < 2000) return 0;
            long days = ((dt.Year - 2000) * 12 * 31) + ((dt.Month - 1) * 31) + (dt.Day - 1);
            return (days * 86400L) + (dt.Hour * 3600L) + (dt.Minute * 60L) + dt.Second;
        }

        /// <summary>
        /// Phân tích cú pháp chuỗi RTLog và kiểm tra tính hợp lệ.
        /// Luôn luôn gán thời gian kích hoạt sự kiện bằng giờ máy tính PC (DateTime.Now).
        /// </summary>
        /// <param name="rawLog">Chuỗi log định dạng CSV (ví dụ: '2000-01-01 00:34:16,0,0,1,221,2,200')</param>
        /// <param name="referenceTime">Thời điểm tham chiếu (mặc định DateTime.Now)</param>
        /// <param name="maxAgeSeconds">Tham số tương thích ngược</param>
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

            // Luôn luôn sử dụng giờ máy tính PC làm mốc thời gian kích hoạt sự kiện
            var triggerTime = referenceTime ?? DateTime.Now;

            return new ZKTecoRtLogParseResult
            {
                Status = ZKTecoRtLogParseStatus.Success,
                AuxPort = portIndex,
                IsActive = isActive,
                EventType = eventType,
                TriggerTime = triggerTime,
                AgeSeconds = 0,
                RawLog = rawLog
            };
        }
    }
}
