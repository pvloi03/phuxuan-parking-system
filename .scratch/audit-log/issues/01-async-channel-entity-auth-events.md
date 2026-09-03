# 01: Nền tảng Async Channel, Entity AuditLog & Ghi nhận Auth Events (Login/Logout)

**What to build:**
Khi người dùng thực hiện đăng nhập (thành công hoặc thất bại) hoặc đăng xuất trên Web Admin, hệ thống tự động bắt sự kiện và ghi nhận một bản ghi AuditLog vào MongoDB thông qua hàng đợi bất đồng bộ (in-process background channel) mà không làm suy giảm thời gian phản hồi của API. Quản trị viên có thể truy vấn danh sách nhật ký kiểm toán này qua API phân trang.

**Blocked by:** None (can start immediately)

**Status:** completed

- [x] Entity `AuditLog`, enum `AuditActionType` và attribute `[SensitiveData]` được định nghĩa đầy đủ trong Domain
- [x] Kênh xử lý hàng đợi bất đồng bộ `System.Threading.Channels` kết hợp `IHostedService` hoạt động ổn định và có cơ chế graceful shutdown
- [x] MongoDB tự động cấu hình TTL Index dọn dẹp các bản ghi cũ hơn 12 tháng (đọc từ cấu hình)
- [x] Tự động ghi nhận thông tin đăng nhập (IP, Username, Role, UserAgent, IsSuccess, ErrorMessage) khi gọi `/api/auth/login`
- [x] Endpoint `GET /api/v1/audit-logs` trả về danh sách nhật ký phân trang chuẩn `ApiResponse<PagedResult<AuditLog>>`
- [x] Kiểm thử tự động `ApiIntegrationTests` xác minh bản ghi AuditLog được sinh ra chính xác sau khi đăng nhập
