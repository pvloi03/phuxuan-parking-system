# ADR 0001: Kiến Trúc Nhật Ký Kiểm Toán (AuditLog Architecture)

## Context
Hệ thống Web Admin cần theo dõi, giám sát toàn bộ hoạt động quản trị, bảo mật và thay đổi dữ liệu trên hệ thống PhuXuanParkingSystem. Cần một giải pháp ghi log hiệu năng cao, không làm chậm các API chính, dữ liệu có tính bất biến cao để truy vết trách nhiệm nhưng vẫn tối ưu hóa dung lượng lưu trữ trên MongoDB theo thời gian.

## Decision
1. **Phạm vi (Scope)**: Tập trung ghi log toàn bộ hoạt động phát sinh từ Web Admin (Auth, CRUD Danh mục, Phân quyền, Thay đổi License).
2. **Cơ chế thu thập (Hybrid Capture)**:
   - Sử dụng Action Filter / Middleware cho các sự kiện Auth (Login, Logout).
   - Sử dụng Service / Interceptor cho các thao tác CRUD và thay đổi nghiệp vụ để lấy chính xác dữ liệu đối chiếu trước (Old) và sau (New).
3. **Hiệu năng & Xử lý Bất đồng bộ (Async Queue via System.Threading.Channels)**:
   - Sử dụng `System.Threading.Channels` in-process kết hợp với `BackgroundService` để ghi log ngầm vào MongoDB, giúp API phản hồi tức thì (<10ms) và không chặn luồng chính.
4. **Lưu trữ & Tính bất biến (Append-Only with TTL Index)**:
   - Dữ liệu `AuditLog` là Append-Only, không cung cấp API Update hoặc Delete cho bất kỳ vai trò nào.
   - Cấu hình TTL Index trên MongoDB tự động dọn dẹp các bản ghi cũ hơn 12 tháng (có thể cấu hình qua `appsettings.json`).
5. **Bảo mật & Chi tiết Thay đổi (Data Masking & Visual Diff)**:
   - Tự động che giấu (mask) dữ liệu nhạy cảm (như `PasswordHash`, `Token`, các property đánh dấu `[SensitiveData]`).
   - Bắt buộc người dùng cung cấp lý do (`Reason`) khi thực hiện các hành động rủi ro cao (Xóa dữ liệu, Đổi Role, Sửa License).
   - Giao diện Web Admin hiển thị Slide-over Drawer với Visual Diff 2 cột (Old vs New highlight).

## Considered Options
- *Ghi log đồng bộ (Synchronous write)*: Bị loại bỏ vì làm tăng độ trễ (latency) của mọi API thêm 10–30ms và có thể gây lỗi API nếu DB ghi log gặp nghẽn tạm thời.
- *Full Snapshot toàn bộ JSON*: Bị loại bỏ vì tiêu tốn nhiều dung lượng DB không cần thiết so với việc chỉ lưu các trường bị thay đổi (`ChangedProperties`).
- *Chỉ ghi qua HTTP Middleware*: Bị loại bỏ vì không capture được trạng thái entity cũ trong DB trước khi cập nhật.
