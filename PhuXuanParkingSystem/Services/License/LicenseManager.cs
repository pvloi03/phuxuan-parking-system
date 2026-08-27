using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PhuXuanParkingSystem.Licensing;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Repositories;

namespace PhuXuanParkingSystem.Services.License
{
    public class LicenseManager
    {
        private static readonly string LicenseFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PhuXuanParkingSystem",
            "license.lic"
        );

        private readonly MongoRepository<LicenseInfo>? _licenseRepo;

        public LicenseManager(MongoRepository<LicenseInfo>? licenseRepo = null)
        {
            _licenseRepo = licenseRepo;
        }

        public async Task<string?> GetCurrentLicenseKeyAsync()
        {
            // 1. Thử đọc từ MongoDB trước
            if (_licenseRepo != null)
            {
                try
                {
                    var all = await _licenseRepo.GetAllAsync();
                    var activeLicense = all.Where(l => l.IsActive && !l.IsDeleted).OrderByDescending(l => l.CreatedAt).FirstOrDefault();
                    if (activeLicense != null && !string.IsNullOrWhiteSpace(activeLicense.LicenseKey))
                    {
                        return activeLicense.LicenseKey;
                    }
                }
                catch
                {
                    // Fallback to local file nếu chưa kết nối được DB
                }
            }

            // 2. Thử đọc từ file cục bộ
            if (File.Exists(LicenseFilePath))
            {
                try
                {
                    string key = File.ReadAllText(LicenseFilePath).Trim();
                    if (!string.IsNullOrWhiteSpace(key))
                        return key;
                }
                catch { }
            }

            return null;
        }

        public async Task SaveLicenseKeyAsync(string licenseKey, LicensePayload payload)
        {
            // 1. Lưu file cục bộ
            try
            {
                string dir = Path.GetDirectoryName(LicenseFilePath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(LicenseFilePath, licenseKey);
            }
            catch { }

            // 2. Lưu vào MongoDB
            if (_licenseRepo != null)
            {
                try
                {
                    var entity = new LicenseInfo(
                        customerName: payload.CustomerName,
                        machineCode: payload.MachineCode,
                        expiryDate: payload.ExpiryDate,
                        licenseKey: licenseKey,
                        maxLanes: payload.MaxLanes,
                        maxCameras: payload.MaxCameras,
                        maxControllers: payload.MaxControllers,
                        features: payload.Features
                    );

                    await _licenseRepo.AddAsync(entity);
                }
                catch { }
            }
        }
    }
}
