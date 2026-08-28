namespace PhuXuanParkingSystem.Models.Enums
{
    /// <summary>
    /// Trạng thái kết nối của thiết bị (Device, Camera, Controller)
    /// Dùng cho WinForms UI layer để hiển thị trạng thái và sync với LiveView/ReadLog
    ///
    /// State Machine:
    ///   Disconnected → (Connect) → Connecting → (Success) → Connected → (StartPreview) → Streaming
    ///        ↑                            ↓
    ///        └─────── (Error/Fail) ← ← ← ┘
    /// </summary>
    public enum DeviceConnectionState
    {
        /// <summary>
        /// Chưa kết nối hoặc đã ngắt kết nối
        /// </summary>
        Disconnected = 0,

        /// <summary>
        /// Đang trong quá trình kết nối
        /// </summary>
        Connecting = 1,

        /// <summary>
        /// Đã kết nối thành công (SDK login OK) nhưng chưa streaming
        /// </summary>
        Connected = 2,

        /// <summary>
        /// Đã kết nối và đang streaming video hoặc nhận log
        /// </summary>
        Streaming = 3,

        /// <summary>
        /// Kết nối bị lỗi (sau khi retry thất bại)
        /// </summary>
        Error = -1
    }
}
