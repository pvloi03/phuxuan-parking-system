using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhuXuanParkingSystem.Api.DTOs;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Repositories;

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
        public async Task<IActionResult> GetList([FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            var filter = MongoDB.Driver.Builders<Person>.Filter.Eq(p => p.IsDeleted, false);

            if (!string.IsNullOrWhiteSpace(search))
            {
                filter &= (MongoDB.Driver.Builders<Person>.Filter.Regex(p => p.FullName, new MongoDB.Bson.BsonRegularExpression(search.Trim(), "i")) |
                           MongoDB.Driver.Builders<Person>.Filter.Regex(p => p.Code, new MongoDB.Bson.BsonRegularExpression(search.Trim(), "i")));
            }

            var totalCount = (int)await _personRepo.CountAsync(filter);
            var skip = (pageNumber - 1) * pageSize;
            var items = await _personRepo.FindAsync(filter, null, skip, pageSize);

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

            await _personRepo.AddAsync(person);
            return Ok(ApiResponse<Person>.Ok(person, "Thêm nhân sự thành công."));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Person person)
        {
            var existing = await _personRepo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy nhân sự."));

            existing.Code = person.Code;
            existing.FullName = person.FullName;
            existing.PhoneNumber = person.PhoneNumber;
            existing.Email = person.Email;
            existing.Type = person.Type;
            existing.DepartmentId = person.DepartmentId;
            existing.CompanyId = person.CompanyId;
            existing.ContractorId = person.ContractorId;
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

            foreach (var p in people)
            {
                if (!string.IsNullOrWhiteSpace(p.FullName))
                {
                    await _personRepo.AddAsync(p);
                }
            }

            return Ok(ApiResponse.Ok($"Nhập thành công {people.Count} nhân sự từ file."));
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
