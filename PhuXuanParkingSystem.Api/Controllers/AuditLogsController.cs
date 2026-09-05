using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PhuXuanParkingSystem.Api.DTOs;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using System;
using System.Drawing;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Api.Controllers
{
    /// <summary>
    /// Controller quản lý và tra cứu Nhật ký kiểm toán (Audit Logs) cho Web Admin
    /// </summary>
    [ApiController]
    [Route("api/v1/audit-logs")]
    [Route("api/audit-logs")]
    [Authorize(Roles = "Admin,Manager")]
    public class AuditLogsController : ControllerBase
    {
        private readonly IRepository<AuditLog> _auditLogRepo;

        public AuditLogsController(IRepository<AuditLog> auditLogRepo)
        {
            _auditLogRepo = auditLogRepo ?? throw new ArgumentNullException(nameof(auditLogRepo));
        }

        private static FilterDefinition<AuditLog> BuildFilter(
            DateTime? fromDate,
            DateTime? toDate,
            string? actor,
            AuditActionType? actionType,
            string? targetEntity,
            bool? isSuccess,
            string? search)
        {
            var filter = Builders<AuditLog>.Filter.Eq(x => x.IsDeleted, false);

            if (fromDate.HasValue)
                filter &= Builders<AuditLog>.Filter.Gte(x => x.CreatedAt, fromDate.Value);

            if (toDate.HasValue)
                filter &= Builders<AuditLog>.Filter.Lte(x => x.CreatedAt, toDate.Value);

            if (!string.IsNullOrWhiteSpace(actor))
                filter &= Builders<AuditLog>.Filter.Regex(x => x.ActorUsername, new MongoDB.Bson.BsonRegularExpression(actor.Trim(), "i"));

            if (actionType.HasValue)
                filter &= Builders<AuditLog>.Filter.Eq(x => x.ActionType, actionType.Value);

            if (!string.IsNullOrWhiteSpace(targetEntity))
                filter &= Builders<AuditLog>.Filter.Regex(x => x.TargetEntity, new MongoDB.Bson.BsonRegularExpression($"^{targetEntity.Trim()}$", "i"));

            if (isSuccess.HasValue)
                filter &= Builders<AuditLog>.Filter.Eq(x => x.IsSuccess, isSuccess.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                filter &= Builders<AuditLog>.Filter.Or(
                    Builders<AuditLog>.Filter.Regex(x => x.ActorUsername, new MongoDB.Bson.BsonRegularExpression(s, "i")),
                    Builders<AuditLog>.Filter.Regex(x => x.TargetDisplay, new MongoDB.Bson.BsonRegularExpression(s, "i")),
                    Builders<AuditLog>.Filter.Regex(x => x.TargetEntity, new MongoDB.Bson.BsonRegularExpression(s, "i")),
                    Builders<AuditLog>.Filter.Regex(x => x.Reason, new MongoDB.Bson.BsonRegularExpression(s, "i")),
                    Builders<AuditLog>.Filter.Regex(x => x.ErrorMessage, new MongoDB.Bson.BsonRegularExpression(s, "i"))
                );
            }

            return filter;
        }

        /// <summary>
        /// Lấy danh sách Nhật ký kiểm toán có phân trang và bộ lọc đa chiều
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? actor = null,
            [FromQuery] AuditActionType? actionType = null,
            [FromQuery] string? targetEntity = null,
            [FromQuery] bool? isSuccess = null,
            [FromQuery] string? search = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var filter = BuildFilter(fromDate, toDate, actor, actionType, targetEntity, isSuccess, search);
            var totalCount = await _auditLogRepo.CountAsync(filter);
            var sort = Builders<AuditLog>.Sort.Descending(x => x.CreatedAt);
            var items = await _auditLogRepo.FindAsync(filter, sort, (pageNumber - 1) * pageSize, pageSize);

            return Ok(ApiResponse<PagedResult<AuditLog>>.Ok(new PagedResult<AuditLog>
            {
                Items = items,
                TotalCount = (int)totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            }, "Lấy danh sách nhật ký kiểm toán thành công."));
        }

        /// <summary>
        /// Xuất danh sách Nhật ký kiểm toán ra file Excel (.xlsx)
        /// </summary>
        [HttpGet("export")]
        public async Task<IActionResult> ExportAuditLogs(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string? actor = null,
            [FromQuery] AuditActionType? actionType = null,
            [FromQuery] string? targetEntity = null,
            [FromQuery] bool? isSuccess = null,
            [FromQuery] string? search = null)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var filter = BuildFilter(fromDate, toDate, actor, actionType, targetEntity, isSuccess, search);
            var sort = Builders<AuditLog>.Sort.Descending(x => x.CreatedAt);
            var logs = await _auditLogRepo.FindAsync(filter, sort, 0, 5000);

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("NhatKyKiemToan");

            ws.Cells["A1:I1"].Merge = true;
            ws.Cells["A1"].Value = "BÁO CÁO NHẬT KÝ KIỂM TOÁN HỆ THỐNG (AUDIT LOGS)";
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.Font.Size = 14;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A2:H2"].Merge = true;
            ws.Cells["A2"].Value = $"Thời gian xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm:ss} | Tổng số bản ghi: {logs.Count}";
            ws.Cells["A2"].Style.Font.Italic = true;
            ws.Cells["A2"].Style.Font.Size = 10;
            ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            string[] headers = { "STT", "Thời Gian", "Người Thực Hiện", "Vai Trò", "Hành Động", "Thực Thể", "Đối Tượng / Lý Do", "Kết Quả" };
            for (int i = 0; i < headers.Length; i++)
            {
                ws.Cells[4, i + 1].Value = headers[i];
                ws.Cells[4, i + 1].Style.Font.Bold = true;
                ws.Cells[4, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[4, i + 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(41, 128, 185));
                ws.Cells[4, i + 1].Style.Font.Color.SetColor(Color.White);
                ws.Cells[4, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            int row = 5;
            int stt = 1;
            foreach (var log in logs)
            {
                ws.Cells[row, 1].Value = stt++;
                ws.Cells[row, 2].Value = log.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss");
                ws.Cells[row, 3].Value = log.ActorUsername;
                ws.Cells[row, 4].Value = log.ActorRole;
                ws.Cells[row, 5].Value = log.ActionType.ToString();
                ws.Cells[row, 6].Value = log.TargetEntity;
                ws.Cells[row, 7].Value = !string.IsNullOrWhiteSpace(log.Reason) ? $"{log.TargetDisplay} (Lý do: {log.Reason})" : (log.TargetDisplay ?? "-");
                ws.Cells[row, 8].Value = log.IsSuccess ? "Thành công" : "Thất bại";

                ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                ws.Cells[row, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                row++;
            }

            ws.Cells.AutoFitColumns();
            return File(package.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"AuditLogs_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
        }

        /// <summary>
        /// Lấy chi tiết một bản ghi Nhật ký kiểm toán theo Id
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAuditLogById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest(ApiResponse.Fail("Id không hợp lệ."));

            var log = await _auditLogRepo.GetByIdAsync(id);
            return log == null
                ? NotFound(ApiResponse.Fail("Không tìm thấy bản ghi nhật ký kiểm toán."))
                : Ok(ApiResponse<AuditLog>.Ok(log, "Lấy chi tiết nhật ký kiểm toán thành công."));
        }
    }
}
