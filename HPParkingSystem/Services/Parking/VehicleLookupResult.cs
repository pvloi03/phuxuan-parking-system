using HPParkingSystem.Models.Enums;

namespace HPParkingSystem.Services.Parking
{
    /// <summary>
    /// Kết quả tra cứu thông tin phương tiện và chủ xe đã đăng ký.
    /// Thay thế cho bộ 8 giá trị (tuple) truyền cùng nhau giữa các luồng xử lý làn xe.
    /// </summary>
    public record VehicleLookupResult(
        string? PersonName,
        string? DepartmentName,
        string? CompanyName,
        string? PersonId,
        VehicleType VehicleType,
        PersonType PersonType,
        bool IsRegistered,
        double? Weight)
    {
        public static VehicleLookupResult Unregistered(VehicleType defaultType = VehicleType.Car) =>
            new(null, null, null, null, defaultType, PersonType.Visitor, false, null);
    }
}

#if NET48 || NETSTANDARD2_0
namespace System.Runtime.CompilerServices
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}
#endif
