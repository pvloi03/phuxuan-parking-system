using System.Threading;
using System.Threading.Tasks;

namespace HPParkingSystem.Api.Services
{
    /// <summary>
    /// Giao diện đại diện cho danh mục tổ chức đã được nạp và lập chỉ mục tra cứu
    /// </summary>
    public interface IOrganizationCatalog
    {
        string? ResolveDepartmentId(string? id, string? name);
        string? ResolveCompanyId(string? id, string? name);
        string? ResolveContractorId(string? id, string? name);
    }

    /// <summary>
    /// Service chuyên trách tải và phân giải thông tin danh mục tổ chức (Phòng ban, Công ty, Nhà thầu)
    /// </summary>
    public interface IOrganizationLookupService
    {
        Task<IOrganizationCatalog> LoadCatalogAsync(CancellationToken cancellationToken = default);
    }
}
