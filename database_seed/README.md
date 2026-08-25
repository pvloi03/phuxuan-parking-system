# HƯỚNG DẪN IMPORT DỮ LIỆU VÀO MONGODB COMPASS
**Cơ sở dữ liệu:** `PhuXuanParkingSystemDb`

---

## 📁 Danh Sách File Dữ Liệu Mẫu (Collection Seed Files)

Toàn bộ các file dữ liệu được lưu trong thư mục `database_seed/` theo chuẩn **Extended JSON** (được MongoDB Compass hỗ trợ nhận diện tự động `ObjectId`, `DateTime`, `Number`...):

| STT | File JSON Seed | Tên Collection trong MongoDB | Ý Nghĩa / Nội Dung Dữ Liệu |
| :---: | :--- | :--- | :--- |
| 1 | [`Companies.json`](Companies.json) | `Companies` | Danh sách Công ty / Đơn vị thành viên |
| 2 | [`Departments.json`](Departments.json) | `Departments` | Danh sách Phòng ban (Ban Giám Đốc, Kỹ thuật, Kế toán, An ninh...) |
| 3 | [`Contractors.json`](Contractors.json) | `Contractors` | Danh sách Nhà thầu / Đơn vị đối tác |
| 4 | [`People.json`](People.json) | `People` | Danh sách Nhân viên, Cán bộ, Nhà thầu |
| 5 | [`Vehicles.json`](Vehicles.json) | `Vehicles` | Danh sách Xe đã đăng ký (Biển số: `29A12345`, `30F99988`, `17A08866`, `17B167890`, `88LD00122`...) |
| 6 | [`Lanes.json`](Lanes.json) | `Lanes` | Cấu hình Làn Vào (`LANE-IN-01`) & Làn Ra (`LANE-OUT-01`) |
| 7 | [`Devices.json`](Devices.json) | `Devices` | Cấu hình 4 Camera (NST, Hikvision) & Controller ZKTeco C3-200 |
| 8 | [`Users.json`](Users.json) | `Users` | Tài khoản đăng nhập hệ thống (`admin`, `baove1`, `baove2`) |
| 9 | [`ParkingSessions.json`](ParkingSessions.json) | `ParkingSessions` | Dữ liệu mẫu các lượt xe đang gửi (Active) và đã hoàn tất (Completed) |

---

## 🛠️ Các Bước Import Bằng MongoDB Compass

### Bước 1: Kết Nối MongoDB
1. Mở ứng dụng **MongoDB Compass**.
2. Nhập Connection String: `mongodb://localhost:27017` (hoặc IP Server của bạn) $\rightarrow$ bấm **Connect**.

### Bước 2: Tạo Cơ Sở Dữ Liệu (Nếu chưa có)
1. Ở thanh menu bên trái, bấm nút **`+`** (Create database).
2. Nhập:
   - **Database Name:** `PhuXuanParkingSystemDb`
   - **Collection Name:** `Companies`
3. Bấm **Create Database**.

### Bước 3: Import Từng Collection
Đối với từng file `.json` trong thư mục `database_seed/`:

1. Bấm vào Database `PhuXuanParkingSystemDb`.
2. Tạo Collection tương ứng nếu chưa có (bấm nút `Create Collection` và nhập đúng tên Collection ở bảng trên).
3. Bấm vào Collection cần import $\rightarrow$ chọn tab **`Documents`** $\rightarrow$ bấm nút **`Add Data`** $\rightarrow$ chọn **`Import JSON or CSV file`**.
4. Chọn file `.json` tương ứng trong thư mục `database_seed/`.
5. Đảm bảo chọn định dạng **JSON** $\rightarrow$ bấm **`Import`**.
6. Compass sẽ thông báo `Import completed successfully`.

---

## 💡 Lưu Ý Quan Trọng
- Hệ thống `PhuXuanParkingSystem` sử dụng quy tắc đặt tên Collection tự động số nhiều (**Humanizer.Pluralize**):
  - Ví dụ thực thể `Person` $\rightarrow$ Collection tên là `People` (không phải `Persons`).
  - Thực thể `Company` $\rightarrow$ Collection tên là `Companies`.
- Các biển số xe trong bảng `Vehicles` đã được chuẩn hóa không dấu cách/chấm (VD: `29A12345`, `30F99988`), khi camera chụp xe vào ứng dụng sẽ tự động nhận diện và khớp chính xác với CSDL.
