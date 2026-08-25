using System;
using System.Runtime.InteropServices;
using System.Text;

namespace PhuXuanParkingSystem.SDK.NST
{
    public static class CHISDK
    {
        public const string DllPath = "HISDK.dll";

        #region Constants

        public const int HI_SUCCESS = 0;
        public const int HI_FAILURE = -1;

        public const uint HI_CHANNEL_1 = 1;
        public const uint HI_CHANNEL_2 = 2;
        public const uint HI_CHANNEL_3 = 3;
        public const uint HI_CHANNEL_4 = 4;

        public const uint HI_STREAM_1 = 0; // Luồng chính (chất lượng cao)
        public const uint HI_STREAM_2 = 1; // Luồng phụ
        public const uint HI_STREAM_3 = 2; // Luồng di động

        public const uint HI_STREAM_MODE_TCP = 0;
        public const uint HI_STREAM_MODE_UDP = 1;

        public const byte HI_STREAM_VIDEO_ONLY = 0x01;
        public const byte HI_STREAM_AUDIO_ONLY = 0x02;
        public const byte HI_STREAM_VIDEO_AUDIO = 0x03;
        public const byte HI_STREAM_ALL = 0x07;

        #endregion

        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        public struct HI_S_STREAM_INFO
        {
            public uint u32Channel;
            public int blFlag;
            public uint u32Mode;
            public byte u8Type;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HI_S_STREAM_INFO_EXT
        {
            public uint u32Channel;
            public uint u32Stream;
            public uint u32Mode;
            public byte u8Type;
        }

        #endregion

        #region Native Methods

        [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
        public static extern int HI_SDK_Init();

        [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
        public static extern int HI_SDK_Cleanup();

        [DllImport(DllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HI_SDK_Login(
            string psHost,
            string psUsername,
            string psPassword,
            ushort u16Port,
            out int ps32Err);

        [DllImport(DllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HI_SDK_LoginExt(
            string psHost,
            string psUsername,
            string psPassword,
            ushort u16Port,
            uint u32TimeOut,
            out int ps32Err);

        [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
        public static extern int HI_SDK_Logout(int lHandle);

        [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
        public static extern int HI_SDK_SetConnectTime(int lHandle, uint u32Timeout);

        [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
        public static extern int HI_SDK_SetReconnect(int lHandle, uint u32Interval);

        [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
        public static extern int HI_SDK_RealPlay(
            int lHandle,
            IntPtr pWnd,
            ref HI_S_STREAM_INFO pstruStreamInfo);

        [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
        public static extern int HI_SDK_RealPlayExt(
            int lHandle,
            IntPtr pWnd,
            ref HI_S_STREAM_INFO_EXT pstruStreamInfo);

        [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
        public static extern int HI_SDK_StopRealPlay(int lHandle);

        [DllImport(DllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HI_SDK_CapturePicture(int lHandle, string pszFilePath);

        [DllImport(DllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HI_SDK_CaptureJPEGPicture(int lHandle, string sFilePath);

        [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
        public static extern int HI_SDK_SnapJpeg(
            int lHandle,
            [Out] byte[] pu8Data,
            int s32BufLen,
            out int pSize);

        [DllImport(DllPath, CallingConvention = CallingConvention.StdCall)]
        public static extern int HI_SDK_SetDisplayCallback(int lHandle, int bDisplayCallback);

        [DllImport(DllPath, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public static extern int HI_SDK_GetSDKVersion(StringBuilder pVersion);

        #endregion
    }
}
