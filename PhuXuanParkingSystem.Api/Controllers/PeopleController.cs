using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PhuXuanParkingSystem.Api.DTOs;
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

        public PeopleController(IRepository<Person> personRepo)
        {
            _personRepo = personRepo;
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
                }
            }

            return Ok(ApiResponse.Ok($"Nhập thành công {addedCount}/{people.Count} nhân sự từ file."));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var existing = await _personRepo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy nhân sự."));

            await _personRepo.DeleteAsync(id);
            return Ok(ApiResponse.Ok("Đã xóa nhân sự thành công."));
        }

        [HttpPost("delete-batch")]
        public async Task<IActionResult> DeleteBatch([FromBody] List<string> ids)
        {
            if (ids == null || ids.Count == 0) return BadRequest(ApiResponse.Fail("Danh sách ID rỗng."));

            foreach (var id in ids)
            {
                await _personRepo.DeleteAsync(id);
            }

            return Ok(ApiResponse.Ok($"Đã xóa {ids.Count} nhân sự thành công."));
        }
    }
}
