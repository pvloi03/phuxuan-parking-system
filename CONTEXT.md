# CONTEXT.md — PhuXuanParkingSystem Domain Model

> **Quy tắc**: File này chỉ chứa **glossary** và định nghĩa ngôn ngữ nghiệp vụ. Không chứa chi tiết implementation, spec, hay scratch pad.

---

## Ubiquitous Language (Ngôn Ngữ Nghiệp Vụ)

### Core Domain Terms

| Term | Canonical Definition | Synonyms | Notes |
|------|---------------------|----------|-------|
| **ParkingSession** | Phiên gửi xe (Aggregate Root). Đại diện cho một lượt xe vào-ra trọn vẹn, bao gồm: biển số, ảnh vào/ra, thời gian, chủ xe, trạng thái. | Lượt xe, AccessEvent, CheckInOut | |
| **Vehicle** | Phương tiện đăng ký trong hệ thống. Có biển số (PlateNumber), loại xe (VehicleType), chủ sở hữu (OwnerPersonId). | Xe | |
| **Person** | Người dùng/hành khách. Có thể là Employee, Contractor, Visitor, VIP. Liên kết đến Department và Company. | Chủ xe, Người lái | |
| **Lane** | Làn kiểm soát (vào hoặc ra). Chứa cấu hình thiết bị: PlateCamera, OverviewCamera, Controller. | Làn xe | |
| **Device** | Thiết bị phần cứng gắn với làn: Camera biển số (PlateCamera), Camera toàn cảnh (OverviewCamera), hoặc Controller (Barrier/Radar). | Thiết bị | ⚠️ Phân biệt với DeviceType |
| **Department** | Phòng ban/bộ phận trong tổ chức. Thuộc về một Company. | Phòng ban | |
| **Company** | Công ty/đơn vị thành viên. Chứa nhiều Department. | Đơn vị | |
| **Contractor** | Đơn vị nhà thầu/đối tác bên ngoài. | Nhà thầu | |
| **User** | Tài khoản đăng nhập hệ thống Web Admin/WinForms. Phân quyền theo UserRole. | Tài khoản | Khác với Person |
| **LicenseInfo** | Bản quyền phần mềm. Chứa Hardware Fingerprint, ExpiryDate, Quota limits (MaxLanes, MaxCameras, MaxControllers). | Bản quyền, License | |

### Value Objects

| Term | Definition |
|------|------------|
| **PlateNumber** | Biển số xe đã chuẩn hóa. Tự động loại bỏ dấu ., -, khoảng trắng. Hỗ trợ format display VN (29A-123.45). Immutable. |
| **ImageStoragePath** | Đường dẫn file ảnh snapshot (UNC hoặc local). Immutable. Có custom BSON serializer. |
| **BaseEntity** | Lớp cơ sở cho tất cả entities. Cung cấp: Id (ObjectId), CreatedAt, UpdatedAt, IsDeleted, DeletedAt. Hỗ trợ Soft-Delete. |

### Enums

| Enum | Values | Usage |
|------|--------|-------|
| **VehicleType** | `Car=1`, `Motorcycle=2`, `Truck=3`, `Other=99` | Loại phương tiện |
| **ParkingSessionStatus** | `Active=1`, `Completed=2`, `UnmatchedOut=3`, `Cancelled=4` | Trạng thái phiên xe |
| **PersonType** | `Employee`, `Contractor`, `Visitor`, `VIP`, `Other` | Loại người dùng |
| **LaneDirection** | `In`, `Out`, `Bidirectional` | Chiều làn |
| **DeviceType** | `PlateCamera=1`, `OverviewCamera=2`, `Controller=3` | Loại thiết bị |
| **DeviceStatus** | `Connected`, `Disconnected`, `Error` | Trạng thái kết nối |
| **UserRole** | `SuperAdmin`, `Manager`, `Operator` | Phân quyền |

---

## Domain Invariants (Ràng Buộc Bất Biến)

1. **ParkingSession.CheckOut()** chỉ gọi được khi `Status == Active`
2. **ParkingSession.UnmatchedOut** chỉ tạo khi xe ra mà không có bản ghi vào
3. **Vehicle** phải có `PlateNumber` hợp lệ (sau khi Clean)
4. **LicenseInfo.IsValid** yêu cầu: `IsActive && !IsDeleted && !IsExpired && !string.IsNullOrWhiteSpace(LicenseKey)`
5. **LicenseInfo.IsPermanent** khi `ExpiryDate.Year >= 2099`

---

## Open Questions / Cần Xác Nhận

1. **DeviceType Controller** có cần phân biệt ZKTeco C3-200 với Relay đơn giản không? Hiện tại gộp chung.
2. **Contractor** có cần liên kết ngược với Persons không? (Hiện chỉ có ContractorId trong Person)
3. **Lane.TriggerAuxPort** có cần hỗ trợ nhiều cổng Aux không, hay chỉ 1 là đủ?

---

## Discrepancies Found (Tài Liệu vs Code)

### ✅ Đã Sửa (2026-08-27)

| Vấn đề | Trạng thái | Chi tiết |
|--------|-----------|----------|
| DeviceType Enum | ✅ **Đã cập nhật DOCS.md** | Code có 3 giá trị: PlateCamera, OverviewCamera, Controller |
| Tên Project | ✅ **Đã cập nhật DOCS.md** | PhuXuanParkingSystem (khớp với code) |
| License System | ✅ **Đã thêm Section 9** | Task-022 đã hoàn thành, bổ sung vào DOCS.md |
| Mô hình dữ liệu | ✅ **Đã cập nhật Section 4** | Code mẫu khớp với implementation thực tế |

### ⚠️ Vấn đề cần theo dõi

1. **DeviceType Controller** gộp chung ZKTeco C3-200 và Relay. Nếu cần phân biệt, có thể bổ sung thêm `DeviceModel` hoặc `DeviceBrand`.

---

*Last updated: 2026-08-27*
*Updated by: Claude Code (Domain Modeling)*
