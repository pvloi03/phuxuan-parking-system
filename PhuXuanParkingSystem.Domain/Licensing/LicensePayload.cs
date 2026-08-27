using System;
using System.Collections.Generic;

namespace PhuXuanParkingSystem.Licensing
{
    /// <summary>
    /// Nội dung gói dữ liệu Bản quyền (Payload JSON) được ký số điện tử
    /// </summary>
    public class LicensePayload
    {
        public string CustomerName { get; set; } = string.Empty;
        public string MachineCode { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public DateTime IssuedAt { get; set; } = DateTime.Now;
        public int MaxLanes { get; set; } = 2;
        public int MaxCameras { get; set; } = 4;
        public int MaxControllers { get; set; } = 1;
        public List<string> Features { get; set; } = new()
        {
            "ANPR_Vietnam",
            "AutoBarrier",
            "DualCameraPerLane"
        };
        public string Version { get; set; } = "1.0";
        public string? Note { get; set; }

        public bool IsPermanent => ExpiryDate.Year >= 2099;
    }

    /// <summary>
    /// Kết quả xác thực License Key
    /// </summary>
    public class LicenseValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public LicensePayload? Payload { get; set; }
        public int DaysRemaining { get; set; }
        public bool IsExpired { get; set; }
        public bool IsMachineMatched { get; set; }
    }
}
