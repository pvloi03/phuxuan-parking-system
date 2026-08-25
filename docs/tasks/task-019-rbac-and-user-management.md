# TASK-019: Phân Quyền RBAC (Role-Based Access Control) & Quản Lý Tài Khoản Người Dùng

## 1. Mục Tiêu (Objective)
Xây dựng phân hệ Quản lý Người dùng & Phân quyền RBAC toàn diện cho hệ thống bãi xe Phú Xuân, đảm bảo các tài khoản được phân vai trò rõ ràng, kiểm soát chặt chẽ quyền truy cập giao diện (Menu, Trang, Nút chức năng) và bảo mật các API endpoints.

## 2. Hệ Thống 5 Vai Trò Người Dùng (User Roles)
1. 👑 **Admin (Quản trị viên)**: Toàn quyền truy cập mọi tính năng (Cấu hình thiết bị, Quản lý Làn, Quản lý tài khoản, Thùng rác & Xóa vĩnh viễn, Cấu hình hệ thống).
2. 👔 **Manager (Quản lý)**: Quản lý tổ chức, công ty, phòng ban, đối tác, nhân sự, phương tiện, xem dashboard và xuất báo cáo lịch sử.
3. 🛡️ **Operator (Nhân viên vận hành / Giám sát)**: Tra cứu xe ra vào, xem dashboard thống kê, tìm kiếm thông tin xe/nhân sự (chỉ xem, không sửa/xóa thiết bị hoặc tổ chức).
4. 👮 **Security (Bảo vệ trực làn)**: Giám sát làn xe và tra cứu lịch sử ca trực.
5. 👁️ **Viewer (Người xem)**: Chỉ xem thống kê và báo cáo.

---

## 3. Kiến Trúc Kỹ Thuật (Technical Design)

### 3.1. Backend API: `UsersController.cs`
- `GET /api/users`: Danh sách tài khoản có phân trang, tìm kiếm từ khóa, lọc theo vai trò (`UserRole`).
- `GET /api/users/{id}`: Chi tiết tài khoản.
- `POST /api/users`: Tạo mới tài khoản (mã hóa mật khẩu bằng BCrypt, kiểm tra trùng lặp username/email).
- `PUT /api/users/{id}`: Cập nhật thông tin, đổi vai trò, cập nhật trạng thái `isActive`.
- `PUT /api/users/{id}/password`: Đổi mật khẩu / Reset mật khẩu.
- `DELETE /api/users/{id}`: Xóa mềm tài khoản (kiểm tra không được xóa chính mình hoặc xóa Admin cuối cùng).

### 3.2. Frontend: `UsersPage.tsx`
- Bảng danh sách tài khoản kèm Avatar, Tên đăng nhập, Họ tên, Email, Số điện thoại, Vai trò (kèm badge màu riêng biệt), Trạng thái, Lần đăng nhập cuối.
- Modal Thêm mới / Chỉnh sửa tài khoản.
- Modal Reset / Đổi mật khẩu.
- Chuyển đổi nhanh trạng thái Khóa / Mở khóa tài khoản.
- Xóa tài khoản với `ConfirmDialog`.

### 3.3. Kiểm Soát Quyền Truy Cập (Access Control)
- **Menu Sidebar (`Sidebar.tsx`)**: Tự động ẩn các mục không thuộc quyền hạn của vai trò đang đăng nhập. Thêm menu "Quản Lý Tài Khoản" cho vai trò `Admin`.
- **Bảo Vệ Route (`ProtectedRoute.tsx`)**: Chặn người dùng truy cập URL trái phép.
- **Bảo Vệ Hành Động (Action Permission Helper)**: Tạo hook `usePermission()` để ẩn/vô hiệu hóa các nút nhạy cảm (Thêm mới, Chỉnh sửa, Xóa, Xuất Excel, Dọn thùng rác) dựa trên vai trò.
- **Backend Authorization**: Gắn `[Authorize(Roles = "Admin,Manager")]` trên các Controller endpoints.

---

## 4. Kế Hoạch Thực Hiện & Checklist
- [ ] 1. Tạo branch git `task/019-rbac-and-user-management`.
- [ ] 2. Xây dựng `UsersController.cs` với đầy đủ bảo mật và xác thực vai trò.
- [ ] 3. Cập nhật `types/index.ts` và tạo `userService.ts`.
- [ ] 4. Xây dựng hook `usePermission` hỗ trợ phân quyền hành động trên UI.
- [ ] 5. Xây dựng `UsersPage.tsx` chuyên nghiệp.
- [ ] 6. Cập nhật `Sidebar.tsx`, `ProtectedRoute.tsx`, `AppRoutes.tsx`.
- [ ] 7. Gắn `[Authorize]` trên các API Controllers.
- [ ] 8. Kiểm thử toàn diện và commit git.
