using PhuXuanParkingSystem.Services.Anpr;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.Parking
{
    /// <summary>
    /// Giao diện dịch vụ nghiệp vụ điều phối làn kiểm soát vào/ra
    /// </summary>
    public interface IParkingLaneService
    {
        Task<LaneProcessResult> ProcessInLaneAsync(PlateRecognitionResult? anprResult, string triggerSource, string filePlate, string fileOverview);
        Task<LaneProcessResult> ProcessOutLaneAsync(PlateRecognitionResult? anprResult, string triggerSource, string filePlate, string fileOverview);
    }
}
