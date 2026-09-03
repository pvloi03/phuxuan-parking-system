# 04: Xuất Báo Cáo Excel & Kiểm Thử An Toàn Tính Bất Biến (Security Hardening)

**What to build:**
Quản trị viên có thể kết xuất toàn bộ hoặc danh sách nhật ký kiểm toán đã lọc ra file Excel (.xlsx) để lưu trữ hoặc gửi báo cáo kiểm toán định kỳ. Hệ thống đảm bảo tính bất biến tuyệt đối của dữ liệu kiểm toán (không tồn tại bất kỳ endpoint hoặc phương thức nào cho phép sửa hoặc xóa AuditLog trực tiếp), và kiểm soát phân quyền chặt chẽ chỉ cho phép các tài khoản có thẩm quyền (`SuperAdmin`, `Manager`) truy cập.

**Blocked by:** 03: Giao Diện Quản Lý Lịch Sử & Visual Diff Drawer trên Web Admin

**Status:** completed

- [x] Endpoint `GET /api/v1/audit-logs/export` tạo file Excel (.xlsx) định dạng chuẩn, bao gồm đầy đủ các cột thông tin và danh sách thay đổi tóm tắt
- [x] Nút "Xuất Excel" trên giao diện Web Admin kết nối API và tự động tải file về máy
- [x] Bảo mật phân quyền: Chỉ cho phép người dùng có `UserRole.SuperAdmin` hoặc `UserRole.Manager` truy cập các API AuditLog
- [x] Rà soát & Khóa cứng tính bất biến: Không có bất kỳ endpoint nào hỗ trợ PUT/PATCH/DELETE đối với collection `AuditLogs`
- [x] Bộ kiểm thử `ApiIntegrationTests` xác minh export Excel thành công và bảo vệ phân quyền hoạt động chính xác
