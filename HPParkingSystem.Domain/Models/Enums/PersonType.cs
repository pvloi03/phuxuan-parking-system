namespace HPParkingSystem.Models.Enums
{
    public enum PersonType
    {
        Employee = 1,     // Cán bộ công nhân viên
        Contractor = 2,   // Nhà thầu / Đối tác
        Visitor = 3,      // Khách thăm (đã đăng ký hồ sơ)
        VIP = 4,          // Khách VIP / Ban giám đốc
        Guest = 5,        // Khách vãng lai (chưa đăng ký hồ sơ)
        Other = 99        // Khác
    }
}
