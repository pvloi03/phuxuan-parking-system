# PhuXuanParkingSystem - Hệ Thống Quản Lý Bãi Xe Thái Thụy

Hệ thống kiểm soát phương tiện tự động 2 làn độc lập (**1 Làn Vào, 1 Làn Ra, 4 Camera, 1 Controller ZKTeco C3-200 và Cơ sở dữ liệu MongoDB**).

> 📖 **Xem toàn bộ tài liệu kỹ thuật chi tiết tại:** [DOCS.md](file:///c:/Users/ADMIN/source/repos/PhuXuanParkingSystem/DOCS.md)

---

## 🚀 Tính Năng Chính
* **4 Camera Streams**:
  * Làn Vào: Camera Biển Số (NST) + Camera Toàn Cảnh (HikVision).
  * Làn Ra: Camera Biển Số (NST) + Camera Toàn Cảnh (HikVision).
* **Controller ZKTeco C3-200 Dùng Chung**:
  * Tự động nhận diện tín hiệu cảm biến Radar ngõ AUX In 1 (Làn Vào) và AUX In 2 (Làn Ra).
  * Kích hoạt chụp ảnh tự động và tức thời khi xe vào vùng quét.
* **Giao diện Tối giản & Chuyên nghiệp**:
  * Bố cục `TableLayoutPanel` cố định tỉ lệ 50/50, không bị lệch khung hình.
  * Tự động kết nối song song khi khởi động, cảnh báo viền đỏ khi mất tín hiệu camera.
  * Bảng hiển thị thông tin chủ xe và lượt gửi xe ở phía dưới.
* **Cơ sở dữ liệu MongoDB**:
  * Lưu trữ lượt xe vào/ra (`ParkingSessions`), phương tiện (`Vehicles`), chủ xe (`Persons`), phòng ban (`Departments`).
* **Tối ưu 24/7**:
  * Chống khóa file ảnh (Zero File-Lock), chống rò rỉ bộ nhớ (0 GDI / Memory Leak).

---

## 🛠️ Yêu Cầu Môi Trường
* **Hệ điều hành**: Windows 10 / Windows 11 / Windows Server (64-bit hoặc 32-bit).
* **Nền tảng**: .NET Framework 4.8.
* **Nền tảng biên dịch**: `x86` (Bắt buộc cho DLL Native C++ của Hikvision, NST, ZKTeco).
* **Database**: MongoDB Server (Cổng mặc định 27017).

---

## ⌨️ Phím Tắt Vận Hành
* <kbd>Space</kbd> / <kbd>F5</kbd>: Chụp nhanh Làn Vào.
* <kbd>F6</kbd>: Chụp nhanh Làn Ra.
* <kbd>Ctrl</kbd> + <kbd>R</kbd>: Tự động kết nối lại toàn bộ thiết bị.
* <kbd>Ctrl</kbd> + <kbd>O</kbd>: Mở thư mục chứa ảnh chụp (`Captures/`).
