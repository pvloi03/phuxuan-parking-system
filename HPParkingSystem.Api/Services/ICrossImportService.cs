using HPParkingSystem.Api.DTOs;
using System.Security.Claims;

namespace HPParkingSystem.Api.Services
{
    public interface ICrossImportService
    {
        Task<CrossImportResultDto> ImportAsync(
            List<VehiclePersonCrossImportItemDto> items,
            ClaimsPrincipal? user = null,
            HttpContext? httpContext = null);
    }
}
