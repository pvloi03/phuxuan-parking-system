using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PhuXuanParkingSystem.Api.DTOs;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LanesController : ControllerBase
    {
        private readonly IRepository<Lane> _laneRepo;
        private readonly IRepository<Device> _deviceRepo;
        private readonly IRepository<LicenseInfo> _licenseRepo;

        public LanesController(
            IRepository<Lane> laneRepo,
            IRepository<Device> deviceRepo,
            IRepository<LicenseInfo> licenseRepo)
        {
            _laneRepo = laneRepo;
            _deviceRepo = deviceRepo;
            _licenseRepo = licenseRepo;
        }

        // GET api/lanes
        [HttpGet]
        public async Task<IActionResult> GetList(
            [FromQuery] string? search,
            [FromQuery] LaneDirection? direction,
            [FromQuery] bool? isActive)
        {
            var filter = Builders<Lane>.Filter.Eq(l => l.IsDeleted, false);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                filter &= (Builders<Lane>.Filter.Regex(l => l.Name, new MongoDB.Bson.BsonRegularExpression(s, "i")) |
                           Builders<Lane>.Filter.Regex(l => l.Code, new MongoDB.Bson.BsonRegularExpression(s, "i")) |
                           Builders<Lane>.Filter.Regex(l => l.Description, new MongoDB.Bson.BsonRegularExpression(s, "i")));
            }

            if (direction.HasValue)
            {
                filter &= Builders<Lane>.Filter.Eq(l => l.Direction, direction.Value);
            }

            if (isActive.HasValue)
            {
                filter &= Builders<Lane>.Filter.Eq(l => l.IsActive, isActive.Value);
            }

            var items = await _laneRepo.FindAsync(filter);
            await EnrichLaneDevicesAsync(items);

            return Ok(ApiResponse<IReadOnlyList<Lane>>.Ok(items ?? [], "Lấy danh sách làn kiểm soát thành công."));
        }

        // GET api/lanes/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var item = await _laneRepo.GetByIdAsync(id);
            if (item == null || item.IsDeleted)
                return NotFound(ApiResponse.Fail("Không tìm thấy làn kiểm soát."));

            var list = new List<Lane> { item };
            await EnrichLaneDevicesAsync(list);

            return Ok(ApiResponse<Lane>.Ok(item, "Lấy thông tin làn thành công."));
        }

        // POST api/lanes
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Lane lane)
        {
            if (string.IsNullOrWhiteSpace(lane.Name))
            {
                return BadRequest(ApiResponse.Fail("Tên làn không được để trống."));
            }

            if (string.IsNullOrWhiteSpace(lane.Code))
            {
                return BadRequest(ApiResponse.Fail("Mã làn không được để trống."));
            }

            // Kiểm tra giới hạn bản quyền MaxLanes
            var payload = await Helpers.LicenseValidationHelper.GetCurrentPayloadAsync(_licenseRepo);
            var existingLanes = await _laneRepo.GetAllAsync();
            int activeCount = existingLanes.Count(l => !l.IsDeleted);
            if (activeCount >= payload.MaxLanes)
            {
                return BadRequest(ApiResponse.Fail($"Số lượng làn xe đã đạt tối đa giới hạn bản quyền ({payload.MaxLanes} làn). Vui lòng liên hệ nhà cung cấp để nâng cấp gói bản quyền."));
            }

            lane.Code = lane.Code.Trim().ToUpperInvariant();
            var exists = await _laneRepo.FindOneAsync(l => l.Code == lane.Code && !l.IsDeleted);
            if (exists != null)
            {
                return BadRequest(ApiResponse.Fail($"Mã làn '{lane.Code}' đã tồn tại trong hệ thống."));
            }

            // Chuẩn hóa ID thiết bị
            lane.OverviewCameraDeviceId = string.IsNullOrWhiteSpace(lane.OverviewCameraDeviceId) ? null : lane.OverviewCameraDeviceId.Trim();
            lane.PlateCameraDeviceId = string.IsNullOrWhiteSpace(lane.PlateCameraDeviceId) ? null : lane.PlateCameraDeviceId.Trim();
            lane.ControllerDeviceId = string.IsNullOrWhiteSpace(lane.ControllerDeviceId) ? null : lane.ControllerDeviceId.Trim();
            lane.CreatedAt = DateTime.Now;
            lane.UpdatedAt = DateTime.Now;
            lane.IsDeleted = false;

            await _laneRepo.AddAsync(lane);
            return Ok(ApiResponse<Lane>.Ok(lane, "Thêm mới làn kiểm soát thành công."));
        }

        // PUT api/lanes/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] Lane lane)
        {
            var existing = await _laneRepo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted)
                return NotFound(ApiResponse.Fail("Không tìm thấy làn kiểm soát."));

            var cleanCode = (lane.Code ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(cleanCode))
            {
                return BadRequest(ApiResponse.Fail("Mã làn không được để trống."));
            }

            var duplicate = await _laneRepo.FindOneAsync(l => l.Code == cleanCode && l.Id != id && !l.IsDeleted);
            if (duplicate != null)
            {
                return BadRequest(ApiResponse.Fail($"Mã làn '{cleanCode}' đã thuộc về làn khác."));
            }

            existing.Code = cleanCode;
            existing.Name = lane.Name.Trim();
            existing.Direction = lane.Direction;
            existing.Description = lane.Description?.Trim();
            existing.IsActive = lane.IsActive;
            existing.OverviewCameraDeviceId = string.IsNullOrWhiteSpace(lane.OverviewCameraDeviceId) ? null : lane.OverviewCameraDeviceId.Trim();
            existing.PlateCameraDeviceId = string.IsNullOrWhiteSpace(lane.PlateCameraDeviceId) ? null : lane.PlateCameraDeviceId.Trim();
            existing.ControllerDeviceId = string.IsNullOrWhiteSpace(lane.ControllerDeviceId) ? null : lane.ControllerDeviceId.Trim();
            existing.TriggerAuxPort = lane.TriggerAuxPort > 0 ? lane.TriggerAuxPort : 1;
            existing.UpdatedAt = DateTime.Now;

            await _laneRepo.UpdateAsync(existing);
            return Ok(ApiResponse<Lane>.Ok(existing, "Cập nhật làn kiểm soát thành công."));
        }

        // DELETE api/lanes/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var existing = await _laneRepo.GetByIdAsync(id);
            if (existing == null || existing.IsDeleted)
                return NotFound(ApiResponse.Fail("Không tìm thấy làn kiểm soát."));

            await _laneRepo.DeleteAsync(id);
            return Ok(ApiResponse.Ok("Đã xóa làn kiểm soát thành công."));
        }

        // POST api/lanes/delete-batch
        [HttpPost("delete-batch")]
        public async Task<IActionResult> DeleteBatch([FromBody] List<string> ids)
        {
            if (ids == null || ids.Count == 0)
                return BadRequest(ApiResponse.Fail("Danh sách ID không được rỗng."));

            foreach (var id in ids)
            {
                var l = await _laneRepo.GetByIdAsync(id);
                if (l != null && !l.IsDeleted)
                {
                    await _laneRepo.DeleteAsync(id);
                }
            }
            return Ok(ApiResponse.Ok($"Đã xóa thành công {ids.Count} làn kiểm soát."));
        }

        private async Task EnrichLaneDevicesAsync(IReadOnlyList<Lane>? list)
        {
            if (list == null || list.Count == 0) return;

            var deviceIds = new HashSet<string>();
            foreach (var lane in list)
            {
                if (!string.IsNullOrEmpty(lane.OverviewCameraDeviceId)) deviceIds.Add(lane.OverviewCameraDeviceId);
                if (!string.IsNullOrEmpty(lane.PlateCameraDeviceId)) deviceIds.Add(lane.PlateCameraDeviceId);
                if (!string.IsNullOrEmpty(lane.ControllerDeviceId)) deviceIds.Add(lane.ControllerDeviceId);
            }

            if (deviceIds.Count == 0) return;

            var devices = await _deviceRepo.FindAsync(Builders<Device>.Filter.In(d => d.Id, deviceIds));
            var deviceMap = devices.ToDictionary(d => d.Id, d => d);

            foreach (var lane in list)
            {
                if (!string.IsNullOrEmpty(lane.OverviewCameraDeviceId) && deviceMap.TryGetValue(lane.OverviewCameraDeviceId, out var ovCam))
                    lane.OverviewCamera = ovCam;

                if (!string.IsNullOrEmpty(lane.PlateCameraDeviceId) && deviceMap.TryGetValue(lane.PlateCameraDeviceId, out var plCam))
                    lane.PlateCamera = plCam;

                if (!string.IsNullOrEmpty(lane.ControllerDeviceId) && deviceMap.TryGetValue(lane.ControllerDeviceId, out var ctrl))
                    lane.Controller = ctrl;
            }
        }
    }
}
