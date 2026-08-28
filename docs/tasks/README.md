# DANH SÁCH & TIẾN ĐỘ CÁC TASK DỰ ÁN (TASK ROADMAP)
**Hệ thống Quản lý Bãi đỗ xe Thái Thụy (PhuXuanParkingSystem)**

---

### 📊 BẢNG TỔNG HỢP TIẾN ĐỘ

| Mã Task | Tên Task | Trạng Thái | Mô Tả & Chi Tiết File |
| :--- | :--- | :---: | :--- |
| **[Task 001](task-001-setup-solution-skeleton.md)** | Khởi Tạo Solution Skeleton | ✅ **Hoàn thành** | Cấu hình Solution, thiết lập target framework .NET 4.8 / x86, nạp native SDKs. |
| **[Task 002](task-002-domain-entities-models.md)** | Triển Khai Domain Entities & Models | ✅ **Hoàn thành** | Xây dựng Entities (`ParkingSession`, `Vehicle`, `Person`, `Lane`, `Device`, `Department`, `Company`, `Contractor`, `User`), Value Object (`PlateNumber`), Enums và `BaseEntity` (Soft-delete). |
| **[Task 003](task-003-application-ports-usecases.md)** | Triển Khai Application & Repository Ports | ✅ **Hoàn thành** | Định nghĩa `IRepository<T>` (Generic CRUD), đăng ký Open Generic DI Container trong `Program.cs`. |
| **[Task 004](task-004-infrastructure-adapters.md)** | Triển Khai Infrastructure & MongoDB Adapter | ✅ **Hoàn thành** | Triển khai `MongoRepository<T>` kết hợp `Humanizer.Pluralize()` tự động suy luận tên Collection, `MongoDbContext` và các Adapters thiết bị ngoại vi. |
| **[Task 004-SDK](task-004-x86-native-sdk-integration.md)** | Tích Hợp x86 Native SDKs | ✅ **Hoàn thành** | Tích hợp ZKTeco Pull SDK (`plcommpro.dll`), Hikvision SDK (`HCNetSDK.dll`), NST SDK (`HISDK.dll`). |
| **[Task 005](task-005-winforms-ui-presentation.md)** | Triển Khai WinForms UI Presentation Layer | ⏳ **Đang hoàn thiện** | Giao diện chính `FrmMain`, Live Stream 4 camera, hiển thị thông tin lượt xe vào/ra. |
| **[Task 006](task-006-unit-and-integration-testing.md)** | Xây Dựng Hệ Thống Kiểm Thử Tự Động | ✅ **Hoàn thành** | Thiết lập `PhuXuanParkingSystem.Tests` (xUnit, FluentAssertions), 58/58 Unit Tests passed 100%. |
| **[Task 007](task-007-logging-system-serilog.md)** | Xây Dựng Hệ Thống Logging Bất Đồng Bộ | ✅ **Hoàn thành** | Triển khai Serilog Async File Sink, Global Exception Handler và Live UI Log. |
| **[Task 008](task-008-application-notification-system.md)** | Xây Dựng Hệ Thống Thông Báo Toàn Cục | ✅ **Hoàn thành** | Triển khai `AppNotificationService` (Pub/Sub pattern) đồng bộ trạng thái phần cứng và thông báo lên UI. |
| **[Task 009](task-009-anpr-vietnam-plate-recognition.md)** | Tích Hợp Nhận Diện Biển Số SimpleLPR3 x86 | ✅ **Hoàn thành** | Tích hợp Engine AI SimpleLPR3 32-bit cho nhận diện biển số xe Việt Nam tốc độ cao (~30-50ms), đồng bộ UI và MongoDB. |
| **[Task 017](task-013-to-019-system-upgrade-plan.md)** | Phân Hệ Quản Lý Thiết Bị Phần Cứng | ✅ **Hoàn thành** | Backend API + Giao diện quản lý Camera IP, Barrier, Đầu đọc RFID, Trạm kiểm soát với trạng thái Online/Offline. |
| **[Task 018](task-013-to-019-system-upgrade-plan.md)** | Phân Hệ Thùng Rác (Soft-Delete Recovery) | ✅ **Hoàn thành** | Xem, lọc nhóm, tìm kiếm, khôi phục từng mục hoặc khôi phục tất cả trong trang hiện tại. |
| **[Task 019](task-013-to-019-system-upgrade-plan.md)** | Hệ Thống Phân Quyền Đa Tầng (RBAC) | ✅ **Hoàn thành** | 3 cấp vai trò: Super Admin, Manager, Operator. JWT Claims + Middleware + Frontend UI Permissions. |
| **[Task 020](task-020-device-status-monitor-winform-sync.md)** | Giám Sát Tình Trạng Thiết Bị WinForms | ✅ **Hoàn thành** | Màn hình `FrmDeviceMonitor` + `DeviceHealthMonitorService` kiểm tra TCP/Ping, đồng bộ trạng thái lên Web Admin. Phím tắt F9. |
| **[Task 021](task-021-dynamic-lane-device-config-sync.md)** | Nạp Cấu Hình Động Từ MongoDB | ✅ **Hoàn thành** | Đọc cấu hình Camera/Controller trực tiếp từ MongoDB theo Làn (`Lane`) → Thiết bị (`Device`). Loại bỏ hardcode App.config. |
| **[Task 022](task-022-license-key-system.md)** | Hệ Thống Bản Quyền Phần Mềm | ✅ **Hoàn thành** | RSA-3072 License Key System với Hardware Fingerprint, WinForms LicenseTool, Footer Status, Web Admin LicensePage. 93/93 tests passed. |

---

### 📂 Danh Sách Tài Liệu Thiết Kế Kỹ Thuật Liên Quan

- **[tai-lieu-kiem-thu-test-cases.md](../tai-lieu-kiem-thu-test-cases.md)**: Đặc tả toàn bộ test cases cho Value Objects, Entities, RTLog Parser, DI Container, Humanizer Pluralize.
- **[thiet-ke-kien-truc-he-thong-kiem-soat-xe.md](../thiet-ke-kien-truc-he-thong-kiem-soat-xe.md)**: Bản thiết kế kiến trúc hệ thống tổng thể.
- **[layout-kien-truc-du-an.md](../layout-kien-truc-du-an.md)**: Bản vẽ kiến trúc phân lớp và luồng dữ liệu.
- **[layout-giao-dien-winforms.md](../layout-giao-dien-winforms.md)**: Thiết kế bố cục giao diện WinForms LiveMonitor 2 làn Vào/Ra.
