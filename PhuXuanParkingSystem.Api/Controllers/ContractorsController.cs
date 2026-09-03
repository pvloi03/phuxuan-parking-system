using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhuXuanParkingSystem.Api.DTOs;
using PhuXuanParkingSystem.Api.Helpers;
using PhuXuanParkingSystem.Api.Services;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ContractorsController : ControllerBase
    {
        private readonly IRepository<Contractor> _contractorRepo;
        private readonly IAuditLogQueue _auditQueue;

        public ContractorsController(IRepository<Contractor> contractorRepo, IAuditLogQueue auditQueue)
        {
            _contractorRepo = contractorRepo;
            _auditQueue = auditQueue;
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            var filter = MongoDB.Driver.Builders<Contractor>.Filter.Eq(c => c.IsDeleted, false);

            if (!string.IsNullOrWhiteSpace(search))
            {
                filter &= (MongoDB.Driver.Builders<Contractor>.Filter.Regex(c => c.Name, new MongoDB.Bson.BsonRegularExpression(search.Trim(), "i")) |
                           MongoDB.Driver.Builders<Contractor>.Filter.Regex(c => c.Code, new MongoDB.Bson.BsonRegularExpression(search.Trim(), "i")) |
                           MongoDB.Driver.Builders<Contractor>.Filter.Regex(c => c.ContactPerson, new MongoDB.Bson.BsonRegularExpression(search.Trim(), "i")));
            }

            var totalCount = (int)await _contractorRepo.CountAsync(filter);
            var skip = (pageNumber - 1) * pageSize;
            var items = await _contractorRepo.FindAsync(filter, null, skip, pageSize);

            var result = new PagedResult<Contractor>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<Contractor>>.Ok(result, "Lấy danh sách đối tác thành công."));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var item = await _contractorRepo.GetByIdAsync(id);
            if (item == null || item.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy đối tác."));
            return Ok(ApiResponse<Contractor>.Ok(item, "Lấy thông tin đối tác thành công."));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Contractor contractor)
        {
            if (string.IsNullOrWhiteSpace(contractor.Name))
            {
                return BadRequest(ApiResponse.Fail("Tên đối tác / nhà thầu không được để trống."));
            }

            await _contractorRepo.AddAsync(contractor);

            var diff = AuditDiffHelper.ComputeDiff<Contractor>(null, contractor);
            await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.Create, "Contractor", contractor.Id, contractor.Name, diff);

            return Ok(ApiResponse<Contractor>.Ok(contractor, "Thêm đối tác thành công."));
        }

        [HttpPost("batch")]
        public async Task<IActionResult> CreateBatch([FromBody] List<Contractor> contractors)
        {
            if (contractors == null || contractors.Count == 0)
            {
                return BadRequest(ApiResponse.Fail("Danh sách đối tác rỗng."));
            }

            int addedCount = 0;
            foreach (var cont in contractors)
            {
                if (!string.IsNullOrWhiteSpace(cont.Name))
                {
                    await _contractorRepo.AddAsync(cont);
                    addedCount++;
                    var diff = AuditDiffHelper.ComputeDiff<Contractor>(null, cont);
                    await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.Create, "Contractor", cont.Id, cont.Name, diff);
                }
            }

            return Ok(ApiResponse.Ok($"Nhập thành công {addedCount}/{contractors.Count} đối tác từ file."));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Contractor contractor)
        {
            var existing = await _contractorRepo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy đối tác."));

            var snapshot = AuditDiffHelper.TakeSnapshot(existing);

            existing.Code = contractor.Code;
            existing.Name = contractor.Name;
            existing.ContactPerson = contractor.ContactPerson;
            existing.PhoneNumber = contractor.PhoneNumber;
            existing.Email = contractor.Email;
            existing.Note = contractor.Note;
            existing.IsActive = contractor.IsActive;
            existing.UpdatedAt = DateTime.Now;

            await _contractorRepo.UpdateAsync(existing);

            var diff = AuditDiffHelper.ComputeDiffFromSnapshot(snapshot, existing);
            if (diff.HasChanges)
            {
                await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.Update, "Contractor", existing.Id, existing.Name, diff);
            }

            return Ok(ApiResponse<Contractor>.Ok(existing, "Cập nhật đối tác thành công."));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, [FromQuery] string? reason = null)
        {
            var existing = await _contractorRepo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy đối tác."));

            await _contractorRepo.DeleteAsync(id);

            var diff = AuditDiffHelper.ComputeDiff<Contractor>(existing, null);
            await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.Delete, "Contractor", existing.Id, existing.Name, diff, reason: reason);

            return Ok(ApiResponse.Ok("Đã xóa đối tác thành công."));
        }

        [HttpPost("delete-batch")]
        public async Task<IActionResult> DeleteBatch([FromBody] List<string> ids, [FromQuery] string? reason = null)
        {
            if (ids == null || ids.Count == 0) return BadRequest(ApiResponse.Fail("Danh sách ID rỗng."));

            foreach (var id in ids)
            {
                var existing = await _contractorRepo.GetByIdAsync(id);
                if (existing != null && !existing.IsDeleted)
                {
                    await _contractorRepo.DeleteAsync(id);
                    var diff = AuditDiffHelper.ComputeDiff<Contractor>(existing, null);
                    await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.Delete, "Contractor", existing.Id, existing.Name, diff, reason: reason);
                }
            }

            return Ok(ApiResponse.Ok($"Đã xóa {ids.Count} đối tác thành công."));
        }
    }
}
