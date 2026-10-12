using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using HPParkingSystem.Api.DTOs;
using HPParkingSystem.Api.Helpers;
using HPParkingSystem.Api.Services;
using HPParkingSystem.Models.Entities;
using HPParkingSystem.Models.Enums;
using HPParkingSystem.Repositories;

namespace HPParkingSystem.Api.Services
{
    public class CrossImportService : ICrossImportService
    {
        private readonly IRepository<Person> _personRepo;
        private readonly IRepository<Vehicle> _vehicleRepo;
        private readonly IRepository<Department> _deptRepo;
        private readonly IRepository<Company> _companyRepo;
        private readonly IRepository<Contractor> _contractorRepo;
        private readonly IAuditLogQueue _auditQueue;

        public CrossImportService(
            IRepository<Person> personRepo,
            IRepository<Vehicle> vehicleRepo,
            IRepository<Department> deptRepo,
            IRepository<Company> companyRepo,
            IRepository<Contractor> contractorRepo,
            IAuditLogQueue auditQueue)
        {
            _personRepo = personRepo;
            _vehicleRepo = vehicleRepo;
            _deptRepo = deptRepo;
            _companyRepo = companyRepo;
            _contractorRepo = contractorRepo;
            _auditQueue = auditQueue;
        }

        public async Task<CrossImportResultDto> ImportAsync(
            List<VehiclePersonCrossImportItemDto> items,
            ClaimsPrincipal? user = null,
            HttpContext? httpContext = null)
        {
            var result = new CrossImportResultDto
            {
                TotalRows = items?.Count ?? 0
            };

            if (items == null || items.Count == 0)
            {
                return result;
            }

            // 1. Tải danh mục tra cứu phòng ban, công ty, nhà thầu
            var departments = await _deptRepo.FindAsync(d => !d.IsDeleted);
            var companies = await _companyRepo.FindAsync(c => !c.IsDeleted);
            var contractors = await _contractorRepo.FindAsync(c => !c.IsDeleted);

            var deptMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var d in departments)
            {
                deptMap[d.Id] = d.Id;
                if (!string.IsNullOrWhiteSpace(d.Name)) deptMap[d.Name.Trim()] = d.Id;
                if (!string.IsNullOrWhiteSpace(d.Code)) deptMap[d.Code.Trim()] = d.Id;
            }

            var compMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in companies)
            {
                compMap[c.Id] = c.Id;
                if (!string.IsNullOrWhiteSpace(c.Name)) compMap[c.Name.Trim()] = c.Id;
                if (!string.IsNullOrWhiteSpace(c.Code)) compMap[c.Code.Trim()] = c.Id;
            }

            var contrMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in contractors)
            {
                contrMap[c.Id] = c.Id;
                if (!string.IsNullOrWhiteSpace(c.Name)) contrMap[c.Name.Trim()] = c.Id;
                if (!string.IsNullOrWhiteSpace(c.Code)) contrMap[c.Code.Trim()] = c.Id;
            }

            // 2. Xử lý từng dòng dữ liệu
            for (int i = 0; i < items.Count; i++)
            {
                var row = items[i];
                int rowIndex = i + 1;

                try
                {
                    var cleanPlate = !string.IsNullOrWhiteSpace(row.PlateNumber)
                        ? Models.ValueObjects.PlateNumber.Clean(row.PlateNumber)
                        : null;
                    var cleanCode = !string.IsNullOrWhiteSpace(row.PersonCode)
                        ? row.PersonCode.Trim().ToUpperInvariant()
                        : null;
                    var cleanFullName = !string.IsNullOrWhiteSpace(row.FullName)
                        ? row.FullName.Trim()
                        : null;

                    // Nếu dòng rỗng cả người lẫn xe thì bỏ qua
                    if (string.IsNullOrWhiteSpace(cleanPlate) && string.IsNullOrWhiteSpace(cleanCode) && string.IsNullOrWhiteSpace(cleanFullName))
                    {
                        continue;
                    }

                    // --- BƯỚC A: XỬ LÝ NHÂN SỰ ---
                    Person? currentPerson = null;

                    if (!string.IsNullOrWhiteSpace(cleanCode))
                    {
                        currentPerson = await _personRepo.FindOneAsync(p => p.Code == cleanCode && !p.IsDeleted);
                    }

                    // Xác định phòng ban, công ty, nhà thầu
                    string? resolvedDeptId = null;
                    if (!string.IsNullOrWhiteSpace(row.DepartmentId) && deptMap.TryGetValue(row.DepartmentId.Trim(), out var dId1))
                        resolvedDeptId = dId1;
                    else if (!string.IsNullOrWhiteSpace(row.DepartmentName) && deptMap.TryGetValue(row.DepartmentName.Trim(), out var dId2))
                        resolvedDeptId = dId2;

                    string? resolvedCompId = null;
                    if (!string.IsNullOrWhiteSpace(row.CompanyId) && compMap.TryGetValue(row.CompanyId.Trim(), out var cId1))
                        resolvedCompId = cId1;
                    else if (!string.IsNullOrWhiteSpace(row.CompanyName) && compMap.TryGetValue(row.CompanyName.Trim(), out var cId2))
                        resolvedCompId = cId2;

                    string? resolvedContrId = null;
                    if (!string.IsNullOrWhiteSpace(row.ContractorId) && contrMap.TryGetValue(row.ContractorId.Trim(), out var contrId1))
                        resolvedContrId = contrId1;
                    else if (!string.IsNullOrWhiteSpace(row.ContractorName) && contrMap.TryGetValue(row.ContractorName.Trim(), out var contrId2))
                        resolvedContrId = contrId2;

                    if (currentPerson == null && !string.IsNullOrWhiteSpace(cleanCode) && !string.IsNullOrWhiteSpace(cleanFullName))
                    {
                        // Thêm mới Nhân sự
                        currentPerson = new Person
                        {
                            Code = cleanCode,
                            FullName = cleanFullName,
                            Type = row.PersonType ?? PersonType.Employee,
                            PhoneNumber = row.PhoneNumber?.Trim(),
                            Email = row.Email?.Trim(),
                            DepartmentId = resolvedDeptId,
                            CompanyId = resolvedCompId,
                            ContractorId = resolvedContrId,
                            IsActive = row.PersonIsActive ?? true
                        };

                        await _personRepo.AddAsync(currentPerson);
                        result.PeopleCreated++;

                        if (user != null && httpContext != null)
                        {
                            var diff = AuditDiffHelper.ComputeDiff<Person>(null, currentPerson);
                            await _auditQueue.LogActivityAsync(user, httpContext, AuditActionType.Create, "Person", currentPerson.Id, currentPerson.FullName, diff);
                        }
                    }
                    else if (currentPerson != null)
                    {
                        // Cập nhật thông tin nhân sự có sẵn
                        var snapshot = AuditDiffHelper.TakeSnapshot(currentPerson);
                        bool changed = false;

                        if (!string.IsNullOrWhiteSpace(cleanFullName) && currentPerson.FullName != cleanFullName)
                        {
                            currentPerson.FullName = cleanFullName;
                            changed = true;
                        }
                        if (!string.IsNullOrWhiteSpace(row.PhoneNumber) && currentPerson.PhoneNumber != row.PhoneNumber.Trim())
                        {
                            currentPerson.PhoneNumber = row.PhoneNumber.Trim();
                            changed = true;
                        }
                        if (!string.IsNullOrWhiteSpace(row.Email) && currentPerson.Email != row.Email.Trim())
                        {
                            currentPerson.Email = row.Email.Trim();
                            changed = true;
                        }
                        if (row.PersonType.HasValue && currentPerson.Type != row.PersonType.Value)
                        {
                            currentPerson.Type = row.PersonType.Value;
                            changed = true;
                        }
                        if (row.PersonIsActive.HasValue && currentPerson.IsActive != row.PersonIsActive.Value)
                        {
                            currentPerson.IsActive = row.PersonIsActive.Value;
                            changed = true;
                        }
                        if (resolvedDeptId != null && currentPerson.DepartmentId != resolvedDeptId)
                        {
                            currentPerson.DepartmentId = resolvedDeptId;
                            changed = true;
                        }
                        if (resolvedCompId != null && currentPerson.CompanyId != resolvedCompId)
                        {
                            currentPerson.CompanyId = resolvedCompId;
                            changed = true;
                        }
                        if (resolvedContrId != null && currentPerson.ContractorId != resolvedContrId)
                        {
                            currentPerson.ContractorId = resolvedContrId;
                            changed = true;
                        }

                        if (changed)
                        {
                            currentPerson.UpdatedAt = DateTime.Now;
                            await _personRepo.UpdateAsync(currentPerson);
                            result.PeopleUpdated++;

                            if (user != null && httpContext != null)
                            {
                                var diff = AuditDiffHelper.ComputeDiffFromSnapshot(snapshot, currentPerson);
                                if (diff.HasChanges)
                                {
                                    await _auditQueue.LogActivityAsync(user, httpContext, AuditActionType.Update, "Person", currentPerson.Id, currentPerson.FullName, diff);
                                }
                            }
                        }
                    }

                    // --- BƯỚC B: XỬ LÝ PHƯƠNG TIỆN ---
                    if (!string.IsNullOrWhiteSpace(cleanPlate))
                    {
                        var currentVehicle = await _vehicleRepo.FindOneAsync(v => v.PlateNumber == cleanPlate && !v.IsDeleted);
                        string? ownerId = currentPerson?.Id;

                        if (currentVehicle == null)
                        {
                            // Thêm mới Phương tiện
                            currentVehicle = new Vehicle
                            {
                                PlateNumber = cleanPlate,
                                Type = row.VehicleType ?? VehicleType.Car,
                                Weight = (row.Weight.HasValue && row.Weight.Value >= 0) ? row.Weight.Value : null,
                                OwnerPersonId = ownerId,
                                IsActive = row.VehicleIsActive ?? true
                            };

                            await _vehicleRepo.AddAsync(currentVehicle);
                            result.VehiclesCreated++;
                            if (ownerId != null)
                            {
                                result.LinksCreated++;
                            }

                            if (user != null && httpContext != null)
                            {
                                var diff = AuditDiffHelper.ComputeDiff<Vehicle>(null, currentVehicle);
                                await _auditQueue.LogActivityAsync(user, httpContext, AuditActionType.Create, "Vehicle", currentVehicle.Id, currentVehicle.PlateNumber, diff);
                            }
                        }
                        else
                        {
                            // Cập nhật Phương tiện đã có
                            var snapshot = AuditDiffHelper.TakeSnapshot(currentVehicle);
                            bool vehicleChanged = false;

                            if (row.VehicleType.HasValue && currentVehicle.Type != row.VehicleType.Value)
                            {
                                currentVehicle.Type = row.VehicleType.Value;
                                vehicleChanged = true;
                            }

                            if (row.Weight.HasValue && row.Weight.Value >= 0 && currentVehicle.Weight != row.Weight.Value)
                            {
                                currentVehicle.Weight = row.Weight.Value;
                                vehicleChanged = true;
                            }

                            if (row.VehicleIsActive.HasValue && currentVehicle.IsActive != row.VehicleIsActive.Value)
                            {
                                currentVehicle.IsActive = row.VehicleIsActive.Value;
                                vehicleChanged = true;
                            }

                            if (ownerId != null && currentVehicle.OwnerPersonId != ownerId)
                            {
                                currentVehicle.OwnerPersonId = ownerId;
                                vehicleChanged = true;
                                result.LinksCreated++;
                            }

                            if (vehicleChanged)
                            {
                                currentVehicle.UpdatedAt = DateTime.Now;
                                await _vehicleRepo.UpdateAsync(currentVehicle);
                                result.VehiclesUpdated++;

                                if (user != null && httpContext != null)
                                {
                                    var diff = AuditDiffHelper.ComputeDiffFromSnapshot(snapshot, currentVehicle);
                                    if (diff.HasChanges)
                                    {
                                        await _auditQueue.LogActivityAsync(user, httpContext, AuditActionType.Update, "Vehicle", currentVehicle.Id, currentVehicle.PlateNumber, diff);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Dòng {rowIndex}: {ex.Message}");
                }
            }

            return result;
        }
    }
}
