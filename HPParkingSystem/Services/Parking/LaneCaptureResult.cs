namespace HPParkingSystem.Services.Parking
{
    /// <summary>
    /// Kết quả chụp ảnh từ các camera tại làn xe.
    /// Thay thế cho kiểu 4-tuple nguyên thủy (bool, bool, string, string).
    /// </summary>
    public class LaneCaptureResult
    {
        /// <summary>
        /// Chụp camera biển số thành công
        /// </summary>
        public bool PlateCamSuccess { get; set; }

        /// <summary>
        /// Chụp camera toàn cảnh thành công
        /// </summary>
        public bool OverviewCamSuccess { get; set; }

        /// <summary>
        /// Đường dẫn file ảnh biển số thực tế sau khi chụp (hoặc sau khi fallback)
        /// </summary>
        public string PlateImagePath { get; set; }

        /// <summary>
        /// Đường dẫn file ảnh toàn cảnh thực tế sau khi chụp (hoặc sau khi fallback)
        /// </summary>
        public string OverviewImagePath { get; set; }

        public LaneCaptureResult(
            bool plateCamSuccess,
            bool overviewCamSuccess,
            string plateImagePath,
            string overviewImagePath)
        {
            PlateCamSuccess = plateCamSuccess;
            OverviewCamSuccess = overviewCamSuccess;
            PlateImagePath = plateImagePath ?? string.Empty;
            OverviewImagePath = overviewImagePath ?? string.Empty;
        }

        /// <summary>
        /// Cho phép phân rã (deconstruction) nếu cần
        /// </summary>
        public void Deconstruct(
            out bool plateCamSuccess,
            out bool overviewCamSuccess,
            out string plateImagePath,
            out string overviewImagePath)
        {
            plateCamSuccess = PlateCamSuccess;
            overviewCamSuccess = OverviewCamSuccess;
            plateImagePath = PlateImagePath;
            overviewImagePath = OverviewImagePath;
        }
    }
}
