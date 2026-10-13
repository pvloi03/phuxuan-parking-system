namespace HPParkingSystem.Models.Enums
{
    public enum VehicleType
    {
        Car = 1,          // Ô tô
        Motorcycle = 2,   // Xe máy
        Truck = 3,        // Xe tải
        Other = 99        // Khác
    }

    public static class VehicleTypeExtensions
    {
        public static string ToDisplayName(this VehicleType vt) => vt switch
        {
            VehicleType.Car => "Ô tô",
            VehicleType.Motorcycle => "Xe máy",
            VehicleType.Truck => "Xe tải",
            _ => "Khác"
        };
    }
}
