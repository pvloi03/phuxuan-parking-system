using System;
using System.Threading;
using System.Threading.Tasks;

namespace HPParkingSystem.Services.Offline
{
    /// <summary>
    /// Giám sát trạng thái kết nối tới máy chủ MongoDB và Thư mục lưu ảnh chia sẻ (Circuit Breaker / Health Tracker)
    /// Cung cấp cờ IsServerOnline trong bộ nhớ để các luồng thời gian thực rẽ nhánh tức thì (< 1ms)
    /// </summary>
    public interface IServerHealthTracker
    {
        /// <summary>
        /// Trạng thái kết nối máy chủ hiện tại (được cập nhật liên tục bởi tiến trình nền)
        /// </summary>
        bool IsServerOnline { get; }

        /// <summary>
        /// Trạng thái thư mục chia sẻ ảnh trên máy chủ (SMB Network Share)
        /// </summary>
        bool IsImageShareOnline { get; }

        /// <summary>
        /// Thông tin lỗi MongoDB gần nhất nếu có
        /// </summary>
        string? LastMongoError { get; }

        /// <summary>
        /// Thông tin lỗi thư mục chia sẻ ảnh gần nhất nếu có
        /// </summary>
        string? LastImageShareError { get; }

        /// <summary>
        /// Có bất kỳ sự cố máy chủ nào đang xảy ra hay không
        /// </summary>
        bool HasIncident { get; }

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
        /// Cập nhật trạng thái khả dụng của thư mục chia sẻ ảnh trên máy chủ
        /// </summary>
        void MarkImageShareStatus(bool isOk, string? error = null);

        /// <summary>
        /// Cập nhật trạng thái và ghi nhận lỗi thao tác MongoDB (truy vấn hoặc ghi dữ liệu)
        /// </summary>
        void MarkMongoStatus(bool isOk, string? error = null);

        /// <summary>
        /// Tạo bản tóm tắt chi tiết sự cố phục vụ hiển thị hộp thoại chẩn đoán
        /// </summary>
        string GetIncidentSummary(int pendingRecords, int pendingImages);

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
