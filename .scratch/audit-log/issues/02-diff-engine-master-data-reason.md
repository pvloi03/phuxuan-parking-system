# 02: Entity Diff Engine & Ghi Log CRUD Master Data (Vehicle/Person/User/License) kèm Reason

**What to build:**
Khi quản trị viên thêm, sửa hoặc xóa các đối tượng dữ liệu cốt lõi (Phương tiện Vehicle, Nhân sự Person, Tài khoản User, Bản quyền LicenseInfo), hệ thống tự động tính toán đối chiếu giá trị trước (Old) và sau (New), ghi nhận danh sách các trường thay đổi (`ChangedProperties`), tự động che giấu thông tin nhạy cảm (như `PasswordHash`), và bắt buộc phải có lý do (`Reason`) đối với các hành động rủi ro cao (Xóa dữ liệu, Đổi vai trò `UserRole`, Sửa bản quyền `LicenseInfo`).

**Blocked by:** 01: Nền tảng Async Channel, Entity AuditLog & Ghi nhận Auth Events (Login/Logout)

**Status:** completed

- [x] Bộ công cụ `AuditDiffHelper` tính toán chính xác dữ liệu cũ vs mới và trích xuất danh sách `ChangedProperties`
- [x] Tự động che giấu các trường nhạy cảm (`PasswordHash`, `Token`, `SecretKey` hoặc các property có `[SensitiveData]`) trong `OldValues` và `NewValues`
- [x] Tích hợp ghi AuditLog vào các endpoint CRUD của `Vehicle`, `Person`, `User`, `LicenseInfo`
- [x] Bắt buộc cung cấp `Reason` khi thực hiện Xóa dữ liệu (Delete), Đổi Role User, hoặc Cập nhật License; từ chối request kèm thông báo lỗi rõ ràng nếu thiếu lý do
- [x] Bộ kiểm thử `ApiIntegrationTests` xác minh Diff được tạo chính xác, dữ liệu nhạy cảm được mask và validation `Reason` hoạt động chuẩn xác
