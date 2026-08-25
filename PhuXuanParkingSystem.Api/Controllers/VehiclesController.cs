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
    public class VehiclesController : ControllerBase
    {
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IRepository<Person> _personRepo;

        public VehiclesController(IRepository<Vehicle> vehicleRepo, IRepository<Person> personRepo)
        {
            _vehicleRepo = vehicleRepo;
            _personRepo = personRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 15)
        {
            var filter = MongoDB.Driver.Builders<Vehicle>.Filter.Eq(v => v.IsDeleted, false);

            if (!string.IsNullOrWhiteSpace(search))
            {
                filter &= MongoDB.Driver.Builders<Vehicle>.Filter.Regex(v => v.PlateNumber, new MongoDB.Bson.BsonRegularExpression(search.Trim(), "i"));
            }

            var totalCount = (int)await _vehicleRepo.CountAsync(filter);
            var skip = (pageNumber - 1) * pageSize;
            var items = await _vehicleRepo.FindAsync(filter, null, skip, pageSize);

            var result = new PagedResult<Vehicle>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<Vehicle>>.Ok(result, "Lấy danh sách phương tiện thành công."));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var item = await _vehicleRepo.GetByIdAsync(id);
            if (item == null || item.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy phương tiện."));
            return Ok(ApiResponse<Vehicle>.Ok(item, "Lấy thông tin phương tiện thành công."));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Vehicle vehicle)
        {
            if (string.IsNullOrWhiteSpace(vehicle.PlateNumber))
            {
                return BadRequest(ApiResponse.Fail("Biển số xe không được để trống."));
            }

            var exists = await _vehicleRepo.FindOneAsync(v => v.PlateNumber == vehicle.PlateNumber && !v.IsDeleted);
            if (exists != null)
            {
                return BadRequest(ApiResponse.Fail($"Biển số '{vehicle.PlateNumber}' đã tồn tại trong hệ thống."));
            }

            await _vehicleRepo.AddAsync(vehicle);
            return Ok(ApiResponse<Vehicle>.Ok(vehicle, "Thêm phương tiện thành công."));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Vehicle vehicle)
        {
            var existing = await _vehicleRepo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy phương tiện."));

            existing.PlateNumber = vehicle.PlateNumber;
            existing.Type = vehicle.Type;
            existing.OwnerPersonId = vehicle.OwnerPersonId;
            existing.IsActive = vehicle.IsActive;
            existing.UpdatedAt = DateTime.Now;

            await _vehicleRepo.UpdateAsync(existing);
            return Ok(ApiResponse<Vehicle>.Ok(existing, "Cập nhật phương tiện thành công."));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var existing = await _vehicleRepo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy phương tiện."));

            await _vehicleRepo.DeleteAsync(id);
            return Ok(ApiResponse.Ok("Đã xóa phương tiện thành công."));
        }
    }
}
