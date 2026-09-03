using Microsoft.AspNetCore.Http;
using PhuXuanParkingSystem.Api.Helpers;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Api.Services
{
    /// <summary>
    /// Các phương thức mở rộng hỗ trợ ghi AuditLog tinh gọn, nhất quán và tránh trùng lặp mã nguồn
    /// </summary>
    public static class AuditLogQueueExtensions
    {
        public static async ValueTask LogActivityAsync(
            this IAuditLogQueue queue,
            ClaimsPrincipal user,
            HttpContext httpContext,
            AuditActionType actionType,
            string targetEntity,
            string? targetId,
            string? targetDisplay,
            AuditDiffResult? diff = null,
            string? reason = null,
            bool isSuccess = true,
            string? errorMessage = null,
            CancellationToken cancellationToken = default)
        {
            if (queue == null) return;

            var (actorId, actorUsername, actorRole) = user.GetActorInfo();

            var log = new AuditLog
            {
                ActorId = actorId,
                ActorUsername = actorUsername,
                ActorRole = actorRole,
                ActionType = actionType,
                TargetEntity = targetEntity,
                TargetId = targetId,
                TargetDisplay = targetDisplay,
                OldValues = diff?.OldValues,
                NewValues = diff?.NewValues,
                ChangedProperties = diff?.ChangedProperties ?? new(),
                Reason = reason,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                Source = "WebAdmin"
            };

            await queue.QueueLogAsync(log, cancellationToken);
        }
    }
}
