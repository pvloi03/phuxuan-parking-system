using System;
using System.Collections.Generic;
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
        public DateTime ExpiryDate { get; set; }                      // [LƯU DB] Thời điểm hết hạn bản quyền (Local)
        
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime IssuedAt { get; set; } = DateTime.Now;        // [LƯU DB] Ngày cấp bản quyền
        
        public string LicenseKey { get; set; } = string.Empty;        // [LƯU DB] Chuỗi License Key mã hóa & ký số RSA
        public string? Signature { get; set; }                        // [LƯU DB] Chữ ký số điện tử xác thực tính toàn vẹn
        public bool IsActive { get; set; } = true;                    // [LƯU DB] Trạng thái hiệu lực

        // --- CÁC GIỚI HẠN BẢN QUYỀN (QUOTA LIMITS) ---
        public int MaxLanes { get; set; } = 2;                        // [LƯU DB] Số làn xe tối đa (Mặc định 2: 1 vào, 1 ra)
        public int MaxCameras { get; set; } = 4;                      // [LƯU DB] Số camera tối đa (2 biển số + 2 toàn cảnh)
        public int MaxControllers { get; set; } = 1;                  // [LƯU DB] Số bộ điều khiển tối đa (1 ZKTeco / Relay)
        public List<string> Features { get; set; } = new()            // [LƯU DB] Danh sách tính năng được cấp phép
        {
            "ANPR_Vietnam",
            "AutoBarrier",
            "DualCameraPerLane"
        };

        // =========================================================================
        // --- 2. CÁC THUỘC TÍNH TÍNH TOÁN TRONG RAM (COMPUTED GETTERS) ---
        // =========================================================================

        /// <summary>
        /// [KHÔNG LƯU DB] Kiểm tra bản quyền có phải là vĩnh viễn hay không
        /// </summary>
        [BsonIgnore]
        public bool IsPermanent => ExpiryDate.Year >= 2099;

        /// <summary>
        /// [KHÔNG LƯU DB] Kiểm tra bản quyền đã hết hạn hay chưa
        /// </summary>
        [BsonIgnore]
        public bool IsExpired => !IsPermanent && DateTime.Now > ExpiryDate;

        /// <summary>
        /// [KHÔNG LƯU DB] Số ngày sử dụng còn lại
        /// </summary>
        [BsonIgnore]
        public int DaysRemaining
        {
            get
            {
                if (IsPermanent) return 99999;
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
            string? signature = null,
            int maxLanes = 2,
            int maxCameras = 4,
            int maxControllers = 1,
            List<string>? features = null)
        {
            CustomerName = customerName;
            MachineCode = machineCode;
            ExpiryDate = expiryDate;
            LicenseKey = licenseKey;
            Signature = signature;
            MaxLanes = maxLanes;
            MaxCameras = maxCameras;
            MaxControllers = maxControllers;
            if (features != null) Features = features;
            IssuedAt = DateTime.Now;
            IsActive = true;
        }
    }
}
