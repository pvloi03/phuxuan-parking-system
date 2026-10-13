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
        private readonly IOrganizationLookupService _orgLookupService;
        private readonly IAuditLogQueue _auditQueue;

        public CrossImportService(
            IRepository<Person> personRepo,
            IRepository<Vehicle> vehicleRepo,
            IOrganizationLookupService orgLookupService,
            IAuditLogQueue auditQueue)
        {
            _personRepo = personRepo;
            _vehicleRepo = vehicleRepo;
            _orgLookupService = orgLookupService;
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

            // 1. Tải danh mục tra cứu phòng ban, công ty, nhà thầu qua lookup service chuyên trách
            var orgCatalog = await _orgLookupService.LoadCatalogAsync();

            // 2. Xử lý từng dòng dữ liệu
            for (int i = 0; i < items.Count; i++)
            {
                var row = items[i];
                int rowIndex = i + 1;

                try
                {
                    var rawPlate = !string.IsNullOrWhiteSpace(row.PlateNumber) ? row.PlateNumber.Trim() : null;
                    var cleanCode = !string.IsNullOrWhiteSpace(row.PersonCode)
                        ? row.PersonCode.Trim().ToUpperInvariant()
                        : null;
                    var cleanFullName = !string.IsNullOrWhiteSpace(row.FullName)
                        ? row.FullName.Trim()
                        : null;

                    // Nếu dòng rỗng cả người lẫn xe thì bỏ qua
                    if (string.IsNullOrWhiteSpace(rawPlate) && string.IsNullOrWhiteSpace(cleanCode) && string.IsNullOrWhiteSpace(cleanFullName))
                    {
                        continue;
                    }

                    // Kiểm tra tính hợp lệ của biển số xe nếu có truyền (Domain Invariant #3)
                    string? validCleanPlate = null;
                    if (!string.IsNullOrWhiteSpace(rawPlate))
                    {
                        if (Models.ValueObjects.PlateNumber.IsValid(rawPlate))
                        {
                            validCleanPlate = Models.ValueObjects.PlateNumber.Clean(rawPlate);
                        }
                        else
                        {
                            result.Errors.Add($"Dòng {rowIndex}: Biển số xe '{row.PlateNumber}' không hợp lệ (Domain Invariant #3).");
                        }
                    }

                    // --- BƯỚC A: XỬ LÝ NHÂN SỰ ---
                    Person? currentPerson = null;
                    bool hasPersonInfo = !string.IsNullOrWhiteSpace(cleanCode)
                        || !string.IsNullOrWhiteSpace(cleanFullName)
                        || !string.IsNullOrWhiteSpace(row.PhoneNumber)
                        || !string.IsNullOrWhiteSpace(row.Email);

                    // Xác định phòng ban, công ty, nhà thầu qua catalog
                    var resolvedDeptId = orgCatalog.ResolveDepartmentId(row.DepartmentId, row.DepartmentName);
                    var resolvedCompId = orgCatalog.ResolveCompanyId(row.CompanyId, row.CompanyName);
                    var resolvedContrId = orgCatalog.ResolveContractorId(row.ContractorId, row.ContractorName);

                    if (hasPersonInfo)
                    {
                        if (string.IsNullOrWhiteSpace(cleanCode))
                        {
                            result.Errors.Add($"Dòng {rowIndex}: Thông tin nhân sự '{cleanFullName ?? "Chưa rõ"}' thiếu mã định danh bắt buộc (PersonCode).");
                        }
                        else
                        {
                            currentPerson = await _personRepo.FindOneAsync(p => p.Code == cleanCode && !p.IsDeleted);

                            if (currentPerson == null)
                            {
                                if (string.IsNullOrWhiteSpace(cleanFullName))
                                {
                                    result.Errors.Add($"Dòng {rowIndex}: Nhân sự mới với mã '{cleanCode}' thiếu họ và tên bắt buộc (FullName).");
                                }
                                else
                                {
                                    // Thêm mới Nhân sự qua Domain Factory Method
                                    currentPerson = Person.Create(
                                        cleanCode,
                                        cleanFullName,
                                        row.PersonType ?? PersonType.Employee,
                                        row.PhoneNumber?.Trim(),
                                        row.Email?.Trim(),
                                        resolvedDeptId,
                                        resolvedCompId,
                                        resolvedContrId,
                                        row.PersonIsActive ?? true);

                                    await _personRepo.AddAsync(currentPerson);
                                    result.PeopleCreated++;

                                    var personCreateDiff = AuditDiffHelper.ComputeDiff<Person>(null, currentPerson);
                                    await _auditQueue.LogActivityAsync(user, httpContext, AuditActionType.Create, "Person", currentPerson.Id, currentPerson.FullName, personCreateDiff);
                                }
                            }
                            else
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

                                    var personUpdateDiff = AuditDiffHelper.ComputeDiffFromSnapshot(snapshot, currentPerson);
                                    if (personUpdateDiff.HasChanges)
                                    {
                                        await _auditQueue.LogActivityAsync(user, httpContext, AuditActionType.Update, "Person", currentPerson.Id, currentPerson.FullName, personUpdateDiff);
                                    }
                                }
                            }
                        }
                    }

                    // Kiểm tra tính nguyên tử của dòng dữ liệu (Row-level Atomicity):
                    // Nếu dòng có thông tin nhân sự nhưng không thể lưu/hợp lệ, hủy bỏ xử lý phương tiện
                    // trên dòng đó để tránh tạo xe mồ côi hoặc giữ sai chủ xe cũ.
                    if (hasPersonInfo && currentPerson == null)
                    {
                        if (!string.IsNullOrWhiteSpace(validCleanPlate))
                        {
                            result.Errors.Add($"Dòng {rowIndex}: Hủy bỏ thao tác với phương tiện '{validCleanPlate}' do thông tin nhân sự đi kèm không hợp lệ (Row-level Atomicity).");
                        }
                        continue;
                    }

                    // --- BƯỚC B: XỬ LÝ PHƯƠNG TIỆN ---
                    if (!string.IsNullOrWhiteSpace(validCleanPlate))
                    {
                        var currentVehicle = await _vehicleRepo.FindOneAsync(v => v.PlateNumber == validCleanPlate && !v.IsDeleted);
                        string? ownerId = currentPerson?.Id;

                        if (currentVehicle == null)
                        {
                            // Thêm mới Phương tiện qua Domain Factory Method
                            currentVehicle = Vehicle.Create(
                                validCleanPlate,
                                row.VehicleType ?? VehicleType.Car,
                                ownerId,
                                (row.Weight.HasValue && row.Weight.Value >= 0) ? row.Weight.Value : null,
                                row.VehicleIsActive ?? true);

                            await _vehicleRepo.AddAsync(currentVehicle);
                            result.VehiclesCreated++;
                            if (ownerId != null)
                            {
                                result.LinksCreated++;
                            }

                            var vehCreateDiff = AuditDiffHelper.ComputeDiff<Vehicle>(null, currentVehicle);
                            await _auditQueue.LogActivityAsync(user, httpContext, AuditActionType.Create, "Vehicle", currentVehicle.Id, currentVehicle.PlateNumber, vehCreateDiff);
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

                                var vehUpdateDiff = AuditDiffHelper.ComputeDiffFromSnapshot(snapshot, currentVehicle);
                                if (vehUpdateDiff.HasChanges)
                                {
                                    await _auditQueue.LogActivityAsync(user, httpContext, AuditActionType.Update, "Vehicle", currentVehicle.Id, currentVehicle.PlateNumber, vehUpdateDiff);
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
