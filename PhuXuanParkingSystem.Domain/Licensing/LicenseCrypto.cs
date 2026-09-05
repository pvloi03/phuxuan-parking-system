using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Configuration;
#if NET6_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace PhuXuanParkingSystem.Licensing
{
    /// <summary>
    /// Engine Mã Hóa & Ký Số RSA 3072-bit cho Hệ Thống Bản Quyền
    /// </summary>
#if NET6_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    public static class LicenseCrypto
    {
        private const int KeySize = 3072;
        public const string LicensePrefix = "PX-LIC-";

        /// <summary>
        /// Lấy Public Key từ file App.config (nguồn cấu hình duy nhất)
        /// </summary>
        public static string GetConfiguredPublicKey()
        {
            string? configKey = ConfigurationManager.AppSettings["License_PublicKey"];
            if (!string.IsNullOrWhiteSpace(configKey))
            {
                return configKey.Trim();
            }

            throw new InvalidOperationException("Khóa công khai (License_PublicKey) chưa được cấu hình trong file App.config.");
        }

        /// <summary>
        /// Lấy Private Key từ file App.config (nguồn cấu hình duy nhất)
        /// </summary>
        public static string GetConfiguredPrivateKey()
        {
            string? configKey = ConfigurationManager.AppSettings["License_PrivateKey"];
            if (!string.IsNullOrWhiteSpace(configKey))
            {
                return configKey.Trim();
            }

            throw new InvalidOperationException("Khóa riêng tư (License_PrivateKey) chưa được cấu hình trong file App.config.");
        }

        /// <summary>
        /// Sinh cặp khóa RSA 3072-bit mới (Dành cho Tool Tạo Key)
        /// </summary>
        public static (string PublicKeyXml, string PrivateKeyXml) GenerateKeyPair()
        {
            using var rsa = RSA.Create();
            rsa.KeySize = KeySize;
            string privateKeyXml = rsa.ToXmlString(true);
            string publicKeyXml = rsa.ToXmlString(false);
            return (publicKeyXml, privateKeyXml);
        }

        /// <summary>
        /// Ký số và tạo chuỗi License Key hoàn chỉnh từ Payload
        /// </summary>
        public static string SignLicense(LicensePayload payload, string? privateKeyXml = null)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            string privKey = string.IsNullOrWhiteSpace(privateKeyXml)
                ? (GetConfiguredPrivateKey() ?? throw new ArgumentException("Private Key không được rỗng (chưa cấu hình trong App.config hoặc tham số).", nameof(privateKeyXml)))
                : privateKeyXml!;

            string jsonPayload = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            byte[] dataBytes = Encoding.UTF8.GetBytes(jsonPayload);

            using (var rsa = RSA.Create())
            {
                rsa.FromXmlString(privKey);
                byte[] signatureBytes = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                string signatureBase64 = Convert.ToBase64String(signatureBytes);

                // Gói Payload + Chữ ký
                string combined = $"{jsonPayload}|||{signatureBase64}";
                string finalBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(combined));

                return $"{LicensePrefix}{finalBase64}";
            }
        }

        /// <summary>
        /// Xác thực và giải mã chuỗi License Key
        /// </summary>
        public static LicenseValidationResult ValidateLicense(
            string rawLicenseKey,
            string? publicKeyXml = null,
            string? expectedMachineCode = null)
        {
            var result = new LicenseValidationResult { IsValid = false };

            if (string.IsNullOrWhiteSpace(rawLicenseKey))
            {
                result.Message = "Chuỗi bản quyền (License Key) đang trống.";
                return result;
            }

            try
            {
                string keyToProcess = rawLicenseKey.Trim();
                if (keyToProcess.StartsWith(LicensePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    keyToProcess = keyToProcess.Substring(LicensePrefix.Length);
                }

                // 1. Giải mã Base64
                byte[] combinedBytes = Convert.FromBase64String(keyToProcess);
                string combinedString = Encoding.UTF8.GetString(combinedBytes);

                string[] parts = combinedString.Split(new[] { "|||" }, StringSplitOptions.None);
                if (parts.Length != 2)
                {
                    result.Message = "Cấu trúc License Key không đúng định dạng.";
                    return result;
                }

                string jsonPayload = parts[0];
                string signatureBase64 = parts[1];

                // 2. Xác thực chữ ký số RSA với Public Key (Ưu tiên từ App.config)
                string pubKey = string.IsNullOrWhiteSpace(publicKeyXml) ? GetConfiguredPublicKey() : publicKeyXml!;
                byte[] dataBytes = Encoding.UTF8.GetBytes(jsonPayload);
                byte[] signatureBytes = Convert.FromBase64String(signatureBase64);

                using (var rsa = RSA.Create())
                {
                    rsa.FromXmlString(pubKey);
                    bool isSignatureValid = rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                    if (!isSignatureValid)
                    {
                        result.Message = "Chữ ký số không hợp lệ hoặc dữ liệu bản quyền đã bị chỉnh sửa.";
                        return result;
                    }
                }

                // 3. Deserialize Payload
                var payload = JsonSerializer.Deserialize<LicensePayload>(jsonPayload);
                if (payload == null)
                {
                    result.Message = "Không thể đọc thông tin bản quyền từ gói dữ liệu.";
                    return result;
                }

                result.Payload = payload;

                // 4. Kiểm tra Machine Code
                string currentMachine = string.IsNullOrWhiteSpace(expectedMachineCode)
                    ? HardwareFingerprint.GetMachineCode()
                    : expectedMachineCode!;

                bool machineMatch = string.Equals(payload.MachineCode?.Trim(), currentMachine?.Trim(), StringComparison.OrdinalIgnoreCase);
                result.IsMachineMatched = machineMatch;

                if (!machineMatch)
                {
                    result.Message = $"Bản quyền không khớp với máy tính hiện tại (Mã máy đăng ký: {payload.MachineCode} | Mã máy này: {currentMachine}).";
                    return result;
                }

                // 5. Kiểm tra thời hạn
                if (payload.IsPermanent)
                {
                    result.DaysRemaining = 99999;
                    result.IsExpired = false;
                    result.IsValid = true;
                    result.Message = "Bản quyền Vĩnh Viễn hợp lệ.";
                    return result;
                }

                DateTime now = DateTime.Now;
                if (now > payload.ExpiryDate)
                {
                    result.IsExpired = true;
                    result.DaysRemaining = 0;
                    result.Message = $"Bản quyền đã hết hạn vào ngày {payload.ExpiryDate:dd/MM/yyyy HH:mm}. Vui lòng liên hệ nhà cung cấp để gia hạn.";
                    return result;
                }

                int days = (int)Math.Ceiling((payload.ExpiryDate - now).TotalDays);
                result.DaysRemaining = days;
                result.IsExpired = false;
                result.IsValid = true;
                result.Message = $"Bản quyền hợp lệ. Còn lại {days} ngày (Hết hạn: {payload.ExpiryDate:dd/MM/yyyy}).";

                return result;
            }
            catch (Exception ex)
            {
                result.Message = "Lỗi khi xử lý bản quyền: " + ex.Message;
                return result;
            }
        }
    }
}
