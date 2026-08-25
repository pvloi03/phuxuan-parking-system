# DANH SÁCH & TIẾN ĐỘ CÁC TASK DỰ ÁN (TASK ROADMAP)
**Hệ thống Quản lý Bãi đỗ xe Thái Thụy (PhuXuanParkingSystem)**

---

### 📊 BẢNG TỔNG HỢP TIẾN ĐỘ

| Mã Task | Tên Task | Trạng Thái | Mô Tả & Chi Tiết File |
| :--- | :--- | :---: | :--- |
| **[Task 001](file:///c:/Users/ADMIN/source/repos/PhuXuanParkingSystem/docs/tasks/task-001-setup-solution-skeleton.md)** | Khởi Tạo Solution Skeleton | ✅ **Hoàn thành** | Cấu hình Solution, thiết lập target framework .NET 4.8 / x86, nạp native SDKs. |
| **[Task 002](file:///c:/Users/ADMIN/source/repos/PhuXuanParkingSystem/docs/tasks/task-002-domain-entities-models.md)** | Triển Khai Domain Entities & Models | ✅ **Hoàn thành** | Xây dựng Entities (`ParkingSession`, `Vehicle`, `Person`, `Lane`, `Device`, `Department`, `Company`, `Contractor`, `User`), Value Object (`PlateNumber`), Enums và `BaseEntity` (Soft-delete). |
| **[Task 003](file:///c:/Users/ADMIN/source/repos/PhuXuanParkingSystem/docs/tasks/task-003-application-ports-usecases.md)** | Triển Khai Application & Repository Ports | ✅ **Hoàn thành** | Định nghĩa `IRepository<T>` (Generic CRUD), đăng ký Open Generic DI Container trong `Program.cs`. |
| **[Task 004](file:///c:/Users/ADMIN/source/repos/PhuXuanParkingSystem/docs/tasks/task-004-infrastructure-adapters.md)** | Triển Khai Infrastructure & MongoDB Adapter | ✅ **Hoàn thành** | Triển khai `MongoRepository<T>` kết hợp `Humanizer.Pluralize()` tự động suy luận tên Collection, `MongoDbContext` và các Adapters thiết bị ngoại vi. |
| **[Task 004-SDK](file:///c:/Users/ADMIN/source/repos/PhuXuanParkingSystem/docs/tasks/task-004-x86-native-sdk-integration.md)** | Tích Hợp x86 Native SDKs | ✅ **Hoàn thành** | Tích hợp ZKTeco Pull SDK (`plcommpro.dll`), Hikvision SDK (`HCNetSDK.dll`), NST SDK (`HISDK.dll`). |
| **[Task 005](file:///c:/Users/ADMIN/source/repos/PhuXuanParkingSystem/docs/tasks/task-005-winforms-ui-presentation.md)** | Triển Khai WinForms UI Presentation Layer | ⏳ **Đang hoàn thiện** | Giao diện chính `FrmMain`, Live Stream 4 camera, hiển thị thông tin lượt xe vào/ra. |
| **[Task 006](file:///c:/Users/ADMIN/source/repos/PhuXuanParkingSystem/docs/tasks/task-006-unit-and-integration-testing.md)** | Xây Dựng Hệ Thống Kiểm Thử Tự Động | ✅ **Hoàn thành** | Thiết lập `PhuXuanParkingSystem.Tests` (xUnit, FluentAssertions), 58/58 Unit Tests passed 100%. |

---

### 📂 Danh Sách Tài Liệu Thiết Kế Kỹ Thuật Liên Quan

- **[tai-lieu-kiem-thu-test-cases.md](file:///c:/Users/ADMIN/source/repos/PhuXuanParkingSystem/docs/tai-lieu-kiem-thu-test-cases.md)**: Đặc tả toàn bộ 58 test cases cho Value Objects, Entities, RTLog Parser, DI Container, Humanizer Pluralize.
- **[thiet-ke-kien-truc-he-thong-kiem-soat-xe.md](file:///c:/Users/ADMIN/source/repos/PhuXuanParkingSystem/docs/thiet-ke-kien-truc-he-thong-kiem-soat-xe.md)**: Bản thiết kế kiến trúc hệ thống tổng thể.
- **[layout-kien-truc-du-an.md](file:///c:/Users/ADMIN/source/repos/PhuXuanParkingSystem/docs/layout-kien-truc-du-an.md)**: Bản vẽ kiến trúc phân lớp và luồng dữ liệu.
- **[layout-giao-dien-winforms.md](file:///c:/Users/ADMIN/source/repos/PhuXuanParkingSystem/docs/layout-giao-dien-winforms.md)**: Thiết kế bố cục giao diện WinForms LiveMonitor 2 làn Vào/Ra.
