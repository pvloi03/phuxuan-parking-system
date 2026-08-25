# Task 003: Triển Khai Application Layer (Ports & Use Cases)

## 1. Mục Tiêu
Cài đặt các Use Cases điều phối luồng nghiệp vụ kiểm soát xe ra/vào, quản lý danh mục, xác thực bản quyền phần mềm (License Key) và định nghĩa hệ thống Ports (Interfaces) trừu tượng trong `HPParkingSystem.Application`.

## 2. Bối Cảnh & Phạm Vi
- **Bối cảnh**: Khi xe đến cổng, cảm biến radar kích hoạt tín hiệu qua controller ZKTeco C3-200. Application Layer chịu trách nhiệm nhận tín hiệu, ra lệnh cho các camera chụp ảnh đồng thời, gửi ảnh sang dịch vụ LPR để nhận diện biển số, tra cứu tên chủ xe (nếu đã đăng ký), lưu ảnh xuống SMB File Share và tạo/cập nhật bản ghi `ParkingSession` trong CSDL.
- **Phạm vi**:
  - **Ports - Quản lý CSDL (Repositories)**:
    - `IParkingSessionRepository`: Lưu, cập nhật và truy vấn phiên đỗ xe (Vào - Ra).
    - `IPersonRepository`, `IVehicleRepository`, `IDepartmentRepository`, `ICompanyRepository`, `IContractorRepository`, `IUserRepository`, `ILaneRepository`, `IDeviceRepository`, `ILicenseRepository`.
  - **Ports - Thiết bị Ngoại vi & Dịch vụ**:
    - `IHardwareEventListener`: Lắng nghe trigger từ radar/controller.
    - `ICameraCapture`: Chụp ảnh từ camera IP (Hikvision / NST / ONVIF / RTSP).
    - `ILicensePlateRecognizer`: Nhận dạng biển số qua LPR REST API.
    - `IImageStore`: Lưu trữ ảnh chụp xuống SMB File Share (đặt tên theo `Timestamp_PlateNumber_Type.jpg`).
    - `ILicenseService`: Lấy mã phần cứng máy (Machine Code) và xác thực chữ ký bản quyền RSA.
  - **Use Cases cốt lõi**:
    - `ProcessVehicleEntryUseCase`: Xử lý lượt xe vào (khởi tạo `ParkingSession.CheckIn`).
    - `ProcessVehicleExitUseCase`: Xử lý lượt xe ra (tìm phiên đang Active theo biển số để gọi `session.CheckOut`, nếu không thấy thì gọi `ParkingSession.CreateUnmatchedOut`).
    - `CheckDeviceStatusUseCase`: Kiểm tra kết nối định kỳ (Heartbeat) các thiết bị ngoại vi.
    - `GetRecentParkingSessionsUseCase`: Lấy danh sách các phiên đỗ xe gần nhất cho UI.
    - `ValidateLicenseUseCase` & `RegisterLicenseUseCase`: Kiểm tra và kích hoạt bản quyền phần mềm.

## 3. Chi Tiết Luồng Thực Hiện Use Cases

### 3.1. Luồng Xe Vào (`ProcessVehicleEntryUseCase`)
1. Nhận sự kiện trigger từ radar/controller tại Làn Vào (Aux In 1).
2. Gọi đồng thời `Task.WhenAll`: Chụp ảnh toàn cảnh (Hikvision) và ảnh biển số (NST).
3. Gửi ảnh biển số đến `_lprRecognizer.RecognizeAsync(...)`.
4. Tra cứu biển số trong danh bạ `IVehicleRepository` và `IPersonRepository` để lấy tên chủ xe `PersonName` (nếu xe đã đăng ký, nếu không thì để null).
5. Lưu file ảnh vào SMB File Share qua `_imageStore.SaveImageAsync(...)` theo tên: `{timestamp}_{plate}_In_Overview.jpg` và `{timestamp}_{plate}_In_Plate.jpg`.
6. Khởi tạo `ParkingSession.CheckIn(...)`.
7. Lưu phiên vào MongoDB qua `_parkingSessionRepository.AddAsync(session)`.

### 3.2. Luồng Xe Ra (`ProcessVehicleExitUseCase`)
1. Nhận sự kiện trigger từ Làn Ra (Aux In 2), chụp ảnh toàn cảnh + biển số và nhận diện OCR.
2. Tra cứu tên chủ xe `PersonName` (nếu có đăng ký).
3. Lưu file ảnh vào SMB File Share qua `_imageStore.SaveImageAsync(...)`.
4. Tìm phiên đỗ đang active gần nhất theo biển số: `_parkingSessionRepository.GetActiveSessionByPlateAsync(cleanPlate)`.
5. Nếu tìm thấy phiên active: gọi `session.CheckOut(...)` và cập nhật qua `_parkingSessionRepository.UpdateAsync(session)`.
6. Nếu không tìm thấy: tạo `ParkingSession.CreateUnmatchedOut(...)` và lưu qua `_parkingSessionRepository.AddAsync(session)`.

### 3.3. Luồng Bản Quyền (`ValidateLicenseUseCase` & `RegisterLicenseUseCase`)
1. Khi ứng dụng khởi động: Gọi `_licenseService.ValidateCurrentLicense()`:
   - Đọc thông tin phần cứng máy tính (`MachineCode`).
   - Xác thực chữ ký số RSA của `LicenseKey`.
   - Kiểm tra ngày hết hạn `ExpiryDate`.
2. Nếu hợp lệ: Cho phép WinForms mở các luồng Camera và kết nối Controller.
3. Nếu không hợp lệ hoặc hết hạn: Hiển thị màn hình kích hoạt bản quyền (Yêu cầu nhập License Key).

## 4. Checklist Tiến Độ
- [ ] Định nghĩa các Repository Ports (`IParkingSessionRepository`, `IPersonRepository`, `IVehicleRepository`, `IDepartmentRepository`, `ICompanyRepository`, `IContractorRepository`, `IUserRepository`, `ILaneRepository`, `IDeviceRepository`, `ILicenseRepository`) trong thư mục `Ports/Repositories/`.
- [ ] Định nghĩa các Hardware/Service Ports (`ICameraCapture`, `ILicensePlateRecognizer`, `IHardwareEventListener`, `IImageStore`, `ILicenseService`) trong thư mục `Ports/Services/`.
- [ ] Tạo các DTOs (Request, Response, ViewModel) trong thư mục `Models/`.
- [ ] Cài đặt `ProcessVehicleEntryUseCase` và `ProcessVehicleExitUseCase`.
- [ ] Cài đặt `CheckDeviceStatusUseCase`.
- [ ] Cài đặt `GetRecentParkingSessionsUseCase`.
- [ ] Cài đặt các Use Case bản quyền (`ValidateLicenseUseCase`, `RegisterLicenseUseCase`, `GetMachineCodeUseCase`).
- [ ] Viết Unit Tests với Mock Ports (Moq / NSubstitute) để kiểm chứng luồng Use Case độc lập với phần cứng thật.

## 5. Lưu Ý Kỹ Thuật
- Sử dụng cơ chế bất đồng bộ `async/await` triệt để để không làm đơ giao diện WinForms khi gọi camera hay LPR service.
- Bổ sung cơ chế Timeout và Try-Catch an toàn cho từng bước chụp ảnh và nhận diện OCR.
