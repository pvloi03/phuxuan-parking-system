using FluentAssertions;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Models.ValueObjects;
using System;
using Xunit;

namespace PhuXuanParkingSystem.Tests.Entities
{
    public class EntityTests
    {
        [Fact]
        public void Vehicle_Constructor_ShouldNormalizePlateNumber()
        {
            // Act
            var vehicle = new Vehicle(" 29A - 888.99 ", VehicleType.Car, "P01");

            // Assert
            vehicle.PlateNumber.Value.Should().Be("29A88899");
            vehicle.Type.Should().Be(VehicleType.Car);
            vehicle.OwnerPersonId.Should().Be("P01");
            vehicle.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Person_Constructor_ShouldInitializeProperties()
        {
            // Act
            var person = new Person("NV001", "Lê Văn D", PersonType.Employee);

            // Assert
            person.Code.Should().Be("NV001");
            person.FullName.Should().Be("Lê Văn D");
            person.Type.Should().Be(PersonType.Employee);
            person.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Lane_Constructor_ShouldInitializeProperties()
        {
            // Act
            var laneIn = new Lane("L01", "Làn Vào Chính", LaneDirection.In, triggerAuxPort: 1);
            var laneOut = new Lane("L02", "Làn Ra Chính", LaneDirection.Out, triggerAuxPort: 2);

            // Assert
            laneIn.Code.Should().Be("L01");
            laneIn.Direction.Should().Be(LaneDirection.In);
            laneIn.TriggerAuxPort.Should().Be(1);
            laneIn.IsActive.Should().BeTrue();

            laneOut.Code.Should().Be("L02");
            laneOut.Direction.Should().Be(LaneDirection.Out);
            laneOut.TriggerAuxPort.Should().Be(2);
        }

        [Fact]
        public void Device_Constructor_ShouldInitializeProperties()
        {
            // Act
            var cam = new Device("CAM_IN_PL", "Cam Biển Số Vào", DeviceType.PlateCamera, "192.168.1.101", 3000);

            // Assert
            cam.Code.Should().Be("CAM_IN_PL");
            cam.Name.Should().Be("Cam Biển Số Vào");
            cam.Type.Should().Be(DeviceType.PlateCamera);
            cam.IpAddress.Should().Be("192.168.1.101");
            cam.Port.Should().Be(3000);
            cam.Status.Should().Be(DeviceStatus.Disconnected);
        }

        [Fact]
        public void Department_Constructor_ShouldInitializeProperties()
        {
            // Act
            var dept = new Department("PB_KT", "Phòng Kỹ Thuật", "CP01", "0901234567", "kt@example.com");

            // Assert
            dept.Code.Should().Be("PB_KT");
            dept.Name.Should().Be("Phòng Kỹ Thuật");
            dept.CompanyId.Should().Be("CP01");
            dept.PhoneNumber.Should().Be("0901234567");
            dept.Email.Should().Be("kt@example.com");
            dept.IsActive.Should().BeTrue();
        }

        [Fact]
        public void OrganizationEntities_ShouldInitializeProperties()
        {
            // Act
            var company = new Company("CP_HP", "Công ty Hải Phòng", "0901112233", "hp@example.com");
            var contractor = new Contractor("NT_TT", "Nhà thầu Thái Thụy", "0988776655");

            // Assert
            company.Code.Should().Be("CP_HP");
            company.PhoneNumber.Should().Be("0901112233");
            company.Email.Should().Be("hp@example.com");
            company.IsActive.Should().BeTrue();

            contractor.Code.Should().Be("NT_TT");
            contractor.PhoneNumber.Should().Be("0988776655");
            contractor.IsActive.Should().BeTrue();
        }

        [Fact]
        public void User_Constructor_ShouldInitializeProperties()
        {
            // Act
            var user = new User("admin", "hash123", "Quản Trị Viên", UserRole.Admin);

            // Assert
            user.Username.Should().Be("admin");
            user.PasswordHash.Should().Be("hash123");
            user.FullName.Should().Be("Quản Trị Viên");
            user.Role.Should().Be(UserRole.Admin);
            user.IsActive.Should().BeTrue();
        }

        [Fact]
        public void LicenseInfo_Constructor_ShouldInitializeProperties()
        {
            // Act
            var license = new LicenseInfo("Khách Hàng A", "MACH-001", DateTime.Now.AddDays(30), "LIC-KEY-999");

            // Assert
            license.CustomerName.Should().Be("Khách Hàng A");
            license.MachineCode.Should().Be("MACH-001");
            license.LicenseKey.Should().Be("LIC-KEY-999");
            license.IsExpired.Should().BeFalse();
            license.IsValid.Should().BeTrue();
            license.DaysRemaining.Should().BeGreaterThan(0);
        }
    }
}
