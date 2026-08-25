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
    public class DepartmentsController : ControllerBase
    {
        private readonly IRepository<Department> _deptRepo;

        public DepartmentsController(IRepository<Department> deptRepo)
        {
            _deptRepo = deptRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] string? search,
            [FromQuery] string? companyId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var filter = MongoDB.Driver.Builders<Department>.Filter.Eq(d => d.IsDeleted, false);

            if (!string.IsNullOrWhiteSpace(companyId))
            {
                filter &= MongoDB.Driver.Builders<Department>.Filter.Eq(d => d.CompanyId, companyId.Trim());
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                filter &= (MongoDB.Driver.Builders<Department>.Filter.Regex(d => d.Name, new MongoDB.Bson.BsonRegularExpression(search.Trim(), "i")) |
                           MongoDB.Driver.Builders<Department>.Filter.Regex(d => d.Code, new MongoDB.Bson.BsonRegularExpression(search.Trim(), "i")));
            }

            var totalCount = (int)await _deptRepo.CountAsync(filter);
            var skip = (pageNumber - 1) * pageSize;
            var items = await _deptRepo.FindAsync(filter, null, skip, pageSize);

            var result = new PagedResult<Department>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<Department>>.Ok(result, "Lấy danh sách phòng ban thành công."));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var item = await _deptRepo.GetByIdAsync(id);
            if (item == null || item.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy phòng ban."));
            return Ok(ApiResponse<Department>.Ok(item, "Lấy thông tin phòng ban thành công."));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Department dept)
        {
            if (string.IsNullOrWhiteSpace(dept.Name))
            {
                return BadRequest(ApiResponse.Fail("Tên phòng ban không được để trống."));
            }

            await _deptRepo.AddAsync(dept);
            return Ok(ApiResponse<Department>.Ok(dept, "Thêm phòng ban thành công."));
        }

        [HttpPost("batch")]
        public async Task<IActionResult> CreateBatch([FromBody] List<Department> departments)
        {
            if (departments == null || departments.Count == 0)
            {
                return BadRequest(ApiResponse.Fail("Danh sách phòng ban rỗng."));
            }

            foreach (var dept in departments)
            {
                if (!string.IsNullOrWhiteSpace(dept.Name))
                {
                    await _deptRepo.AddAsync(dept);
                }
            }

            return Ok(ApiResponse.Ok($"Nhập thành công {departments.Count} phòng ban từ file."));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Department dept)
        {
            var existing = await _deptRepo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy phòng ban."));

            existing.Code = dept.Code;
            existing.Name = dept.Name;
            existing.CompanyId = dept.CompanyId;
            existing.ManagerName = dept.ManagerName;
            existing.PhoneNumber = dept.PhoneNumber;
            existing.Email = dept.Email;
            existing.Note = dept.Note;
            existing.IsActive = dept.IsActive;
            existing.UpdatedAt = DateTime.Now;

            await _deptRepo.UpdateAsync(existing);
            return Ok(ApiResponse<Department>.Ok(existing, "Cập nhật phòng ban thành công."));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var existing = await _deptRepo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy phòng ban."));

            await _deptRepo.DeleteAsync(id);
            return Ok(ApiResponse.Ok("Đã xóa phòng ban thành công."));
        }

        [HttpPost("delete-batch")]
        public async Task<IActionResult> DeleteBatch([FromBody] List<string> ids)
        {
            if (ids == null || ids.Count == 0) return BadRequest(ApiResponse.Fail("Danh sách ID rỗng."));

            foreach (var id in ids)
            {
                await _deptRepo.DeleteAsync(id);
            }

            return Ok(ApiResponse.Ok($"Đã xóa {ids.Count} phòng ban thành công."));
        }
    }
}
