namespace HPParkingThaiThuy.Models.Enums
{
    public enum ParkingSessionStatus
    {
        Active = 1,       // Đang trong bãi (Đã vào, chưa ra)
        Completed = 2,    // Đã hoàn thành (Đã vào và đã ra)
        UnmatchedOut = 3, // Xe ra không tìm thấy lượt vào tương ứng
        Cancelled = 4     // Hủy bỏ
    }
}
