# Đặc Tả Kỹ Thuật: Chức Năng AuditLog Cho Web Admin

## Problem Statement

Hiện tại, hệ thống PhuXuanParkingSystem trên Web Admin chưa có cơ chế ghi nhận nhật ký kiểm toán (Audit Trail) tập trung. Quản trị viên không thể theo dõi ai (Actor) đã thực hiện những thay đổi gì đối với dữ liệu hệ thống (User, Person, Vehicle, Department, Company, Contractor, Lane, Device, LicenseInfo), vào thời điểm nào, từ địa chỉ IP nào, và giá trị dữ liệu trước/sau khi thay đổi (Diff) ra sao. Điều này gây khó khăn trong việc điều tra sự cố, phát hiện gian lận, quy trách nhiệm và đảm bảo tính tuân thủ an toàn thông tin cho hệ thống quản lý bãi đỗ xe.

## Solution

Xây dựng hệ thống Nhật ký kiểm toán (AuditLog) toàn diện, hiệu năng cao và bất biến (Append-Only) cho Web Admin:
- **Backend (.NET 8 Web API + MongoDB)**:
  - Tự động ghi nhận các sự kiện xác thực (Login, Logout) qua Action Filter / Middleware.
  - Ghi nhận chi tiết các thao tác CRUD và can thiệp nhạy cảm (Đổi Role, Reset Password, Cập nhật License) kèm dữ liệu so sánh trước/sau (Old vs New Diff) và lý do can thiệp (`Reason`).
  - Ghi log bất đồng bộ (Non-blocking) qua hàng đợi bộ nhớ `System.Threading.Channels` kết hợp `BackgroundService`, đảm bảo API phản hồi tức thì (<10ms).
  - Tự động che giấu (Mask) dữ liệu nhạy cảm (`PasswordHash`, `Token`, `SecretKey`).
  - Bất biến (Append-Only): Không cung cấp API sửa/xóa; tự động dọn dẹp log cũ theo MongoDB TTL Index (mặc định 12 tháng, cấu hình qua `appsettings.json`).
- **Frontend (React 19 + Tailwind CSS + Radix UI + TanStack Query)**:
  - Trang Quản lý Nhật ký kiểm toán với bộ lọc đa chiều (Khoảng thời gian, Người thực hiện, Nhóm nghiệp vụ, Loại hành động).
  - Visual Diff Drawer (Slide-over panel bên phải) hiển thị trực quan so sánh 2 cột: Cũ (Highlight Đỏ) vs Mới (Highlight Xanh).
  - Xuất báo cáo kiểm toán ra file Excel.

## User Stories

1. As a SuperAdmin, I want to view a paginated list of all system audit logs, so that I can monitor all administrative activities on the platform.
2. As a SuperAdmin, I want to filter audit logs by date range (From Date - To Date), so that I can investigate incidents occurring within a specific timeframe.
3. As a SuperAdmin, I want to filter audit logs by Actor (Username/User ID), so that I can track all actions performed by a specific administrator.
4. As a SuperAdmin, I want to filter audit logs by Target Entity (User, Person, Vehicle, Department, Company, Contractor, Lane, Device, LicenseInfo), so that I can inspect the lifecycle history of a specific resource.
5. As a SuperAdmin, I want to filter audit logs by Action Type (Login, Logout, Create, Update, Delete, ChangePassword, ChangeRole, LicenseUpdate, Export), so that I can quickly pinpoint high-risk operations.
6. As a SuperAdmin, I want to click on an audit log row to open a Visual Diff Drawer, so that I can inspect the exact side-by-side changes (Old Values in red vs New Values in green) without leaving the list.
7. As a SuperAdmin, I want to see the reason (`Reason`) provided by an operator for high-risk actions (Delete entity, Change role, License update), so that I understand why the modification was made.
8. As a SuperAdmin, I want to export the filtered audit log list to an Excel spreadsheet, so that I can archive reports or share them with management and external auditors.
9. As a SuperAdmin, I want to see the Actor's IP Address and User-Agent in the audit log details, so that I can verify the origin and environment of the request.
10. As a Manager, I want to view audit logs relevant to my department and master data, so that I can ensure data integrity across my team.
11. As a System, I want to automatically mask sensitive fields such as passwords, tokens, and secrets in `OldValues` and `NewValues`, so that credentials are never exposed in log storage or on the UI.
12. As a System, I want to write audit logs asynchronously via in-process background channels, so that core API response latency is not degraded.
13. As a System, I want audit logs to be immutable (Append-Only) with no delete/update endpoints, so that audit records cannot be tampered with or erased by malicious users.
14. As a System, I want old audit logs beyond the retention window (12 months) to be automatically purged via MongoDB TTL Index, so that database storage remains optimized over time.

## Implementation Decisions

### 1. Domain Layer (`PhuXuanParkingSystem.Domain`)
- **Entity `AuditLog`**: Kế thừa `BaseEntity`, thuộc tính gồm:
  - `ActorId` (string?), `ActorUsername` (string), `ActorRole` (string)
  - `Source` (string: "WebAdmin")
  - `IpAddress` (string?), `UserAgent` (string?)
  - `ActionType` (`AuditActionType` Enum: Login, Logout, Create, Update, Delete, ChangePassword, ChangeRole, LicenseUpdate, Export)
  - `TargetEntity` (string: User, Person, Vehicle, Department, Company, Contractor, Lane, Device, LicenseInfo, System)
  - `TargetId` (string?), `TargetDisplay` (string?)
  - `OldValues` (string? - JSON diff), `NewValues` (string? - JSON diff), `ChangedProperties` (List<string>)
  - `Reason` (string?)
  - `IsSuccess` (bool), `ErrorMessage` (string?)
- **Value Object `AuditDiff` & Attribute `[SensitiveData]`**: Đánh dấu các trường cần che giấu khi serialize sang JSON.

### 2. API & Application Layer (`PhuXuanParkingSystem.Api`)
- **Asynchronous Audit Queue (`IAuditLogQueue` / `AuditLogChannel`)**:
  - Triển khai bằng `System.Threading.Channels.Channel<AuditLog>` unbounded/bounded với `SingleWriter = false`, `SingleReader = true`.
- **Background Worker (`AuditLogBackgroundWorker : BackgroundService`)**:
  - Đọc liên tục từ channel và ghi batch hoặc ghi từng item vào MongoDB qua `IRepository<AuditLog>`.
  - Hỗ trợ graceful shutdown (drain toàn bộ channel trước khi dừng server).
- **Audit Interceptors & Filters**:
  - `AuditActionFilter`: Tự động bắt đăng nhập thành công / thất bại, đăng xuất.
  - `IAuditService` / Helper: Cung cấp API `LogActivityAsync(...)` và `CreateDiff<T>(oldEntity, newEntity)` để các Controller/Service nghiệp vụ gọi khi thực hiện Create/Update/Delete.
- **REST Endpoints (`/api/v1/audit-logs`)**:
  - `GET /api/v1/audit-logs`: Lấy danh sách phân trang (kèm filter: `fromDate`, `toDate`, `actor`, `actionType`, `targetEntity`, `searchTerm`, `page`, `pageSize`).
  - `GET /api/v1/audit-logs/{id}`: Lấy chi tiết một bản ghi log.
  - `GET /api/v1/audit-logs/export`: Xuất danh sách log ra file Excel (.xlsx) qua EPPlus.
  - Phân quyền: Chỉ `SuperAdmin` và `Manager` mới có quyền truy cập.

### 3. Web Admin Frontend (`PhuXuanParkingSystem.Web`)
- **Trang `AuditLogsPage`** (Route: `/audit-logs`):
  - Thanh bộ lọc (DateRangePicker, Select Actor, Select ActionType, Select TargetEntity, Search Box).
  - Bảng dữ liệu (Table) hiển thị: Thời gian, Người thực hiện (kèm Badge Role), Hành động (Badge màu), Đối tượng tác động, Trạng thái, Cột thao tác (Nút Xem chi tiết).
  - Nút "Xuất Excel".
- **Component `AuditLogDetailDrawer`**:
  - Slide-over panel (Radix UI Dialog/Sheet + Tailwind) mở từ bên phải.
  - Header: Thời gian, Actor, IP, Action, Lý do (`Reason`).
  - Body: Visual Diff Component 2 cột (Cột đỏ: Giá trị cũ | Cột xanh: Giá trị mới) cho từng trường trong `ChangedProperties`.

## Testing Decisions

- **What makes a good test**: Kiểm thử tập trung vào hành vi từ ngoài vào (Black-box / Functional API tests), xác minh rằng khi thực hiện một hành động quản trị (ví dụ: Tạo hoặc Sửa Person/Vehicle/User), hệ thống tự động sinh ra bản ghi AuditLog tương ứng trong DB với đầy đủ Diff và mask dữ liệu nhạy cảm, có thể truy vấn lại qua API.
- **Test Seam chính**:
  - **API Integration Test Seam (`WebApplicationFactory<Program>`)**:
    - `Test_AuditLog_Created_On_User_Login`
    - `Test_AuditLog_Created_On_Vehicle_Create_Update_Delete`
    - `Test_AuditLog_Sensitive_Data_Masked`
    - `Test_AuditLog_Query_With_Filters_And_Pagination`
    - `Test_AuditLog_Export_Excel_Returns_File`
    - `Test_AuditLog_Immutable_Endpoints_Deny_Direct_Mutation`
  - **Background Channel Seam**:
    - Unit test cho `AuditLogQueue` và `AuditDiffHelper` đảm bảo diff chính xác các kiểu dữ liệu và bỏ qua trường `[SensitiveData]`.
- **Prior Art**: Kế thừa cấu trúc kiểm thử từ `tests/PhuXuanParkingSystem.Api.Tests/ApiIntegrationTests.cs`.

## Out of Scope

- Không ghi log các thao tác từ máy trạm WinForms Client (đã chốt ở ADR 0001).
- Không hỗ trợ xóa log thủ công qua giao diện người dùng (chỉ dọn tự động qua TTL Index).
- Không xây dựng cơ chế phục hồi dữ liệu tự động (Rollback / Undo) từ Audit Diff trong giai đoạn này.

## Further Notes

- Cấu hình thời gian lưu trữ trong `appsettings.json`:
  ```json
  "AuditLog": {
    "RetentionDays": 365,
    "ChannelCapacity": 5000
  }
  ```
