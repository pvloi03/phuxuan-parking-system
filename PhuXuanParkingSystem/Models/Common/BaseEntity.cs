using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace PhuXuanParkingSystem.Models.Common
{
    /// <summary>
    /// Lớp cơ sở cho toàn bộ Entities trong hệ thống tương thích với MongoDB Driver
    /// Tự động sinh ObjectId khi Insert nếu Id để rỗng
    /// Hỗ trợ cơ chế Xóa mềm (Soft Delete)
    /// </summary>
    [BsonIgnoreExtraElements]
    public abstract class BaseEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? DeletedAt { get; set; }

        /// <summary>
        /// Thực hiện xóa mềm thực thể (không xóa mất dữ liệu trong CSDL)
        /// </summary>
        public virtual void MarkDeleted()
        {
            IsDeleted = true;
            DeletedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }

        /// <summary>
        /// Khôi phục thực thể đã xóa mềm
        /// </summary>
        public virtual void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
            UpdatedAt = DateTime.Now;
        }
    }
}
