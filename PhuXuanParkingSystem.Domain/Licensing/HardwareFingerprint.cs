using System;
using System.IO;
using System.Management;
#if NET6_0_OR_GREATER
using System.Runtime.Versioning;
#endif
using System.Security.Cryptography;
using System.Text;

namespace PhuXuanParkingSystem.Licensing
{
    /// <summary>
    /// Thuật toán tạo Mã Định Danh Phần Cứng (Hardware Fingerprint / Machine Code)
    /// Kết hợp CPU ID + Mainboard Serial + Disk Serial qua WMI
    /// </summary>
#if NET6_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    public static class HardwareFingerprint
    {
        private static string? _cachedMachineCode;

        /// <summary>
        /// Lấy Mã Máy Tính định dạng: PX-XXXX-XXXX-XXXX-XXXX
        /// </summary>
        public static string GetMachineCode()
        {
            if (!string.IsNullOrEmpty(_cachedMachineCode))
                return _cachedMachineCode!;

            try
            {
                string cpuId = GetWmiValue("Win32_Processor", "ProcessorId");
                string motherBoardSerial = GetWmiValue("Win32_BaseBoard", "SerialNumber");
                string diskSerial = GetWmiValue("Win32_DiskDrive", "SerialNumber");
                string biosSerial = GetWmiValue("Win32_BIOS", "SerialNumber");

                // Fallback nếu WMI trả về chuỗi rỗng
                if (string.IsNullOrWhiteSpace(cpuId) && string.IsNullOrWhiteSpace(motherBoardSerial) && string.IsNullOrWhiteSpace(diskSerial))
                {
                    cpuId = Environment.MachineName;
                    motherBoardSerial = Environment.OSVersion.VersionString;
                    diskSerial = Environment.SystemDirectory;
                }

                string rawHardwareData = $"CPU:{cpuId}|MB:{motherBoardSerial}|DISK:{diskSerial}|BIOS:{biosSerial}";

                using (var sha256 = SHA256.Create())
                {
                    byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawHardwareData));
                    string hex = BitConverter.ToString(hashBytes).Replace("-", "").ToUpperInvariant();

                    // Lấy 16 ký tự đầu và format thành PX-AAAA-BBBB-CCCC-DDDD
                    string p1 = hex.Substring(0, 4);
                    string p2 = hex.Substring(4, 4);
                    string p3 = hex.Substring(8, 4);
                    string p4 = hex.Substring(12, 4);

                    _cachedMachineCode = $"PX-{p1}-{p2}-{p3}-{p4}";
                    return _cachedMachineCode;
                }
            }
            catch
            {
                // Fallback khẩn cấp nếu gặp ngoại lệ cấp hệ thống
                string fallback = Environment.MachineName + "_" + Environment.UserName;
                using (var md5 = MD5.Create())
                {
                    byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(fallback));
                    string hex = BitConverter.ToString(hash).Replace("-", "").Substring(0, 16);
                    _cachedMachineCode = $"PX-{hex.Substring(0, 4)}-{hex.Substring(4, 4)}-{hex.Substring(8, 4)}-{hex.Substring(12, 4)}";
                    return _cachedMachineCode;
                }
            }
        }

        private static string GetWmiValue(string wmiClass, string propertyName)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {wmiClass}"))
                {
                    foreach (ManagementObject item in searcher.Get())
                    {
                        var value = item[propertyName];
                        if (value != null)
                        {
                            string str = value.ToString()?.Trim() ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(str) && !str.Equals("None", StringComparison.OrdinalIgnoreCase) && !str.Equals("To be filled by O.E.M.", StringComparison.OrdinalIgnoreCase))
                            {
                                return str;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Bỏ qua lỗi WMI trên môi trường hạn chế quyền
            }
            return string.Empty;
        }
    }
}
