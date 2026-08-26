using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PhuXuanParkingSystem.Licensing;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Repositories;

namespace PhuXuanParkingSystem.Api.Helpers
{
    public static class LicenseValidationHelper
    {
        private static readonly string LicenseFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PhuXuanParkingSystem",
            "license.lic"
        );

        public static async Task<LicensePayload> GetCurrentPayloadAsync(IRepository<LicenseInfo> licenseRepo)
        {
            string machineCode = HardwareFingerprint.GetMachineCode();
            string? key = null;

            try
            {
                var all = await licenseRepo.GetAllAsync();
                var active = all.Where(l => l.IsActive && !l.IsDeleted).OrderByDescending(l => l.CreatedAt).FirstOrDefault();
                if (active != null && !string.IsNullOrWhiteSpace(active.LicenseKey))
                {
                    key = active.LicenseKey;
                }
            }
            catch { }

            if (string.IsNullOrWhiteSpace(key) && File.Exists(LicenseFilePath))
            {
                try
                {
                    key = await File.ReadAllTextAsync(LicenseFilePath);
                }
                catch { }
            }

            if (!string.IsNullOrWhiteSpace(key))
            {
                var val = LicenseCrypto.ValidateLicense(key!, null, machineCode);
                if (val.IsValid && val.Payload != null)
                {
                    return val.Payload;
                }
            }

            // Gói mặc định cơ bản khi chưa có key hoặc chưa kích hoạt
            return new LicensePayload
            {
                CustomerName = "Dùng Thử / Chưa Kích Hoạt",
                MachineCode = machineCode,
                ExpiryDate = DateTime.Now.AddDays(7),
                MaxLanes = 2,
                MaxCameras = 4,
                MaxControllers = 1
            };
        }
    }
}
