using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhuXuanParkingSystem.Api.DTOs;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;

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
        public async Task<IActionResult> GetMetrics()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            // 1. Số xe đang trong bãi (Active)
            var activeCount = (int)await _sessionRepo.CountAsync(s => s.Status == ParkingSessionStatus.Active && !s.IsDeleted);

            // 2. Lấy toàn bộ sessions trong ngày hôm nay
            var todaySessions = await _sessionRepo.FindAsync(s => s.InTime >= today && s.InTime < tomorrow && !s.IsDeleted);

            int todayIn = todaySessions.Count;
            int todayOut = todaySessions.Count(s => s.Status == ParkingSessionStatus.Completed);
            int todayUnmatchedOut = (int)await _sessionRepo.CountAsync(s => s.Status == ParkingSessionStatus.UnmatchedOut && s.InTime >= today && s.InTime < tomorrow && !s.IsDeleted);

            // 3. Thống kê lưu lượng theo từng giờ
            var hourlyTraffic = new List<HourlyTrafficDto>();
            for (int h = 0; h < 24; h++)
            {
                int inCount = todaySessions.Count(s => s.InTime.HasValue && s.InTime.Value.Hour == h);
                int outCount = todaySessions.Count(s => s.OutTime.HasValue && s.OutTime.Value.Hour == h);

                hourlyTraffic.Add(new HourlyTrafficDto
                {
                    Hour = h,
                    InCount = inCount,
                    OutCount = outCount
                });
            }

            var metrics = new DashboardMetricsDto
            {
                ActiveVehiclesCount = activeCount,
                TodayInCount = todayIn,
                TodayOutCount = todayOut,
                TodayUnmatchedOutCount = todayUnmatchedOut,
                HourlyTraffic = hourlyTraffic
            };

            return Ok(ApiResponse<DashboardMetricsDto>.Ok(metrics, "Lấy dữ liệu thống kê dashboard thành công."));
        }
    }
}
