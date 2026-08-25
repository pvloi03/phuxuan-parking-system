# Task 021: Nạp Cấu Hình Thiết Bị Camera & Controller Động Theo Làn (Lanes) Từ MongoDB

## 1. Mục Tiêu
Nâng cấp cơ chế nạp cấu hình phần cứng trong ứng dụng WinForms (`PhuXuanParkingSystem`) từ việc hardcode mã thiết bị / App.config sang việc **đọc cấu hình liên kết Làn kiểm soát (`Lane`) - Thiết bị (`Device`) trực tiếp từ MongoDB**.

---

## 2. Bối Cảnh & Phạm Vi
- **Vấn đề trước đây**: `FrmMain.Cameras.cs` nạp cấu hình theo tên `Contains("Vào")` hoặc mã hardcode `CAM-IN-PLT`. Khi người dùng tạo hoặc chỉnh sửa thiết bị/làn trên Web Admin (ví dụ mã `CAM-01`, `CAM-02`), WinForms không map được đúng mà fallback về `App.config`.
- **Giải pháp**:
  1. `FrmMain` inject `IRepository<Lane> _laneRepo` và `IRepository<Device> _deviceRepo`.
  2. Hàm `LoadConfigurationsFromDbAsync()`:
     - Truy vấn các Làn kiểm soát (`Lane`) đang kích hoạt (`IsActive == true && !IsDeleted`).
     - Lấy Làn Vào (`Direction == LaneDirection.In`) ➔ Đọc `PlateCameraDeviceId`, `OverviewCameraDeviceId`, `ControllerDeviceId`.
     - Lấy Làn Ra (`Direction == LaneDirection.Out`) ➔ Đọc `PlateCameraDeviceId`, `OverviewCameraDeviceId`, `ControllerDeviceId`.
     - Tra cứu thông tin chi tiết của các thiết bị này từ `_deviceRepo` để nạp IP, Port, Username, Password cho từng Camera và Controller.
     - Giữ cơ chế Fallback an toàn (nếu Làn chưa gán ID thiết bị, tự động tìm theo `DeviceType` hoặc dùng `App.config`).

---

## 3. Checklist Thực Hiện
- [x] 1. Cập nhật constructor & trường `IRepository<Lane> _laneRepo` trong `FrmMain.cs`.
- [x] 2. Đăng ký `IRepository<Lane>` vào DI trong `Program.cs`.
- [x] 3. Nâng cấp phương thức `LoadConfigurationsFromDbAsync()` trong `FrmMain.Cameras.cs`.
- [x] 4. Biên dịch và kiểm thử WinForms x86 (`0 Error, 0 Warning`).
