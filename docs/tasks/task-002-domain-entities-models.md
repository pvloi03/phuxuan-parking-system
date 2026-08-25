# Task 002: Triển Khai Domain Layer (Entities, Value Objects, Enums & Events)

## 1. Mục Tiêu
Cài đặt toàn bộ mô hình Domain Core thuần túy trong `HPParkingSystem.Domain`, bao gồm Entities, Value Objects, Enums và Domain Events, đảm bảo không phụ thuộc vào bất kỳ thư viện ngoài hay hạ tầng nào.

## 2. Bối Cảnh & Phạm Vi
- **Bối cảnh**: Hệ thống quản lý kiểm soát lượt xe ra/vào qua 2 làn (Vào/Ra) nhận diện biển số tự động qua Camera LPR và Cảm biến radar (không barrier, không thẻ RFID), hỗ trợ xác thực bản quyền máy (License Key).
- **Phạm vi**:
  - `Entities`: `Vehicle`, `Person`, `Department`, `Contractor`, `Company`, `User`, `Lane`, `Device`, `ParkingSession`, `LicenseInfo`.
  - `ValueObjects`: `PlateNumber` (chuẩn hóa biển số VN), `ImageStoragePath`, `RecognitionResult`.
  - `Enums`: `LaneDirection`, `VehicleType`, `DeviceType`, `DeviceStatus`, `PersonType`, `UserRole`, `ParkingSessionStatus`.
  - `Events`: `VehicleArrivedDomainEvent`, `VehicleCheckedInDomainEvent`, `VehicleCheckedOutDomainEvent`.
  - `Common`: `BaseEntity` (hỗ trợ MongoDB auto-generated Id và Soft Delete `IsDeleted`, `DeletedAt`).

## 3. Chi Tiết Thiết Kế

### 3.1. Entity Danh Mục Quản Trị (`Company`, `Department`, `Contractor`, `Person`, `Vehicle`, `User`, `LicenseInfo`)
- **Company**: `Code`, `Name`, `PhoneNumber`, `Email`, `IsActive`.
- **Department**: `Code`, `Name`, `CompanyId`, `PhoneNumber`, `Email`, `IsActive`.
- **Contractor**: `Code`, `Name`, `PhoneNumber`, `IsActive` (tối giản).
- **User**: `Username`, `PasswordHash`, `FullName`, `Email`, `PhoneNumber`, `Role` (`UserRole`), `IsActive`, `LastLoginAt`.
- **Person**: `Code`, `FullName`, `DepartmentId`, `PhoneNumber`, `Email`, `Type` (`PersonType`), `CompanyId`, `ContractorId`, `IsActive`.
- **Vehicle**: `PlateNumber`, `Type` (`VehicleType`), `OwnerPersonId`, `IsActive` (tối giản: đã bỏ `Brand`, `Model`, `Color`).
- **LicenseInfo**: `CustomerName`, `MachineCode`, `ExpiryDate`, `IssuedAt`, `LicenseKey`, `Signature`, `IsActive` (hỗ trợ getters: `IsExpired`, `DaysRemaining`, `IsValid`).

### 3.2. Entity `Device` (Tài nguyên phần cứng vật lý)
- **Thông tin chung**: `Code`, `Name`, `Type` (`DeviceType`: CameraOverview, CameraPlate, ControllerC3_200, RadarSensor).
- **Mạng & Xác thực**: `IpAddress`, `Port`, `UserName`, `Password` (Camera login hoặc ZKTeco CommPassword).
- **Thông số Camera (Vendor SDK / ONVIF / RTSP)**: `CameraChannel`, `RtspUrl` (Liveview UI), `OnvifPort` (80/8080), `SnapshotUrl`.
- **Sức khỏe thiết bị**: `Status` (`DeviceStatus`), `LastHeartbeat`, `ErrorMessage`.
*(Tất cả thuộc tính trên đều được `[LƯU DB]`)*.

### 3.3. Entity `Lane` (Cấu hình phân bổ thiết bị cho từng làn)
- **`[LƯU DB]`**: `Code`, `Name`, `Direction`, `OverviewCameraDeviceId`, `PlateCameraDeviceId`, `ControllerDeviceId`, `TriggerAuxPort`, `Description`, `IsActive`.
- **`[KHÔNG LƯU DB]` (Navigation objects trong RAM)**: `OverviewCamera`, `PlateCamera`, `Controller`.
*(Hệ thống giám sát thuần túy, không có barrier / không dùng thẻ).*

### 3.4. Entity `ParkingSession` (Mô hình 1 bản ghi phiên đỗ xe Vào - Ra tối giản)
- **`[LƯU DB]` (13 trường nghiệp vụ + 3 trường BaseEntity)**:
  - `Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`, `DeletedAt`.
  - `PlateNumber` (chuỗi biển số), `VehicleType` (loại xe), `Status` (`Active`, `Completed`, `UnmatchedOut`).
  - `PersonName` (tên chủ xe / người lái - null nếu xe lạ/người lạ).
  - `InTime`, `InLaneId`, `InOverviewImagePath`, `InPlateImagePath`.
  - `OutTime`, `OutLaneId`, `OutOverviewImagePath`, `OutPlateImagePath`.
  - `Note`.
- **`[KHÔNG LƯU DB]` (Computed getters)**:
  - `Duration` (OutTime - InTime).
  - `IsUnknown` (khi `PersonName` là null/rỗng).
- **Domain Methods**:
  - `CheckIn(...)`: Tạo phiên đỗ mới khi xe vào.
  - `CheckOut(...)`: Cập nhật thông tin khi xe ra và chuyển trạng thái `Completed`.
  - `CreateUnmatchedOut(...)`: Tạo phiên khi xe ra mà không có lượt vào trước đó.

### 3.5. Value Object `PlateNumber`
- Logic chuẩn hóa chuỗi biển số: Viết hoa, xóa khoảng trắng thừa, xóa dấu gạch nối/chấm khi so sánh (`29A-123.45` $\rightarrow$ `29A12345`).
- Regex validation biển số xe ô tô/xe máy Việt Nam.

## 4. Checklist Tiến Độ
- [x] Cài đặt các Enums (`LaneDirection`, `VehicleType`, `DeviceType`, `DeviceStatus`, `PersonType`, `UserRole`, `ParkingSessionStatus`).
- [x] Cài đặt Value Object `PlateNumber` với unit test chuẩn hóa chuỗi.
- [x] Cài đặt Entity `Vehicle`, `Person`, `Department`, `Contractor`, `Company`, `User`, `LicenseInfo`.
- [x] Cài đặt Entity `Lane`, `Device` (phân bổ Controller dùng chung qua cổng Aux In, hỗ trợ ONVIF, hoàn toàn không có Barrier / Thẻ).
- [x] Cài đặt Entity chính `ParkingSession` và các Domain Events liên quan (`VehicleCheckedInDomainEvent`, `VehicleCheckedOutDomainEvent`).
- [x] Cài đặt `BaseEntity` với cơ chế Xóa mềm (`IsDeleted`, `DeletedAt`, `MarkDeleted()`, `Restore()`) và Id rỗng để MongoDB Driver tự sinh.
- [x] Viết Unit Tests kiểm tra tính bất biến của Value Objects và logic nghiệp vụ thuần túy của Entities (37 unit tests passed).

## 5. Lưu Ý Kỹ Thuật
- Không sử dụng các annotation của MongoDB (`[BsonId]`, `[BsonElement]`) trực tiếp trong Domain Entities để giữ Domain 100% POCO (cấu hình ClassMap thực hiện trong Infrastructure Layer).
