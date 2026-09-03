using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PhuXuanParkingSystem.Models.Common;
using PhuXuanParkingSystem.Models.Enums;
using System;
using System.Collections.Generic;

namespace PhuXuanParkingSystem.Models.Entities
{
    /// <summary>
    /// Thực thể Nhật ký kiểm toán (AuditLog) đại diện cho các thao tác quản trị,
    /// bảo mật và thay đổi dữ liệu trong hệ thống (Append-Only).
    /// </summary>
    [BsonIgnoreExtraElements]
    public class AuditLog : BaseEntity
    {
        // =========================================================================
        // --- THÔNG TIN NGƯỜI THỰC HIỆN (ACTOR) ---
        // =========================================================================
        public string? ActorId { get; set; }
        public string ActorUsername { get; set; } = string.Empty;
        public string ActorRole { get; set; } = string.Empty;

        // =========================================================================
        // --- NGUỒN & MÔI TRƯỜNG THỰC HIỆN (SOURCE / ENVIRONMENT) ---
        // =========================================================================
        public string Source { get; set; } = "WebAdmin";
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        // =========================================================================
        // --- HÀNH ĐỘNG & THỰC THỂ TÁC ĐỘNG (ACTION & TARGET) ---
        // =========================================================================
        [BsonRepresentation(BsonType.String)]
        public AuditActionType ActionType { get; set; } = AuditActionType.Login;
        public string TargetEntity { get; set; } = string.Empty;
        public string? TargetId { get; set; }
        public string? TargetDisplay { get; set; }

        // =========================================================================
        // --- CHI TIẾT THAY ĐỔI DỮ LIỆU (DIFF & REASON) ---
        // =========================================================================
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public List<string> ChangedProperties { get; set; } = new List<string>();
        public string? Reason { get; set; }

        // =========================================================================
        // --- KẾT QUẢ & TRẠNG THÁI (STATUS & ERROR) ---
        // =========================================================================
        public bool IsSuccess { get; set; } = true;
        public string? ErrorMessage { get; set; }

        public AuditLog()
        {
        }

        public static AuditLog CreateAuthLog(
            string username,
            AuditActionType actionType,
            bool isSuccess,
            string? ipAddress = null,
            string? userAgent = null,
            string? actorId = null,
            string? actorRole = null,
            string? errorMessage = null)
        {
            return new AuditLog
            {
                ActorId = actorId,
                ActorUsername = username,
                ActorRole = actorRole ?? string.Empty,
                ActionType = actionType,
                TargetEntity = "User",
                TargetDisplay = username,
                TargetId = actorId,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                Source = "WebAdmin"
            };
        }
    }
}
