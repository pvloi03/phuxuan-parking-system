using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhuXuanParkingSystem.Api.DTOs;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using MongoDB.Driver;

namespace PhuXuanParkingSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DevicesController : ControllerBase
    {
        private readonly IRepository<Device> _deviceRepo;

        public DevicesController(IRepository<Device> deviceRepo)
        {
            _deviceRepo = deviceRepo;
        }

        // GET api/devices?search=&type=Camera&pageNumber=1&pageSize=10
        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] string? search,
            [FromQuery] string? type,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            var filter = Builders<Device>.Filter.Eq(d => d.IsDeleted, false);

            // Lọc theo loại thiết bị (Camera / Controller)
            if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<DeviceType>(type, true, out var parsedType))
            {
                filter &= Builders<Device>.Filter.Eq(d => d.Type, parsedType);
            }

            // Tìm kiếm theo mã thiết bị, tên hoặc địa chỉ IP
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                filter &= (Builders<Device>.Filter.Regex(d => d.Name, new MongoDB.Bson.BsonRegularExpression(s, "i"))
                         | Builders<Device>.Filter.Regex(d => d.Code, new MongoDB.Bson.BsonRegularExpression(s, "i"))
                         | Builders<Device>.Filter.Regex(d => d.IpAddress, new MongoDB.Bson.BsonRegularExpression(s, "i")));
            }

            var totalCount = (int)await _deviceRepo.CountAsync(filter);
            var skip = (pageNumber - 1) * pageSize;
            var items = await _deviceRepo.FindAsync(filter, null, skip, pageSize);

            var result = new PagedResult<Device>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<Device>>.Ok(result, "Lấy danh sách thiết bị thành công."));
        }

        // GET api/devices/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var device = await _deviceRepo.GetByIdAsync(id);
            if (device == null || device.IsDeleted)
                return NotFound(ApiResponse.Fail("Không tìm thấy thiết bị."));

            return Ok(ApiResponse<Device>.Ok(device, "Lấy thông tin thiết bị thành công."));
        }

        // POST api/devices
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Device device)
        {
            if (string.IsNullOrWhiteSpace(device.Name))
                return BadRequest(ApiResponse.Fail("Tên thiết bị không được để trống."));

            if (string.IsNullOrWhiteSpace(device.IpAddress))
                return BadRequest(ApiResponse.Fail("Địa chỉ IP không được để trống."));

            await _deviceRepo.AddAsync(device);
            return Ok(ApiResponse<Device>.Ok(device, "Thêm thiết bị mới thành công."));
        }

        // PUT api/devices/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Device updated)
        {
            var existing = await _deviceRepo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted)
                return NotFound(ApiResponse.Fail("Không tìm thấy thiết bị."));

            existing.Code = updated.Code ?? existing.Code;
            existing.Name = updated.Name ?? existing.Name;
            existing.Type = updated.Type;
            existing.IpAddress = updated.IpAddress ?? existing.IpAddress;
            existing.Port = updated.Port > 0 ? updated.Port : existing.Port;
            existing.UserName = updated.UserName;
            existing.Password = updated.Password;
            existing.Note = updated.Note;
            existing.IsActive = updated.IsActive;
            existing.UpdatedAt = DateTime.Now;

            await _deviceRepo.UpdateAsync(existing);
            return Ok(ApiResponse<Device>.Ok(existing, "Cập nhật thiết bị thành công."));
        }

        // DELETE api/devices/{id}  — Xóa mềm (IRepository.DeleteAsync tự xử lý IsDeleted = true)
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var device = await _deviceRepo.GetByIdAsync(id);
            if (device == null || device.IsDeleted)
                return NotFound(ApiResponse.Fail("Không tìm thấy thiết bị."));

            await _deviceRepo.DeleteAsync(id);
            return Ok(ApiResponse.Ok("Xóa thiết bị thành công."));
        }

        // POST api/devices/delete-batch  — Xóa mềm hàng loạt
        [HttpPost("delete-batch")]
        public async Task<IActionResult> DeleteBatch([FromBody] List<string> ids)
        {
            if (ids == null || ids.Count == 0)
                return BadRequest(ApiResponse.Fail("Danh sách ID không được để trống."));

            foreach (var id in ids)
            {
                var device = await _deviceRepo.GetByIdAsync(id);
                if (device != null && !device.IsDeleted)
                {
                    await _deviceRepo.DeleteAsync(id);
                }
            }

            return Ok(ApiResponse.Ok($"Đã xóa {ids.Count} thiết bị thành công."));
        }

        // POST api/devices/batch  — Nhập hàng loạt từ Excel
        [HttpPost("batch")]
        public async Task<IActionResult> BatchCreate([FromBody] List<Device> devices)
        {
            if (devices == null || devices.Count == 0)
                return BadRequest(ApiResponse.Fail("Danh sách thiết bị không được để trống."));

            int createdCount = 0;
            foreach (var device in devices)
            {
                if (string.IsNullOrWhiteSpace(device.Name)) continue;
                await _deviceRepo.AddAsync(device);
                createdCount++;
            }

            return Ok(ApiResponse.Ok($"Đã nhập {createdCount} thiết bị thành công."));
        }
    }
}
