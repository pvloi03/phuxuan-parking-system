using System.Collections.Generic;

namespace PhuXuanParkingSystem.Api.DTOs
{
    public class PagedResult<T>
    {
        public IReadOnlyList<T> Items { get; set; } = System.Array.Empty<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages => PageSize > 0 ? (int)System.Math.Ceiling((double)TotalCount / PageSize) : 0;
    }

    public class DashboardMetricsDto
    {
        public int ActiveVehiclesCount { get; set; }
        public int PeriodInCount { get; set; }
        public int PeriodOutCount { get; set; }
        public int PeriodUnmatchedOutCount { get; set; }
        public string PeriodLabel { get; set; } = "Hôm Nay";
        public string PeriodType { get; set; } = "today";
        public List<TrafficDataPointDto> TrafficChart { get; set; } = new();

        // Thuộc tính tương thích ngược
        public int TodayInCount
        {
            get => PeriodInCount;
            set => PeriodInCount = value;
        }
        public int TodayOutCount
        {
            get => PeriodOutCount;
            set => PeriodOutCount = value;
        }
        public int TodayUnmatchedOutCount
        {
            get => PeriodUnmatchedOutCount;
            set => PeriodUnmatchedOutCount = value;
        }
        public List<HourlyTrafficDto> HourlyTraffic { get; set; } = new();
    }

    public class TrafficDataPointDto
    {
        public string Label { get; set; } = string.Empty;
        public int InCount { get; set; }
        public int OutCount { get; set; }
    }

    public class HourlyTrafficDto
    {
        public int Hour { get; set; }
        public string HourLabel => $"{Hour:D2}:00";
        public int InCount { get; set; }
        public int OutCount { get; set; }
    }
}
