using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using PhuXuanParkingSystem.Api.Helpers;
using PhuXuanParkingSystem.Api.Services;
using PhuXuanParkingSystem.Models.Common;
using PhuXuanParkingSystem.Models.Data;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Api.Controllers
{
    [ApiController]
    [Route("api/recycle-bin")]
    public class RecycleBinController : ControllerBase
    {
        private readonly MongoDbContext _context;
        private readonly IAuditLogQueue _auditQueue;

        public RecycleBinController(MongoDbContext context, IAuditLogQueue auditQueue)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _auditQueue = auditQueue ?? throw new ArgumentNullException(nameof(auditQueue));
        }

        public class RecycleBinItemDto
        {
            public string Id { get; set; } = string.Empty;
            public string ItemType { get; set; } = string.Empty; // Vehicle, Person, Contractor, Department, Company, Device, Lane, ParkingSession
            public string ItemTypeLabel { get; set; } = string.Empty;
            public string Identifier { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public DateTime? DeletedAt { get; set; }
            public DateTime CreatedAt { get; set; }
            public bool CanRestore { get; set; } = true;
            public string? WarningMessage { get; set; }
        }

        public class RecycleBinCountsDto
        {
            public int TotalCount { get; set; }
            public int VehicleCount { get; set; }
            public int PersonCount { get; set; }
            public int ContractorCount { get; set; }
            public int DepartmentCount { get; set; }
            public int CompanyCount { get; set; }
            public int DeviceCount { get; set; }
            public int LaneCount { get; set; }
            public int ParkingSessionCount { get; set; }
            public int UserCount { get; set; }
        }

        public class RestoreItemRequest
        {
            public string ItemType { get; set; } = string.Empty;
            public string Id { get; set; } = string.Empty;
        }

        public class BatchActionRequest
        {
            public List<RestoreItemRequest> Items { get; set; } = new();
        }

        /// <summary>
        /// Lấy số lượng mục trong thùng rác theo từng phân hệ
        /// </summary>
        [HttpGet("counts")]
        public async Task<IActionResult> GetCounts()
        {
            var isDeletedFilter = Builders<Vehicle>.Filter.Eq(x => x.IsDeleted, true);
            var personFilter = Builders<Person>.Filter.Eq(x => x.IsDeleted, true);
            var contractorFilter = Builders<Contractor>.Filter.Eq(x => x.IsDeleted, true);
            var departmentFilter = Builders<Department>.Filter.Eq(x => x.IsDeleted, true);
            var companyFilter = Builders<Company>.Filter.Eq(x => x.IsDeleted, true);
            var deviceFilter = Builders<Device>.Filter.Eq(x => x.IsDeleted, true);
            var laneFilter = Builders<Lane>.Filter.Eq(x => x.IsDeleted, true);
            var sessionFilter = Builders<ParkingSession>.Filter.Eq(x => x.IsDeleted, true);
            var userFilter = Builders<User>.Filter.Eq(x => x.IsDeleted, true);

            var vehicleTask = _context.GetCollection<Vehicle>().CountDocumentsAsync(isDeletedFilter);
            var personTask = _context.GetCollection<Person>().CountDocumentsAsync(personFilter);
            var contractorTask = _context.GetCollection<Contractor>().CountDocumentsAsync(contractorFilter);
            var departmentTask = _context.GetCollection<Department>().CountDocumentsAsync(departmentFilter);
            var companyTask = _context.GetCollection<Company>().CountDocumentsAsync(companyFilter);
            var deviceTask = _context.GetCollection<Device>().CountDocumentsAsync(deviceFilter);
            var laneTask = _context.GetCollection<Lane>().CountDocumentsAsync(laneFilter);
            var sessionTask = _context.GetCollection<ParkingSession>().CountDocumentsAsync(sessionFilter);
            var userTask = _context.GetCollection<User>().CountDocumentsAsync(userFilter);

            await Task.WhenAll(vehicleTask, personTask, contractorTask, departmentTask, companyTask, deviceTask, laneTask, sessionTask, userTask);

            var counts = new RecycleBinCountsDto
            {
                VehicleCount = (int)vehicleTask.Result,
                PersonCount = (int)personTask.Result,
                ContractorCount = (int)contractorTask.Result,
                DepartmentCount = (int)departmentTask.Result,
                CompanyCount = (int)companyTask.Result,
                DeviceCount = (int)deviceTask.Result,
                LaneCount = (int)laneTask.Result,
                ParkingSessionCount = (int)sessionTask.Result,
                UserCount = (int)userTask.Result,
            };

            counts.TotalCount = counts.VehicleCount + counts.PersonCount + counts.ContractorCount +
                                counts.DepartmentCount + counts.CompanyCount + counts.DeviceCount +
                                counts.LaneCount + counts.ParkingSessionCount + counts.UserCount;

            return Ok(new { success = true, data = counts });
        }

        /// <summary>
        /// Lấy danh sách các mục trong thùng rác (hỗ trợ phân trang, lọc theo itemType, tìm kiếm từ khóa)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetItems(
            [FromQuery] string? itemType = null,
            [FromQuery] string? search = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var allItems = new List<RecycleBinItemDto>();
            var s = search?.Trim().ToLowerInvariant();

            // Load lookup maps để kiểm tra quan hệ ràng buộc
            var allCompanies = await _context.GetCollection<Company>().Find(FilterDefinition<Company>.Empty).ToListAsync();
            var allDepts = await _context.GetCollection<Department>().Find(FilterDefinition<Department>.Empty).ToListAsync();
            var allPeople = await _context.GetCollection<Person>().Find(FilterDefinition<Person>.Empty).ToListAsync();
            var allContractors = await _context.GetCollection<Contractor>().Find(FilterDefinition<Contractor>.Empty).ToListAsync();
            var allLanes = await _context.GetCollection<Lane>().Find(FilterDefinition<Lane>.Empty).ToListAsync();

            var compMap = allCompanies.ToDictionary(x => x.Id, x => x);
            var deptMap = allDepts.ToDictionary(x => x.Id, x => x);
            var personMap = allPeople.ToDictionary(x => x.Id, x => x);
            var contractorMap = allContractors.ToDictionary(x => x.Id, x => x);

            // 1. VEHICLES
            if (string.IsNullOrWhiteSpace(itemType) || itemType.Equals("Vehicle", StringComparison.OrdinalIgnoreCase))
            {
                var filter = Builders<Vehicle>.Filter.Eq(x => x.IsDeleted, true);
                var vehicles = await _context.GetCollection<Vehicle>().Find(filter).ToListAsync();

                foreach (var v in vehicles)
                {
                    string? warning = null;
                    string desc = $"Loại xe: {v.Type}";
                    if (!string.IsNullOrWhiteSpace(v.OwnerPersonId))
                    {
                        if (personMap.TryGetValue(v.OwnerPersonId, out var owner))
                        {
                            desc += $" • Chủ xe: {owner.FullName} ({owner.Code})";
                            if (owner.IsDeleted)
                            {
                                warning = $"Chủ xe [{owner.FullName}] hiện cũng đang nằm trong thùng rác.";
                            }
                        }
                    }

                    allItems.Add(new RecycleBinItemDto
                    {
                        Id = v.Id,
                        ItemType = "Vehicle",
                        ItemTypeLabel = "Phương Tiện",
                        Identifier = v.PlateNumber,
                        Title = v.PlateNumber,
                        Description = desc,
                        DeletedAt = v.DeletedAt,
                        CreatedAt = v.CreatedAt,
                        WarningMessage = warning
                    });
                }
            }

            // 2. PEOPLE
            if (string.IsNullOrWhiteSpace(itemType) || itemType.Equals("Person", StringComparison.OrdinalIgnoreCase))
            {
                var filter = Builders<Person>.Filter.Eq(x => x.IsDeleted, true);
                var people = await _context.GetCollection<Person>().Find(filter).ToListAsync();

                foreach (var p in people)
                {
                    string? warning = null;
                    var parts = new List<string>();
                    if (!string.IsNullOrWhiteSpace(p.DepartmentId) && deptMap.TryGetValue(p.DepartmentId, out var d))
                    {
                        parts.Add($"Phòng: {d.Name}");
                        if (d.IsDeleted) warning = $"Phòng ban [{d.Name}] hiện đang nằm trong thùng rác.";
                    }
                    if (!string.IsNullOrWhiteSpace(p.ContractorId) && contractorMap.TryGetValue(p.ContractorId, out var c))
                    {
                        parts.Add($"Nhà thầu: {c.Name}");
                        if (c.IsDeleted) warning = $"Nhà thầu [{c.Name}] hiện đang nằm trong thùng rác.";
                    }
                    if (!string.IsNullOrWhiteSpace(p.PhoneNumber)) parts.Add($"SĐT: {p.PhoneNumber}");

                    allItems.Add(new RecycleBinItemDto
                    {
                        Id = p.Id,
                        ItemType = "Person",
                        ItemTypeLabel = "Nhân Sự",
                        Identifier = p.Code ?? "--",
                        Title = p.FullName,
                        Description = string.Join(" • ", parts),
                        DeletedAt = p.DeletedAt,
                        CreatedAt = p.CreatedAt,
                        WarningMessage = warning
                    });
                }
            }

            // 3. CONTRACTORS
            if (string.IsNullOrWhiteSpace(itemType) || itemType.Equals("Contractor", StringComparison.OrdinalIgnoreCase))
            {
                var filter = Builders<Contractor>.Filter.Eq(x => x.IsDeleted, true);
                var contractors = await _context.GetCollection<Contractor>().Find(filter).ToListAsync();

                foreach (var c in contractors)
                {
                    allItems.Add(new RecycleBinItemDto
                    {
                        Id = c.Id,
                        ItemType = "Contractor",
                        ItemTypeLabel = "Đối Tác / Nhà Thầu",
                        Identifier = c.Code ?? "--",
                        Title = c.Name,
                        Description = $"Người liên hệ: {c.ContactPerson ?? "--"} • SĐT: {c.PhoneNumber ?? "--"}",
                        DeletedAt = c.DeletedAt,
                        CreatedAt = c.CreatedAt
                    });
                }
            }

            // 4. DEPARTMENTS
            if (string.IsNullOrWhiteSpace(itemType) || itemType.Equals("Department", StringComparison.OrdinalIgnoreCase))
            {
                var filter = Builders<Department>.Filter.Eq(x => x.IsDeleted, true);
                var departments = await _context.GetCollection<Department>().Find(filter).ToListAsync();

                foreach (var d in departments)
                {
                    string? warning = null;
                    string desc = $"Mã: {d.Code ?? "--"}";
                    if (!string.IsNullOrWhiteSpace(d.CompanyId) && compMap.TryGetValue(d.CompanyId, out var comp))
                    {
                        desc += $" • Công ty: {comp.Name}";
                        if (comp.IsDeleted) warning = $"Công ty trực thuộc [{comp.Name}] hiện đang nằm trong thùng rác.";
                    }

                    allItems.Add(new RecycleBinItemDto
                    {
                        Id = d.Id,
                        ItemType = "Department",
                        ItemTypeLabel = "Phòng Ban",
                        Identifier = d.Code ?? "--",
                        Title = d.Name,
                        Description = desc,
                        DeletedAt = d.DeletedAt,
                        CreatedAt = d.CreatedAt,
                        WarningMessage = warning
                    });
                }
            }

            // 5. COMPANIES
            if (string.IsNullOrWhiteSpace(itemType) || itemType.Equals("Company", StringComparison.OrdinalIgnoreCase))
            {
                var filter = Builders<Company>.Filter.Eq(x => x.IsDeleted, true);
                var companies = await _context.GetCollection<Company>().Find(filter).ToListAsync();

                foreach (var comp in companies)
                {
                    allItems.Add(new RecycleBinItemDto
                    {
                        Id = comp.Id,
                        ItemType = "Company",
                        ItemTypeLabel = "Công Ty",
                        Identifier = comp.Code ?? "--",
                        Title = comp.Name,
                        Description = $"Mã: {comp.Code ?? "--"} • SĐT: {comp.PhoneNumber ?? "--"}",
                        DeletedAt = comp.DeletedAt,
                        CreatedAt = comp.CreatedAt
                    });
                }
            }

            // 6. DEVICES
            if (string.IsNullOrWhiteSpace(itemType) || itemType.Equals("Device", StringComparison.OrdinalIgnoreCase))
            {
                var filter = Builders<Device>.Filter.Eq(x => x.IsDeleted, true);
                var devices = await _context.GetCollection<Device>().Find(filter).ToListAsync();

                foreach (var dev in devices)
                {
                    allItems.Add(new RecycleBinItemDto
                    {
                        Id = dev.Id,
                        ItemType = "Device",
                        ItemTypeLabel = "Thiết Bị",
                        Identifier = dev.IpAddress ?? "--",
                        Title = dev.Name,
                        Description = $"Loại: {dev.Type} • IP: {dev.IpAddress}:{dev.Port}",
                        DeletedAt = dev.DeletedAt,
                        CreatedAt = dev.CreatedAt
                    });
                }
            }

            // 7. LANES
            if (string.IsNullOrWhiteSpace(itemType) || itemType.Equals("Lane", StringComparison.OrdinalIgnoreCase))
            {
                var filter = Builders<Lane>.Filter.Eq(x => x.IsDeleted, true);
                var lanes = await _context.GetCollection<Lane>().Find(filter).ToListAsync();

                foreach (var l in lanes)
                {
                    allItems.Add(new RecycleBinItemDto
                    {
                        Id = l.Id,
                        ItemType = "Lane",
                        ItemTypeLabel = "Làn Kiểm Soát",
                        Identifier = l.Code ?? "--",
                        Title = l.Name,
                        Description = $"Mã: {l.Code ?? "--"} • Hướng: {(l.Direction == PhuXuanParkingSystem.Models.Enums.LaneDirection.In ? "Vào" : "Ra")}",
                        DeletedAt = l.DeletedAt,
                        CreatedAt = l.CreatedAt
                    });
                }
            }

            // 8. PARKING SESSIONS
            if (string.IsNullOrWhiteSpace(itemType) || itemType.Equals("ParkingSession", StringComparison.OrdinalIgnoreCase))
            {
                var filter = Builders<ParkingSession>.Filter.Eq(x => x.IsDeleted, true);
                var sessions = await _context.GetCollection<ParkingSession>().Find(filter).ToListAsync();

                foreach (var ps in sessions)
                {
                    allItems.Add(new RecycleBinItemDto
                    {
                        Id = ps.Id,
                        ItemType = "ParkingSession",
                        ItemTypeLabel = "Lượt Gửi Xe",
                        Identifier = ps.PlateNumber ?? "--",
                        Title = $"Lượt xe: {ps.PlateNumber}",
                        Description = $"Thời gian vào: {ps.InTime:dd/MM/yyyy HH:mm} • Làn: {ps.InLaneName ?? "--"}",
                        DeletedAt = ps.DeletedAt,
                        CreatedAt = ps.CreatedAt
                    });
                }
            }

            // 9. USERS
            if (string.IsNullOrWhiteSpace(itemType) || itemType.Equals("User", StringComparison.OrdinalIgnoreCase))
            {
                var filter = Builders<User>.Filter.Eq(x => x.IsDeleted, true);
                var users = await _context.GetCollection<User>().Find(filter).ToListAsync();

                foreach (var u in users)
                {
                    allItems.Add(new RecycleBinItemDto
                    {
                        Id = u.Id,
                        ItemType = "User",
                        ItemTypeLabel = "Tài Khoản",
                        Identifier = u.Username,
                        Title = $"{u.FullName} (@{u.Username})",
                        Description = $"Vai trò: {u.Role} • Email: {u.Email ?? "--"}",
                        DeletedAt = u.DeletedAt,
                        CreatedAt = u.CreatedAt
                    });
                }
            }

            // Filter search
            if (!string.IsNullOrWhiteSpace(s))
            {
                allItems = allItems.Where(x =>
                    (x.Identifier != null && x.Identifier.ToLowerInvariant().Contains(s)) ||
                    (x.Title != null && x.Title.ToLowerInvariant().Contains(s)) ||
                    (x.Description != null && x.Description.ToLowerInvariant().Contains(s)) ||
                    (x.ItemTypeLabel != null && x.ItemTypeLabel.ToLowerInvariant().Contains(s))
                ).ToList();
            }

            // Sắp xếp thời điểm xóa mới nhất lên trước
            allItems = allItems.OrderByDescending(x => x.DeletedAt ?? x.CreatedAt).ToList();

            var totalCount = allItems.Count;
            var pagedItems = allItems.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return Ok(new
            {
                success = true,
                data = new
                {
                    items = pagedItems,
                    totalCount,
                    pageNumber,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }

        /// <summary>
        /// <summary>
        /// Khôi phục 1 mục đã xóa mềm
        /// </summary>
        [HttpPost("restore")]
        public async Task<IActionResult> RestoreItem([FromBody] RestoreItemRequest request, [FromQuery] string? reason = null)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Id) || string.IsNullOrWhiteSpace(request.ItemType))
            {
                return BadRequest(new { success = false, message = "Dữ liệu yêu cầu không hợp lệ." });
            }

            var (oldEntity, identifier) = await GetEntityWithIdentifierAsync(request.ItemType, request.Id);
            var (success, message) = await RestoreSingleInternalAsync(request.ItemType, request.Id);
            if (!success)
            {
                return BadRequest(new { success = false, message });
            }

            var (restoredEntity, _) = await GetEntityWithIdentifierAsync(request.ItemType, request.Id);
            var diff = AuditDiffHelper.ComputeDiff(oldEntity, restoredEntity);
            string effectiveReason = string.IsNullOrWhiteSpace(reason) ? "Khôi phục từ Thùng rác" : reason;
            await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.Restore, request.ItemType, request.Id, identifier, diff, reason: effectiveReason);

            return Ok(new { success = true, message = "Khôi phục dữ liệu thành công!" });
        }

        /// <summary>
        /// Khôi phục nhiều mục đã xóa mềm
        /// </summary>
        [HttpPost("restore-batch")]
        public async Task<IActionResult> RestoreBatch([FromBody] BatchActionRequest request, [FromQuery] string? reason = null)
        {
            if (request?.Items == null || request.Items.Count == 0)
            {
                return BadRequest(new { success = false, message = "Danh sách mục cần khôi phục rỗng." });
            }

            int restoredCount = 0;
            var errors = new List<string>();
            string effectiveReason = string.IsNullOrWhiteSpace(reason) ? "Khôi phục hàng loạt từ Thùng rác" : reason;

            foreach (var item in request.Items)
            {
                var (oldEntity, identifier) = await GetEntityWithIdentifierAsync(item.ItemType, item.Id);
                var (success, msg) = await RestoreSingleInternalAsync(item.ItemType, item.Id);
                if (success)
                {
                    restoredCount++;
                    var (restoredEntity, _) = await GetEntityWithIdentifierAsync(item.ItemType, item.Id);
                    var diff = AuditDiffHelper.ComputeDiff(oldEntity, restoredEntity);
                    await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.Restore, item.ItemType, item.Id, identifier, diff, reason: effectiveReason);
                }
                else errors.Add(msg);
            }

            return Ok(new
            {
                success = true,
                message = $"Đã khôi phục thành công {restoredCount}/{request.Items.Count} mục.",
                restoredCount,
                errors
            });
        }

        /// <summary>
        /// Xóa vĩnh viễn 1 mục khỏi CSDL (Hard Delete có kiểm tra ràng buộc quan hệ)
        /// </summary>
        [HttpDelete("hard-delete/{itemType}/{id}")]
        public async Task<IActionResult> HardDeleteItem(string itemType, string id, [FromQuery] string? reason = null)
        {
            if (string.IsNullOrWhiteSpace(itemType) || string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(new { success = false, message = "Thông số không hợp lệ." });
            }

            var (entity, identifier) = await GetEntityWithIdentifierAsync(itemType, id);
            var (success, message) = await HardDeleteSingleInternalAsync(itemType, id);
            if (!success)
            {
                return BadRequest(new { success = false, message });
            }

            var diff = AuditDiffHelper.ComputeDiff(entity, null);
            string effectiveReason = string.IsNullOrWhiteSpace(reason) ? "Xóa vĩnh viễn từ Thùng rác" : reason;
            await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.PermanentDelete, itemType, id, identifier, diff, reason: effectiveReason);

            return Ok(new { success = true, message = "Đã xóa vĩnh viễn dữ liệu khỏi CSDL." });
        }

        /// <summary>
        /// Xóa vĩnh viễn nhiều mục khỏi CSDL
        /// </summary>
        [HttpPost("hard-delete-batch")]
        public async Task<IActionResult> HardDeleteBatch([FromBody] BatchActionRequest request, [FromQuery] string? reason = null)
        {
            if (request?.Items == null || request.Items.Count == 0)
            {
                return BadRequest(new { success = false, message = "Danh sách mục cần xóa rỗng." });
            }

            int deletedCount = 0;
            var errors = new List<string>();
            string effectiveReason = string.IsNullOrWhiteSpace(reason) ? "Xóa vĩnh viễn từ Thùng rác" : reason;

            foreach (var item in request.Items)
            {
                var (entity, identifier) = await GetEntityWithIdentifierAsync(item.ItemType, item.Id);
                var (success, msg) = await HardDeleteSingleInternalAsync(item.ItemType, item.Id);
                if (success)
                {
                    deletedCount++;
                    var diff = AuditDiffHelper.ComputeDiff(entity, null);
                    await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.PermanentDelete, item.ItemType, item.Id, identifier, diff, reason: effectiveReason);
                }
                else errors.Add(msg);
            }

            return Ok(new
            {
                success = true,
                message = $"Đã xóa vĩnh viễn {deletedCount}/{request.Items.Count} mục khỏi CSDL.",
                deletedCount,
                errors
            });
        }

        /// <summary>
        /// Dọn sạch toàn bộ thùng rác (Hard delete toàn bộ mục IsDeleted = true)
        /// </summary>
        [HttpDelete("empty")]
        public async Task<IActionResult> EmptyRecycleBin([FromQuery] string? itemType = null, [FromQuery] string? reason = null)
        {
            int totalDeleted = 0;
            string effectiveReason = string.IsNullOrWhiteSpace(reason) ? "Dọn sạch thùng rác" : reason;

            async Task PurgeEntitiesAsync<T>(IMongoCollection<T> collection, string entityName, Func<T, string> getIdentifier) where T : BaseEntity
            {
                var deletedEntities = await collection.Find(Builders<T>.Filter.Eq(x => x.IsDeleted, true)).ToListAsync();
                if (deletedEntities.Count == 0) return;

                foreach (var e in deletedEntities)
                {
                    var diff = AuditDiffHelper.ComputeDiff(e, null);
                    await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.PermanentDelete, entityName, e.Id, getIdentifier(e), diff, reason: effectiveReason);
                }

                var res = await collection.DeleteManyAsync(Builders<T>.Filter.Eq(x => x.IsDeleted, true));
                totalDeleted += (int)res.DeletedCount;
            }

            if (string.IsNullOrWhiteSpace(itemType) || itemType.Equals("Vehicle", StringComparison.OrdinalIgnoreCase))
                await PurgeEntitiesAsync(_context.GetCollection<Vehicle>(), "Vehicle", v => v.PlateNumber);

            if (string.IsNullOrWhiteSpace(itemType) || itemType.Equals("Person", StringComparison.OrdinalIgnoreCase))
                await PurgeEntitiesAsync(_context.GetCollection<Person>(), "Person", p => $"{p.FullName} ({p.Code})");

            if (string.IsNullOrWhiteSpace(itemType) || itemType.Equals("Contractor", StringComparison.OrdinalIgnoreCase))
                await PurgeEntitiesAsync(_context.GetCollection<Contractor>(), "Contractor", c => $"{c.Name} ({c.Code})");

            if (string.IsNullOrWhiteSpace(itemType) || itemType.Equals("Department", StringComparison.OrdinalIgnoreCase))
                await PurgeEntitiesAsync(_context.GetCollection<Department>(), "Department", d => $"{d.Name} ({d.Code})");

            if (string.IsNullOrWhiteSpace(itemType) || itemType.Equals("Company", StringComparison.OrdinalIgnoreCase))
                await PurgeEntitiesAsync(_context.GetCollection<Company>(), "Company", comp => $"{comp.Name} ({comp.Code})");

            if (string.IsNullOrWhiteSpace(itemType) || itemType.Equals("Device", StringComparison.OrdinalIgnoreCase))
                await PurgeEntitiesAsync(_context.GetCollection<Device>(), "Device", dev => $"{dev.Name} ({dev.IpAddress})");

            if (string.IsNullOrWhiteSpace(itemType) || itemType.Equals("Lane", StringComparison.OrdinalIgnoreCase))
                await PurgeEntitiesAsync(_context.GetCollection<Lane>(), "Lane", l => $"{l.Name} ({l.Code})");

            if (string.IsNullOrWhiteSpace(itemType) || itemType.Equals("ParkingSession", StringComparison.OrdinalIgnoreCase))
                await PurgeEntitiesAsync(_context.GetCollection<ParkingSession>(), "ParkingSession", ps => ps.PlateNumber);

            if (string.IsNullOrWhiteSpace(itemType) || itemType.Equals("User", StringComparison.OrdinalIgnoreCase))
                await PurgeEntitiesAsync(_context.GetCollection<User>(), "User", u => $"{u.FullName} ({u.Username})");

            return Ok(new { success = true, message = $"Đã dọn sạch thùng rác (xóa {totalDeleted} mục vĩnh viễn).", totalDeleted });
        }

        private async Task<(object? Entity, string Identifier)> GetEntityWithIdentifierAsync(string itemType, string id)
        {
            if (string.IsNullOrWhiteSpace(itemType) || string.IsNullOrWhiteSpace(id))
                return (null, id);

            switch (itemType.ToLowerInvariant())
            {
                case "vehicle":
                    var v = await _context.GetCollection<Vehicle>().Find(BuildIdFilter<Vehicle>(id)).FirstOrDefaultAsync();
                    return (v, v?.PlateNumber ?? id);

                case "person":
                    var p = await _context.GetCollection<Person>().Find(BuildIdFilter<Person>(id)).FirstOrDefaultAsync();
                    return (p, p != null ? $"{p.FullName} ({p.Code})" : id);

                case "contractor":
                    var c = await _context.GetCollection<Contractor>().Find(BuildIdFilter<Contractor>(id)).FirstOrDefaultAsync();
                    return (c, c != null ? $"{c.Name} ({c.Code})" : id);

                case "department":
                    var d = await _context.GetCollection<Department>().Find(BuildIdFilter<Department>(id)).FirstOrDefaultAsync();
                    return (d, d != null ? $"{d.Name} ({d.Code})" : id);

                case "company":
                    var comp = await _context.GetCollection<Company>().Find(BuildIdFilter<Company>(id)).FirstOrDefaultAsync();
                    return (comp, comp != null ? $"{comp.Name} ({comp.Code})" : id);

                case "device":
                    var dev = await _context.GetCollection<Device>().Find(BuildIdFilter<Device>(id)).FirstOrDefaultAsync();
                    return (dev, dev != null ? $"{dev.Name} ({dev.IpAddress})" : id);

                case "lane":
                    var l = await _context.GetCollection<Lane>().Find(BuildIdFilter<Lane>(id)).FirstOrDefaultAsync();
                    return (l, l != null ? $"{l.Name} ({l.Code})" : id);

                case "parkingsession":
                    var ps = await _context.GetCollection<ParkingSession>().Find(BuildIdFilter<ParkingSession>(id)).FirstOrDefaultAsync();
                    return (ps, ps?.PlateNumber ?? id);

                case "user":
                    var u = await _context.GetCollection<User>().Find(BuildIdFilter<User>(id)).FirstOrDefaultAsync();
                    return (u, u != null ? $"{u.FullName} ({u.Username})" : id);

                default:
                    return (null, id);
            }
        }

        // =====================================================================
        // HELPER LOGIC: KHÔI PHỤC VÀ XÓA VĨNH VIỄN CÓ KIỂM TRA RÀNG BUỘC
        // =====================================================================

        private FilterDefinition<T> BuildIdFilter<T>(string id) where T : BaseEntity
        {
            if (MongoDB.Bson.ObjectId.TryParse(id, out var objectId))
            {
                return Builders<T>.Filter.Or(
                    Builders<T>.Filter.Eq(x => x.Id, id),
                    Builders<T>.Filter.Eq("_id", objectId),
                    Builders<T>.Filter.Eq("_id", id)
                );
            }
            return Builders<T>.Filter.Or(
                Builders<T>.Filter.Eq(x => x.Id, id),
                Builders<T>.Filter.Eq("_id", id)
            );
        }

        private async Task<(bool Success, string Message)> RestoreSingleInternalAsync(string itemType, string id)
        {
            switch (itemType.ToLowerInvariant())
            {
                case "vehicle":
                    var vFilter = BuildIdFilter<Vehicle>(id);
                    var vRes = await _context.GetCollection<Vehicle>().UpdateOneAsync(vFilter, Builders<Vehicle>.Update.Set(x => x.IsDeleted, false).Set(x => x.DeletedAt, null).Set(x => x.UpdatedAt, DateTime.Now));
                    return vRes.MatchedCount > 0 ? (true, "Thành công") : (false, "Không tìm thấy phương tiện");

                case "person":
                    var pFilter = BuildIdFilter<Person>(id);
                    var pRes = await _context.GetCollection<Person>().UpdateOneAsync(pFilter, Builders<Person>.Update.Set(x => x.IsDeleted, false).Set(x => x.DeletedAt, null).Set(x => x.UpdatedAt, DateTime.Now));
                    return pRes.MatchedCount > 0 ? (true, "Thành công") : (false, "Không tìm thấy nhân sự");

                case "contractor":
                    var cFilter = BuildIdFilter<Contractor>(id);
                    var cRes = await _context.GetCollection<Contractor>().UpdateOneAsync(cFilter, Builders<Contractor>.Update.Set(x => x.IsDeleted, false).Set(x => x.DeletedAt, null).Set(x => x.UpdatedAt, DateTime.Now));
                    return cRes.MatchedCount > 0 ? (true, "Thành công") : (false, "Không tìm thấy đối tác");

                case "department":
                    var dFilter = BuildIdFilter<Department>(id);
                    var dRes = await _context.GetCollection<Department>().UpdateOneAsync(dFilter, Builders<Department>.Update.Set(x => x.IsDeleted, false).Set(x => x.DeletedAt, null).Set(x => x.UpdatedAt, DateTime.Now));
                    return dRes.MatchedCount > 0 ? (true, "Thành công") : (false, "Không tìm thấy phòng ban");

                case "company":
                    var compFilter = BuildIdFilter<Company>(id);
                    var compRes = await _context.GetCollection<Company>().UpdateOneAsync(compFilter, Builders<Company>.Update.Set(x => x.IsDeleted, false).Set(x => x.DeletedAt, null).Set(x => x.UpdatedAt, DateTime.Now));
                    return compRes.MatchedCount > 0 ? (true, "Thành công") : (false, "Không tìm thấy công ty");

                case "device":
                    var devFilter = BuildIdFilter<Device>(id);
                    var devRes = await _context.GetCollection<Device>().UpdateOneAsync(devFilter, Builders<Device>.Update.Set(x => x.IsDeleted, false).Set(x => x.DeletedAt, null).Set(x => x.UpdatedAt, DateTime.Now));
                    return devRes.MatchedCount > 0 ? (true, "Thành công") : (false, "Không tìm thấy thiết bị");

                case "lane":
                    var lFilter = BuildIdFilter<Lane>(id);
                    var lRes = await _context.GetCollection<Lane>().UpdateOneAsync(lFilter, Builders<Lane>.Update.Set(x => x.IsDeleted, false).Set(x => x.DeletedAt, null).Set(x => x.UpdatedAt, DateTime.Now));
                    return lRes.MatchedCount > 0 ? (true, "Thành công") : (false, "Không tìm thấy làn");

                case "parkingsession":
                    var psFilter = BuildIdFilter<ParkingSession>(id);
                    var psRes = await _context.GetCollection<ParkingSession>().UpdateOneAsync(psFilter, Builders<ParkingSession>.Update.Set(x => x.IsDeleted, false).Set(x => x.DeletedAt, null).Set(x => x.UpdatedAt, DateTime.Now));
                    return psRes.MatchedCount > 0 ? (true, "Thành công") : (false, "Không tìm thấy lượt gửi xe");

                case "user":
                    var uFilter = BuildIdFilter<User>(id);
                    var uRes = await _context.GetCollection<User>().UpdateOneAsync(uFilter, Builders<User>.Update.Set(x => x.IsDeleted, false).Set(x => x.DeletedAt, null).Set(x => x.UpdatedAt, DateTime.Now));
                    return uRes.MatchedCount > 0 ? (true, "Thành công") : (false, "Không tìm thấy tài khoản người dùng");

                default:
                    return (false, $"Loại thực thể [{itemType}] không được hỗ trợ.");
            }
        }

        private async Task<(bool Success, string Message)> HardDeleteSingleInternalAsync(string itemType, string id)
        {
            switch (itemType.ToLowerInvariant())
            {
                case "company":
                    // Ràng buộc: Kiểm tra xem có phòng ban hoặc nhân sự đang hoạt động thuộc công ty này không
                    var activeDeptsCount = await _context.GetCollection<Department>().CountDocumentsAsync(Builders<Department>.Filter.And(
                        Builders<Department>.Filter.Eq(x => x.CompanyId, id),
                        Builders<Department>.Filter.Eq(x => x.IsDeleted, false)
                    ));
                    if (activeDeptsCount > 0)
                    {
                        return (false, $"Không thể xóa vĩnh viễn: Còn {activeDeptsCount} phòng ban đang hoạt động trực thuộc công ty này.");
                    }

                    var activePeopleCount = await _context.GetCollection<Person>().CountDocumentsAsync(Builders<Person>.Filter.And(
                        Builders<Person>.Filter.Eq(x => x.CompanyId, id),
                        Builders<Person>.Filter.Eq(x => x.IsDeleted, false)
                    ));
                    if (activePeopleCount > 0)
                    {
                        return (false, $"Không thể xóa vĩnh viễn: Còn {activePeopleCount} nhân sự đang hoạt động trực thuộc công ty này.");
                    }

                    var compDel = await _context.GetCollection<Company>().DeleteOneAsync(BuildIdFilter<Company>(id));
                    return compDel.DeletedCount > 0 ? (true, "Thành công") : (false, "Không tìm thấy công ty");

                case "department":
                    // Ràng buộc: Kiểm tra nhân sự trực thuộc
                    var deptPeopleCount = await _context.GetCollection<Person>().CountDocumentsAsync(Builders<Person>.Filter.And(
                        Builders<Person>.Filter.Eq(x => x.DepartmentId, id),
                        Builders<Person>.Filter.Eq(x => x.IsDeleted, false)
                    ));
                    if (deptPeopleCount > 0)
                    {
                        return (false, $"Không thể xóa vĩnh viễn: Còn {deptPeopleCount} nhân sự đang hoạt động trực thuộc phòng ban này.");
                    }

                    var deptDel = await _context.GetCollection<Department>().DeleteOneAsync(BuildIdFilter<Department>(id));
                    return deptDel.DeletedCount > 0 ? (true, "Thành công") : (false, "Không tìm thấy phòng ban");

                case "contractor":
                    // Ràng buộc: Kiểm tra nhân sự đối tác
                    var contrPeopleCount = await _context.GetCollection<Person>().CountDocumentsAsync(Builders<Person>.Filter.And(
                        Builders<Person>.Filter.Eq(x => x.ContractorId, id),
                        Builders<Person>.Filter.Eq(x => x.IsDeleted, false)
                    ));
                    if (contrPeopleCount > 0)
                    {
                        return (false, $"Không thể xóa vĩnh viễn: Còn {contrPeopleCount} nhân sự nhà thầu đang hoạt động.");
                    }

                    var cDel = await _context.GetCollection<Contractor>().DeleteOneAsync(BuildIdFilter<Contractor>(id));
                    return cDel.DeletedCount > 0 ? (true, "Thành công") : (false, "Không tìm thấy đối tác");

                case "person":
                    // Ràng buộc: Nếu có xe đang gắn chủ xe này, cập nhật OwnerPersonId = null
                    await _context.GetCollection<Vehicle>().UpdateManyAsync(
                        Builders<Vehicle>.Filter.Eq(x => x.OwnerPersonId, id),
                        Builders<Vehicle>.Update.Set(x => x.OwnerPersonId, null)
                    );

                    var pDel = await _context.GetCollection<Person>().DeleteOneAsync(BuildIdFilter<Person>(id));
                    return pDel.DeletedCount > 0 ? (true, "Thành công") : (false, "Không tìm thấy nhân sự");

                case "vehicle":
                    var vDel = await _context.GetCollection<Vehicle>().DeleteOneAsync(BuildIdFilter<Vehicle>(id));
                    return vDel.DeletedCount > 0 ? (true, "Thành công") : (false, "Không tìm thấy phương tiện");

                case "device":
                    // Ràng buộc: Kiểm tra xem có làn đang hoạt động gán thiết bị này không
                    var laneAssigned = await _context.GetCollection<Lane>().Find(Builders<Lane>.Filter.And(
                        Builders<Lane>.Filter.Eq(x => x.IsDeleted, false),
                        Builders<Lane>.Filter.Or(
                            Builders<Lane>.Filter.Eq(x => x.PlateCameraDeviceId, id),
                            Builders<Lane>.Filter.Eq(x => x.OverviewCameraDeviceId, id),
                            Builders<Lane>.Filter.Eq(x => x.ControllerDeviceId, id)
                        )
                    )).FirstOrDefaultAsync();

                    if (laneAssigned != null)
                    {
                        return (false, $"Không thể xóa vĩnh viễn: Thiết bị đang được gán vào làn [{laneAssigned.Name}].");
                    }

                    var devDel = await _context.GetCollection<Device>().DeleteOneAsync(BuildIdFilter<Device>(id));
                    return devDel.DeletedCount > 0 ? (true, "Thành công") : (false, "Không tìm thấy thiết bị");

                case "lane":
                    var lDel = await _context.GetCollection<Lane>().DeleteOneAsync(BuildIdFilter<Lane>(id));
                    return lDel.DeletedCount > 0 ? (true, "Thành công") : (false, "Không tìm thấy làn");

                case "parkingsession":
                    var psDel = await _context.GetCollection<ParkingSession>().DeleteOneAsync(BuildIdFilter<ParkingSession>(id));
                    return psDel.DeletedCount > 0 ? (true, "Thành công") : (false, "Không tìm thấy lượt gửi xe");

                case "user":
                    var uDel = await _context.GetCollection<User>().DeleteOneAsync(BuildIdFilter<User>(id));
                    return uDel.DeletedCount > 0 ? (true, "Thành công") : (false, "Không tìm thấy tài khoản người dùng");

                default:
                    return (false, $"Loại thực thể [{itemType}] không được hỗ trợ.");
            }
        }
    }
}
