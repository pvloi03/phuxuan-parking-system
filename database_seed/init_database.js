// =============================================================================
// SCRIPT KHỞI TẠO TOÀN BỘ CƠ SỞ DỮ LIỆU PHU XUAN PARKING SYSTEM
// Database: PhuXuanParkingSystemDb
// Chạy bằng lệnh: mongosh "mongodb://localhost:27017/PhuXuanParkingSystemDb" init_database.js
// =============================================================================

const dbName = "PhuXuanParkingSystemDb";
const targetDb = db.getSiblingDB(dbName);

print(">>> Bắt đầu khởi tạo dữ liệu cho CSDL: " + dbName);

// 1. Companies
targetDb.Companies.drop();
targetDb.Companies.insertMany([
  {
    "_id": ObjectId("66c8e001a1b2c3d4e5f67901"),
    "Code": "CP-PX-01",
    "Name": "Công ty Cổ phần Năng lượng Phú Xuân",
    "PhoneNumber": "02253888999",
    "Email": "contact@phuxuan.vn",
    "Address": "Khu công nghiệp Tiền Hải, Tỉnh Thái Bình",
    "IsActive": true,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e001a1b2c3d4e5f67902"),
    "Code": "CP-HP-02",
    "Name": "Công ty Cổ phần Cơ điện Hải Phòng",
    "PhoneNumber": "02253777666",
    "Email": "info@haiphong-me.vn",
    "Address": "Quận Hồng Bàng, TP. Hải Phòng",
    "IsActive": true,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  }
]);

// 2. Contractors
targetDb.Contractors.drop();
targetDb.Contractors.insertMany([
  {
    "_id": ObjectId("66c8e002a1b2c3d4e5f67901"),
    "Code": "NT-TT-01",
    "Name": "Nhà thầu Xây dựng Thái Thụy",
    "PhoneNumber": "0988776655",
    "Email": "contact@thaithuy-xd.vn",
    "Address": "Huyện Thái Thụy, Tỉnh Thái Bình",
    "IsActive": true,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e002a1b2c3d4e5f67902"),
    "Code": "NT-HN-02",
    "Name": "Nhà thầu Cơ điện & PCCC Hà Nội",
    "PhoneNumber": "0911223344",
    "Email": "pccc@hanoi-me.com",
    "Address": "Quận Cầu Giấy, TP. Hà Nội",
    "IsActive": true,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  }
]);

// 3. Departments
targetDb.Departments.drop();
targetDb.Departments.insertMany([
  {
    "_id": ObjectId("66c8e003a1b2c3d4e5f67801"),
    "Code": "PB-BGD",
    "Name": "Ban Giám Đốc",
    "CompanyId": "66c8e001a1b2c3d4e5f67901",
    "PhoneNumber": "02253888001",
    "Email": "bgd@phuxuan.vn",
    "IsActive": true,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e003a1b2c3d4e5f67802"),
    "Code": "PB-KT",
    "Name": "Phòng Kỹ Thuật & Vận Hành",
    "CompanyId": "66c8e001a1b2c3d4e5f67901",
    "PhoneNumber": "02253888002",
    "Email": "kythuat@phuxuan.vn",
    "IsActive": true,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e003a1b2c3d4e5f67803"),
    "Code": "PB-HCNS",
    "Name": "Phòng Hành Chính - Nhân Sự",
    "CompanyId": "66c8e001a1b2c3d4e5f67901",
    "PhoneNumber": "02253888003",
    "Email": "hr@phuxuan.vn",
    "IsActive": true,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e003a1b2c3d4e5f67804"),
    "Code": "PB-KETOAN",
    "Name": "Phòng Tài Chính - Kế Toán",
    "CompanyId": "66c8e001a1b2c3d4e5f67901",
    "PhoneNumber": "02253888004",
    "Email": "ketoan@phuxuan.vn",
    "IsActive": true,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  }
]);

// 4. Devices
targetDb.Devices.drop();
targetDb.Devices.insertMany([
  {
    "_id": ObjectId("66c8e001a1b2c3d4e5f67801"),
    "Code": "CAM-IN-PLT",
    "Name": "Camera Biển Số Làn Vào (NST)",
    "Type": "PlateCamera",
    "IpAddress": "192.168.1.200",
    "Port": 3000,
    "UserName": "admin",
    "Password": "admin",
    "CameraChannel": 1,
    "OnvifPort": 80,
    "SnapshotUrl": null,
    "Status": "Disconnected",
    "LastHeartbeat": null,
    "ErrorMessage": null,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e001a1b2c3d4e5f67802"),
    "Code": "CAM-IN-OVW",
    "Name": "Camera Toàn Cảnh Làn Vào (Hikvision)",
    "Type": "OverviewCamera",
    "IpAddress": "192.168.1.61",
    "Port": 8000,
    "UserName": "admin",
    "Password": "Hoangphat130225",
    "CameraChannel": 1,
    "OnvifPort": 80,
    "SnapshotUrl": null,
    "Status": "Disconnected",
    "LastHeartbeat": null,
    "ErrorMessage": null,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e001a1b2c3d4e5f67803"),
    "Code": "CAM-OUT-PLT",
    "Name": "Camera Biển Số Làn Ra (NST)",
    "Type": "PlateCamera",
    "IpAddress": "192.168.1.203",
    "Port": 3000,
    "UserName": "admin",
    "Password": "admin",
    "CameraChannel": 1,
    "OnvifPort": 80,
    "SnapshotUrl": null,
    "Status": "Disconnected",
    "LastHeartbeat": null,
    "ErrorMessage": null,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e001a1b2c3d4e5f67804"),
    "Code": "CAM-OUT-OVW",
    "Name": "Camera Toàn Cảnh Làn Ra (Hikvision)",
    "Type": "OverviewCamera",
    "IpAddress": "192.168.1.62",
    "Port": 8000,
    "UserName": "admin",
    "Password": "Hoangphat130225",
    "CameraChannel": 1,
    "OnvifPort": 80,
    "SnapshotUrl": null,
    "Status": "Disconnected",
    "LastHeartbeat": null,
    "ErrorMessage": null,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e001a1b2c3d4e5f67805"),
    "Code": "CTRL-C3-200",
    "Name": "Bộ Điều Khiển ZKTeco C3-200 (Radar & Barrier)",
    "Type": "Controller",
    "IpAddress": "192.168.1.202",
    "Port": 4370,
    "UserName": null,
    "Password": null,
    "CameraChannel": null,
    "OnvifPort": null,
    "SnapshotUrl": null,
    "Status": "Disconnected",
    "LastHeartbeat": null,
    "ErrorMessage": null,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  }
]);

// 5. Lanes
targetDb.Lanes.drop();
targetDb.Lanes.insertMany([
  {
    "_id": ObjectId("66c8e002a1b2c3d4e5f67801"),
    "Code": "LANE-IN-01",
    "Name": "Làn Vào Số 1 (Làn Vào Chính)",
    "Direction": "In",
    "Description": "Làn kiểm soát xe vào trạm chính, chụp ảnh biển số và toàn cảnh qua cảm biến Radar",
    "IsActive": true,
    "OverviewCameraDeviceId": "66c8e001a1b2c3d4e5f67802",
    "PlateCameraDeviceId": "66c8e001a1b2c3d4e5f67801",
    "ControllerDeviceId": "66c8e001a1b2c3d4e5f67805",
    "TriggerAuxPort": 1,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e002a1b2c3d4e5f67802"),
    "Code": "LANE-OUT-01",
    "Name": "Làn Ra Số 1 (Làn Ra Chính)",
    "Direction": "Out",
    "Description": "Làn kiểm soát xe ra trạm chính, nhận diện biển số đối soát lượt vào tự động",
    "IsActive": true,
    "OverviewCameraDeviceId": "66c8e001a1b2c3d4e5f67804",
    "PlateCameraDeviceId": "66c8e001a1b2c3d4e5f67803",
    "ControllerDeviceId": "66c8e001a1b2c3d4e5f67805",
    "TriggerAuxPort": 2,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  }
]);

// 6. LicenseInfo
targetDb.LicenseInfo.drop();
targetDb.LicenseInfo.insertMany([
  {
    "_id": ObjectId("66c8e010a1b2c3d4e5f67801"),
    "CustomerName": "Công ty Cổ phần Năng lượng Phú Xuân",
    "MachineCode": "PX-STATION-01-HWID-998877",
    "ExpiryDate": new Date("2027-12-31T23:59:59.000Z"),
    "IssuedAt": new Date("2026-08-01T08:00:00.000Z"),
    "LicenseKey": "PX-2026-PARKING-COMMERCIAL-UNLIMITED-KEY",
    "Signature": "RSA_SIG_MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA",
    "IsActive": true,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  }
]);

// 7. People
targetDb.People.drop();
targetDb.People.insertMany([
  {
    "_id": ObjectId("66c8e004a1b2c3d4e5f67801"),
    "Code": "NV-001",
    "FullName": "Nguyễn Văn An",
    "DepartmentId": "66c8e003a1b2c3d4e5f67801",
    "PhoneNumber": "0901234567",
    "Email": "an.nguyen@phuxuan.vn",
    "Type": "Employee",
    "CompanyId": "66c8e001a1b2c3d4e5f67901",
    "ContractorId": null,
    "IsActive": true,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e004a1b2c3d4e5f67802"),
    "Code": "NV-002",
    "FullName": "Trần Thị Bình",
    "DepartmentId": "66c8e003a1b2c3d4e5f67802",
    "PhoneNumber": "0912345678",
    "Email": "binh.tran@phuxuan.vn",
    "Type": "Employee",
    "CompanyId": "66c8e001a1b2c3d4e5f67901",
    "ContractorId": null,
    "IsActive": true,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e004a1b2c3d4e5f67803"),
    "Code": "NV-003",
    "FullName": "Lê Hoàng Cường",
    "DepartmentId": "66c8e003a1b2c3d4e5f67803",
    "PhoneNumber": "0923456789",
    "Email": "cuong.le@phuxuan.vn",
    "Type": "Employee",
    "CompanyId": "66c8e001a1b2c3d4e5f67901",
    "ContractorId": null,
    "IsActive": true,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e004a1b2c3d4e5f67804"),
    "Code": "NV-004",
    "FullName": "Phạm Văn Dũng",
    "DepartmentId": "66c8e003a1b2c3d4e5f67804",
    "PhoneNumber": "0934567890",
    "Email": "dung.pham@phuxuan.vn",
    "Type": "Employee",
    "CompanyId": "66c8e001a1b2c3d4e5f67901",
    "ContractorId": null,
    "IsActive": true,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e004a1b2c3d4e5f67805"),
    "Code": "NT-001",
    "FullName": "Vũ Đình Em",
    "DepartmentId": null,
    "PhoneNumber": "0945678901",
    "Email": "em.vu@thaithuy.vn",
    "Type": "Contractor",
    "CompanyId": null,
    "ContractorId": "66c8e002a1b2c3d4e5f67901",
    "IsActive": true,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  }
]);

// 8. Users
targetDb.Users.drop();
targetDb.Users.insertMany([
  {
    "_id": ObjectId("66c8e009a1b2c3d4e5f67801"),
    "Username": "admin",
    "PasswordHash": "$2a$11$N9qo8uLOICKgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy",
    "FullName": "Quản Trị Viên Hệ Thống",
    "Email": "admin@phuxuan.vn",
    "PhoneNumber": "0900000001",
    "Role": "Admin",
    "IsActive": true,
    "LastLoginAt": null,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e009a1b2c3d4e5f67802"),
    "Username": "operator01",
    "PasswordHash": "$2a$11$N9qo8uLOICKgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy",
    "FullName": "Nhân Viên Vận Hành 01",
    "Email": "operator01@phuxuan.vn",
    "PhoneNumber": "0900000002",
    "Role": "Operator",
    "IsActive": true,
    "LastLoginAt": null,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e009a1b2c3d4e5f67803"),
    "Username": "security01",
    "PasswordHash": "$2a$11$N9qo8uLOICKgx2ZMRZoMyeIjZAgcfl7p92ldGxad68LJZdL17lhWy",
    "FullName": "Bảo Vệ Trực Cổng Ca Ngày",
    "Email": "security01@phuxuan.vn",
    "PhoneNumber": "0900000003",
    "Role": "Security",
    "IsActive": true,
    "LastLoginAt": null,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  }
]);

// 9. Vehicles
targetDb.Vehicles.drop();
targetDb.Vehicles.insertMany([
  {
    "_id": ObjectId("66c8e005a1b2c3d4e5f67801"),
    "PlateNumber": "29A12345",
    "Type": "Car",
    "OwnerPersonId": "66c8e004a1b2c3d4e5f67801",
    "IsActive": true,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e005a1b2c3d4e5f67802"),
    "PlateNumber": "30F99988",
    "Type": "Car",
    "OwnerPersonId": "66c8e004a1b2c3d4e5f67802",
    "IsActive": true,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e005a1b2c3d4e5f67803"),
    "PlateNumber": "17A08866",
    "Type": "Car",
    "OwnerPersonId": "66c8e004a1b2c3d4e5f67803",
    "IsActive": true,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e005a1b2c3d4e5f67804"),
    "PlateNumber": "17B167890",
    "Type": "Motorcycle",
    "OwnerPersonId": "66c8e004a1b2c3d4e5f67804",
    "IsActive": true,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e005a1b2c3d4e5f67805"),
    "PlateNumber": "88LD00122",
    "Type": "Car",
    "OwnerPersonId": "66c8e004a1b2c3d4e5f67805",
    "IsActive": true,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e005a1b2c3d4e5f67806"),
    "PlateNumber": "51H12345",
    "Type": "Car",
    "OwnerPersonId": null,
    "IsActive": true,
    "CreatedAt": new Date("2026-08-01T08:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  }
]);

// 10. ParkingSessions
targetDb.ParkingSessions.drop();
targetDb.ParkingSessions.insertMany([
  {
    "_id": ObjectId("66c8e008a1b2c3d4e5f67801"),
    "PlateNumber": "29A12345",
    "VehicleType": "Car",
    "Status": "Completed",
    "PersonName": "Nguyễn Văn An",
    "InTime": new Date("2026-08-25T07:30:00.000Z"),
    "InLaneId": "LANE-IN-01",
    "InOverviewImagePath": "Captures/2026-08-25/20260825_073000_panoramic.jpg",
    "InPlateImagePath": "Captures/2026-08-25/20260825_073000_plate.jpg",
    "OutTime": new Date("2026-08-25T11:45:00.000Z"),
    "OutLaneId": "LANE-OUT-01",
    "OutOverviewImagePath": "Captures/2026-08-25/20260825_114500_panoramic.jpg",
    "OutPlateImagePath": "Captures/2026-08-25/20260825_114500_plate.jpg",
    "Note": "Vào ra nội bộ ca sáng",
    "CreatedAt": new Date("2026-08-25T07:30:00.000Z"),
    "UpdatedAt": new Date("2026-08-25T11:45:00.000Z"),
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e008a1b2c3d4e5f67802"),
    "PlateNumber": "30F99988",
    "VehicleType": "Car",
    "Status": "Active",
    "PersonName": "Trần Thị Bình",
    "InTime": new Date("2026-08-25T08:15:00.000Z"),
    "InLaneId": "LANE-IN-01",
    "InOverviewImagePath": "Captures/2026-08-25/20260825_081500_panoramic.jpg",
    "InPlateImagePath": "Captures/2026-08-25/20260825_081500_plate.jpg",
    "OutTime": null,
    "OutLaneId": null,
    "OutOverviewImagePath": "",
    "OutPlateImagePath": "",
    "Note": "Đang đỗ trong bãi",
    "CreatedAt": new Date("2026-08-25T08:15:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  },
  {
    "_id": ObjectId("66c8e008a1b2c3d4e5f67803"),
    "PlateNumber": "51H12345",
    "VehicleType": "Car",
    "Status": "Active",
    "PersonName": null,
    "InTime": new Date("2026-08-25T09:00:00.000Z"),
    "InLaneId": "LANE-IN-01",
    "InOverviewImagePath": "Captures/2026-08-25/20260825_090000_panoramic.jpg",
    "InPlateImagePath": "Captures/2026-08-25/20260825_090000_plate.jpg",
    "OutTime": null,
    "OutLaneId": null,
    "OutOverviewImagePath": "",
    "OutPlateImagePath": "",
    "Note": "Khách vãng lai - xe lạ",
    "CreatedAt": new Date("2026-08-25T09:00:00.000Z"),
    "UpdatedAt": null,
    "IsDeleted": false,
    "DeletedAt": null
  }
]);

print(">>> Khởi tạo toàn bộ CSDL PhuXuanParkingSystemDb THÀNH CÔNG!");
