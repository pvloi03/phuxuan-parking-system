using System;

namespace PhuXuanParkingSystem.Services.Notification
{
    /// <summary>
    /// Dịch vụ trung tâm phát sự kiện thông báo cho toàn bộ ứng dụng PhuXuanParkingSystem
    /// - Cho phép UI (Form, StatusStrip, Toast, Popup, ListBox) đăng ký nhận mọi thông báo
    /// - Hỗ trợ phân loại: Thành công, Thông tin, Cảnh báo, Lỗi
    /// - An toàn tuyệt đối (Exception-safe & Thread-safe): Lỗi từ Subscriber không bao giờ làm crash Service
    /// </summary>
    public static class AppNotificationService
    {
        /// <summary>
        /// Sự kiện phát ra mỗi khi có thông báo mới trong ứng dụng
        /// </summary>
        public static event EventHandler<AppNotification>? OnNotificationReceived;

        /// <summary>
        /// Phát thông báo tổng quát
        /// </summary>
        public static void Notify(
            NotificationType type,
            NotificationCategory category,
            string title,
            string message,
            object? data = null)
        {
            var notification = new AppNotification(type, category, title, message, data);

            try
            {
                OnNotificationReceived?.Invoke(null, notification);
            }
            catch
            {
                // Bảo vệ luồng phát thông báo khỏi Exception của Subscriber UI
            }
        }

        /// <summary>
        /// Phát thông báo Thành Công (Màu xanh / Biểu tượng thành công)
        /// </summary>
        public static void NotifySuccess(
            NotificationCategory category,
            string title,
            string message,
            object? data = null)
        {
            Notify(NotificationType.Success, category, title, message, data);
        }

        /// <summary>
        /// Phát thông báo Thông Tin (Màu xanh dương / Tiến trình)
        /// </summary>
        public static void NotifyInfo(
            NotificationCategory category,
            string title,
            string message,
            object? data = null)
        {
            Notify(NotificationType.Info, category, title, message, data);
        }

        /// <summary>
        /// Phát thông báo Cảnh Báo (Màu cam/vàng / Cảnh báo cần chú ý)
        /// </summary>
        public static void NotifyWarning(
            NotificationCategory category,
            string title,
            string message,
            object? data = null)
        {
            Notify(NotificationType.Warning, category, title, message, data);
        }

        /// <summary>
        /// Phát thông báo Lỗi (Màu đỏ / Sự cố phát sinh)
        /// </summary>
        public static void NotifyError(
            NotificationCategory category,
            string title,
            string message,
            object? data = null)
        {
            Notify(NotificationType.Error, category, title, message, data);
        }
    }
}