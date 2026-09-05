using System;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Offline
{
    /// <summary>
    /// Giám sát trạng thái kết nối tới máy chủ MongoDB (Circuit Breaker / Health Tracker)
    /// Cung cấp cờ IsServerOnline trong bộ nhớ để các luồng thời gian thực rẽ nhánh tức thì (< 1ms)
    /// </summary>
    public interface IServerHealthTracker
    {
        /// <summary>
        /// Trạng thái kết nối máy chủ hiện tại (được cập nhật liên tục bởi tiến trình nền)
        /// </summary>
        bool IsServerOnline { get; }

        /// <summary>
        /// Số lần kiểm tra thành công liên tiếp gần nhất
        /// </summary>
        int ConsecutiveSuccesses { get; }

        /// <summary>
        /// Số lần kiểm tra thất bại liên tiếp gần nhất
        /// </summary>
        int ConsecutiveFailures { get; }

        /// <summary>
        /// Kích hoạt chuyển ngay lập tức sang trạng thái Ngoại tuyến khi một thao tác I/O thực tế gặp lỗi mạng đột xuất
        /// </summary>
        void MarkOffline(string? reason = null);

        /// <summary>
        /// Đánh dấu máy chủ đã sẵn sàng kết nối lại
        /// </summary>
        void MarkOnline();

        /// <summary>
        /// Thực hiện một chu kỳ kiểm tra sức khỏe 2 lớp (Socket TCP Ping + Mongo Driver Ping)
        /// </summary>
        Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sự kiện phát ra khi trạng thái kết nối chuyển đổi giữa Online và Offline
        /// </summary>
        event EventHandler<bool>? ServerStatusChanged;
    }
}
