using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HPParkingSystem.Models.Entities;
using HPParkingSystem.Repositories;

namespace HPParkingSystem.Api.Services
{
    public class OrganizationLookupService : IOrganizationLookupService
    {
        private readonly IRepository<Department> _deptRepo;
        private readonly IRepository<Company> _companyRepo;
        private readonly IRepository<Contractor> _contractorRepo;

        public OrganizationLookupService(
            IRepository<Department> deptRepo,
            IRepository<Company> companyRepo,
            IRepository<Contractor> contractorRepo)
        {
            _deptRepo = deptRepo;
            _companyRepo = companyRepo;
            _contractorRepo = contractorRepo;
        }

        public async Task<IOrganizationCatalog> LoadCatalogAsync(CancellationToken cancellationToken = default)
        {
            var departments = await _deptRepo.FindAsync(d => !d.IsDeleted, cancellationToken);
            var companies = await _companyRepo.FindAsync(c => !c.IsDeleted, cancellationToken);
            var contractors = await _contractorRepo.FindAsync(c => !c.IsDeleted, cancellationToken);

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

            return new OrganizationCatalog(deptMap, compMap, contrMap);
        }

        private sealed class OrganizationCatalog : IOrganizationCatalog
        {
            private readonly Dictionary<string, string> _deptMap;
            private readonly Dictionary<string, string> _compMap;
            private readonly Dictionary<string, string> _contrMap;

            public OrganizationCatalog(
                Dictionary<string, string> deptMap,
                Dictionary<string, string> compMap,
                Dictionary<string, string> contrMap)
            {
                _deptMap = deptMap;
                _compMap = compMap;
                _contrMap = contrMap;
            }

            public string? ResolveDepartmentId(string? id, string? name)
            {
                if (!string.IsNullOrWhiteSpace(id) && _deptMap.TryGetValue(id.Trim(), out var dId1))
                    return dId1;
                if (!string.IsNullOrWhiteSpace(name) && _deptMap.TryGetValue(name.Trim(), out var dId2))
                    return dId2;
                return null;
            }

            public string? ResolveCompanyId(string? id, string? name)
            {
                if (!string.IsNullOrWhiteSpace(id) && _compMap.TryGetValue(id.Trim(), out var cId1))
                    return cId1;
                if (!string.IsNullOrWhiteSpace(name) && _compMap.TryGetValue(name.Trim(), out var cId2))
                    return cId2;
                return null;
            }

            public string? ResolveContractorId(string? id, string? name)
            {
                if (!string.IsNullOrWhiteSpace(id) && _contrMap.TryGetValue(id.Trim(), out var contrId1))
                    return contrId1;
                if (!string.IsNullOrWhiteSpace(name) && _contrMap.TryGetValue(name.Trim(), out var contrId2))
                    return contrId2;
                return null;
            }
        }
    }
}
