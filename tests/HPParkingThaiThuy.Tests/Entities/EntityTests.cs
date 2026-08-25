using FluentAssertions;
using HPParkingThaiThuy.Models.Entities;
using HPParkingThaiThuy.Models.Enums;
using Xunit;

namespace HPParkingThaiThuy.Tests.Entities
{
    public class EntityTests
    {
        [Fact]
        public void Vehicle_Constructor_ShouldNormalizePlateNumber()
        {
            // Act
            var vehicle = new Vehicle(" 29A - 888.99 ", VehicleType.Car, "P01", "Trần Văn C");

            // Assert
            vehicle.PlateNumber.Should().Be("29A88899");
            vehicle.Type.Should().Be(VehicleType.Car);
            vehicle.OwnerPersonId.Should().Be("P01");
            vehicle.OwnerName.Should().Be("Trần Văn C");
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
            var company = new Company("CP_HP", "Công ty Hải Phòng", "0123456789", "Hải Phòng");
            var contractor = new Contractor("NT_TT", "Nhà thầu Thái Thụy", "Mr. Hùng", "0988776655");

            // Assert
            company.Code.Should().Be("CP_HP");
            company.TaxCode.Should().Be("0123456789");
            company.IsActive.Should().BeTrue();

            contractor.Code.Should().Be("NT_TT");
            contractor.ContactPerson.Should().Be("Mr. Hùng");
            contractor.PhoneNumber.Should().Be("0988776655");
            contractor.IsActive.Should().BeTrue();
        }

        [Fact]
        public void User_Constructor_ShouldInitializeProperties()
        {
            // Act
            var user = new User("admin", "Quản Trị Viên", UserRole.Admin);

            // Assert
            user.Username.Should().Be("admin");
            user.FullName.Should().Be("Quản Trị Viên");
            user.Role.Should().Be(UserRole.Admin);
            user.IsActive.Should().BeTrue();
        }
    }
}
