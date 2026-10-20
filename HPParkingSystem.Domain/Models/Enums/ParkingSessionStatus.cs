namespace HPParkingSystem.Models.Enums
{
    public enum ParkingSessionStatus
    {
        Active = 1,       // Đang trong bãi (Đã vào, chưa ra)
        Completed = 2,    // Đã hoàn thành (Đã vào và đã ra)
        UnmatchedOut = 3, // Xe ra không tìm thấy lượt vào tương ứng
        Cancelled = 4     // Hủy bỏ
    }

    public static class ParkingSessionStatusExtensions
    {
        public static string ToDisplayName(this ParkingSessionStatus status) => status switch
        {
            ParkingSessionStatus.Active => "Đang trong bãi",
            ParkingSessionStatus.Completed => "Đã hoàn thành",
            ParkingSessionStatus.UnmatchedOut => "Xe ra",
            ParkingSessionStatus.Cancelled => "Đã hủy",
            _ => status.ToString()
        };

        public static string ToDisplayName(this ParkingSessionStatus? status) =>
            status.HasValue ? status.Value.ToDisplayName() : "";
    }
}