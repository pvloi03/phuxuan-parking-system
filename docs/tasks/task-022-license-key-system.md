# Task 022: Hệ Thống Bản Quyền Phần Mềm (License Key System)

## 1. Mục Tiêu
Triển khai hệ thống Bản quyền (License Key) sử dụng chữ ký số RSA 3072-bit kết hợp Hardware Fingerprint (Mã máy WMI) với các quota:
- `MaxLanes`: 2 (1 vào 1 ra)
- `MaxCameras`: 4 (2 biển số + 2 toàn cảnh)
- `MaxControllers`: 1 (ZKTeco/Relay)
- WinForms Khách: Footer label "Thời gian sử dụng: ... ngày", tự chuyển sang `LicenseExpiredForm` khi hết hạn.
- Web Admin: Header Badge, trang quản lý và nạp key `LicensePage.tsx`.
- Tool WinForms riêng `HPLicenseTool` để sinh key bản quyền.

## 2. Checklist Tiến Độ
- [x] **Bước 1: Core Licensing Library (`PhuXuanParkingSystem.Domain` / `PhuXuanParkingSystem.Licensing`)**
  - [x] Nâng cấp `LicenseInfo.cs` thêm `MaxLanes`, `MaxCameras`, `MaxControllers`, `Features`, `IsPermanent`.
  - [x] Thuật toán `HardwareFingerprint.cs` (WMI: CPU, Motherboard, Disk, BIOS Serial).
  - [x] Ký số RSA & Xác thực `LicenseCrypto.cs` (RSA 3072-bit, SHA256).
  - [x] Viết unit tests kiểm thử chữ ký số, sai mã máy, hết hạn, giả mạo key.
- [x] **Bước 2: WinForms Tool Tạo Key (`HPLicenseTool`)**
  - [x] Tạo project WinForms .NET 8 `HPLicenseTool`.
  - [x] Giao diện phát hành key: nhập khách hàng, mã máy, chọn thời hạn (30 ngày, 90 ngày, 1 năm, 3 năm, vĩnh viễn), cấu hình số làn / camera / controller.
  - [x] Tự động sinh khóa RSA 3072-bit, xuất file `.lic`, sao chép key.
  - [x] Tab giải mã & kiểm tra tính toàn vẹn chữ ký số của key / file `.lic`.
- [x] **Bước 3: Tích hợp vào WinForms Trạm Vận Hành Khách (`PhuXuanParkingSystem`)**
  - [x] Tích hợp `LicenseManager.cs` kiểm tra bản quyền khi khởi động.
  - [x] Hiển thị Label Footer StatusStrip: `Thời gian sử dụng: ... ngày` (Xanh > 15 ngày, Cam <= 15 ngày, Đỏ khi hết hạn).
  - [x] Thiết kế form `LicenseExpiredForm` tự động khóa và chuyển hướng khi hết hạn / chưa kích hoạt kèm nút sao chép mã máy, ô dán key và chọn file `.lic`.
  - [x] Hỗ trợ click đúp vào footer để gia hạn sớm.
- [x] **Bước 4: Tích hợp vào Web API & Web Frontend**
  - [x] `LicenseController.cs` (Status, MachineCode, Activate, Upload-Lic).
  - [x] Kiểm tra Quota giới hạn `MaxLanes` (2 làn), `MaxCameras` (4 camera), `MaxControllers` (1 bộ) trong `LanesController.cs` và `DevicesController.cs`.
  - [x] Giao diện Web `LicensePage.tsx`, Header Badge trên Navbar, Menu sidebar.
- [x] **Bước 5: Kiểm thử và hoàn thiện**
  - [x] 93/93 Unit tests chạy thành công 100%.
  - [x] Build solution thành công 100% không lỗi.
