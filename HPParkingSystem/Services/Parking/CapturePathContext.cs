namespace HPParkingSystem.Services.Parking
{
    /// <summary>
    /// Chứa thông tin ngữ cảnh đường dẫn ảnh chụp cho một lượt xe (vào/ra).
    /// Gom cụm các tham số đường dẫn và cờ fallback giữa PrepareCapturePaths, CaptureSnapshotsAsync và QueueOfflineImageSync.
    /// </summary>
    public class CapturePathContext
    {
        /// <summary>
        /// Đường dẫn đầy đủ file ảnh biển số ban đầu (máy chủ hoặc cục bộ)
        /// </summary>
        public string PlateFilePath { get; set; }

        /// <summary>
        /// Đường dẫn đầy đủ file ảnh toàn cảnh ban đầu (máy chủ hoặc cục bộ)
        /// </summary>
        public string OverviewFilePath { get; set; }

        /// <summary>
        /// True nếu đã fallback sang lưu cục bộ do mất kết nối máy chủ/share mạng
        /// </summary>
        public bool IsLocalFallback { get; set; }

        /// <summary>
        /// Tên thư mục ngày (định dạng yyyy-MM-dd)
        /// </summary>
        public string DateFolder { get; set; }

        /// <summary>
        /// Tên file ảnh biển số (ví dụ: 20261012_110000_000_a1b2_RADAR_in_plate.jpg)
        /// </summary>
        public string PlateFileName { get; set; }

        /// <summary>
        /// Tên file ảnh toàn cảnh (ví dụ: 20261012_110000_000_a1b2_RADAR_in_overview.jpg)
        /// </summary>
        public string OverviewFileName { get; set; }

        public CapturePathContext(
            string plateFilePath,
            string overviewFilePath,
            bool isLocalFallback,
            string dateFolder,
            string plateFileName,
            string overviewFileName)
        {
            PlateFilePath = plateFilePath ?? string.Empty;
            OverviewFilePath = overviewFilePath ?? string.Empty;
            IsLocalFallback = isLocalFallback;
            DateFolder = dateFolder ?? string.Empty;
            PlateFileName = plateFileName ?? string.Empty;
            OverviewFileName = overviewFileName ?? string.Empty;
        }
    }
}
