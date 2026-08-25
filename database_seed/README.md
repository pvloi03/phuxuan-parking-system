# HƯỚNG DẪN IMPORT TOÀN BỘ CƠ SỞ DỮ LIỆU VÀO MONGODB
**Tên Database:** `PhuXuanParkingSystemDb`

---

## 🚀 Cách 1: Nạp Toàn Bộ CSDL Cùng Lúc (Khuyến Nghị - Nhanh Nhất)

### Lựa chọn A: Dùng MongoDB Shell (1 Lệnh duy nhất)
Mở Terminal / PowerShell tại thư mục `database_seed/` và chạy:
```bash
mongosh "mongodb://localhost:27017/PhuXuanParkingSystemDb" init_database.js
```
*Hoặc nhấp đúp chạy file [`import_database.bat`](file:///c:/Users/ADMIN/source/repos/PhuXuanParkingSystem/database_seed/import_database.bat).*

### Lựa chọn B: Dùng MongoDB Compass (MongoSH Console)
1. Mở **MongoDB Compass** $\rightarrow$ Kết nối tới `mongodb://localhost:27017`
2. Mở tab **>_ MongoSH** ở thanh công cụ dưới đáy màn hình Compass.
3. Mở file [`init_database.js`](file:///c:/Users/ADMIN/source/repos/PhuXuanParkingSystem/database_seed/init_database.js), copy toàn bộ nội dung và dán vào tab MongoSH rồi nhấn **Enter**.

---

## 📁 Cách 2: File JSON Trọn Bộ CSDL & Từng Collection

### 1. File trọn bộ dữ liệu CSDL:
- [`PhuXuanParkingSystemDb.json`](file:///c:/Users/ADMIN/source/repos/PhuXuanParkingSystem/database_seed/PhuXuanParkingSystemDb.json) (Bao gồm toàn bộ 10 bảng dữ liệu).

### 2. Danh sách file từng Collection (Extended JSON):
| STT | File JSON Seed | Tên Collection MongoDB | Mô Tả & Quy Chuẩn Lưu Trữ |
|:---:|:---|:---|:---|
| 1 | `Companies.json` | `Companies` | Công ty / đơn vị thành viên |
| 2 | `Contractors.json` | `Contractors` | Danh sách nhà thầu phụ |
| 3 | `Departments.json` | `Departments` | Phòng ban / bộ phận |
| 4 | `Devices.json` | `Devices` | Camera biển số, Camera toàn cảnh, Controller ZKTeco C3-200 (`Type`, `Status` dạng chuỗi) |
| 5 | `Lanes.json` | `Lanes` | Cấu hình làn vào/ra (`Direction` dạng chuỗi: `"In"`, `"Out"`) |
| 6 | `LicenseInfo.json` | `LicenseInfo` | Bản quyền phần mềm |
| 7 | `People.json` | `People` | Cán bộ / nhân viên / nhà thầu (`Type` dạng chuỗi: `"Employee"`, `"Contractor"`, `"Visitor"`) |
| 8 | `Users.json` | `Users` | Tài khoản đăng nhập (`Role` dạng chuỗi: `"Admin"`, `"Operator"`, `"Security"`) |
| 9 | `Vehicles.json` | `Vehicles` | Phương tiện giao thông (`PlateNumber` dạng chuỗi, `Type`: `"Car"`, `"Motorcycle"`) |
| 10 | `ParkingSessions.json` | `ParkingSessions` | Phiên xe vào / ra (`PlateNumber` chuỗi, `VehicleType`, `Status` dạng chuỗi) |

---

## ⚙️ Quy Chuẩn Lưu Trữ CSDL Mới
- **PlateNumber**: Toàn bộ biển số xe được lưu trữ trực tiếp dưới dạng chuỗi (`string`) đã chuẩn hóa (ví dụ: `"29A12345"`).
- **Enum Fields**: Toàn bộ các giá trị Enum được lưu trữ dưới dạng chuỗi (`string`), không lưu số int.
- **Thiết bị (Devices)**: Đã lược bỏ trường `RtspUrl`.
- **Hình ảnh hiển thị UI**: Giao diện hiển thị ảnh toàn cảnh và ảnh vùng biển số đã cắt (`CroppedPlateImage`), đồng thời lưu ảnh chụp gốc chưa cắt vào thư mục lưu trữ trên máy tính.
