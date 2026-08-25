# TÀI LIỆU ĐẶC TẢ CÁC CA KIỂM THỬ (TEST CASES SPECIFICATION)
**Dự án: HPParkingThaiThuy - Hệ thống Quản lý Bãi đỗ xe Thái Thụy**

---

## 📌 1. TỔNG QUAN HỆ THỐNG KIỂM THỬ

- **Framework kiểm thử:** xUnit 2.9.2
- **Thư viện Assertion:** FluentAssertions 6.12.1
- **Thư viện Giả lập:** Moq 4.20.72
- **Môi trường thực thi:** .NET Framework 4.8 / x86
- **Vị trí Project Test:** `tests/HPParkingThaiThuy.Tests/`

---

## 📋 2. DANH MỤC CHI TIẾT CÁC TEST CASES

### 2.1. Nhóm Kiểm Thử Value Objects (`PlateNumberTests`)

| Mã Test Case | Tên Ca Kiểm Thử | Dữ Liệu Đầu Vào (Input) | Kết Quả Mong Đợi (Expected) |
| :--- | :--- | :--- | :--- |
| **TC_PN_001** | Làm sạch biển số ô tô có dấu gạch & chấm | `"29A-123.45"` | `"29A12345"` |
| **TC_PN_002** | Làm sạch biển số xe máy 2 dòng | `"29-B1\n678.90"` | `"29B167890"` |
| **TC_PN_003** | Lọc khoảng trắng và ký tự đặc biệt | `" 30F - 999.88 "` | `"30F99988"` |
| **TC_PN_004** | Xử lý chuỗi rỗng / null | `""`, `null`, `"   "` | `""` (Chuỗi rỗng an toàn) |
| **TC_PN_005** | Tự động chuyển ký tự thường thành chữ in hoa | `"51h-123.45"` | `"51H12345"` |

---

### 2.2. Nhóm Kiểm Thử Vòng Đời Phiên Gửi Xe (`ParkingSessionTests`)

| Mã Test Case | Tên Ca Kiểm Thử | Mô Tả & Điều Kiện Thực Hiện | Kết Quả Mong Đợi |
| :--- | :--- | :--- | :--- |
| **TC_PS_001** | Khởi tạo phiên xe vào (`CheckIn`) | Gọi `ParkingSession.CheckIn("L01", "29A-123.45", "ov.jpg", "pl.jpg")` | `Status == Active`, `InTime` có giá trị, `PlateNumber` được làm sạch thành `"29A12345"`. |
| **TC_PS_002** | Hoàn thành phiên khi xe ra (`CheckOut`) | Gọi `session.CheckOut("L02", "out_ov.jpg", "out_pl.jpg")` trên phiên đang active | `Status == Completed`, `OutTime` có giá trị, `Duration` tính đúng khoảng thời gian chênh lệch. |
| **TC_PS_003** | Tạo phiên xe ra không khớp vào (`CreateUnmatchedOut`) | Gọi `ParkingSession.CreateUnmatchedOut("L02", "30F-999.99", "out_ov.jpg", "out_pl.jpg")` | `Status == UnmatchedOut`, `InTime == null`, `OutTime` có giá trị. |
| **TC_PS_004** | Kiểm tra cờ `IsUnknown` khi không có tên chủ xe | `PersonName = null` hoặc `""` | `IsUnknown == true`. Khi có tên: `IsUnknown == false`. |
| **TC_PS_005** | Nối chuỗi ghi chú khi CheckOut | Phiên đã có Note `"Khách vip"`, gọi `CheckOut(..., note: "Thanh toán tiền mặt")` | `Note == "Khách vip; Thanh toán tiền mặt"`. |

---

### 2.3. Nhóm Kiểm Thử Xóa Mềm BaseEntity (`BaseEntityTests`)

| Mã Test Case | Tên Ca Kiểm Thử | Mô Tả Thực Hiện | Kết Quả Mong Đợi |
| :--- | :--- | :--- | :--- |
| **TC_BE_001** | Khởi tạo giá trị mặc định | Tạo mới thực thể kế thừa `BaseEntity` | `IsDeleted == false`, `DeletedAt == null`, `CreatedAt` được gán thời gian hiện tại. |
| **TC_BE_002** | Đánh dấu xóa mềm (`MarkDeleted`) | Gọi `entity.MarkDeleted()` | `IsDeleted == true`, `DeletedAt != null`, `UpdatedAt != null`. |
| **TC_BE_003** | Khôi phục thực thể xóa mềm (`Restore`) | Gọi `entity.Restore()` trên thực thể đã xóa | `IsDeleted == false`, `DeletedAt == null`, `UpdatedAt != null`. |

---

### 2.4. Nhóm Kiểm Thử Giải Mã Sự Kiện ZKTeco C3-200 RTLog (`ZKTecoLogEventTests`)

| Mã Test Case | Tên Ca Kiểm Thử | Chuỗi Log CSV Giả Lập | Kết Quả Mong Đợi |
| :--- | :--- | :--- | :--- |
| **TC_ZK_001** | Phân tích sự kiện Radar Làn Vào có xe | `"2026-08-25 08:00:00,0,0,1,221,0,0"` | `DoorID == 1` (Làn Vào), `EventType == 221` (Có xe), `IsVehicleEntering == true`. |
| **TC_ZK_002** | Phân tích sự kiện Radar Làn Ra có xe | `"2026-08-25 08:05:00,0,0,2,221,0,0"` | `DoorID == 2` (Làn Ra), `EventType == 221` (Có xe), `IsVehicleExiting == true`. |
| **TC_ZK_003** | Phân tích sự kiện Hết xe (Xe rời vùng quét) | `"2026-08-25 08:05:05,0,0,1,220,0,0"` | `EventType == 220` (Hết xe), không kích hoạt chụp ảnh. |
| **TC_ZK_004** | Xử lý chuỗi log lỗi / không đúng định dạng | `"invalid,csv,string"`, `""`, `null` | Hàm parse trả về `null` an toàn, không gây crash ứng dụng. |

---

### 2.5. Nhóm Kiểm Thử Dependency Injection Container (`DiContainerTests`)

| Mã Test Case | Tên Ca Kiểm Thử | Mục Đích Kiểm Thử | Kết Quả Mong Đợi |
| :--- | :--- | :--- | :--- |
| **TC_DI_001** | Resolve Singleton `MongoDbContext` | Kiểm tra tính duy nhất của kết nối DB | Không null, cùng 1 instance xuyên suốt. |
| **TC_DI_002** | Resolve Open Generic `IRepository<T>` cho mọi Entity | Kiểm tra tự động mapping `IRepository<ParkingSession>`, `IRepository<Vehicle>`, `IRepository<Person>`, `IRepository<Department>`, `IRepository<Company>`, `IRepository<Lane>`, `IRepository<Device>` | Toàn bộ đều resolve thành công, trả về instance `MongoRepository<T>`. |
| **TC_DI_003** | Resolve Giao diện chính `FrmMain` | Kiểm tra khả năng tạo Form từ DI | Form được tạo thành công với đầy đủ dependencies. |

---

### 2.6. Nhóm Kiểm Thử Tên Collection Bằng Humanizer (`MongoRepositoryUnitTests`)

| Mã Test Case | Tên Ca Kiểm Thử | Kiểu Thực Thể (Entity Type) | Tên Collection Sinh Ra Qua Pluralize() |
| :--- | :--- | :--- | :--- |
| **TC_MR_001** | Sinh tên collection `ParkingSession` | `typeof(ParkingSession)` | `"ParkingSessions"` |
| **TC_MR_002** | Sinh tên collection `Vehicle` | `typeof(Vehicle)` | `"Vehicles"` |
| **TC_MR_003** | Sinh tên collection bất quy tắc `Person` | `typeof(Person)` | `"People"` (Chuẩn ngữ pháp tiếng Anh) |
| **TC_MR_004** | Sinh tên collection tận cùng bằng y `Company` | `typeof(Company)` | `"Companies"` |
| **TC_MR_005** | Sinh tên collection `Department` | `typeof(Department)` | `"Departments"` |

---

## 🎯 3. HƯỚNG DẪN THỰC THI KIỂM THỬ

Chạy toàn bộ bộ test bằng lệnh terminal:
```powershell
dotnet test .\tests\HPParkingThaiThuy.Tests\HPParkingThaiThuy.Tests.csproj
```
