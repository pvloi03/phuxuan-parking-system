using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using HPParkingSystem.Api.DTOs;
using HPParkingSystem.Models.Entities;
using HPParkingSystem.Models.Enums;
using HPParkingSystem.Repositories;
using System.Drawing;

namespace HPParkingSystem.Api.Controllers
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
            [FromQuery] string? plateNumber = null,
            [FromQuery] string? keyword = null,
            [FromQuery] ParkingSessionStatus? status = null,
            [FromQuery] VehicleType? vehicleType = null,
            [FromQuery] PersonType? personType = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int? hour = null,
            [FromQuery] string? laneName = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 15)
        {
            var searchTerm = !string.IsNullOrWhiteSpace(keyword) ? keyword : plateNumber;
            var filter = BuildSessionFilter(
                searchTerm: searchTerm,
                status: status,
                vehicleType: vehicleType,
                personType: personType,
                fromDate: fromDate,
                toDate: toDate,
                hour: hour,
                laneName: laneName);

            var totalCount = (int)await _sessionRepo.CountAsync(filter);
            var skip = (pageNumber - 1) * pageSize;

            var sort = MongoDB.Driver.Builders<ParkingSession>.Sort.Descending(s => s.CreatedAt);
            var items = await _sessionRepo.FindAsync(filter, sort, skip, pageSize);

            // Tổng hợp toàn bộ tập dữ liệu (Full Dataset Aggregation) theo bộ lọc
            var totalWeight = await _sessionRepo.SumAsync(filter, s => s.Weight);
            var totalTruckCount = (int)await _sessionRepo.CountAsync(
                filter & MongoDB.Driver.Builders<ParkingSession>.Filter.Eq(s => s.VehicleType, VehicleType.Truck));
            var totalActiveCount = (int)await _sessionRepo.CountAsync(
                filter & MongoDB.Driver.Builders<ParkingSession>.Filter.Eq(s => s.Status, ParkingSessionStatus.Active));

            var result = new PagedResult<ParkingSession>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalWeight = Math.Round(totalWeight, 2),
                TotalTruckCount = totalTruckCount,
                TotalActiveCount = totalActiveCount
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
            [FromQuery] string? plateNumber = null,
            [FromQuery] string? keyword = null,
            [FromQuery] ParkingSessionStatus? status = null,
            [FromQuery] VehicleType? vehicleType = null,
            [FromQuery] PersonType? personType = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int? hour = null)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var searchTerm = !string.IsNullOrWhiteSpace(keyword) ? keyword : plateNumber;
            var filter = BuildSessionFilter(
                searchTerm: searchTerm,
                status: status,
                vehicleType: vehicleType,
                personType: personType,
                fromDate: fromDate,
                toDate: toDate,
                hour: hour);

            var sort = MongoDB.Driver.Builders<ParkingSession>.Sort.Descending(s => s.CreatedAt);
            var sessions = await _sessionRepo.FindAsync(filter, sort, 0, 5000);

            using var package = new ExcelPackage();

            // =========================================================================
            // SHEET 1: BÁO CÁO TỔNG HỢP — ĐẾM SỐ LƯỢT VÀO RA THEO TỪNG XE
            // =========================================================================
            var wsSummary = package.Workbook.Worksheets.Add("Thống Kê Số Lượt Từng Xe");

            // Tiêu đề Sheet 1
            wsSummary.Cells["A1:M1"].Merge = true;
            wsSummary.Cells["A1"].Value = "BÁO CÁO THỐNG KÊ SỐ LƯỢT XE VÀO RA THEO TỪNG XE";
            wsSummary.Cells["A1"].Style.Font.Bold = true;
            wsSummary.Cells["A1"].Style.Font.Size = 15;
            wsSummary.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Thông tin thời gian lọc
            wsSummary.Cells["A2:M2"].Merge = true;
            static string FormatDateDisplay(DateTime dt, bool isStart)
            {
                if (isStart && dt.TimeOfDay == TimeSpan.Zero) return dt.ToString("dd/MM/yyyy");
                if (!isStart && (dt.TimeOfDay == new TimeSpan(23, 59, 59) || dt.TimeOfDay == new TimeSpan(23, 59, 0))) return dt.ToString("dd/MM/yyyy");
                return dt.ToString("dd/MM/yyyy HH:mm");
            }

            string searchFilterStr = !string.IsNullOrWhiteSpace(searchTerm) ? $" | Từ khóa: {searchTerm.Trim()}" : "";
            string timeRangeStr = $"Khoảng thời gian: {(fromDate.HasValue ? FormatDateDisplay(fromDate.Value, true) : "Toàn bộ")} - {(toDate.HasValue ? FormatDateDisplay(toDate.Value, false) : "Hiện tại")}{searchFilterStr}";
            wsSummary.Cells["A2"].Value = timeRangeStr;
            wsSummary.Cells["A2"].Style.Font.Italic = true;
            wsSummary.Cells["A2"].Style.Font.Size = 11;
            wsSummary.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Header Bảng Thống Kê
            string[] summaryHeaders = {
                "STT",
                "Biển Số Xe",
                "Chủ Xe",
                "Đối Tượng",
                "Loại Xe",
                "Khối Lượng Xe (m³)",
                "Tổng Lượt Vào",
                "Tổng Lượt Ra",
                "Tổng Khối Lượng (m³)",
                "Đang Trong Bãi",
                "Tổng Thời Gian Đỗ",
                "Lần Vào Mới Nhất",
                "Lần Ra Mới Nhất"
            };
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

            static double? GetRegisteredWeight(IGrouping<string, ParkingSession> g) =>
                g.FirstOrDefault(s => s.Weight.HasValue)?.Weight;

            int sumRow = 5;
            int sumStt = 1;
            int grandTotalIn = 0;
            int grandTotalOut = 0;
            double grandTotalWeightSheet1 = 0;

            foreach (var g in vehicleGroups)
            {
                var first = g.First();
                int inCount = g.Count(s => s.InTime.HasValue);
                int outCount = g.Count(s => s.HasExited);
                bool isCurrentlyInParking = g.Any(s => s.Status == ParkingSessionStatus.Active);

                double? vehicleWeight = GetRegisteredWeight(g);
                double totalGroupWeight = (vehicleWeight ?? 0) * outCount;

                grandTotalIn += inCount;
                grandTotalOut += outCount;
                grandTotalWeightSheet1 += totalGroupWeight;

                var totalDurationMinutes = g.Where(s => s.Duration.HasValue).Sum(s => s.Duration!.Value.TotalMinutes);
                int totalHours = (int)(totalDurationMinutes / 60);
                int totalMins = (int)(totalDurationMinutes % 60);
                string totalDurationStr = totalDurationMinutes > 0 ? $"{totalHours}h {totalMins}m" : "--";

                var latestIn = g.Where(s => s.InTime.HasValue).OrderByDescending(s => s.InTime).FirstOrDefault()?.InTime;
                var latestOut = g.Where(s => s.OutTime.HasValue).OrderByDescending(s => s.OutTime).FirstOrDefault()?.OutTime;

                wsSummary.Cells[sumRow, 1].Value = sumStt++;
                wsSummary.Cells[sumRow, 2].Value = g.Key;
                wsSummary.Cells[sumRow, 3].Value = first.PersonName ?? "Xe vãng lai";
                wsSummary.Cells[sumRow, 4].Value = first.PersonType.ToDisplayName(first.PersonName);
                wsSummary.Cells[sumRow, 5].Value = first.VehicleType.ToDisplayName();
                if (vehicleWeight.HasValue && vehicleWeight.Value > 0)
                {
                    SetWeightCellValue(wsSummary.Cells[sumRow, 6], vehicleWeight.Value);
                }
                else
                {
                    wsSummary.Cells[sumRow, 6].Value = "--";
                }
                wsSummary.Cells[sumRow, 7].Value = inCount;
                wsSummary.Cells[sumRow, 8].Value = outCount;
                if (totalGroupWeight > 0)
                {
                    SetWeightCellValue(wsSummary.Cells[sumRow, 9], totalGroupWeight);
                }
                else
                {
                    wsSummary.Cells[sumRow, 9].Value = "--";
                }
                wsSummary.Cells[sumRow, 10].Value = isCurrentlyInParking ? "Có (Đang đỗ)" : "Không";
                wsSummary.Cells[sumRow, 11].Value = totalDurationStr;
                wsSummary.Cells[sumRow, 12].Value = latestIn.HasValue ? latestIn.Value.ToString("dd/MM/yyyy HH:mm:ss") : "--";
                wsSummary.Cells[sumRow, 13].Value = latestOut.HasValue ? latestOut.Value.ToString("dd/MM/yyyy HH:mm:ss") : "--";

                // Canh lề
                wsSummary.Cells[sumRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                wsSummary.Cells[sumRow, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                wsSummary.Cells[sumRow, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                wsSummary.Cells[sumRow, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                wsSummary.Cells[sumRow, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                wsSummary.Cells[sumRow, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                wsSummary.Cells[sumRow, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                wsSummary.Cells[sumRow, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                wsSummary.Cells[sumRow, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                sumRow++;
            }

            if (vehicleGroups.Count > 0)
            {
                wsSummary.Cells[sumRow, 1, sumRow, 6].Merge = true;
                wsSummary.Cells[sumRow, 1].Value = "TỔNG CỘNG";
                wsSummary.Cells[sumRow, 1].Style.Font.Bold = true;
                wsSummary.Cells[sumRow, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                wsSummary.Cells[sumRow, 7].Value = grandTotalIn;
                wsSummary.Cells[sumRow, 7].Style.Font.Bold = true;
                wsSummary.Cells[sumRow, 7].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                wsSummary.Cells[sumRow, 8].Value = grandTotalOut;
                wsSummary.Cells[sumRow, 8].Style.Font.Bold = true;
                wsSummary.Cells[sumRow, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                SetWeightCellValue(wsSummary.Cells[sumRow, 9], grandTotalWeightSheet1);
                wsSummary.Cells[sumRow, 9].Style.Font.Bold = true;
                wsSummary.Cells[sumRow, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                wsSummary.Cells[sumRow, 1, sumRow, 13].Style.Fill.PatternType = ExcelFillStyle.Solid;
                wsSummary.Cells[sumRow, 1, sumRow, 13].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(241, 245, 249));
                sumRow++;
            }

            wsSummary.Cells.AutoFitColumns();

            // =========================================================================
            // SHEET 2: LỊCH SỬ CHI TIẾT TỪNG LƯỢT XE
            // =========================================================================
            var ws = package.Workbook.Worksheets.Add("Lịch Sử Chi Tiết");

            // Header Title
            ws.Cells["A1:J1"].Merge = true;
            ws.Cells["A1"].Value = "BÁO CÁO CHI TIẾT CÁC PHIÊN XE RA VÀO";
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.Font.Size = 15;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Table Headers
            string[] headers = {
                "STT",
                "Biển Số Xe",
                "Chủ Xe",
                "Đối Tượng",
                "Loại Xe",
                "Khối Lượng (m³)",
                "Thời Gian Vào",
                "Thời Gian Ra",
                "Thời Gian Đỗ",
                "Trạng Thái"
            };
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
                ws.Cells[row, 4].Value = s.PersonType.ToDisplayName(s.PersonName);
                ws.Cells[row, 5].Value = s.VehicleType.ToDisplayName();
                if (s.Weight.HasValue && s.Weight.Value > 0)
                {
                    SetWeightCellValue(ws.Cells[row, 6], s.Weight.Value);
                }
                else
                {
                    ws.Cells[row, 6].Value = "--";
                }
                ws.Cells[row, 7].Value = s.InTime.HasValue ? s.InTime.Value.ToString("dd/MM/yyyy HH:mm:ss") : "--";
                ws.Cells[row, 8].Value = s.OutTime.HasValue ? s.OutTime.Value.ToString("dd/MM/yyyy HH:mm:ss") : "--";
                ws.Cells[row, 9].Value = s.Duration.HasValue ? $"{s.Duration.Value.Hours}h {s.Duration.Value.Minutes}m" : "--";
                ws.Cells[row, 10].Value = s.Status.ToDisplayName();

                ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                ws.Cells[row, 10].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                row++;
            }

            if (sessions.Count > 0)
            {
                ws.Cells[row, 1, row, 5].Merge = true;
                ws.Cells[row, 1].Value = "TỔNG CỘNG KHỐI LƯỢNG (LƯỢT ĐÃ RA)";
                ws.Cells[row, 1].Style.Font.Bold = true;
                ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                double grandTotalSessionsWeight = sessions
                    .Where(s => s.HasExited && s.Weight.HasValue)
                    .Sum(s => s.Weight!.Value);
                SetWeightCellValue(ws.Cells[row, 6], grandTotalSessionsWeight);
                ws.Cells[row, 6].Style.Font.Bold = true;
                ws.Cells[row, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                ws.Cells[row, 1, row, 10].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[row, 1, row, 10].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(241, 245, 249));
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

        private static MongoDB.Driver.FilterDefinition<ParkingSession> BuildTimeFilter(
            DateTime? fromDate,
            DateTime? toDate,
            int? hour,
            ParkingSessionStatus? status)
        {
            var fb = MongoDB.Driver.Builders<ParkingSession>.Filter;
            var dateFilter = fb.Empty;

            if (fromDate.HasValue || toDate.HasValue)
            {
                if (status == ParkingSessionStatus.UnmatchedOut)
                {
                    if (fromDate.HasValue) dateFilter &= fb.Gte(s => s.OutTime, fromDate.Value);
                    if (toDate.HasValue) dateFilter &= fb.Lte(s => s.OutTime, toDate.Value);
                }
                else if (status == ParkingSessionStatus.Active)
                {
                    if (fromDate.HasValue) dateFilter &= fb.Gte(s => s.InTime, fromDate.Value);
                    if (toDate.HasValue) dateFilter &= fb.Lte(s => s.InTime, toDate.Value);
                }
                else
                {
                    var inConditions = fb.Empty;
                    var outConditions = fb.Empty;

                    if (fromDate.HasValue)
                    {
                        inConditions &= fb.Gte(s => s.InTime, fromDate.Value);
                        outConditions &= fb.Gte(s => s.OutTime, fromDate.Value);
                    }

                    if (toDate.HasValue)
                    {
                        inConditions &= fb.Lte(s => s.InTime, toDate.Value);
                        outConditions &= fb.Lte(s => s.OutTime, toDate.Value);
                    }

                    dateFilter = inConditions | outConditions;
                }
            }

            var hourFilter = fb.Empty;
            if (hour.HasValue && hour.Value >= 0 && hour.Value <= 23)
            {
                var inHourDoc = new MongoDB.Bson.BsonDocument("$expr", new MongoDB.Bson.BsonDocument("$and", new MongoDB.Bson.BsonArray
                {
                    new MongoDB.Bson.BsonDocument("$ne", new MongoDB.Bson.BsonArray { "$InTime", MongoDB.Bson.BsonNull.Value }),
                    new MongoDB.Bson.BsonDocument("$eq", new MongoDB.Bson.BsonArray
                    {
                        new MongoDB.Bson.BsonDocument("$hour", new MongoDB.Bson.BsonDocument { { "date", "$InTime" }, { "timezone", "+07:00" } }),
                        hour.Value
                    })
                }));

                var outHourDoc = new MongoDB.Bson.BsonDocument("$expr", new MongoDB.Bson.BsonDocument("$and", new MongoDB.Bson.BsonArray
                {
                    new MongoDB.Bson.BsonDocument("$ne", new MongoDB.Bson.BsonArray { "$OutTime", MongoDB.Bson.BsonNull.Value }),
                    new MongoDB.Bson.BsonDocument("$eq", new MongoDB.Bson.BsonArray
                    {
                        new MongoDB.Bson.BsonDocument("$hour", new MongoDB.Bson.BsonDocument { { "date", "$OutTime" }, { "timezone", "+07:00" } }),
                        hour.Value
                    })
                }));

                var inHourFilter = new MongoDB.Driver.BsonDocumentFilterDefinition<ParkingSession>(inHourDoc);
                var outHourFilter = new MongoDB.Driver.BsonDocumentFilterDefinition<ParkingSession>(outHourDoc);

                if (status == ParkingSessionStatus.UnmatchedOut)
                {
                    hourFilter = outHourFilter;
                }
                else if (status == ParkingSessionStatus.Active)
                {
                    hourFilter = inHourFilter;
                }
                else
                {
                    hourFilter = inHourFilter | outHourFilter;
                }
            }

            if (dateFilter != fb.Empty && hourFilter != fb.Empty)
            {
                return dateFilter & hourFilter;
            }
            if (dateFilter != fb.Empty) return dateFilter;
            if (hourFilter != fb.Empty) return hourFilter;

            return fb.Empty;
        }

        private static MongoDB.Driver.FilterDefinition<ParkingSession> BuildSessionFilter(
            string? searchTerm = null,
            ParkingSessionStatus? status = null,
            VehicleType? vehicleType = null,
            PersonType? personType = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int? hour = null,
            string? laneName = null)
        {
            var fb = MongoDB.Driver.Builders<ParkingSession>.Filter;
            var filter = fb.Eq(s => s.IsDeleted, false);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var trimmed = searchTerm.Trim();
                var safePattern = System.Text.RegularExpressions.Regex.Escape(trimmed).Replace(@"\ ", " ");
                var regex = new MongoDB.Bson.BsonRegularExpression(safePattern, "i");

                // Tìm kiếm theo Biển số xe HOẶC Tên lái xe (PersonName)
                var textConditions = fb.Regex(s => s.PlateNumber, regex) |
                                     fb.Regex(s => s.PersonName, regex);

                // Tự động bóc tách đơn vị để tìm kiếm theo Khối lượng (m³)
                // Hỗ trợ nhập số trực tiếp (ví dụ: 15, 15.5) hoặc kèm đơn vị (ví dụ: 15m3, 15 m3, 15m³, 15 khối, 15 khoi)
                var weightCleaned = System.Text.RegularExpressions.Regex.Replace(
                    trimmed,
                    @"\s*(m3|m³|khối|khoi)$",
                    "",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase).Trim();

                if (double.TryParse(weightCleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double parsedWeight)
                    || double.TryParse(weightCleaned, System.Globalization.NumberStyles.Any, new System.Globalization.CultureInfo("vi-VN"), out parsedWeight))
                {
                    filter &= (textConditions | fb.Eq(s => s.Weight, parsedWeight));
                }
                else
                {
                    filter &= textConditions;
                }
            }

            if (status.HasValue)
            {
                filter &= fb.Eq(s => s.Status, status.Value);
            }

            if (vehicleType.HasValue)
            {
                filter &= fb.Eq(s => s.VehicleType, vehicleType.Value);
            }

            var personTypeFilter = BuildPersonTypeFilter(personType);
            if (personTypeFilter != null)
            {
                filter &= personTypeFilter;
            }

            var timeFilter = BuildTimeFilter(fromDate, toDate, hour, status);
            if (timeFilter != fb.Empty)
            {
                filter &= timeFilter;
            }

            if (!string.IsNullOrWhiteSpace(laneName))
            {
                filter &= (fb.Eq(s => s.InLaneName, laneName) |
                           fb.Eq(s => s.OutLaneName, laneName));
            }

            return filter;
        }

        private static MongoDB.Driver.FilterDefinition<ParkingSession>? BuildPersonTypeFilter(PersonType? personType)
        {
            if (!personType.HasValue)
            {
                return null;
            }

            var fb = MongoDB.Driver.Builders<ParkingSession>.Filter;

            if (personType.Value == PersonType.Guest)
            {
                return fb.Eq(s => s.PersonType, PersonType.Guest) |
                       fb.Eq(s => s.PersonType, null) |
                       fb.Eq(s => s.PersonType, PersonType.Other);
            }

            return fb.Eq(s => s.PersonType, personType.Value);
        }

        private static void SetWeightCellValue(ExcelRange cell, double weight)
        {
            cell.Value = weight;
            cell.Style.Numberformat.Format = (Math.Abs(weight % 1) < 0.0001) ? "#,##0" : "#,##0.##";
        }
    }
}
