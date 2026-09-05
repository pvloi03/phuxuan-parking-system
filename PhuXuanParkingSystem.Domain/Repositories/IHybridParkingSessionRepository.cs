using PhuXuanParkingSystem.Models.Entities;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Repositories
{
    /// <summary>
    /// Giao diện Repository chuyên biệt cho Phiên gửi xe, kết hợp linh hoạt giữa MongoDB Server và LiteDB Ngoại tuyến
    /// </summary>
    public interface IHybridParkingSessionRepository : IRepository<ParkingSession>
    {
        /// <summary>
        /// Tìm kiếm phiên xe đang hoạt động trong bãi (Status = Active) theo biển số xe đã làm sạch
        /// Kiểm tra thông minh xuyên suốt giữa MongoDB Server và LiteDB Local
        /// </summary>
        Task<ParkingSession?> GetActiveSessionByPlateAsync(string cleanPlateNumber);

        /// <summary>
        /// Ghi nhận lượt xe vào bãi với độ trễ < 2ms: ghi trực tiếp MongoDB nếu Online, fallback LiteDB nếu Offline
        /// </summary>
        Task CheckInAsync(ParkingSession session);

        /// <summary>
        /// Ghi nhận lượt xe ra bãi, giải quyết xung đột lượt vào chưa kịp sync lên server
        /// </summary>
        Task CheckOutAsync(ParkingSession session);

        /// <summary>
        /// Đếm số lượng phiên gửi xe đang tồn đọng trong hàng đợi ngoại tuyến chưa đồng bộ lên máy chủ
        /// </summary>
        Task<int> GetPendingSyncCountAsync();
    }
}
