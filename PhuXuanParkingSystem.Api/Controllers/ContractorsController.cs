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
    public class ContractorsController : ControllerBase
    {
        private readonly IRepository<Contractor> _contractorRepo;

        public ContractorsController(IRepository<Contractor> contractorRepo)
        {
            _contractorRepo = contractorRepo;
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
            return Ok(ApiResponse<Contractor>.Ok(contractor, "Thêm đối tác thành công."));
        }

        [HttpPost("batch")]
        public async Task<IActionResult> CreateBatch([FromBody] List<Contractor> contractors)
        {
            if (contractors == null || contractors.Count == 0)
            {
                return BadRequest(ApiResponse.Fail("Danh sách đối tác rỗng."));
            }

            foreach (var cont in contractors)
            {
                if (!string.IsNullOrWhiteSpace(cont.Name))
                {
                    await _contractorRepo.AddAsync(cont);
                }
            }

            return Ok(ApiResponse.Ok($"Nhập thành công {contractors.Count} đối tác từ file."));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Contractor contractor)
        {
            var existing = await _contractorRepo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy đối tác."));

            existing.Code = contractor.Code;
            existing.Name = contractor.Name;
            existing.ContactPerson = contractor.ContactPerson;
            existing.PhoneNumber = contractor.PhoneNumber;
            existing.Email = contractor.Email;
            existing.Note = contractor.Note;
            existing.IsActive = contractor.IsActive;
            existing.UpdatedAt = DateTime.Now;

            await _contractorRepo.UpdateAsync(existing);
            return Ok(ApiResponse<Contractor>.Ok(existing, "Cập nhật đối tác thành công."));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var existing = await _contractorRepo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy đối tác."));

            await _contractorRepo.DeleteAsync(id);
            return Ok(ApiResponse.Ok("Đã xóa đối tác thành công."));
        }

        [HttpPost("delete-batch")]
        public async Task<IActionResult> DeleteBatch([FromBody] List<string> ids)
        {
            if (ids == null || ids.Count == 0) return BadRequest(ApiResponse.Fail("Danh sách ID rỗng."));

            foreach (var id in ids)
            {
                await _contractorRepo.DeleteAsync(id);
            }

            return Ok(ApiResponse.Ok($"Đã xóa {ids.Count} đối tác thành công."));
        }
    }
}
