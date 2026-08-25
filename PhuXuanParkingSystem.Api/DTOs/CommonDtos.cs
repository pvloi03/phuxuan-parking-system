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
        public int TodayInCount { get; set; }
        public int TodayOutCount { get; set; }
        public int TodayUnmatchedOutCount { get; set; }
        public List<HourlyTrafficDto> HourlyTraffic { get; set; } = new();
    }

    public class HourlyTrafficDto
    {
        public int Hour { get; set; }
        public string HourLabel => $"{Hour:D2}:00";
        public int InCount { get; set; }
        public int OutCount { get; set; }
    }
}
