# Task 007: Xây Dựng Hệ Thống Logging Bất Đồng Bộ (Serilog & Global Exception Handler)

## 1. Mục Tiêu
Triển khai hệ thống Logging chuyên nghiệp với **Serilog** cho `PhuXuanParkingSystem`:
- Ghi log bất đồng bộ ngầm (`Serilog.Sinks.Async`) tránh nghẽn luồng UI và camera.
- Tự động chia file theo ngày (`Logs/app-YYYY-MM-DD.log`) và tự dọn dẹp log sau 30 ngày (`retainedFileCountLimit: 30`).
- Mặc định chỉ ghi các sự cố kỹ thuật (**Warning, Error, Fatal**) để giữ file log gọn nhẹ và tiết kiệm ổ đĩa.
- Hỗ trợ tùy biến `LogLevel` qua `App.config` (cho phép chuyển sang `Debug`/`Information` khi kỹ thuật viên cần dò lỗi).
- Bắt và ghi log 100% lỗi ngoại lệ toàn cục (**Global Unhandled Exception Handler**).
- Kênh Event Sink phát sự kiện Live Log lên giao diện WinForms (`FrmMain`).

## 2. Bối Cảnh & Phạm Vi
- **Bối cảnh**: Ứng dụng bãi xe vận hành 24/7 với 4 Camera IP, 1 Controller ZKTeco C3-200 và kết nối MongoDB. Dữ liệu lượt xe bình thường đã được lưu trữ trong MongoDB `ParkingSessions`. Do đó, file log kỹ thuật chỉ cần tập trung vào các cảnh báo mất kết nối, lỗi phần cứng, timeout và ngoại lệ hệ thống.
- **Phạm vi triển khai**:
  - `NuGet Packages`: `Serilog`, `Serilog.Sinks.File`, `Serilog.Sinks.Async`.
  - `Logging Infrastructure`: Tạo `AppLogger.cs` quản lý cấu hình Serilog, khởi tạo Rolling File Sink, Live UI Event Sink.
  - `App.config`: Bổ sung cấu hình `LogLevel` (mặc định: `Warning`), `Log_Path` (`Logs/app-.log`), `Log_RetainedDays` (`30`).
  - `Global Exception Handlers`: Tích hợp trong `Program.cs` (`Application.ThreadException`, `AppDomain.CurrentDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException`).
  - `Service Integration`: Thay thế `Debug.WriteLine` trong `ZKTecoDeviceAdapter`, `OverviewCameraService`, `PlateCameraService`, `MongoRepository` bằng `AppLogger`.
  - `Unit Tests`: Viết bộ kiểm thử kiểm chứng cấu hình Logger và cơ chế ghi log trong `tests/PhuXuanParkingSystem.Tests/Logging/LoggingTests.cs`.

## 3. Checklist Tiến Độ
- [ ] Cài đặt các package Serilog vào `PhuXuanParkingSystem.csproj` và `PhuXuanParkingSystem.Tests.csproj`.
- [ ] Bổ sung cấu hình `LogLevel` trong `App.config`.
- [ ] Xây dựng lớp `AppLogger.cs` với Asynchronous Rolling File Sink và Live Log Event Sink.
- [ ] Cài đặt Global Exception Handlers trong `Program.cs`.
- [ ] Tích hợp `AppLogger` vào các services phần cứng và kết nối MongoDB.
- [ ] Viết Unit Tests cho `AppLogger`.
- [ ] Chạy kiểm thử tự động `dotnet test` đạt 100% Passed.
- [ ] Commit và push nhánh `task/007-logging-system-serilog` lên GitHub.

## 4. Lưu Ý Kỹ Thuật
- Sử dụng cơ chế ghi bất đồng bộ (Non-blocking Ring Buffer) để đảm bảo không làm gián đoạn việc nhận frame RTSP hoặc log controller.
- Khi bắn log lên giao diện WinForms, đảm bảo kiểm tra `InvokeRequired` hoặc dùng `SynchronizationContext`.
