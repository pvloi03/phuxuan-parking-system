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

    public static class PersonTypeExtensions
    {
        public static string ToDisplayName(this PersonType pt) => pt switch
        {
            PersonType.Employee => "Cán bộ / Nhân viên",
            PersonType.Contractor => "Đối tác / Nhà thầu",
            PersonType.Visitor => "Khách thăm",
            PersonType.VIP => "Khách VIP",
            PersonType.Guest => "Khách vãng lai",
            _ => "Khác"
        };

        public static string ToDisplayName(this PersonType? pt, string? personName = null)
        {
            if (pt.HasValue)
            {
                return pt.Value.ToDisplayName();
            }
            return !string.IsNullOrWhiteSpace(personName) ? "Xe đăng ký" : "Khách vãng lai";
        }
    }
}
