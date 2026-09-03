using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhuXuanParkingSystem.Api.DTOs;
using PhuXuanParkingSystem.Api.Helpers;
using PhuXuanParkingSystem.Api.Services;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DevicesController : ControllerBase
    {
        private readonly IRepository<Device> _deviceRepo;
        private readonly IRepository<LicenseInfo> _licenseRepo;
        private readonly IAuditLogQueue _auditQueue;

        public DevicesController(
            IRepository<Device> deviceRepo,
            IRepository<LicenseInfo> licenseRepo,
            IAuditLogQueue auditQueue)
        {
            _deviceRepo = deviceRepo;
            _licenseRepo = licenseRepo;
            _auditQueue = auditQueue;
        }

        // GET api/devices
        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] string? search,
            [FromQuery] DeviceType? type,
            [FromQuery] bool? isActive,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var filter = Builders<Device>.Filter.Eq(d => d.IsDeleted, false);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                filter &= (Builders<Device>.Filter.Regex(d => d.Name, new MongoDB.Bson.BsonRegularExpression(s, "i")) |
                           Builders<Device>.Filter.Regex(d => d.Code, new MongoDB.Bson.BsonRegularExpression(s, "i")) |
                           Builders<Device>.Filter.Regex(d => d.IpAddress, new MongoDB.Bson.BsonRegularExpression(s, "i")));
            }

            if (type.HasValue)
            {
                filter &= Builders<Device>.Filter.Eq(d => d.Type, type.Value);
            }

            if (isActive.HasValue)
            {
                filter &= Builders<Device>.Filter.Eq(d => d.IsActive, isActive.Value);
            }

            var totalCount = (int)await _deviceRepo.CountAsync(filter);
            var skip = (page - 1) * pageSize;
            var items = await _deviceRepo.FindAsync(filter, Builders<Device>.Sort.Descending(d => d.CreatedAt), skip, pageSize);

            var result = new PagedResult<Device>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = page,
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

            // Kiểm tra giới hạn bản quyền MaxCameras / MaxControllers
            var payload = await Helpers.LicenseValidationHelper.GetCurrentPayloadAsync(_licenseRepo);
            var allDevices = await _deviceRepo.GetAllAsync();

            if (device.Type == DeviceType.PlateCamera || device.Type == DeviceType.OverviewCamera)
            {
                int cameraCount = allDevices.Count(d => !d.IsDeleted && (d.Type == DeviceType.PlateCamera || d.Type == DeviceType.OverviewCamera));
                if (cameraCount >= payload.MaxCameras)
                {
                    return BadRequest(ApiResponse.Fail($"Số lượng Camera đã đạt tối đa giới hạn bản quyền ({payload.MaxCameras} camera). Vui lòng liên hệ nhà cung cấp để nâng cấp gói bản quyền."));
                }
            }
            else if (device.Type == DeviceType.Controller)
            {
                int controllerCount = allDevices.Count(d => !d.IsDeleted && d.Type == DeviceType.Controller);
                if (controllerCount >= payload.MaxControllers)
                {
                    return BadRequest(ApiResponse.Fail($"Số lượng Bộ điều khiển đã đạt tối đa giới hạn bản quyền ({payload.MaxControllers} bộ). Vui lòng liên hệ nhà cung cấp để nâng cấp gói bản quyền."));
                }
            }

            await _deviceRepo.AddAsync(device);

            var diff = AuditDiffHelper.ComputeDiff<Device>(null, device);
            await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.Create, "Device", device.Id, device.Name, diff);

            return Ok(ApiResponse<Device>.Ok(device, "Thêm thiết bị mới thành công."));
        }

        // PUT api/devices/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Device updated)
        {
            var existing = await _deviceRepo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted)
                return NotFound(ApiResponse.Fail("Không tìm thấy thiết bị."));

            var snapshot = AuditDiffHelper.TakeSnapshot(existing);

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

            var diff = AuditDiffHelper.ComputeDiffFromSnapshot(snapshot, existing);
            if (diff.HasChanges)
            {
                await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.Update, "Device", existing.Id, existing.Name, diff);
            }

            return Ok(ApiResponse<Device>.Ok(existing, "Cập nhật thiết bị thành công."));
        }

        // DELETE api/devices/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id, [FromQuery] string? reason = null)
        {
            var device = await _deviceRepo.GetByIdAsync(id);
            if (device == null || device.IsDeleted)
                return NotFound(ApiResponse.Fail("Không tìm thấy thiết bị."));

            await _deviceRepo.DeleteAsync(id);

            var diff = AuditDiffHelper.ComputeDiff<Device>(device, null);
            await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.Delete, "Device", device.Id, device.Name, diff, reason: reason);

            return Ok(ApiResponse.Ok("Xóa thiết bị thành công."));
        }

        // POST api/devices/delete-batch
        [HttpPost("delete-batch")]
        public async Task<IActionResult> DeleteBatch([FromBody] List<string> ids, [FromQuery] string? reason = null)
        {
            if (ids == null || ids.Count == 0)
                return BadRequest(ApiResponse.Fail("Danh sách ID không được để trống."));

            foreach (var id in ids)
            {
                var device = await _deviceRepo.GetByIdAsync(id);
                if (device != null && !device.IsDeleted)
                {
                    await _deviceRepo.DeleteAsync(id);
                    var diff = AuditDiffHelper.ComputeDiff<Device>(device, null);
                    await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.Delete, "Device", device.Id, device.Name, diff, reason: reason);
                }
            }

            return Ok(ApiResponse.Ok($"Đã xóa {ids.Count} thiết bị thành công."));
        }

        // POST api/devices/batch
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

                var diff = AuditDiffHelper.ComputeDiff<Device>(null, device);
                await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.Create, "Device", device.Id, device.Name, diff);
            }

            return Ok(ApiResponse.Ok($"Đã nhập {createdCount} thiết bị thành công."));
        }
    }
}
