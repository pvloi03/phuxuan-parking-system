using System;
using PhuXuanParkingSystem.Models.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace PhuXuanParkingSystem.Models.Entities
{
    /// <summary>
    /// Entity đại diện cho thông tin Bản quyền / Giấy phép sử dụng phần mềm (License Key)
    /// </summary>
    [BsonIgnoreExtraElements]
    public class LicenseInfo : BaseEntity
    {
        // =========================================================================
        // --- 1. CÁC TRƯỜNG LƯU TRỮ DATABASE (PERSISTED PROPERTIES) ---
        // =========================================================================
        public string CustomerName { get; set; } = string.Empty;      // [LƯU DB] Tên khách hàng / Đơn vị được cấp bản quyền
        public string MachineCode { get; set; } = string.Empty;       // [LƯU DB] Mã máy tính đã đăng ký (Hardware Fingerprint)
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime ExpiryDate { get; set; }                      // [LƯU DB] Thời điểm hết hạn bản quyền (UTC)
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime IssuedAt { get; set; } = DateTime.Now;        // [LƯU DB] Ngày cấp bản quyền
        public string LicenseKey { get; set; } = string.Empty;        // [LƯU DB] Chuỗi License Key mã hóa & ký số RSA
        public string? Signature { get; set; }                        // [LƯU DB] Chữ ký số điện tử xác thực tính toàn vẹn
        public bool IsActive { get; set; } = true;                    // [LƯU DB] Trạng thái hiệu lực

        // =========================================================================
        // --- 2. CÁC THUỘC TÍNH TÍNH TOÁN TRONG RAM (COMPUTED GETTERS) ---
        // =========================================================================

        /// <summary>
        /// [KHÔNG LƯU DB] Kiểm tra bản quyền đã hết hạn hay chưa
        /// </summary>
        [BsonIgnore]
        public bool IsExpired => DateTime.Now > ExpiryDate;

        /// <summary>
        /// [KHÔNG LƯU DB] Số ngày sử dụng còn lại
        /// </summary>
        [BsonIgnore]
        public int DaysRemaining
        {
            get
            {
                if (IsExpired) return 0;
                return (int)Math.Ceiling((ExpiryDate - DateTime.Now).TotalDays);
            }
        }

        /// <summary>
        /// [KHÔNG LƯU DB] Bản quyền có đang hợp lệ để sử dụng hay không
        /// </summary>
        [BsonIgnore]
        public bool IsValid => IsActive && !IsDeleted && !IsExpired && !string.IsNullOrWhiteSpace(LicenseKey);

        public LicenseInfo() { }

        public LicenseInfo(
            string customerName,
            string machineCode,
            DateTime expiryDate,
            string licenseKey,
            string? signature = null)
        {
            CustomerName = customerName;
            MachineCode = machineCode;
            ExpiryDate = expiryDate;
            LicenseKey = licenseKey;
            Signature = signature;
            IssuedAt = DateTime.Now;
            IsActive = true;
        }
    }
}
