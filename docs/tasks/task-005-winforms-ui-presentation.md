# Task 005: Triển Khai WinForms UI Presentation Layer

## 1. Mục Tiêu
Cài đặt giao diện WinForms hiện đại, trực quan, tối ưu cho màn hình trực ca của bảo vệ (LiveMonitor 2 làn Vào/Ra, Quản lý thiết bị, Tra cứu lịch sử) và thiết lập Composition Root (Dependency Injection) trong `PhuXuanParkingSystem`.

## 2. Bối Cảnh & Phạm Vi
- **Bối cảnh**: Ứng dụng WinForms chạy trực tiếp tại bốt bảo vệ trên máy x86, hiển thị live stream 4 camera, thông tin nhận diện biển số thời gian thực và trạng thái sức khỏe của toàn bộ thiết bị ngoại vi.
- **Phạm vi thực tế** (đã triển khai):

## 3. Chi Tiết Thiết Kế Kỹ Thuật

### 3.1. Các Forms đã triển khai

| File | Mô tả | Trạng thái |
|------|--------|------------|
| `Forms/FrmMain.cs` | Main form với 2 làn vào/ra, 4 camera, header/footer status | ✅ Hoàn thành |
| `Forms/FrmMain.Cameras.cs` | Xử lý camera (Hikvision/NST), video panels | ✅ Hoàn thành |
| `Forms/FrmMain.LaneControl.cs` | Điều khiển làn, xử lý sự kiện radar/barrier | ✅ Hoàn thành |
| `Forms/FrmDeviceMonitor.cs` | Giám sát trạng thái thiết bị (TCP/Ping) | ✅ Hoàn thành |
| `Forms/FrmDeviceMonitor.Designer.cs` | Designer cho màn hình giám sát | ✅ Hoàn thành |
| `Forms/LicenseExpiredForm.cs` | Form kích hoạt bản quyền | ✅ Hoàn thành |
| `Forms/LicenseExpiredForm.Designer.cs` | Designer cho form bản quyền | ✅ Hoàn thành |

### 3.2. Giao diện FrmMain (Đã triển khai)

```
┌─────────────────────────────────────────────────────────────────┐
│ HEADER: [HPPARKING] [System Status] [Clock] [Device Monitor]   │
├────────────────────────────┬────────────────────────────────────┤
│     LÀN VÀO (IN-LANE)     │      LÀN RA (OUT-LANE)            │
│  ┌──────────┬──────────┐  │  ┌──────────┬──────────┐          │
│  │ PlateCam │ OverView │  │  │ PlateCam │ OverView │          │
│  │  Video   │  Video   │  │  │  Video   │  Video   │          │
│  ├──────────┴──────────┤  │  ├──────────┴──────────┤          │
│  │   Ảnh chụp biển số  │  │  │   Ảnh chụp biển số  │          │
│  └─────────────────────┘  │  └─────────────────────┘          │
├────────────────────────────┼────────────────────────────────────┤
│   THÔNG TIN XE VÀO       │      THÔNG TIN XE RA              │
│  Biển số: ___  Thời gian │  Biển số: ___  Thời gian         │
│  Chủ xe: ___  Đơn vị: _  │  Chủ xe: ___  Đơn vị: _          │
│  Loại xe: ___  Status: _  │  Loại xe: ___  Status: _         │
├─────────────────────────────────────────────────────────────────┤
│ FOOTER: [Status] [License Days] [Machine Code] [License Info] │
└─────────────────────────────────────────────────────────────────┘
```

### 3.3. Composition Root (`Program.cs`)
```csharp
var services = new ServiceCollection();
ConfigureServices(services);
using (var serviceProvider = services.BuildServiceProvider())
{
    var mainForm = serviceProvider.GetRequiredService<FrmMain>();
    Application.Run(mainForm);
}
```

### 3.4. Thread-Safe UI Updates
- Mọi sự kiện từ background thread (SDK callback, Task async) khi cập nhật lên WinForms Controls sử dụng `InvokeRequired`/`BeginInvoke`.

## 4. Checklist Tiến Độ

- [x] **✅ Composition Root + DI** trong `Program.cs` - Đã đăng ký Services, Repositories, Adapters.
- [x] **✅ FrmMain** - Form chính với layout 2 làn vào/ra, header, footer.
- [x] **✅ Camera Controls** - 4 video panels (Plate + Overview cho mỗi làn), PictureBox tối ưu.
- [x] **✅ PlateResultCard** - Hiển thị biển số, độ tin cậy, ảnh crop trong Info panels.
- [x] **✅ DeviceStatusIndicator** - Đèn báo trạng thái (màu sắc: Xanh/Cam/Đỏ).
- [x] **✅ FrmDeviceMonitor** - Màn hình kiểm tra ping/reconnect thiết bị, F9 shortcut.
- [x] **✅ Thread-Safe UI Updates** - InvokeRequired/BeginInvoke pattern.
- [x] **✅ LicenseExpiredForm** - Form kích hoạt bản quyền với click đúp footer.
- [ ] **🔲 HistoryQueryView** - Màn hình tra cứu lịch sử xe (chưa triển khai).

## 5. Còn Lại

| Thành phần | Priority | Ghi chú |
|------------|----------|---------|
| `HistoryQueryView` | Medium | Tra cứu lịch sử xe qua cổng, lọc theo ngày/biển số |

## 6. Lưu Ý Kỹ Thuật Đã Áp Dụng
- ✅ Double Buffered cho PictureBox để tránh flickering
- ✅ Keyboard shortcuts (F9 mở Device Monitor)
- ✅ Maximized window mode
- ✅ Fixed border style (Single)
- ✅ Thread-safe UI updates với InvokeRequired pattern