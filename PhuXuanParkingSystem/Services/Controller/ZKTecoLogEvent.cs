using System;
using System.Globalization;

namespace PhuXuanParkingSystem.Services.Controller
{
    public class ZKTecoLogEvent
    {
        public DateTime Time { get; set; } = DateTime.Now;

        public string Pin { get; set; } = "";

        public string CardNo { get; set; } = "";

        public int DoorOrAuxId { get; set; } = 1;

        public int EventType { get; set; }

        public int InOutState { get; set; }

        public int VerifyMode { get; set; }

        public string RawLog { get; set; } = "";

        /// <summary>
        /// Xác định xem sự kiện có phải từ Cảm biến Radar / Đầu vào phụ (AUX Input) hay không
        /// </summary>
        public bool IsRadarAuxEvent => EventType == 220 || EventType == 221 || EventType == 25 || EventType == 1 || EventType == 4;

        /// <summary>
        /// Phát hiện xe vào vùng quét radar (221 = Aux In Shorted / Có xe)
        /// </summary>
        public bool IsVehicleDetected => EventType == 221 || EventType == 25 || EventType == 1;

        public string EventDescription => GetEventDescription(EventType);

        public string InOutDescription => InOutState switch
        {
            0 => "Vào (In)",
            1 => "Ra (Out)",
            _ => $"Khác ({InOutState})"
        };

        public static ZKTecoLogEvent? Parse(string rawCsv)
        {
            if (string.IsNullOrWhiteSpace(rawCsv)) return null;

            var parts = rawCsv.Split(',');
            if (parts.Length < 7) return null;

            var evt = new ZKTecoLogEvent
            {
                RawLog = rawCsv.Trim()
            };

            // parts[0]: Time - e.g. "2026-08-24 19:55:00"
            if (DateTime.TryParse(parts[0].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                evt.Time = dt;
            }
            else
            {
                evt.Time = DateTime.Now;
            }

            evt.Pin = parts[1].Trim();
            evt.CardNo = parts[2].Trim();

            if (int.TryParse(parts[3].Trim(), out int id))
                evt.DoorOrAuxId = id;

            if (int.TryParse(parts[4].Trim(), out int eventType))
                evt.EventType = eventType;

            if (int.TryParse(parts[5].Trim(), out int inOutState))
                evt.InOutState = inOutState;

            if (int.TryParse(parts[6].Trim(), out int verifyMode))
                evt.VerifyMode = verifyMode;

            return evt;
        }

        private static string GetEventDescription(int eventType) => eventType switch
        {
            221 => "📡 Cảm biến Radar AUX kích hoạt (Có xe trong vùng quét)",
            220 => "📡 Cảm biến Radar AUX ngắt (Xe đã ra khỏi vùng quét)",
            25 => "📡 Tín hiệu báo động AUX Input",
            1 => "📡 Kích hoạt cảm biến ngõ vào",
            4 => "Trạng thái cảm biến cửa",
            _ => $"Sự kiện #{eventType}"
        };
    }
}
