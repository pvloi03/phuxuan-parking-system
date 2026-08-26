# Kế Hoạch Triển Khai Hệ Thống Phân Hệ Mới & Nâng Cấp Toàn Diện HPParking

Tài liệu này chi tiết hóa kế hoạch xây dựng các phân hệ mới và nâng cấp hệ thống theo yêu cầu:
1. **Sidebar Menu Phân Cấp (Submenu)**: Tổ Chức & Đơn Vị -> Công ty, Phòng ban, Đối tác, Nhân sự.
2. **Nhập / Xuất Excel Hàng Loạt (Import/Export Excel)** cho các danh mục.
3. **Mở Rộng Dữ Liệu `ParkingSession`**: Lưu `PersonId`, `PersonType`, `CompanyName`, `DepartmentName` để hiển thị đầy đủ hồ sơ đối tượng (Nhân viên, Đối tác, Khách thăm, Người lạ).
4. **Phân Hệ Quản Lý Thiết Bị Phần Cứng (Hardware Devices)**: Camera IP, Barrier, Đầu đọc thẻ RFID, Trạm kiểm soát.
5. **Phân Hệ Thùng Rác (Recycle Bin / Soft-Delete Recovery)**: Xem, lọc nhóm, tìm kiếm, khôi phục từng mục hoặc khôi phục tất cả trong trang hiện tại.
6. **Hệ Thống Phân Quyền (RBAC)**: Phân quyền vai trò rõ ràng (Super Admin, Manager, Operator) ở cả Backend API và Frontend Web.

---

## Danh Sách Các Task Chi Tiết

| Mã Task | Tên Phân Hệ / Nhiệm Vụ | Mục Tiêu & Phạm Vi |
| :--- | :--- | :--- |
| **`TASK-013`** | **Mở Rộng Schema & Domain Entities** | Bổ sung Entity `Company`, `Device`, mở rộng `Person` (loại đối tượng: Nhân viên, Đối tác, Khách thăm, Người lạ), mở rộng `ParkingSession` (PersonId, CompanyName, DepartmentName, PersonType). |
| **`TASK-014`** | **Sidebar Menu Con (Collapsible Submenu) & Định Tuyến** | Cấu trúc lại Sidebar: Nhóm *Tổ Chức & Đơn Vị* có submenu xổ xuống (Công ty, Phòng ban, Đối tác, Nhân sự) và các menu mới (Thiết bị, Thùng rác). |
| **`TASK-015`** | **Phân Hệ Quản Lý Tổ Chức & Đơn Vị (CRUD + Excel)** | Xây dựng CRUD, Import Excel mẫu, Export Excel cho: Công ty, Phòng ban, Đối tác, Nhân sự. |
| **`TASK-016`** | **Liên Kết & Hiển Thị Hồ Sơ Đối Tượng Trong Lịch Sử Ra Vào** | Cập nhật WinForms Sync + API + Modal Chi tiết `HistoryPage` hiển thị chính xác đối tượng: Thuộc Công ty nào, Phòng ban nào, Phân loại (Nhân viên / Đối tác / Khách thăm / Vãng lai). |
| **`TASK-017`** | **Phân Hệ Quản Lý Thiết Bị Phần Cứng (Device Management)** | Backend API + Giao diện quản lý Camera IP (Hikvision/Dahua/RTSP), Barrier, Đầu đọc RFID, Trạm kiểm soát với trạng thái Online/Offline, IP, Port, Vị trí. |
| **`TASK-018`** | **Phân Hệ Thùng Rác (Recycle Bin / Soft-Delete Restore)** | Quản lý toàn bộ dữ liệu bị xóa mềm (`IsDeleted = true`), lọc theo nhóm danh mục, tìm kiếm, khôi phục đơn lẻ, khôi phục cả trang, xóa vĩnh viễn có cảnh báo. |
| **`TASK-019`** | **Hệ Thống Phân Quyền Đa Tầng (RBAC)** | Phân quyền Backend JWT Claims + Middleware `[Authorize(Roles = "...")]` và Frontend UI Permissions (SuperAdmin, Manager, Operator) ẩn/hiện chức năng tương ứng. |

---

## Chi Tiết Kỹ Thuật Từng Task

### Task 013: Mở Rộng Schema CSDL & Domain
- **New Entity**: `Company` (Id, Code, Name, TaxCode, Address, Phone, Email, Note, BaseEntity).
- **New Entity**: `Device` (Id, DeviceCode, Name, DeviceType [Camera, Barrier, CardReader, LedBoard], IpAddress, Port, RTSPUrl, LaneId, Status [Online, Offline, Error], Note, BaseEntity).
- **Enhance Entity `Person`**:
  - `PersonType`: Enum (`Employee`, `Partner`, `Visitor`, `Stranger`).
  - `CompanyId`, `CompanyName`, `DepartmentId`, `DepartmentName`.
- **Enhance Entity `ParkingSession`**:
  - `PersonId`, `PersonType`, `CompanyName`, `DepartmentName`.

### Task 014: Sidebar Menu Con (Collapsible Submenu)
- Xây dựng Submenu Accordion xổ xuống mượt mà trong Sidebar:
  - 🏢 **Tổ Chức & Đơn Vị** `[v]`
    - 🏢 Công ty & Doanh Nghiệp (`/companies`)
    - 🏬 Phòng Ban (`/departments`)
    - 🤝 Đối Tác & Nhà Thầu (`/partners`)
    - 👥 Danh Sách Nhân Sự (`/people`)
  - 🖥️ **Thiết Bị Phần Cứng** (`/devices`)
  - 🗑️ **Thùng Rác Hệ Thống** (`/recycle-bin`)

### Task 015: Nhập / Xuất Excel & Quản Lý Dữ Liệu
- Sử dụng `EPPlus` ở Backend và tải template Excel chuẩn ở Frontend.
- Modal kéo thả file Excel để Import hàng loạt hàng trăm bản ghi nhân sự/công ty chỉ trong 1 click.
- Nút Xuất Excel định dạng chuẩn bảng biểu có màu sắc và auto fit cột.

### Task 016: Cập Nhật Lịch Sử Ra Vào
- Khi xe vào/ra: Tra cứu `Person` theo Biển số xe hoặc Mã thẻ -> Tự động điền `PersonId`, `CompanyName`, `DepartmentName`, `PersonType` vào `ParkingSession`.
- Cập nhật Card "Phương Tiện & Chủ Xe" trong `HistoryPage.tsx`: Hiển thị Badge Phân Loại (Nhân viên / Đối tác / Khách / Người lạ) + Tên Công ty + Phòng ban.

### Task 017: Quản Lý Thiết Bị Phần Cứng
- Trang `/devices` quản lý danh mục Camera IP, Barrier tự động, Đầu đọc thẻ.
- Card thống kê thiết bị: Tổng số, Đang hoạt động (Online), Mất kết nối (Offline).
- Form thêm/sửa thông số IP, Port, RTSP stream URL, kiểm tra kết nối Ping (Ping Test).

### Task 018: Phân Hệ Thùng Rác (Recycle Bin)
- Backend: Endpoint truy vấn chung hoặc theo entity với filter `IsDeleted = true`.
- Endpoint `POST /api/recyclebin/restore` (khôi phục 1 hoặc nhiều ID: set `IsDeleted = false`).
- Endpoint `POST /api/recyclebin/permanent-delete` (xóa cứng khỏi MongoDB).
- Frontend: Bảng dữ liệu có Filter Tabs (`Tất cả`, `Lịch sử xe`, `Nhân sự`, `Công ty/Phòng ban`, `Thiết bị`), nút Khôi phục từng mục và Khôi phục cả trang.

### Task 019: Phân Quyền Rõ Ràng (RBAC)
- 3 cấp độ vai trò:
  1. **Super Admin**: Toàn quyền tất cả các phân hệ, cấu hình thiết bị, thùng rác, tài khoản.
  2. **Manager**: Quản lý nghiệp vụ (Công ty, Phòng ban, Nhân sự, Phương tiện, Xem và xuất báo cáo). Không can thiệp thiết bị và thùng rác xóa vĩnh viễn.
  3. **Operator (Bảo vệ)**: Chỉ xem Lịch Sử Ra Vào và Dashboard trực ban, không có quyền xóa dữ liệu.

---

## Kế Hoạch Thực Hiện Tiếp Theo

Theo quy tắc dự án, chúng ta sẽ bắt đầu từ **`TASK-013 & TASK-014`** trước (tạo branch Git, cập nhật Domain Entity và cấu trúc Sidebar Submenu), sau đó lần lượt hoàn thiện các phân hệ.

Xin mời bạn duyệt kế hoạch để bắt đầu triển khai!
