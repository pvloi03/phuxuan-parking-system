using MongoDB.Driver;
using PhuXuanParkingSystem.Licensing;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Repositories;
using PhuXuanParkingSystem.Services.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Services.License
{
    /// <summary>
    /// Quản lý License bản quyền của hệ thống: Chỉ lưu và truy vấn trực tiếp từ CSDL MongoDB
    /// </summary>
    public class LicenseManager(IRepository<LicenseInfo>? licenseRepo = null)
    {
        private readonly IRepository<LicenseInfo> _licenseRepo = licenseRepo ?? new MongoRepository<LicenseInfo>();

        /// <summary>
        /// Lấy LicenseKey đang kích hoạt gần nhất trực tiếp từ MongoDB
        /// </summary>
        public async Task<string?> GetCurrentLicenseKeyAsync()
        {
            try
            {
                var filter = Builders<LicenseInfo>.Filter.Where(l => l.IsActive && !l.IsDeleted);
                var sort = Builders<LicenseInfo>.Sort.Descending(l => l.CreatedAt);
                var licenses = await _licenseRepo.FindAsync(filter, sort);
                var activeLicense = licenses.FirstOrDefault();

                if (activeLicense != null && !string.IsNullOrWhiteSpace(activeLicense.LicenseKey))
                {
                    return activeLicense.LicenseKey;
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Lỗi khi truy vấn LicenseKey từ CSDL MongoDB", "LicenseManager");
            }

            return null;
        }

        /// <summary>
        /// Lưu thông tin License mới kích hoạt trực tiếp vào MongoDB
        /// </summary>
        public async Task SaveLicenseKeyAsync(string licenseKey, LicensePayload payload)
        {
            try
            {
                var entity = new LicenseInfo(
                    customerName: payload.CustomerName,
                    machineCode: payload.MachineCode,
                    expiryDate: payload.ExpiryDate,
                    licenseKey: licenseKey
                );

                await _licenseRepo.AddAsync(entity);
                AppLogger.Information($"Đã lưu License bản quyền cho khách hàng [{payload.CustomerName}] vào MongoDB.", "LicenseManager");
            }
            catch (Exception ex)
            {
                AppLogger.Error(ex, "Lỗi khi lưu License bản quyền vào CSDL MongoDB", "LicenseManager");
                throw;
            }
        }
    }
}
