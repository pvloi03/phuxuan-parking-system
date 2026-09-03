# 03: Giao Diện Quản Lý Lịch Sử & Visual Diff Drawer trên Web Admin

**What to build:**
Quản trị viên đăng nhập vào Web Admin có thể truy cập trang "Nhật ký kiểm toán" (/audit-logs) để xem danh sách lịch sử thao tác với giao diện trực quan, lọc nhanh theo khoảng thời gian, người thực hiện, loại hành động và thực thể tác động. Khi bấm vào một dòng log, thanh Drawer trượt ra từ bên phải màn hình hiển thị toàn bộ chi tiết và so sánh trực quan hai cột (Cột Đỏ: Giá trị cũ | Cột Xanh: Giá trị mới) cho các trường bị thay đổi.

**Blocked by:** 02: Entity Diff Engine & Ghi Log CRUD Master Data (Vehicle/Person/User/License) kèm Reason

**Status:** completed

- [x] Route `/audit-logs` và menu điều hướng được tích hợp vào Web Admin layout
- [x] Bảng dữ liệu hỗ trợ phân trang, sắp xếp và các bộ lọc: Khoảng ngày giờ, Actor, Target Entity, Action Type
- [x] Badge màu phân loại hành động trực quan (Tạo mới: Xanh lá, Cập nhật: Vàng/Xanh dương, Xóa/Rủi ro cao: Đỏ, Auth: Tím)
- [x] Component `AuditLogDetailDrawer` (Slide-over panel từ cạnh phải) hiển thị đầy đủ thông tin: Người thao tác, IP, User-Agent, Thời gian, Lý do can thiệp
- [x] Giao diện Visual Diff 2 cột làm nổi bật sự khác biệt giữa `OldValues` và `NewValues` rõ ràng, dễ đọc
