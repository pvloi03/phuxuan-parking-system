using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using HPParkingSystem.Api.DTOs;

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
