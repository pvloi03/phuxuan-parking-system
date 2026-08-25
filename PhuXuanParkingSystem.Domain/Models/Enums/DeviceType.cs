namespace PhuXuanParkingSystem.Models.Enums
{
    /// <summary>
    /// Phân loại thiết bị phần cứng trong hệ thống bãi đỗ xe HPParking:
    /// - PlateCamera: Camera chuyên dụng chụp và nhận diện biển số xe (ANPR/LPR)
    /// - OverviewCamera: Camera quan sát toàn cảnh người và phương tiện
    /// - Controller: Bộ điều khiển Barrier nhận tín hiệu cảm biến/Radar & điều khiển đóng/mở cổng
    /// </summary>
    public enum DeviceType
    {
        PlateCamera = 1,     // Camera chụp ảnh nhận diện biển số xe
        OverviewCamera = 2,  // Camera chụp ảnh toàn cảnh làn xe
        Controller = 3,      // Bộ điều khiển Barrier & Cảm biến Radar
    }
}
