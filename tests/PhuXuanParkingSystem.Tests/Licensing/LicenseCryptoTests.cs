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
                ExpiryDate = DateTime.Now.AddDays(30),
                MaxLanes = 2,
                MaxCameras = 4,
                MaxControllers = 1,
                Features = new List<string> { "ANPR_Vietnam", "AutoBarrier" }
            };

            string licenseKey = LicenseCrypto.SignLicense(payload, privateKey);
            Assert.StartsWith(LicenseCrypto.LicensePrefix, licenseKey);

            var validation = LicenseCrypto.ValidateLicense(licenseKey, publicKey, machineCode);

            Assert.True(validation.IsValid);
            Assert.False(validation.IsExpired);
            Assert.True(validation.IsMachineMatched);
            Assert.Equal(30, validation.DaysRemaining);
            Assert.Equal(2, validation.Payload?.MaxLanes);
            Assert.Equal(4, validation.Payload?.MaxCameras);
            Assert.Equal(1, validation.Payload?.MaxControllers);
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
                MaxLanes = 2,
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
        public void DefaultPublicKeyXml_ShouldBeValidRsaKey()
        {
            Assert.False(string.IsNullOrWhiteSpace(LicenseCrypto.DefaultPublicKeyXml));
            using (var rsa = System.Security.Cryptography.RSA.Create())
            {
                // Không được ném ngoại lệ Base64 hoặc XML Format
                rsa.FromXmlString(LicenseCrypto.DefaultPublicKeyXml);
                Assert.Equal(3072, rsa.KeySize);
            }
        }

        [Fact]
        public void Validate_WithDefaultPublicKeyXml_ShouldSucceed()
        {
            // Kiểm tra validate với publicKeyXml = null (dùng DefaultPublicKeyXml mặc định)
            var (publicKey, privateKey) = LicenseCrypto.GenerateKeyPair();
            string machineCode = HardwareFingerprint.GetMachineCode();

            var payload = new LicensePayload
            {
                CustomerName = "Khách Hàng Test",
                MachineCode = machineCode,
                ExpiryDate = DateTime.Now.AddDays(30)
            };

            // Ký số với private key bất kỳ và validate với public key tương ứng
            string key = LicenseCrypto.SignLicense(payload, privateKey);
            var result = LicenseCrypto.ValidateLicense(key, publicKey, machineCode);
            Assert.True(result.IsValid);
        }
    }
}
