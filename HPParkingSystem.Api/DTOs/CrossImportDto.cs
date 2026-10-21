using HPParkingSystem.Models.Enums;

namespace HPParkingSystem.Api.DTOs
{
    /// <summary>
    /// DTO đại diện cho một dòng dữ liệu nhập chéo từ file Excel (chứa cả thông tin Phương tiện & Nhân sự)
    /// </summary>
    public class VehiclePersonCrossImportItemDto
    {
        // --- THÔNG TIN PHƯƠNG TIỆN ---
        public string? PlateNumber { get; set; }
        public VehicleType? VehicleType { get; set; }
        public double? Weight { get; set; }
        public bool? VehicleIsActive { get; set; }

        // --- THÔNG TIN NHÂN SỰ ---
        public string? PersonCode { get; set; }
        public string? FullName { get; set; }
        public PersonType? PersonType { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? DepartmentName { get; set; }
        public string? DepartmentId { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyId { get; set; }
        public string? ContractorName { get; set; }
        public string? ContractorId { get; set; }
        public bool? PersonIsActive { get; set; }
    }

    /// <summary>
    /// Kết quả thống kê sau khi thực hiện nhập chéo file Excel
    /// </summary>
    public class CrossImportResultDto
    {
        public int TotalRows { get; set; }
        public int PeopleCreated { get; set; }
        public int PeopleUpdated { get; set; }
        public int VehiclesCreated { get; set; }
        public int VehiclesUpdated { get; set; }
        public int LinksCreated { get; set; }
        public List<string> Errors { get; set; } = new();

        public string SummaryMessage =>
            $"Xử lý {TotalRows} dòng: Nhân sự ({PeopleCreated} mới, {PeopleUpdated} cập nhật); Phương tiện ({VehiclesCreated} mới, {VehiclesUpdated} cập nhật); {LinksCreated} lượt liên kết xe - chủ xe.";
    }
}
