using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhuXuanParkingSystem.Api.DTOs;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IRepository<ParkingSession> _sessionRepo;

        public DashboardController(IRepository<ParkingSession> sessionRepo)
        {
            _sessionRepo = sessionRepo;
        }

        [HttpGet("metrics")]
        public async Task<IActionResult> GetMetrics(
            [FromQuery] string period = "today",
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            DateTime now = DateTime.Now;
            DateTime start;
            DateTime end;
            string periodLabel;
            string normalizedPeriod = (period ?? "today").ToLowerInvariant();

            switch (normalizedPeriod)
            {
                case "month":
                    start = new DateTime(now.Year, now.Month, 1);
                    end = start.AddMonths(1);
                    periodLabel = $"Tháng {now:MM/yyyy}";
                    break;

                case "year":
                    start = new DateTime(now.Year, 1, 1);
                    end = start.AddYears(1);
                    periodLabel = $"Năm {now:yyyy}";
                    break;

                case "custom":
                    start = fromDate.HasValue ? fromDate.Value.Date : DateTime.Today.AddDays(-7);
                    end = (toDate.HasValue ? toDate.Value.Date : DateTime.Today).AddDays(1);
                    if (end <= start) end = start.AddDays(1);
                    periodLabel = $"{start:dd/MM/yyyy} - {end.AddDays(-1):dd/MM/yyyy}";
                    break;

                case "today":
                default:
                    normalizedPeriod = "today";
                    start = DateTime.Today;
                    end = start.AddDays(1);
                    periodLabel = $"Hôm Nay ({start:dd/MM/yyyy})";
                    break;
            }

            // 1. Số xe đang trong bãi (Real-time active)
            var activeCount = (int)await _sessionRepo.CountAsync(s => s.Status == ParkingSessionStatus.Active && !s.IsDeleted);

            // 2. Lấy toàn bộ sessions có lượt vào hoặc lượt ra trong khoảng thời gian [start, end)
            var periodSessions = await _sessionRepo.FindAsync(s =>
                !s.IsDeleted &&
                ((s.InTime >= start && s.InTime < end) || (s.OutTime.HasValue && s.OutTime >= start && s.OutTime < end)));

            int periodIn = periodSessions.Count(s => s.InTime.HasValue && s.InTime >= start && s.InTime < end);
            int periodOut = periodSessions.Count(s => s.OutTime.HasValue && s.OutTime >= start && s.OutTime < end && s.Status == ParkingSessionStatus.Completed);
            int periodUnmatchedOut = periodSessions.Count(s => s.Status == ParkingSessionStatus.UnmatchedOut && s.InTime >= start && s.InTime < end);

            // 3. Tạo dữ liệu biểu đồ lưu lượng
            var trafficChart = new List<TrafficDataPointDto>();
            var hourlyLegacy = new List<HourlyTrafficDto>();

            if (normalizedPeriod == "today" || (normalizedPeriod == "custom" && (end - start).TotalDays <= 1.05))
            {
                // Thống kê theo 24 giờ
                for (int h = 0; h < 24; h++)
                {
                    int inCount = periodSessions.Count(s => s.InTime.HasValue && s.InTime >= start && s.InTime < end && s.InTime.Value.Hour == h);
                    int outCount = periodSessions.Count(s => s.OutTime.HasValue && s.OutTime >= start && s.OutTime < end && s.OutTime.Value.Hour == h);

                    var point = new TrafficDataPointDto
                    {
                        Label = $"{h:D2}:00",
                        InCount = inCount,
                        OutCount = outCount
                    };
                    trafficChart.Add(point);
                    hourlyLegacy.Add(new HourlyTrafficDto
                    {
                        Hour = h,
                        InCount = inCount,
                        OutCount = outCount
                    });
                }
            }
            else if (normalizedPeriod == "month" || (normalizedPeriod == "custom" && (end - start).TotalDays <= 31.05))
            {
                // Thống kê theo từng ngày
                for (var day = start; day < end; day = day.AddDays(1))
                {
                    var nextDay = day.AddDays(1);
                    int inCount = periodSessions.Count(s => s.InTime.HasValue && s.InTime >= day && s.InTime < nextDay);
                    int outCount = periodSessions.Count(s => s.OutTime.HasValue && s.OutTime >= day && s.OutTime < nextDay);

                    trafficChart.Add(new TrafficDataPointDto
                    {
                        Label = $"{day:dd/MM}",
                        InCount = inCount,
                        OutCount = outCount
                    });
                }
            }
            else if (normalizedPeriod == "year")
            {
                // Thống kê theo 12 tháng
                for (int m = 1; m <= 12; m++)
                {
                    var monthStart = new DateTime(start.Year, m, 1);
                    var monthEnd = monthStart.AddMonths(1);

                    int inCount = periodSessions.Count(s => s.InTime.HasValue && s.InTime >= monthStart && s.InTime < monthEnd);
                    int outCount = periodSessions.Count(s => s.OutTime.HasValue && s.OutTime >= monthStart && s.OutTime < monthEnd);

                    trafficChart.Add(new TrafficDataPointDto
                    {
                        Label = $"Tháng {m:D2}",
                        InCount = inCount,
                        OutCount = outCount
                    });
                }
            }
            else
            {
                // Custom khoảng thời gian dài > 31 ngày: Thống kê theo từng ngày
                for (var day = start; day < end; day = day.AddDays(1))
                {
                    var nextDay = day.AddDays(1);
                    int inCount = periodSessions.Count(s => s.InTime.HasValue && s.InTime >= day && s.InTime < nextDay);
                    int outCount = periodSessions.Count(s => s.OutTime.HasValue && s.OutTime >= day && s.OutTime < nextDay);

                    trafficChart.Add(new TrafficDataPointDto
                    {
                        Label = $"{day:dd/MM}",
                        InCount = inCount,
                        OutCount = outCount
                    });
                }
            }

            var metrics = new DashboardMetricsDto
            {
                ActiveVehiclesCount = activeCount,
                PeriodInCount = periodIn,
                PeriodOutCount = periodOut,
                PeriodUnmatchedOutCount = periodUnmatchedOut,
                PeriodLabel = periodLabel,
                PeriodType = normalizedPeriod,
                TrafficChart = trafficChart,
                HourlyTraffic = hourlyLegacy
            };

            return Ok(ApiResponse<DashboardMetricsDto>.Ok(metrics, "Lấy dữ liệu thống kê dashboard thành công."));
        }
    }
}
