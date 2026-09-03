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
    public class VehiclesController : ControllerBase
    {
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IAuditLogQueue _auditQueue;

        public VehiclesController(IRepository<Vehicle> vehicleRepo, IAuditLogQueue auditQueue)
        {
            _vehicleRepo = vehicleRepo;
            _auditQueue = auditQueue;
        }

        // GET api/vehicles?search=&type=&pageNumber=1&pageSize=15
        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] string? search,
            [FromQuery] string? type,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 15)
        {
            var filter = Builders<Vehicle>.Filter.Eq(v => v.IsDeleted, false);

            if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<VehicleType>(type, true, out var parsedType))
            {
                filter &= Builders<Vehicle>.Filter.Eq(v => v.Type, parsedType);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                filter &= Builders<Vehicle>.Filter.Regex(v => v.PlateNumber, new MongoDB.Bson.BsonRegularExpression(s, "i"));
            }

            var totalCount = (int)await _vehicleRepo.CountAsync(filter);
            var skip = (pageNumber - 1) * pageSize;
            var items = await _vehicleRepo.FindAsync(filter, Builders<Vehicle>.Sort.Descending(v => v.CreatedAt), skip, pageSize);

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

            var cleanPlate = Models.ValueObjects.PlateNumber.Clean(vehicle.PlateNumber);
            vehicle.PlateNumber = cleanPlate;

            var exists = await _vehicleRepo.FindOneAsync(v => v.PlateNumber == cleanPlate && !v.IsDeleted);
            if (exists != null)
            {
                return BadRequest(ApiResponse.Fail($"Biển số '{cleanPlate}' đã tồn tại trong hệ thống."));
            }

            // Chuẩn hóa OwnerPersonId (nếu rỗng thì thành null)
            if (string.IsNullOrWhiteSpace(vehicle.OwnerPersonId))
            {
                vehicle.OwnerPersonId = null;
            }
            else
            {
                vehicle.OwnerPersonId = vehicle.OwnerPersonId.Trim();
            }

            await _vehicleRepo.AddAsync(vehicle);

            // Ghi nhận AuditLog Create
            var diff = AuditDiffHelper.ComputeDiff<Vehicle>(null, vehicle);
            await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.Create, "Vehicle", vehicle.Id, vehicle.PlateNumber, diff);

            return Ok(ApiResponse<Vehicle>.Ok(vehicle, "Thêm phương tiện thành công."));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Vehicle vehicle)
        {
            var existing = await _vehicleRepo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy phương tiện."));

            var cleanPlate = Models.ValueObjects.PlateNumber.Clean(vehicle.PlateNumber);
            if (string.IsNullOrWhiteSpace(cleanPlate))
            {
                return BadRequest(ApiResponse.Fail("Biển số xe không hợp lệ."));
            }

            var duplicate = await _vehicleRepo.FindOneAsync(v => v.PlateNumber == cleanPlate && v.Id != id && !v.IsDeleted);
            if (duplicate != null)
            {
                return BadRequest(ApiResponse.Fail($"Biển số '{cleanPlate}' đã thuộc về một phương tiện khác."));
            }

            // Snapshot trạng thái cũ trước khi cập nhật
            var snapshot = AuditDiffHelper.TakeSnapshot(existing);

            existing.PlateNumber = cleanPlate;
            existing.Type = vehicle.Type;
            existing.OwnerPersonId = string.IsNullOrWhiteSpace(vehicle.OwnerPersonId) ? null : vehicle.OwnerPersonId.Trim();
            existing.IsActive = vehicle.IsActive;
            existing.UpdatedAt = DateTime.Now;

            await _vehicleRepo.UpdateAsync(existing);

            // Ghi nhận AuditLog Update nếu có thay đổi
            var diff = AuditDiffHelper.ComputeDiffFromSnapshot(snapshot, existing);
            if (diff.HasChanges)
            {
                await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.Update, "Vehicle", existing.Id, existing.PlateNumber, diff);
            }

            return Ok(ApiResponse<Vehicle>.Ok(existing, "Cập nhật phương tiện thành công."));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, [FromQuery] string? reason = null)
        {
            var existing = await _vehicleRepo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy phương tiện."));

            await _vehicleRepo.DeleteAsync(id);

            // Ghi nhận AuditLog Delete
            var diff = AuditDiffHelper.ComputeDiff<Vehicle>(existing, null);
            await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.Delete, "Vehicle", existing.Id, existing.PlateNumber, diff, reason: reason);

            return Ok(ApiResponse.Ok("Đã xóa phương tiện thành công."));
        }

        [HttpPost("delete-batch")]
        public async Task<IActionResult> DeleteBatch([FromBody] List<string> ids, [FromQuery] string? reason = null)
        {
            if (ids == null || ids.Count == 0) return BadRequest(ApiResponse.Fail("Danh sách ID không được rỗng."));

            foreach (var id in ids)
            {
                var v = await _vehicleRepo.GetByIdAsync(id);
                if (v != null && !v.IsDeleted)
                {
                    await _vehicleRepo.DeleteAsync(id);

                    var diff = AuditDiffHelper.ComputeDiff<Vehicle>(v, null);
                    await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.Delete, "Vehicle", v.Id, v.PlateNumber, diff, reason: reason);
                }
            }
            return Ok(ApiResponse.Ok($"Đã xóa thành công {ids.Count} phương tiện."));
        }

        [HttpPost("batch")]
        public async Task<IActionResult> BatchImport([FromBody] List<Vehicle> vehicles)
        {
            if (vehicles == null || vehicles.Count == 0) return BadRequest(ApiResponse.Fail("Danh sách phương tiện không được rỗng."));

            int addedCount = 0;

            foreach (var v in vehicles)
            {
                if (string.IsNullOrWhiteSpace(v.PlateNumber)) continue;
                var cleanPlate = Models.ValueObjects.PlateNumber.Clean(v.PlateNumber);

                var exists = await _vehicleRepo.FindOneAsync(x => x.PlateNumber == cleanPlate && !x.IsDeleted);
                if (exists == null)
                {
                    v.PlateNumber = cleanPlate;
                    if (string.IsNullOrWhiteSpace(v.OwnerPersonId)) v.OwnerPersonId = null;
                    await _vehicleRepo.AddAsync(v);
                    addedCount++;

                    var diff = AuditDiffHelper.ComputeDiff<Vehicle>(null, v);
                    await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.Create, "Vehicle", v.Id, v.PlateNumber, diff);
                }
            }

            return Ok(ApiResponse.Ok($"Đã nhập thành công {addedCount}/{vehicles.Count} phương tiện mới."));
        }
    }
}
