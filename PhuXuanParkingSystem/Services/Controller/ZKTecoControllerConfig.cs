namespace PhuXuanParkingSystem.Services.Controller
{
    public class ZKTecoControllerConfig
    {
        public string ConnectionType { get; set; } = "TCP"; // "TCP" or "RS485"

        public string Ip { get; set; } = "192.168.1.202";

        public int Port { get; set; } = 4370;

        public int Timeout { get; set; } = 3000;

        public string Password { get; set; } = "";

        // Dành cho kết nối RS485
        public string ComPort { get; set; } = "COM1";

        public int BaudRate { get; set; } = 38400;

        public int DeviceId { get; set; } = 1;

        public string ToConnectionString()
        {
            if (ConnectionType.ToUpperInvariant() == "RS485")
            {
                return $"protocol=RS485,port={ComPort},baudrate={BaudRate}bps,deviceid={DeviceId},timeout={Timeout},passwd={Password}";
            }

            return $"protocol=TCP,ipaddress={Ip},port={Port},timeout={Timeout},passwd={Password}";
        }
    }
}
