using System;

namespace PhuXuanParkingSystem.Services.Notification
{
    /// <summary>
    /// Mức độ / Phân loại tính chất thông báo
    /// </summary>
    public enum NotificationType
    {
        /// <summary>
        /// Thông tin tiến trình / hoạt động bình thường
        /// </summary>
        Info,

        /// <summary>
        /// Tác vụ thành công (kết nối OK, chụp ảnh OK, lưu CSDL OK)
        /// </summary>
        Success,

        /// <summary>
        /// Cảnh báo (mất kết nối tạm thời, chưa kết nối DB, thiết bị chưa sẵn sàng)
        /// </summary>
        Warning,

        /// <summary>
        /// Lỗi sự cố (lỗi SDK, lỗi phần cứng, ngoại lệ không mong muốn)
        /// </summary>
        Error
    }

    /// <summary>
    /// Nguồn gốc / Phân nhóm nghiệp vụ của thông báo
    /// </summary>
    public enum NotificationCategory
    {
        /// <summary>
        /// Toàn hệ thống (khởi động, sẵn sàng, cấu hình)
        /// </summary>
        System,

        /// <summary>
        /// Camera (Toàn cảnh, Biển số, luồng video, snapshot)
        /// </summary>
        Camera,

        /// <summary>
        /// Bộ điều khiển Controller ZKTeco C3-200 / Cảm biến Radar
        /// </summary>
        Controller,

        /// <summary>
        /// Làn Vào (Xe vào, phát hiện xe, chụp ảnh làn vào)
        /// </summary>
        LaneIn,

        /// <summary>
        /// Làn Ra (Xe ra, phát hiện xe, chụp ảnh làn ra)
        /// </summary>
        LaneOut,

        /// <summary>
        /// Cơ sở dữ liệu (MongoDB, Index, truy vấn)
        /// </summary>
        Database,

        /// <summary>
        /// Phương tiện / Thẻ xe (Quẹt thẻ, kiểm tra thông tin chủ xe)
        /// </summary>
        Vehicle,

        /// <summary>
        /// An ninh / Cảnh báo bất thường (Xe không hợp lệ, biển số lạ)
        /// </summary>
        Security
    }

    /// <summary>
    /// Đối tượng thông báo nghiệp vụ toàn cục của PhuXuanParkingSystem
    /// </summary>
    public class AppNotification : EventArgs
    {
        public Guid Id { get; } = Guid.NewGuid();

        public DateTime Timestamp { get; } = DateTime.Now;

        public NotificationType Type { get; }

        public NotificationCategory Category { get; }

        public string Title { get; }

        public string Message { get; }

        public object? Data { get; }

        public AppNotification(
            NotificationType type,
            NotificationCategory category,
            string title,
            string message,
            object? data = null)
        {
            Type = type;
            Category = category;
            Title = title ?? string.Empty;
            Message = message ?? string.Empty;
            Data = data;
        }

        /// <summary>
        /// Chuỗi tóm tắt trực quan có biểu tượng tương ứng
        /// </summary>
        public string FormattedSummary
        {
            get
            {
                string icon = Type switch
                {
                    NotificationType.Success => "🟢",
                    NotificationType.Info => "ℹ️",
                    NotificationType.Warning => "⚠️",
                    NotificationType.Error => "❌",
                    _ => "ℹ️"
                };

                return $"[{Timestamp:HH:mm:ss}] {icon} [{Category}] {Title}: {Message}";
            }
        }
    }
}