# Task 020: Màn Hình WinForms Giám Sát Tình Trạng Thiết Bị & Đồng Bộ Lên Web Admin

## 1. Mục Tiêu
Xây dựng màn hình WinForms chuyên dụng (`FrmDeviceMonitor`) và dịch vụ giám sát thiết bị (`DeviceHealthMonitorService`) để:
- Kiểm tra trực tiếp tình trạng kết nối TCP/IP, ICMP và đo độ trễ (ms) của Camera Làn Vào/Ra và Bộ điều khiển Barrier C3-200.
- Tự động đồng bộ trạng thái (`Connected`, `Disconnected`, `Error`), thời gian kiểm tra (`LastHeartbeat`) và độ trễ vào MongoDB collection `Devices`.
- Cho phép Web Admin (`DevicesPage`) hiển thị trạng thái thiết bị thời gian thực được đồng bộ từ trạm WinForms.

## 2. Kiến Trúc & Phạm Vi Thay Đổi

### 2.1. Dịch Vụ Giám Sát (`Services/DeviceHealth`)
- `IDeviceHealthMonitorService.cs`: Interface chuẩn Clean Architecture.
- `DeviceHealthMonitorService.cs`:
  - Kiểm tra song song (`Task.WhenAll`) trạng thái mạng qua Socket TCP Port connect & ICMP Ping.
  - Cập nhật dữ liệu vào MongoDB collection `Devices`.

### 2.2. Giao Diện WinForms (`Forms/FrmDeviceMonitor`)
- `FrmDeviceMonitor.cs` & `FrmDeviceMonitor.Designer.cs`:
  - Thẻ thống kê: Tổng thiết bị, Đang kết nối, Mất kết nối.
  - Danh sách thiết bị với đèn LED trạng thái (Xanh/Đỏ/Vàng), IP:Port, Độ trễ (ms), Lần phản hồi cuối.
  - Chức năng: Kiểm tra ngay tất cả, Kiểm tra từng thiết bị, Tự động quét theo chu kỳ (10s, 30s, 1m).

### 2.3. Tích Hợp Vào `FrmMain`
- Nút / menu "Giám Sát Thiết Bị (F9)" trên thanh công cụ của `FrmMain`.
- Phím tắt `F9` để mở nhanh màn hình.
- Chạy ngầm định kỳ để cập nhật trạng thái thiết bị lên Web Admin 24/7.

## 3. Checklist Tiến Độ
- [ ] Tạo branch git `task/020-device-status-monitor-winform-sync`
- [ ] Tạo `IDeviceHealthMonitorService.cs` và `DeviceHealthMonitorService.cs`
- [ ] Tạo WinForms `FrmDeviceMonitor.cs` & `FrmDeviceMonitor.Designer.cs`
- [ ] Tích hợp phím tắt F9 và nút mở màn hình trên `FrmMain`
- [ ] Thêm Background Worker tự động đồng bộ trạng thái định kỳ lên MongoDB
- [ ] Kiểm thử biên dịch và chạy thử nghiệm
- [ ] Kiểm thử hiển thị trên Web Admin `DevicesPage`
