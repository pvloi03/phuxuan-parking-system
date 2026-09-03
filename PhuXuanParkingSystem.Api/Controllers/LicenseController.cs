using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhuXuanParkingSystem.Api.Helpers;
using PhuXuanParkingSystem.Api.Services;
using PhuXuanParkingSystem.Licensing;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;

namespace PhuXuanParkingSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LicenseController : ControllerBase
    {
        private readonly IRepository<LicenseInfo> _licenseRepo;
        private readonly IRepository<Lane> _laneRepo;
        private readonly IRepository<Device> _deviceRepo;
        private readonly IAuditLogQueue _auditQueue;

        private static readonly string LicenseFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PhuXuanParkingSystem",
            "license.lic"
        );

        public LicenseController(
            IRepository<LicenseInfo> licenseRepo,
            IRepository<Lane> laneRepo,
            IRepository<Device> deviceRepo,
            IAuditLogQueue auditQueue)
        {
            _licenseRepo = licenseRepo;
            _laneRepo = laneRepo;
            _deviceRepo = deviceRepo;
            _auditQueue = auditQueue;
        }

        /// <summary>
        /// Lấy trạng thái Bản quyền và Quota hiện tại của hệ thống
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetLicenseStatus()
        {
            string currentMachineCode = HardwareFingerprint.GetMachineCode();
            var (licenseKey, licenseEntity) = await GetActiveLicenseAsync();

            var validation = !string.IsNullOrWhiteSpace(licenseKey)
                ? LicenseCrypto.ValidateLicense(licenseKey!, null, currentMachineCode)
                : new LicenseValidationResult
                {
                    IsValid = false,
                    Message = "Hệ thống chưa được kích hoạt bản quyền."
                };

            // Đếm số lượng thực tế trong cơ sở dữ liệu
            var allLanes = await _laneRepo.GetAllAsync();
            int currentLanes = allLanes.Count(l => !l.IsDeleted);

            var allDevices = await _deviceRepo.GetAllAsync();
            int currentCameras = allDevices.Count(d => !d.IsDeleted && (d.Type == DeviceType.PlateCamera || d.Type == DeviceType.OverviewCamera));
            int currentControllers = allDevices.Count(d => !d.IsDeleted && d.Type == DeviceType.Controller);

            int maxLanes = validation.Payload?.MaxLanes ?? 2;
            int maxCameras = validation.Payload?.MaxCameras ?? 4;
            int maxControllers = validation.Payload?.MaxControllers ?? 1;

            return Ok(new
            {
                machineCode = currentMachineCode,
                isValid = validation.IsValid,
                isExpired = validation.IsExpired,
                isPermanent = validation.Payload?.IsPermanent ?? false,
                daysRemaining = validation.DaysRemaining,
                customerName = validation.Payload?.CustomerName ?? licenseEntity?.CustomerName ?? "Chưa kích hoạt",
                expiryDate = validation.Payload?.ExpiryDate ?? licenseEntity?.ExpiryDate,
                issuedAt = validation.Payload?.IssuedAt ?? licenseEntity?.IssuedAt,
                message = validation.Message,
                quota = new
                {
                    maxLanes,
                    currentLanes,
                    isLanesLimitReached = currentLanes >= maxLanes,
                    
                    maxCameras,
                    currentCameras,
                    isCamerasLimitReached = currentCameras >= maxCameras,

                    maxControllers,
                    currentControllers,
                    isControllersLimitReached = currentControllers >= maxControllers
                },
                features = validation.Payload?.Features ?? licenseEntity?.Features ?? new()
            });
        }

        /// <summary>
        /// Lấy Mã Máy Tính (Machine Code) của Server / Trạm
        /// </summary>
        [HttpGet("machine-code")]
        public IActionResult GetMachineCode()
        {
            return Ok(new { machineCode = HardwareFingerprint.GetMachineCode() });
        }

        public class ActivateLicenseRequest
        {
            public string LicenseKey { get; set; } = string.Empty;
        }

        /// <summary>
        /// Kích hoạt bản quyền qua chuỗi License Key
        /// </summary>
        [HttpPost("activate")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Activate([FromBody] ActivateLicenseRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.LicenseKey))
            {
                return BadRequest(new { message = "Vui lòng cung cấp chuỗi License Key." });
            }

            string currentMachineCode = HardwareFingerprint.GetMachineCode();
            var validation = LicenseCrypto.ValidateLicense(request.LicenseKey.Trim(), null, currentMachineCode);

            if (!validation.IsValid || validation.Payload == null)
            {
                return BadRequest(new { message = "Kích hoạt thất bại: " + validation.Message });
            }

            var payload = validation.Payload;

            // 1. Lưu file cục bộ
            try
            {
                string dir = Path.GetDirectoryName(LicenseFilePath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                await System.IO.File.WriteAllTextAsync(LicenseFilePath, request.LicenseKey.Trim());
            }
            catch { }

            // 2. Vô hiệu hóa các license cũ trong DB
            var existingLicenses = await _licenseRepo.GetAllAsync();
            foreach (var lic in existingLicenses)
            {
                lic.IsActive = false;
                await _licenseRepo.UpdateAsync(lic);
            }

            // 3. Thêm bản ghi License mới
            var newEntity = new LicenseInfo(
                customerName: payload.CustomerName,
                machineCode: payload.MachineCode,
                expiryDate: payload.ExpiryDate,
                licenseKey: request.LicenseKey.Trim(),
                maxLanes: payload.MaxLanes,
                maxCameras: payload.MaxCameras,
                maxControllers: payload.MaxControllers,
                features: payload.Features
            );

            await _licenseRepo.AddAsync(newEntity);

            // Ghi nhận AuditLog LicenseUpdate
            await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.LicenseUpdate, "LicenseInfo", newEntity.Id, payload.CustomerName, reason: "Kích hoạt bản quyền phần mềm mới");

            return Ok(new
            {
                success = true,
                message = $"Kích hoạt bản quyền thành công cho '{payload.CustomerName}'!",
                customerName = payload.CustomerName,
                expiryDate = payload.ExpiryDate,
                daysRemaining = validation.DaysRemaining,
                isPermanent = payload.IsPermanent,
                maxLanes = payload.MaxLanes,
                maxCameras = payload.MaxCameras,
                maxControllers = payload.MaxControllers
            });
        }

        /// <summary>
        /// Kích hoạt bản quyền bằng upload File .lic
        /// </summary>
        [HttpPost("upload-lic")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> UploadLicenseFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Vui lòng chọn file bản quyền (.lic)." });
            }

            string content;
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                content = await reader.ReadToEndAsync();
            }

            return await Activate(new ActivateLicenseRequest { LicenseKey = content });
        }

        private async Task<(string? LicenseKey, LicenseInfo? Entity)> GetActiveLicenseAsync()
        {
            // 1. Kiểm tra trong MongoDB
            try
            {
                var all = await _licenseRepo.GetAllAsync();
                var active = all.Where(l => l.IsActive && !l.IsDeleted).OrderByDescending(l => l.CreatedAt).FirstOrDefault();
                if (active != null && !string.IsNullOrWhiteSpace(active.LicenseKey))
                {
                    return (active.LicenseKey, active);
                }
            }
            catch { }

            // 2. Kiểm tra file cục bộ
            if (System.IO.File.Exists(LicenseFilePath))
            {
                try
                {
                    string key = await System.IO.File.ReadAllTextAsync(LicenseFilePath);
                    if (!string.IsNullOrWhiteSpace(key))
                        return (key.Trim(), null);
                }
                catch { }
            }

            return (null, null);
        }
    }
}
