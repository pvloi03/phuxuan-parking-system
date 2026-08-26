using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PhuXuanParkingSystem.Api.DTOs;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using System.Drawing;

namespace PhuXuanParkingSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ParkingSessionsController : ControllerBase
    {
        private readonly IRepository<ParkingSession> _sessionRepo;

        public ParkingSessionsController(IRepository<ParkingSession> sessionRepo)
        {
            _sessionRepo = sessionRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] string? plateNumber,
            [FromQuery] ParkingSessionStatus? status,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate,
            [FromQuery] string? laneName,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 15)
        {
            var filter = MongoDB.Driver.Builders<ParkingSession>.Filter.Eq(s => s.IsDeleted, false);

            if (!string.IsNullOrWhiteSpace(plateNumber))
            {
                filter &= MongoDB.Driver.Builders<ParkingSession>.Filter.Regex(
                    s => s.PlateNumber,
                    new MongoDB.Bson.BsonRegularExpression(plateNumber.Trim(), "i"));
            }

            if (status.HasValue)
            {
                filter &= MongoDB.Driver.Builders<ParkingSession>.Filter.Eq(s => s.Status, status.Value);
            }

            if (fromDate.HasValue)
            {
                filter &= MongoDB.Driver.Builders<ParkingSession>.Filter.Gte(s => s.InTime, fromDate.Value);
            }

            if (toDate.HasValue)
            {
                filter &= MongoDB.Driver.Builders<ParkingSession>.Filter.Lte(s => s.InTime, toDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(laneName))
            {
                filter &= (MongoDB.Driver.Builders<ParkingSession>.Filter.Eq(s => s.InLaneName, laneName) |
                           MongoDB.Driver.Builders<ParkingSession>.Filter.Eq(s => s.OutLaneName, laneName));
            }

            var totalCount = (int)await _sessionRepo.CountAsync(filter);
            var skip = (pageNumber - 1) * pageSize;

            var sort = MongoDB.Driver.Builders<ParkingSession>.Sort.Descending(s => s.InTime);
            var items = await _sessionRepo.FindAsync(filter, sort, skip, pageSize);

            var result = new PagedResult<ParkingSession>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<ParkingSession>>.Ok(result, "Lấy danh sách phiên đỗ xe thành công."));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var session = await _sessionRepo.GetByIdAsync(id);
            if (session == null || session.IsDeleted)
            {
                return NotFound(ApiResponse.Fail("Không tìm thấy phiên đỗ xe."));
            }

            return Ok(ApiResponse<ParkingSession>.Ok(session, "Lấy chi tiết phiên đỗ xe thành công."));
        }

        [HttpGet("export-excel")]
        public async Task<IActionResult> ExportExcel(
            [FromQuery] string? plateNumber,
            [FromQuery] ParkingSessionStatus? status,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var filter = MongoDB.Driver.Builders<ParkingSession>.Filter.Eq(s => s.IsDeleted, false);

            if (!string.IsNullOrWhiteSpace(plateNumber))
            {
                filter &= MongoDB.Driver.Builders<ParkingSession>.Filter.Regex(s => s.PlateNumber, new MongoDB.Bson.BsonRegularExpression(plateNumber.Trim(), "i"));
            }

            if (status.HasValue)
            {
                filter &= MongoDB.Driver.Builders<ParkingSession>.Filter.Eq(s => s.Status, status.Value);
            }

            if (fromDate.HasValue)
            {
                filter &= MongoDB.Driver.Builders<ParkingSession>.Filter.Gte(s => s.InTime, fromDate.Value);
            }

            if (toDate.HasValue)
            {
                filter &= MongoDB.Driver.Builders<ParkingSession>.Filter.Lte(s => s.InTime, toDate.Value);
            }

            var sort = MongoDB.Driver.Builders<ParkingSession>.Sort.Descending(s => s.InTime);
            var sessions = await _sessionRepo.FindAsync(filter, sort, 0, 5000);

            using var package = new ExcelPackage();

            // =========================================================================
            // SHEET 1: BÁO CÁO TỔNG HỢP — ĐẾM SỐ LƯỢT VÀO RA THEO TỪNG XE
            // =========================================================================
            var wsSummary = package.Workbook.Worksheets.Add("Thống Kê Số Lượt Từng Xe");

            // Tiêu đề Sheet 1
            wsSummary.Cells["A1:J1"].Merge = true;
            wsSummary.Cells["A1"].Value = "BÁO CÁO THỐNG KÊ SỐ LƯỢT XE VÀO RA THEO TỪNG XE";
            wsSummary.Cells["A1"].Style.Font.Bold = true;
            wsSummary.Cells["A1"].Style.Font.Size = 15;
            wsSummary.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Thông tin thời gian lọc
            wsSummary.Cells["A2:J2"].Merge = true;
            string timeRangeStr = $"Khoảng thời gian: {(fromDate.HasValue ? fromDate.Value.ToString("dd/MM/yyyy") : "Toàn bộ")} - {(toDate.HasValue ? toDate.Value.ToString("dd/MM/yyyy") : "Hiện tại")}";
            wsSummary.Cells["A2"].Value = timeRangeStr;
            wsSummary.Cells["A2"].Style.Font.Italic = true;
            wsSummary.Cells["A2"].Style.Font.Size = 11;
            wsSummary.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Header Bảng Thống Kê
            string[] summaryHeaders = { "STT", "Biển Số Xe", "Chủ Xe", "Loại Xe", "Tổng Lượt Vào", "Tổng Lượt Ra", "Đang Trong Bãi", "Tổng Thời Gian Đỗ", "Lần Vào Mới Nhất", "Lần Ra Mới Nhất" };
            for (int i = 0; i < summaryHeaders.Length; i++)
            {
                wsSummary.Cells[4, i + 1].Value = summaryHeaders[i];
                wsSummary.Cells[4, i + 1].Style.Font.Bold = true;
                wsSummary.Cells[4, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                wsSummary.Cells[4, i + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(30, 64, 175)); // Blue 800
                wsSummary.Cells[4, i + 1].Style.Font.Color.SetColor(Color.White);
                wsSummary.Cells[4, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // Nhóm theo từng Biển Số Xe để đếm số lượt vào ra
            var vehicleGroups = sessions
                .GroupBy(s => s.PlateNumber.Trim().ToUpper())
                .OrderByDescending(g => g.Count())
                .ToList();

            int sumRow = 5;
            int sumStt = 1;
            foreach (var g in vehicleGroups)
            {
                var first = g.First();
                int inCount = g.Count(s => s.InTime.HasValue);
                int outCount = g.Count(s => s.OutTime.HasValue || s.Status == ParkingSessionStatus.Completed);
                bool isCurrentlyInParking = g.Any(s => s.Status == ParkingSessionStatus.Active);

                var totalDurationMinutes = g.Where(s => s.Duration.HasValue).Sum(s => s.Duration!.Value.TotalMinutes);
                int totalHours = (int)(totalDurationMinutes / 60);
                int totalMins = (int)(totalDurationMinutes % 60);
                string totalDurationStr = totalDurationMinutes > 0 ? $"{totalHours}h {totalMins}m" : "--";

                var latestIn = g.Where(s => s.InTime.HasValue).OrderByDescending(s => s.InTime).FirstOrDefault()?.InTime;
                var latestOut = g.Where(s => s.OutTime.HasValue).OrderByDescending(s => s.OutTime).FirstOrDefault()?.OutTime;

                wsSummary.Cells[sumRow, 1].Value = sumStt++;
                wsSummary.Cells[sumRow, 2].Value = g.Key;
                wsSummary.Cells[sumRow, 3].Value = first.PersonName ?? "Xe vãng lai";
                wsSummary.Cells[sumRow, 4].Value = first.VehicleType == VehicleType.Car ? "Ô tô" : "Xe máy";
                wsSummary.Cells[sumRow, 5].Value = inCount;
                wsSummary.Cells[sumRow, 6].Value = outCount;
                wsSummary.Cells[sumRow, 7].Value = isCurrentlyInParking ? "Có (Đang đỗ)" : "Không";
                wsSummary.Cells[sumRow, 8].Value = totalDurationStr;
                wsSummary.Cells[sumRow, 9].Value = latestIn.HasValue ? latestIn.Value.ToString("dd/MM/yyyy HH:mm:ss") : "--";
                wsSummary.Cells[sumRow, 10].Value = latestOut.HasValue ? latestOut.Value.ToString("dd/MM/yyyy HH:mm:ss") : "--";

                // Canh lề
                wsSummary.Cells[sumRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                wsSummary.Cells[sumRow, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                wsSummary.Cells[sumRow, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                wsSummary.Cells[sumRow, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                wsSummary.Cells[sumRow, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                wsSummary.Cells[sumRow, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                sumRow++;
            }
            wsSummary.Cells.AutoFitColumns();

            // =========================================================================
            // SHEET 2: LỊCH SỬ CHI TIẾT TỪNG LƯỢT XE
            // =========================================================================
            var ws = package.Workbook.Worksheets.Add("Lịch Sử Chi Tiết");

            // Header Title
            ws.Cells["A1:H1"].Merge = true;
            ws.Cells["A1"].Value = "BÁO CÁO CHI TIẾT CÁC PHIÊN XE RA VÀO";
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.Font.Size = 15;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Table Headers
            string[] headers = { "STT", "Biển Số Xe", "Chủ Xe", "Loại Xe", "Thời Gian Vào", "Thời Gian Ra", "Thời Gian Đỗ", "Trạng Thái" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cells[3, i + 1].Value = headers[i];
                ws.Cells[3, i + 1].Style.Font.Bold = true;
                ws.Cells[3, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[3, i + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(41, 128, 185));
                ws.Cells[3, i + 1].Style.Font.Color.SetColor(Color.White);
                ws.Cells[3, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // Data Rows
            int row = 4;
            int stt = 1;
            foreach (var s in sessions)
            {
                ws.Cells[row, 1].Value = stt++;
                ws.Cells[row, 2].Value = s.PlateNumber;
                ws.Cells[row, 3].Value = s.PersonName ?? "Xe vãng lai";
                ws.Cells[row, 4].Value = s.VehicleType == VehicleType.Car ? "Ô tô" : "Xe máy";
                ws.Cells[row, 5].Value = s.InTime.HasValue ? s.InTime.Value.ToString("dd/MM/yyyy HH:mm:ss") : "--";
                ws.Cells[row, 6].Value = s.OutTime.HasValue ? s.OutTime.Value.ToString("dd/MM/yyyy HH:mm:ss") : "--";
                ws.Cells[row, 7].Value = s.Duration.HasValue ? $"{s.Duration.Value.Hours}h {s.Duration.Value.Minutes}m" : "--";
                ws.Cells[row, 8].Value = s.Status == ParkingSessionStatus.Active ? "Đang trong bãi" :
                                         s.Status == ParkingSessionStatus.Completed ? "Đã hoàn thành" : "Xe ra không có vào";

                ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                row++;
            }

            ws.Cells.AutoFitColumns();

            var fileBytes = package.GetAsByteArray();
            string fileName = $"BaoCao_LichSuXe_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var success = await _sessionRepo.DeleteAsync(id, softDelete: true);
            if (!success)
            {
                return NotFound(ApiResponse.Fail("Không tìm thấy phiên đỗ xe để xóa."));
            }

            return Ok(ApiResponse.Ok("Đã xóa bản ghi phiên đỗ xe thành công."));
        }

        [HttpPost("delete-batch")]
        public async Task<IActionResult> DeleteBatch([FromBody] List<string> ids)
        {
            if (ids == null || ids.Count == 0)
            {
                return BadRequest(ApiResponse.Fail("Danh sách ID cần xóa không hợp lệ."));
            }

            int count = 0;
            foreach (var id in ids)
            {
                if (await _sessionRepo.DeleteAsync(id, softDelete: true))
                {
                    count++;
                }
            }

            return Ok(ApiResponse.Ok($"Đã xóa thành công {count} bản ghi phiên đỗ xe."));
        }
    }
}
