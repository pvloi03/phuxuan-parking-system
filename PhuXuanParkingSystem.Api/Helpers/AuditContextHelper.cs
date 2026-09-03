using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace PhuXuanParkingSystem.Api.Helpers
{
    public static class AuditContextHelper
    {
        public static string GetClientIp(this HttpContext context) =>
            context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "127.0.0.1";

        public static string GetUserAgent(this HttpContext context) =>
            context.Request.Headers["User-Agent"].ToString() ?? string.Empty;

        public static (string? actorId, string actorUsername, string actorRole) GetActorInfo(this ClaimsPrincipal user) => (
            user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user.FindFirstValue("sub"),
            user.FindFirstValue(ClaimTypes.Name) ?? user.FindFirstValue(JwtRegisteredClaimNames.Name) ?? user.FindFirstValue("name") ?? user.Identity?.Name ?? "admin",
            user.FindFirstValue(ClaimTypes.Role) ?? user.FindFirstValue("role") ?? string.Empty
        );
    }
}
