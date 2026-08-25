using System;
using System.Runtime.InteropServices;

namespace HPParkingThaiThuy.SDK.ZKTeco
{
    /// <summary>
    /// P/Invoke Wrapper cho ZKTeco Pull SDK (plcommpro.dll)
    /// Hỗ trợ kết nối và đọc realtime log từ bộ điều khiển ZKTeco C3-200 / C3-400
    /// </summary>
    public static class ZKTecoPullSDK
    {
        private const string DllName = "plcommpro.dll";

        /// <summary>
        /// Kết nối tới thiết bị qua chuỗi tham số (TCP/IP hoặc RS485)
        /// </summary>
        /// <param name="parameters">Chuỗi tham số kết nối, ví dụ: "protocol=TCP,ipaddress=192.168.1.202,port=4370,timeout=4000,passwd="</param>
        /// <returns>IntPtr Handle kết nối (IntPtr.Zero nếu thất bại)</returns>
        [DllImport(DllName, EntryPoint = "Connect", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern IntPtr Connect(string parameters);

        /// <summary>
        /// Ngắt kết nối tới thiết bị và giải phóng Handle
        /// </summary>
        [DllImport(DllName, EntryPoint = "Disconnect", CallingConvention = CallingConvention.StdCall)]
        public static extern void Disconnect(IntPtr h);

        /// <summary>
        /// Lấy mã lỗi cuối cùng từ SDK
        /// </summary>
        [DllImport(DllName, EntryPoint = "PullLastError", CallingConvention = CallingConvention.StdCall)]
        public static extern int PullLastError();

        /// <summary>
        /// Đọc log sự kiện thời gian thực (Real-time Log) từ thiết bị
        /// </summary>
        /// <param name="h">Handle kết nối</param>
        /// <param name="buffer">Mảng byte nhận dữ liệu log (thường 256 bytes)</param>
        /// <param name="bufferSize">Kích thước buffer</param>
        /// <returns>>= 0 nếu có sự kiện mới, < 0 nếu không có sự kiện hoặc lỗi</returns>
        [DllImport(DllName, EntryPoint = "GetRTLog", CallingConvention = CallingConvention.StdCall)]
        public static extern int GetRTLog(IntPtr h, ref byte buffer, int bufferSize);

        /// <summary>
        /// Đọc thông số cấu hình từ thiết bị
        /// </summary>
        [DllImport(DllName, EntryPoint = "GetDeviceParam", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int GetDeviceParam(IntPtr h, ref byte buffer, int bufferSize, string item);

        /// <summary>
        /// Ghi thông số cấu hình xuống thiết bị
        /// </summary>
        [DllImport(DllName, EntryPoint = "SetDeviceParam", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int SetDeviceParam(IntPtr h, string item);

        /// <summary>
        /// Điều khiển thiết bị (Mở cửa, kích hoạt relay, v.v.)
        /// </summary>
        [DllImport(DllName, EntryPoint = "ControlDevice", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int ControlDevice(IntPtr h, int operationId, int param1, int param2, int param3, int param4, string options);

        /// <summary>
        /// Tìm kiếm thiết bị trong mạng LAN
        /// </summary>
        [DllImport(DllName, EntryPoint = "SearchDevice", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int SearchDevice(string commType, string address, ref byte buffer);
    }
}
