using System;
using System.Collections.Generic;
using PhuXuanParkingSystem.Licensing;
using Xunit;

namespace PhuXuanParkingSystem.Tests.Licensing
{
    public class LicenseCryptoTests
    {
        [Fact]
        public void GenerateKeyPair_ShouldReturnValidRsaKeys()
        {
            var (publicKey, privateKey) = LicenseCrypto.GenerateKeyPair();

            Assert.False(string.IsNullOrWhiteSpace(publicKey));
            Assert.False(string.IsNullOrWhiteSpace(privateKey));
            Assert.Contains("<RSAKeyValue>", publicKey);
            Assert.Contains("<RSAKeyValue>", privateKey);
            Assert.Contains("<Modulus>", publicKey);
            Assert.Contains("<D>", privateKey); // Private key chứa D exponent
        }

        [Fact]
        public void SignAndValidate_ValidLicense_ShouldSucceed()
        {
            var (publicKey, privateKey) = LicenseCrypto.GenerateKeyPair();
            string machineCode = HardwareFingerprint.GetMachineCode();

            var payload = new LicensePayload
            {
                CustomerName = "Bãi Đỗ Xe Phú Xuân",
                MachineCode = machineCode,
                ExpiryDate = DateTime.Now.AddDays(30)
            };

            string licenseKey = LicenseCrypto.SignLicense(payload, privateKey);
            Assert.StartsWith(LicenseCrypto.LicensePrefix, licenseKey);

            var validation = LicenseCrypto.ValidateLicense(licenseKey, publicKey, machineCode);

            Assert.True(validation.IsValid);
            Assert.False(validation.IsExpired);
            Assert.True(validation.IsMachineMatched);
            Assert.Equal(30, validation.DaysRemaining);
            Assert.Equal("Bãi Đỗ Xe Phú Xuân", validation.Payload?.CustomerName);
        }

        [Fact]
        public void Validate_ExpiredLicense_ShouldFailWithExpiredMessage()
        {
            var (publicKey, privateKey) = LicenseCrypto.GenerateKeyPair();
            string machineCode = HardwareFingerprint.GetMachineCode();

            var payload = new LicensePayload
            {
                CustomerName = "Khách Hàng Hết Hạn",
                MachineCode = machineCode,
                ExpiryDate = DateTime.Now.AddDays(-1), // Đã hết hạn hôm qua
            };

            string licenseKey = LicenseCrypto.SignLicense(payload, privateKey);
            var validation = LicenseCrypto.ValidateLicense(licenseKey, publicKey, machineCode);

            Assert.False(validation.IsValid);
            Assert.True(validation.IsExpired);
            Assert.Equal(0, validation.DaysRemaining);
            Assert.Contains("hết hạn", validation.Message);
        }

        [Fact]
        public void Validate_WrongMachineCode_ShouldFail()
        {
            var (publicKey, privateKey) = LicenseCrypto.GenerateKeyPair();

            var payload = new LicensePayload
            {
                CustomerName = "Khách Hàng Máy A",
                MachineCode = "PX-AAAA-BBBB-CCCC-DDDD",
                ExpiryDate = DateTime.Now.AddDays(30)
            };

            string licenseKey = LicenseCrypto.SignLicense(payload, privateKey);
            var validation = LicenseCrypto.ValidateLicense(licenseKey, publicKey, "PX-XXXX-YYYY-ZZZZ-WWWW");

            Assert.False(validation.IsValid);
            Assert.False(validation.IsMachineMatched);
            Assert.Contains("không khớp", validation.Message);
        }

        [Fact]
        public void Validate_TamperedKey_ShouldFailSignatureCheck()
        {
            var (publicKey, privateKey) = LicenseCrypto.GenerateKeyPair();
            string machineCode = HardwareFingerprint.GetMachineCode();

            var payload = new LicensePayload
            {
                CustomerName = "Khách Hàng",
                MachineCode = machineCode,
                ExpiryDate = DateTime.Now.AddDays(30)
            };

            string licenseKey = LicenseCrypto.SignLicense(payload, privateKey);
            // Cố tình sửa 1 ký tự trong key
            string tamperedKey = licenseKey.Substring(0, licenseKey.Length - 5) + "ABCD=";

            var validation = LicenseCrypto.ValidateLicense(tamperedKey, publicKey, machineCode);

            Assert.False(validation.IsValid);
        }

        [Fact]
        public void GetConfiguredPublicKey_ShouldLoadFromAppConfig()
        {
            string pubKey = LicenseCrypto.GetConfiguredPublicKey();
            Assert.False(string.IsNullOrWhiteSpace(pubKey));
            Assert.Contains("<RSAKeyValue>", pubKey);
            Assert.Contains("<Modulus>", pubKey);
            Assert.DoesNotContain("<D>", pubKey); // Public key không chứa thành phần private D
        }

        [Fact]
        public void GetConfiguredPrivateKey_ShouldLoadFromAppConfig()
        {
            string privKey = LicenseCrypto.GetConfiguredPrivateKey();
            Assert.False(string.IsNullOrWhiteSpace(privKey));
            Assert.Contains("<RSAKeyValue>", privKey);
            Assert.Contains("<Modulus>", privKey);
            Assert.Contains("<D>", privKey); // Private key chứa thành phần private D
        }

        [Fact]
        public void Validate_WithAppConfigPublicKey_ShouldSucceed()
        {
            // Kiểm tra validate với publicKeyXml = null (Client nạp duy nhất từ App.config)
            string machineCode = HardwareFingerprint.GetMachineCode();

            var payload = new LicensePayload
            {
                CustomerName = "Khách Hàng Phú Xuân Test",
                MachineCode = machineCode,
                ExpiryDate = DateTime.Now.AddDays(30)
            };

            // Ký số và xác thực nạp khóa duy nhất từ App.config
            string key = LicenseCrypto.SignLicense(payload);
            var result = LicenseCrypto.ValidateLicense(key, null, machineCode);

            Assert.True(result.IsValid);
            Assert.True(result.IsMachineMatched);
            Assert.False(result.IsExpired);
            Assert.Equal(30, result.DaysRemaining);
            Assert.Equal("Khách Hàng Phú Xuân Test", result.Payload?.CustomerName);
        }
    }
}
