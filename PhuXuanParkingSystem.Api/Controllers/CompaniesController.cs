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
    public class CompaniesController : ControllerBase
    {
        private readonly IRepository<Company> _companyRepo;

        public CompaniesController(IRepository<Company> companyRepo)
        {
            _companyRepo = companyRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        {
            var filter = MongoDB.Driver.Builders<Company>.Filter.Eq(c => c.IsDeleted, false);

            if (!string.IsNullOrWhiteSpace(search))
            {
                filter &= (MongoDB.Driver.Builders<Company>.Filter.Regex(c => c.Name, new MongoDB.Bson.BsonRegularExpression(search.Trim(), "i")) |
                           MongoDB.Driver.Builders<Company>.Filter.Regex(c => c.Code, new MongoDB.Bson.BsonRegularExpression(search.Trim(), "i")));
            }

            var totalCount = (int)await _companyRepo.CountAsync(filter);
            var skip = (pageNumber - 1) * pageSize;
            var items = await _companyRepo.FindAsync(filter, null, skip, pageSize);

            var result = new PagedResult<Company>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<Company>>.Ok(result, "Lấy danh sách công ty thành công."));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var item = await _companyRepo.GetByIdAsync(id);
            if (item == null || item.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy công ty."));
            return Ok(ApiResponse<Company>.Ok(item, "Lấy thông tin công ty thành công."));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Company company)
        {
            if (string.IsNullOrWhiteSpace(company.Name))
            {
                return BadRequest(ApiResponse.Fail("Tên công ty không được để trống."));
            }

            await _companyRepo.AddAsync(company);
            return Ok(ApiResponse<Company>.Ok(company, "Thêm công ty thành công."));
        }

        [HttpPost("batch")]
        public async Task<IActionResult> CreateBatch([FromBody] List<Company> companies)
        {
            if (companies == null || companies.Count == 0)
            {
                return BadRequest(ApiResponse.Fail("Danh sách công ty rỗng."));
            }

            foreach (var comp in companies)
            {
                if (!string.IsNullOrWhiteSpace(comp.Name))
                {
                    await _companyRepo.AddAsync(comp);
                }
            }

            return Ok(ApiResponse.Ok($"Nhập thành công {companies.Count} công ty từ file."));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Company company)
        {
            var existing = await _companyRepo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy công ty."));

            existing.Code = company.Code;
            existing.Name = company.Name;
            existing.PhoneNumber = company.PhoneNumber;
            existing.Email = company.Email;
            existing.Note = company.Note;
            existing.IsActive = company.IsActive;
            existing.UpdatedAt = DateTime.Now;

            await _companyRepo.UpdateAsync(existing);
            return Ok(ApiResponse<Company>.Ok(existing, "Cập nhật công ty thành công."));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var existing = await _companyRepo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy công ty."));

            await _companyRepo.DeleteAsync(id);
            return Ok(ApiResponse.Ok("Đã xóa công ty thành công."));
        }

        [HttpPost("delete-batch")]
        public async Task<IActionResult> DeleteBatch([FromBody] List<string> ids)
        {
            if (ids == null || ids.Count == 0) return BadRequest(ApiResponse.Fail("Danh sách ID rỗng."));

            foreach (var id in ids)
            {
                await _companyRepo.DeleteAsync(id);
            }

            return Ok(ApiResponse.Ok($"Đã xóa {ids.Count} công ty thành công."));
        }
    }
}
