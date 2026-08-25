using Microsoft.AspNetCore.SignalR;

namespace PhuXuanParkingSystem.Api.Hubs
{
    /// <summary>
    /// Hub SignalR truyền phát sự kiện realtime xe vào/ra cho Web Admin
    /// </summary>
    public class ParkingRealtimeHub : Hub
    {
        public async Task NotifyLaneEvent(string laneType, string plateNumber, string status)
        {
            await Clients.All.SendAsync("ReceiveLaneEvent", new
            {
                LaneType = laneType,
                PlateNumber = plateNumber,
                Status = status,
                Timestamp = DateTime.Now
            });
        }
    }
}
