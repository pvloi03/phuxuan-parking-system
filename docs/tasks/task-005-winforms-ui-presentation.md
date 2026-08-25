# Task 005: Triển Khai WinForms UI Presentation Layer

## 1. Mục Tiêu
Cài đặt giao diện WinForms hiện đại, trực quan, tối ưu cho màn hình trực ca của bảo vệ (LiveMonitor 2 làn Vào/Ra, Quản lý thiết bị, Tra cứu lịch sử) và thiết lập Composition Root (Dependency Injection) trong `HPParkingSystem.WinForms`.

## 2. Bối Cảnh & Phạm Vi
- **Bối cảnh**: Ứng dụng WinForms chạy trực tiếp tại bốt bảo vệ trên máy x86, hiển thị live stream 4 camera, thông tin nhận diện biển số thời gian thực và trạng thái sức khỏe của toàn bộ thiết bị ngoại vi.
- **Phạm vi**:
  - `Views`:
    - `MainForm.cs`: Khung điều hướng chính.
    - `LiveMonitorView.cs`: Màn hình giám sát 2 làn song song.
    - `DeviceStatusView.cs`: Màn hình cấu hình & kiểm tra sức khỏe thiết bị.
    - `HistoryQueryView.cs`: Màn hình tra cứu & tìm kiếm lịch sử xe qua cổng.
  - `Controls`:
    - `LaneMonitorControl.cs`: UserControl gom nhóm 2 video camera + panel kết quả nhận diện cho 1 làn.
    - `CameraVideoBox.cs`: PictureBox/Panel tối ưu hóa hiển thị khung hình camera mượt mà.
    - `PlateResultCard.cs`: Card hiển thị biển số, độ tin cậy, ảnh crop và thông tin chủ xe.
    - `DeviceStatusIndicator.cs`: Đèn báo trạng thái thiết bị ngoại vi.
  - `Presenters`:
    - `LiveMonitorPresenter.cs`: Lắng nghe sự kiện từ Application Layer, cập nhật dữ liệu lên UI thread (`Invoke/BeginInvoke`).
  - `Program.cs`: Cấu hình `Microsoft.Extensions.DependencyInjection` để đăng ký toàn bộ Services, Repositories, Adapters và Views.

## 3. Chi Tiết Thiết Kế Kỹ Thuật

### 3.1. Composition Root (`Program.cs`)
```csharp
static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        var services = new ServiceCollection();
        ConfigureServices(services);

        using (var serviceProvider = services.BuildServiceProvider())
        {
            var mainForm = serviceProvider.GetRequiredService<MainForm>();
            Application.Run(mainForm);
        }
    }
}
```

### 3.2. Cập nhật UI đa luồng an toàn (Thread-Safe UI Updates)
- Mọi sự kiện từ background thread (SDK callback, Task async) khi cập nhật lên WinForms Controls bắt buộc phải kiểm tra `control.InvokeRequired` hoặc sử dụng `SynchronizationContext.Current`.

## 4. Checklist Tiến Độ
- [ ] Thiết lập Composition Root và Dependency Injection container trong `Program.cs`.
- [ ] Tạo các Custom UserControls (`CameraVideoBox`, `PlateResultCard`, `DeviceStatusIndicator`).
- [ ] Xây dựng `LaneMonitorControl` và ghép nối vào `LiveMonitorView`.
- [ ] Xây dựng `DeviceStatusView` cho phép kiểm tra ping/kết nối lại thiết bị.
- [ ] Xây dựng `HistoryQueryView` cho phép lọc theo ngày và biển số.
- [ ] Cài đặt `LiveMonitorPresenter` kết nối với Use Cases của Application Layer.
- [ ] Kiểm tra hiển thị giao diện, đảm bảo không bị giật lag hay treo UI khi xử lý chụp ảnh và OCR.

## 5. Lưu Ý Kỹ Thuật
- Tối ưu hóa việc vẽ hình ảnh camera (Double Buffered) để tránh hiện tượng nhấp nháy (flickering).
- Hỗ trợ đầy đủ phím tắt nhanh (`F1`, `F2`, `F5`, `Esc`) cho nhân viên bảo vệ.
