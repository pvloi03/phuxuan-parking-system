# Task 008: Xây Dựng Hệ Thống Sự Kiện Thông Báo Toàn Cục (AppNotificationService)

## 1. Mục Tiêu
Cung cấp một kênh phát sự kiện thông báo nghiệp vụ tập trung (**AppNotificationService**) theo mô hình Publish/Subscribe cho toàn bộ ứng dụng `PhuXuanParkingSystem`.

Hệ thống cho phép các tầng UI (Form, StatusStrip, Toast, Popup, Notification List) có thể đăng ký nhận mọi thông báo hoạt động từ ứng dụng:
- Kết nối Camera (Toàn cảnh, Biển số) thành công / thất bại.
- Kết nối Bộ điều khiển ZKTeco C3-200 thành công / thất bại.
- Sự kiện Radar phát hiện xe vào / xe ra / xe qua cổng.
- Tác vụ chụp ảnh và lưu trữ file ảnh xe vào / xe ra.
- Trạng thái CSDL MongoDB và sẵn sàng hệ thống.

---

## 2. Cấu Trúc & Thành Phần Đã Triển Khai

### 2.1. Lớp Mô Hình Thông Báo `AppNotification`
- Vị trí: `PhuXuanParkingSystem/Services/Notification/AppNotification.cs`
- Thuộc tính:
  - `Id` (Guid): Mã sự kiện duy nhất.
  - `Timestamp` (DateTime): Thời gian phát sinh sự kiện.
  - `Type` (NotificationType): `Info`, `Success`, `Warning`, `Error`.
  - `Category` (NotificationCategory): `System`, `Camera`, `Controller`, `LaneIn`, `LaneOut`, `Database`, `Vehicle`, `Security`.
  - `Title` (string): Tiêu đề ngắn gọn.
  - `Message` (string): Nội dung thông báo thân thiện với người dùng.
  - `Data` (object?): Dữ liệu đính kèm (IP thiết bị, tên file ảnh, raw log...).
  - `FormattedSummary` (string): Chuỗi định dạng trực quan kèm biểu tượng (🟢, ℹ️, ⚠️, ❌).

### 2.2. Dịch Vụ Phát Sự Kiện `AppNotificationService`
- Vị trí: `PhuXuanParkingSystem/Services/Notification/AppNotificationService.cs`
- Sự kiện trung tâm:
  ```csharp
  public static event EventHandler<AppNotification>? OnNotificationReceived;
  ```
- Các phương thức tiện ích:
  - `Notify(...)`
  - `NotifySuccess(category, title, message, data)`
  - `NotifyInfo(category, title, message, data)`
  - `NotifyWarning(category, title, message, data)`
  - `NotifyError(category, title, message, data)`
- **Exception-Safe & Thread-Safe:** Đảm bảo ngoại lệ từ UI subscriber không bao giờ làm gián đoạn luồng nghiệp vụ.

---

## 3. Danh Sách Điểm Phát Thông Báo Trong Ứng Dụng

1. **`OverviewCameraService` (Hikvision):**
   - Kết nối thành công $\rightarrow$ `NotifySuccess(Camera, "Camera Toàn Cảnh", ...)`
   - Login thất bại $\rightarrow$ `NotifyError(Camera, "Camera Toàn Cảnh", ...)`
   - Preview thất bại $\rightarrow$ `NotifyError(Camera, "Camera Toàn Cảnh", ...)`
2. **`PlateCameraService` (NST Camera):**
   - Kết nối thành công $\rightarrow$ `NotifySuccess(Camera, "Camera Biển Số", ...)`
   - Login thất bại $\rightarrow$ `NotifyError(Camera, "Camera Biển Số", ...)`
   - Preview thất bại $\rightarrow$ `NotifyError(Camera, "Camera Biển Số", ...)`
3. **`ZKTecoDeviceAdapter` (Controller C3-200):**
   - Kết nối thành công $\rightarrow$ `NotifySuccess(Controller, "Bộ Điều Khiển ZKTeco", ...)`
   - Kết nối thất bại $\rightarrow$ `NotifyError(Controller, "Bộ Điều Khiển ZKTeco", ...)`
   - Radar Làn Vào có xe $\rightarrow$ `NotifyInfo(LaneIn, "Phát hiện xe vào", ...)`
   - Radar Làn Vào hết xe $\rightarrow$ `NotifyInfo(LaneIn, "Xe đã qua cổng vào", ...)`
   - Radar Làn Ra có xe $\rightarrow$ `NotifyInfo(LaneOut, "Phát hiện xe ra", ...)`
   - Radar Làn Ra hết xe $\rightarrow$ `NotifyInfo(LaneOut, "Xe đã qua cổng ra", ...)`
4. **`FrmMain.cs` (Giao Diện & Xử Lý Làn):**
   - Bắt đầu kết nối $\rightarrow$ `NotifyInfo(System, "Khởi động hệ thống", ...)`
   - Kết nối hoàn tất $\rightarrow$ `NotifySuccess(System, "Hệ thống sẵn sàng", ...)` / `NotifyWarning(...)`
   - Chụp ảnh Làn Vào $\rightarrow$ `NotifySuccess(LaneIn, "Chụp ảnh Làn Vào", ...)` / `NotifyWarning(...)`
   - Chụp ảnh Làn Ra $\rightarrow$ `NotifySuccess(LaneOut, "Chụp ảnh Làn Ra", ...)` / `NotifyWarning(...)`
5. **`MongoDbContext.cs` (Cơ Sở Dữ Liệu):**
   - Cấu hình Index OK $\rightarrow$ `NotifySuccess(Database, "Cơ Sở Dữ Liệu", ...)`
   - Lỗi kết nối DB $\rightarrow$ `NotifyWarning(Database, "Cơ Sở Dữ Liệu", ...)`

---

## 4. Kết Quả Kiểm Thử (Unit Tests)
- Thêm file kiểm thử: `tests/PhuXuanParkingSystem.Tests/Notification/NotificationTests.cs` (7 bài test).
- Tổng số test của solution: **70/70 Tests Passed 100%**.