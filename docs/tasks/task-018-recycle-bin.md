# TASK-018: Phân Hệ Thùng Rác Soft-Delete (Recycle Bin)

## 1. Mục Tiêu (Objective)
Xây dựng phân hệ quản lý Thùng Rác (Recycle Bin) toàn diện cho hệ thống bãi xe thông minh Phú Xuân, cho phép quản trị viên xem, tìm kiếm, lọc, khôi phục hoặc xóa vĩnh viễn các thực thể đã bị xóa mềm (`IsDeleted = true`).

## 2. Phạm Vi & Các Nhóm Thực Thể (Scope & Supported Entity Types)
Phân hệ hỗ trợ quản lý xóa mềm và khôi phục cho 7 nhóm dữ liệu cốt lõi:
1. **Phương Tiện (Vehicle)**: Biển số, loại xe, chủ xe.
2. **Nhân Sự (Person)**: Mã định danh, họ tên, phòng ban/nhà thầu.
3. **Đối Tác / Nhà Thầu (Contractor)**: Mã đối tác, tên đối tác.
4. **Phòng Ban (Department)**: Mã phòng ban, tên phòng ban, công ty trực thuộc.
5. **Công Ty (Company)**: Mã công ty, tên công ty.
6. **Thiết Bị (Device)**: Tên thiết bị, loại thiết bị, địa chỉ IP.
7. **Làn Kiểm Soát (Lane)**: Mã làn, tên làn, hướng (Vào/Ra).

---

## 3. Kiến Trúc & Thiết Kế Kỹ Thuật (Technical Design)

### 3.1. Backend (ASP.NET Core API)
- **Controller**: `PhuXuanParkingSystem.Api/Controllers/RecycleBinController.cs`
- **Endpoints**:
  - `GET /api/recycle-bin`: Lấy danh sách các mục trong thùng rác có phân trang, tìm kiếm từ khóa, lọc theo nhóm `itemType`.
  - `GET /api/recycle-bin/counts`: Thống kê số lượng mục bị xóa theo từng nhóm (để hiển thị badge trên menu và tab).
  - `POST /api/recycle-bin/restore`: Khôi phục 1 mục cụ thể (`itemType`, `id`).
  - `POST /api/recycle-bin/restore-batch`: Khôi phục nhiều mục đã chọn.
  - `DELETE /api/recycle-bin/hard-delete/{itemType}/{id}`: Xóa vĩnh viễn 1 mục khỏi CSDL.
  - `POST /api/recycle-bin/hard-delete-batch`: Xóa vĩnh viễn nhiều mục đã chọn.
  - `DELETE /api/recycle-bin/empty`: Dọn sạch thùng rác (xóa vĩnh viễn toàn bộ mục bị xóa mềm).

### 3.2. Frontend (React + TypeScript + TailwindCSS)
- **Page Component**: `PhuXuanParkingSystem.Web/src/pages/RecycleBinPage.tsx`
  - Bảng danh sách mục xóa mềm với avatar/icon và badge định danh phân loại theo nhóm.
  - Tabs hoặc bộ lọc nhóm trực quan kèm badge số lượng.
  - Tìm kiếm theo từ khóa (Biển số, Họ tên, Tên đối tác, Tên thiết bị, Tên làn).
  - Khôi phục từng mục hoặc xóa vĩnh viễn từng mục.
  - Thanh công cụ nổi Floating Bulk Action Bar khi chọn dòng: Khôi phục tất cả mục đã chọn / Xóa vĩnh viễn tất cả mục đã chọn.
  - Modal xác nhận xóa vĩnh viễn chuyên nghiệp (`ConfirmDialog`).
- **Sidebar Integration**: `PhuXuanParkingSystem.Web/src/components/layout/Sidebar.tsx`
  - Thêm menu "Thùng Rác" với icon `Trash2` và badge số lượng mục đang nằm trong thùng rác.
- **Routing**: `PhuXuanParkingSystem.Web/src/routes/AppRoutes.tsx`
  - Đăng ký đường dẫn `/recycle-bin` hoặc `/trash`.

---

## 4. Kế Hoạch Thực Hiện & Checklist Tiến Độ
- [ ] 1. Tạo branch git `task/018-recycle-bin-module`.
- [ ] 2. Xây dựng `RecycleBinController.cs` trong `PhuXuanParkingSystem.Api`.
- [ ] 3. Kiểm thử API endpoint thùng rác (Query, Restore, HardDelete, Counts).
- [ ] 4. Xây dựng `RecycleBinPage.tsx` và cập nhật kiểu dữ liệu `types/index.ts`.
- [ ] 5. Tích hợp menu Thùng rác vào `Sidebar.tsx` và `AppRoutes.tsx`.
- [ ] 6. Kiểm thử giao diện và tương tác hoàn chỉnh.
- [ ] 7. Build kiểm tra toàn hệ thống và commit git.
