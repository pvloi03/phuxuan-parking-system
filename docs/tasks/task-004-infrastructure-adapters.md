# Task 004: Triển Khai Infrastructure Adapters (Hardware SDKs, LPR HTTP, License & MongoDB)

## 1. Mục Tiêu
Cài đặt các Adapter cụ thể trong `HPParkingSystem.Infrastructure` để giao tiếp với phần cứng thực tế (ZKTeco C3-200, Camera Hikvision, Camera NST, ONVIF/RTSP), dịch vụ nhận dạng LPR, hệ thống bảo mật bản quyền (License Key) và cơ sở dữ liệu MongoDB.

## 2. Bối Cảnh & Phạm Vi
- **Bối cảnh**: Hệ thống yêu cầu tích hợp đồng thời nhiều SDK native C/C++ 32-bit (`plcommpro.dll`, `HCNetSDK.dll`, `CamWizard SDK`), dịch vụ HTTP loopback LPR, lưu trữ tệp mạng SMB và xác thực bản quyền máy offline.
- **Phạm vi**:
  - **Phần cứng & Ngoại vi**:
    - `ZKTecoDeviceAdapter`: Kết nối Controller C3-200 qua Pull SDK TCP/IP (Port 4370), lắng nghe RTLog hoặc poll trạng thái cổng Aux In (Làn Vào: Aux 1, Làn Ra: Aux 2) $\rightarrow$ implement `IHardwareEventListener`.
    - `HikvisionCameraAdapter`: P/Invoke `HCNetSDK.dll`, đăng nhập camera IP Hikvision (Port 8000), lấy luồng ảnh JPEG $\rightarrow$ implement `ICameraCapture`.
    - `NstCameraAdapter`: P/Invoke NST Native DLL, đăng nhập camera IP NST (Port 3000), lấy snapshot $\rightarrow$ implement `ICameraCapture`.
    - `OnvifCameraAdapter`: Chuẩn giao tiếp ONVIF lấy snapshot trực tiếp qua HTTP SOAP/REST $\rightarrow$ implement `ICameraCapture`.
    - `LprHttpClientAdapter`: Gửi HTTP POST JSON/Multipart chứa ảnh tới `http://127.0.0.1:<port>/api/v1/lpr/recognize` $\rightarrow$ implement `ILicensePlateRecognizer`.
  - **Lưu trữ & Bản quyền**:
    - `SmbFileShareImageStore`: Lưu file ảnh JPG vào thư mục chia sẻ mạng (UNC Path `\\server\Captures\YYYY\MM\DD\{timestamp}_{plate}_Type.jpg`) $\rightarrow$ implement `IImageStore`.
    - `WindowsHardwareService`: Đọc WMI (Motherboard Serial, CPU ID, BIOS UUID) băm SHA256 thành `MachineCode` $\rightarrow$ implement `ILicenseService`.
    - `RsaLicenseValidator`: Xác thực chữ ký số RSA 2048-bit để chống làm giả bản quyền phần mềm.
  - **Cơ sở dữ liệu (MongoDB Repositories)**:
    - Cài đặt NuGet package `MongoDB.Driver`.
    - Cấu hình tự động ánh xạ BSON ClassMap và Global Filter cho Xóa mềm (`IsDeleted == false`).
    - Triển khai toàn bộ Repositories: `MongoParkingSessionRepository`, `MongoPersonRepository`, `MongoVehicleRepository`, `MongoDepartmentRepository`, `MongoCompanyRepository`, `MongoContractorRepository`, `MongoUserRepository`, `MongoLaneRepository`, `MongoDeviceRepository`, `MongoLicenseRepository`.

## 3. Chi Tiết Kỹ Thuật

### 3.1. ZKTeco Pull SDK Adapter (`ZKTecoDeviceAdapter.cs`)
- Nạp thư viện `plcommpro.dll`.
- Khởi tạo kết nối qua `Connect("protocol=TCP,ipaddress=192.168.1.201,port=4370,timeout=4000,passwd=")`.
- Vòng lặp ngầm (Background Thread) đọc `GetRTLog` để bắt sự kiện tín hiệu cảm biến từ cổng Aux In 1 (Làn Vào) và Aux In 2 (Làn Ra).

### 3.2. Camera Adapters (Hikvision, NST, ONVIF)
- Hikvision: `NET_DVR_Init()`, `NET_DVR_Login_V40()`, `NET_DVR_CaptureJPEGPicture_NEW()`.
- NST: Đăng nhập SDK Port 3000, chụp ảnh snapshot.
- ONVIF: Giao tiếp SOAP/HTTP Snapshot URL.

### 3.3. Dịch vụ Bản quyền License Key (`LicenseService.cs`)
- Đọc thông tin phần cứng máy tính thông qua `System.Management` (WMI):
  - Motherboard Serial Number (`Win32_BaseBoard.SerialNumber`)
  - Processor ID (`Win32_Processor.ProcessorId`)
  - BIOS UUID (`Win32_ComputerSystemProduct.UUID`)
- Ghép và băm SHA256 tạo `MachineCode` dạng: `HP-XXXX-XXXX-XXXX`.
- Xác minh `LicenseKey` bằng thuật toán RSA-SHA256 với Embedded Public Key.

### 3.4. MongoDB Repositories
- Cấu hình `StringObjectIdGenerator` để MongoDB Driver tự sinh mã 24 hex khi `Id` rỗng.
- Tất cả truy vấn `Find` mặc định lọc `Builders<T>.Filter.Eq(x => x.IsDeleted, false)`.
- Xóa mềm: Cập nhật `IsDeleted = true`, `DeletedAt = DateTime.UtcNow`.

## 4. Checklist Tiến Độ
- [ ] Cấu hình sao chép tự động các file DLL native 32-bit vào thư mục `bin/x86/Debug` khi build.
- [ ] Cài đặt `ZKTecoDeviceAdapter` với kết nối và đọc realtime log.
- [ ] Cài đặt `HikvisionCameraAdapter`, `NstCameraAdapter` và `OnvifCameraAdapter`.
- [ ] Cài đặt `LprHttpClientAdapter` kết nối dịch vụ LPR Service.
- [ ] Cài đặt `SmbFileShareImageStore` ghi ảnh an toàn và tạo thư mục theo ngày (`YYYY/MM/DD`).
- [ ] Cài đặt `WindowsHardwareService` và `RsaLicenseValidator`.
- [ ] Cài đặt toàn bộ `Mongo*Repository` và cấu hình MongoDB Driver.
- [ ] Viết Integration Tests kiểm tra từng adapter độc lập.

## 5. Lưu Ý Kỹ Thuật
- Xử lý giải phóng bộ nhớ unmanaged (`Marshal.FreeHGlobal`, `GC.KeepAlive`) triệt để trong các adapter P/Invoke để tránh memory leak.
- Đảm bảo cơ chế tự động thử kết nối lại (Auto Reconnect) khi thiết bị mạng bị ngắt quãng.
