using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
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
    public class PeopleController : ControllerBase
    {
        private readonly IRepository<Person> _personRepo;
        private readonly IAuditLogQueue _auditQueue;

        public PeopleController(IRepository<Person> personRepo, IAuditLogQueue auditQueue)
        {
            _personRepo = personRepo;
            _auditQueue = auditQueue;
        }

        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] string? search,
            [FromQuery] string? type,
            [FromQuery] string? departmentId,
            [FromQuery] string? companyId,
            [FromQuery] string? contractorId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var filter = Builders<Person>.Filter.Eq(p => p.IsDeleted, false);

            if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<PersonType>(type, true, out var parsedType))
            {
                filter &= Builders<Person>.Filter.Eq(p => p.Type, parsedType);
            }

            if (!string.IsNullOrWhiteSpace(departmentId))
            {
                filter &= Builders<Person>.Filter.Eq(p => p.DepartmentId, departmentId.Trim());
            }

            if (!string.IsNullOrWhiteSpace(companyId))
            {
                filter &= Builders<Person>.Filter.Eq(p => p.CompanyId, companyId.Trim());
            }

            if (!string.IsNullOrWhiteSpace(contractorId))
            {
                filter &= Builders<Person>.Filter.Eq(p => p.ContractorId, contractorId.Trim());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                filter &= (Builders<Person>.Filter.Regex(p => p.FullName, new MongoDB.Bson.BsonRegularExpression(s, "i")) |
                           Builders<Person>.Filter.Regex(p => p.Code, new MongoDB.Bson.BsonRegularExpression(s, "i")) |
                           Builders<Person>.Filter.Regex(p => p.PhoneNumber, new MongoDB.Bson.BsonRegularExpression(s, "i")));
            }

            var totalCount = (int)await _personRepo.CountAsync(filter);
            var skip = (pageNumber - 1) * pageSize;
            var items = await _personRepo.FindAsync(filter, Builders<Person>.Sort.Descending(p => p.CreatedAt), skip, pageSize);

            var result = new PagedResult<Person>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<Person>>.Ok(result, "Lấy danh sách nhân sự thành công."));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var item = await _personRepo.GetByIdAsync(id);
            if (item == null || item.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy nhân sự."));
            return Ok(ApiResponse<Person>.Ok(item, "Lấy thông tin nhân sự thành công."));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Person person)
        {
            if (string.IsNullOrWhiteSpace(person.FullName))
            {
                return BadRequest(ApiResponse.Fail("Họ và tên không được để trống."));
            }

            if (string.IsNullOrWhiteSpace(person.Code))
            {
                return BadRequest(ApiResponse.Fail("Mã định danh không được để trống."));
            }

            person.Code = person.Code.Trim().ToUpperInvariant();
            var exists = await _personRepo.FindOneAsync(p => p.Code == person.Code && !p.IsDeleted);
            if (exists != null)
            {
                return BadRequest(ApiResponse.Fail($"Mã định danh '{person.Code}' đã tồn tại trong hệ thống."));
            }

            if (string.IsNullOrWhiteSpace(person.DepartmentId)) person.DepartmentId = null;
            if (string.IsNullOrWhiteSpace(person.CompanyId)) person.CompanyId = null;
            if (string.IsNullOrWhiteSpace(person.ContractorId)) person.ContractorId = null;

            await _personRepo.AddAsync(person);

            // Ghi nhận AuditLog Create
            var diff = AuditDiffHelper.ComputeDiff<Person>(null, person);
            var (actorId, actorUsername, actorRole) = User.GetActorInfo();
            await _auditQueue.QueueLogAsync(new AuditLog
            {
                ActorId = actorId,
                ActorUsername = actorUsername,
                ActorRole = actorRole,
                ActionType = AuditActionType.Create,
                TargetEntity = "Person",
                TargetId = person.Id,
                TargetDisplay = person.FullName,
                NewValues = diff.NewValues,
                ChangedProperties = diff.ChangedProperties,
                IpAddress = HttpContext.GetClientIp(),
                UserAgent = HttpContext.GetUserAgent(),
                IsSuccess = true,
                Source = "WebAdmin"
            });

            return Ok(ApiResponse<Person>.Ok(person, "Thêm nhân sự thành công."));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Person person)
        {
            var existing = await _personRepo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy nhân sự."));

            var cleanCode = (person.Code ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(cleanCode))
            {
                return BadRequest(ApiResponse.Fail("Mã định danh không được để trống."));
            }

            var duplicate = await _personRepo.FindOneAsync(p => p.Code == cleanCode && p.Id != id && !p.IsDeleted);
            if (duplicate != null)
            {
                return BadRequest(ApiResponse.Fail($"Mã định danh '{cleanCode}' đã thuộc về nhân sự khác."));
            }

            // Lưu trạng thái cũ để tính Diff
            var oldState = new Person
            {
                Id = existing.Id,
                Code = existing.Code,
                FullName = existing.FullName,
                PhoneNumber = existing.PhoneNumber,
                Email = existing.Email,
                Type = existing.Type,
                DepartmentId = existing.DepartmentId,
                CompanyId = existing.CompanyId,
                ContractorId = existing.ContractorId,
                IsActive = existing.IsActive,
                CreatedAt = existing.CreatedAt
            };

            existing.Code = cleanCode;
            existing.FullName = person.FullName.Trim();
            existing.PhoneNumber = person.PhoneNumber?.Trim();
            existing.Email = person.Email?.Trim();
            existing.Type = person.Type;
            existing.DepartmentId = string.IsNullOrWhiteSpace(person.DepartmentId) ? null : person.DepartmentId.Trim();
            existing.CompanyId = string.IsNullOrWhiteSpace(person.CompanyId) ? null : person.CompanyId.Trim();
            existing.ContractorId = string.IsNullOrWhiteSpace(person.ContractorId) ? null : person.ContractorId.Trim();
            existing.IsActive = person.IsActive;
            existing.UpdatedAt = DateTime.Now;

            await _personRepo.UpdateAsync(existing);

            // Ghi nhận AuditLog Update
            var diff = AuditDiffHelper.ComputeDiff(oldState, existing);
            if (diff.HasChanges)
            {
                var (actorId, actorUsername, actorRole) = User.GetActorInfo();
                await _auditQueue.QueueLogAsync(new AuditLog
                {
                    ActorId = actorId,
                    ActorUsername = actorUsername,
                    ActorRole = actorRole,
                    ActionType = AuditActionType.Update,
                    TargetEntity = "Person",
                    TargetId = existing.Id,
                    TargetDisplay = existing.FullName,
                    OldValues = diff.OldValues,
                    NewValues = diff.NewValues,
                    ChangedProperties = diff.ChangedProperties,
                    IpAddress = HttpContext.GetClientIp(),
                    UserAgent = HttpContext.GetUserAgent(),
                    IsSuccess = true,
                    Source = "WebAdmin"
                });
            }

            return Ok(ApiResponse<Person>.Ok(existing, "Cập nhật nhân sự thành công."));
        }

        [HttpPost("batch")]
        public async Task<IActionResult> CreateBatch([FromBody] List<Person> people)
        {
            if (people == null || people.Count == 0)
            {
                return BadRequest(ApiResponse.Fail("Danh sách nhân sự rỗng."));
            }

            int addedCount = 0;
            var (actorId, actorUsername, actorRole) = User.GetActorInfo();
            var ip = HttpContext.GetClientIp();
            var ua = HttpContext.GetUserAgent();

            foreach (var p in people)
            {
                if (string.IsNullOrWhiteSpace(p.FullName) || string.IsNullOrWhiteSpace(p.Code)) continue;
                p.Code = p.Code.Trim().ToUpperInvariant();

                var exists = await _personRepo.FindOneAsync(x => x.Code == p.Code && !x.IsDeleted);
                if (exists == null)
                {
                    if (string.IsNullOrWhiteSpace(p.DepartmentId)) p.DepartmentId = null;
                    if (string.IsNullOrWhiteSpace(p.CompanyId)) p.CompanyId = null;
                    if (string.IsNullOrWhiteSpace(p.ContractorId)) p.ContractorId = null;
                    await _personRepo.AddAsync(p);
                    addedCount++;

                    var diff = AuditDiffHelper.ComputeDiff<Person>(null, p);
                    await _auditQueue.QueueLogAsync(new AuditLog
                    {
                        ActorId = actorId,
                        ActorUsername = actorUsername,
                        ActorRole = actorRole,
                        ActionType = AuditActionType.Create,
                        TargetEntity = "Person",
                        TargetId = p.Id,
                        TargetDisplay = p.FullName,
                        NewValues = diff.NewValues,
                        ChangedProperties = diff.ChangedProperties,
                        IpAddress = ip,
                        UserAgent = ua,
                        IsSuccess = true,
                        Source = "WebAdmin"
                    });
                }
            }

            return Ok(ApiResponse.Ok($"Nhập thành công {addedCount}/{people.Count} nhân sự từ file."));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, [FromQuery] string? reason = null)
        {
            var existing = await _personRepo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy nhân sự."));

            await _personRepo.DeleteAsync(id);

            // Ghi nhận AuditLog Delete
            var diff = AuditDiffHelper.ComputeDiff<Person>(existing, null);
            var (actorId, actorUsername, actorRole) = User.GetActorInfo();
            await _auditQueue.QueueLogAsync(new AuditLog
            {
                ActorId = actorId,
                ActorUsername = actorUsername,
                ActorRole = actorRole,
                ActionType = AuditActionType.Delete,
                TargetEntity = "Person",
                TargetId = existing.Id,
                TargetDisplay = existing.FullName,
                OldValues = diff.OldValues,
                ChangedProperties = diff.ChangedProperties,
                Reason = reason,
                IpAddress = HttpContext.GetClientIp(),
                UserAgent = HttpContext.GetUserAgent(),
                IsSuccess = true,
                Source = "WebAdmin"
            });

            return Ok(ApiResponse.Ok("Đã xóa nhân sự thành công."));
        }

        [HttpPost("delete-batch")]
        public async Task<IActionResult> DeleteBatch([FromBody] List<string> ids, [FromQuery] string? reason = null)
        {
            if (ids == null || ids.Count == 0) return BadRequest(ApiResponse.Fail("Danh sách ID rỗng."));
            var (actorId, actorUsername, actorRole) = User.GetActorInfo();
            var ip = HttpContext.GetClientIp();
            var ua = HttpContext.GetUserAgent();

            foreach (var id in ids)
            {
                var p = await _personRepo.GetByIdAsync(id);
                if (p != null && !p.IsDeleted)
                {
                    await _personRepo.DeleteAsync(id);

                    var diff = AuditDiffHelper.ComputeDiff<Person>(p, null);
                    await _auditQueue.QueueLogAsync(new AuditLog
                    {
                        ActorId = actorId,
                        ActorUsername = actorUsername,
                        ActorRole = actorRole,
                        ActionType = AuditActionType.Delete,
                        TargetEntity = "Person",
                        TargetId = p.Id,
                        TargetDisplay = p.FullName,
                        OldValues = diff.OldValues,
                        ChangedProperties = diff.ChangedProperties,
                        Reason = reason,
                        IpAddress = ip,
                        UserAgent = ua,
                        IsSuccess = true,
                        Source = "WebAdmin"
                    });
                }
            }

            return Ok(ApiResponse.Ok($"Đã xóa {ids.Count} nhân sự thành công."));
        }
    }
}
