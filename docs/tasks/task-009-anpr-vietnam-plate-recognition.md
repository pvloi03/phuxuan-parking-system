# Task 009: Tích Hợp Nhận Diện Biển Số Xe Việt Nam Tốc Độ Cao (SimpleLPR3 x86 Engine)

## 1. Mục Tiêu
Tích hợp động cơ AI/OCR chuyên dụng **SimpleLPR v3.x (32-bit Native x86)** dành cho nhận diện biển số xe cơ giới Việt Nam:
- Hoàn toàn tương thích kiến trúc 32-bit (x86) của toàn bộ hệ thống (`PhuXuanParkingSystem` .NET Framework 4.8 / WinForms).
- Nhận diện biển số tốc độ cao (thời gian xử lý ~30-60ms/ảnh trên CPU), không phụ thuộc GPU hay Python runtime.
- Cấu hình trọng số quốc gia Việt Nam (`VN = 1.0f`) để tối ưu hóa nhận diện biển số 1 dòng (ô tô dài), 2 dòng (xe máy, ô tô vuông), biển trắng/xanh/vàng/đỏ.
- Tự động trích xuất vùng tọa độ biển số (BoundingBox), cắt ảnh crop biển số và chuẩn hóa chuỗi ký tự biển số thông qua Value Object `PlateNumber`.
- Tự động điền kết quả lên màn hình vận hành `FrmMain` (`txtInPlate`, `txtOutPlate`) và tra cứu dữ liệu phương tiện/nhân sự từ MongoDB.

---

## 2. Bối Cảnh & Kiến Trúc Kỹ Thuật
- **Bối cảnh**: Hệ thống bãi xe đang sử dụng camera chụp ảnh trực tiếp từ NST Plate Camera và Hikvision Panoramic Camera. Khi có tín hiệu kích hoạt từ Radar/Vòng từ hoặc bấm phím F1/F2, hệ thống cần tự động nhận dạng biển số từ file ảnh chụp của camera biển số và hiển thị ngay lập tức lên giao diện giám sát.
- **Thành phần & Thư viện**:
  - Managed Wrapper: `SimpleLPR3.dll` (.NET Framework 4.8 x86).
  - Native x86 Dependencies: `SimpleLPR3_native.dll`, `opencv_core971.dll`, `opencv_imgproc971.dll`, `opencv_imgcodecs971.dll`, `kdispatcher.dll`, `tbb.dll`, `tbbmalloc.dll`, `msvcp140*.dll`, `vcruntime140.dll` (đặt trong thư mục `SDK/Native/x86/SimpleLPR3/`).
  - License Key: `SimpleLPR3_key.xml` (bản quyền SimpleLPR chính hãng, nhúng Resource an toàn).
  - Interface: `IPlateRecognitionService` (Application layer port).
  - Implementation: `SimpleLprAnprService` (Infrastructure/Services layer adapter, Thread-Safe, tự động giải phóng bộ nhớ unmanaged).

---

## 3. Checklist Công Việc
- [x] Sao chép và cấu hình bộ thư viện Native x86 SimpleLPR3 vào thư mục `PhuXuanParkingSystem/SDK/Native/x86/SimpleLPR3/`.
- [x] Cập nhật `PhuXuanParkingSystem.csproj` để reference `SimpleLPR3.dll`, nhúng `SimpleLPR3_key.xml` và tự động copy các file DLL native ra thư mục Output `bin/x86/Debug` & `bin/x86/Release`.
- [x] Tạo Interface `IPlateRecognitionService` và Model `PlateRecognitionResult` trong `Services/Anpr/`.
- [x] Xây dựng lớp dịch vụ `SimpleLprAnprService` (khởi tạo Engine, nạp Key bản quyền, xử lý nhận diện, chuẩn hóa và thread-safe).
- [x] Bổ sung hàm định dạng chuẩn `PlateNumber.FormatDisplay(string plate)` trong Value Object `PlateNumber`.
- [x] Đăng ký `IPlateRecognitionService` vào DI Container trong `Program.cs`.
- [x] Tích hợp luồng nhận diện vào `FrmMain.cs` (`CaptureInLaneAsync` và `CaptureOutLaneAsync`):
  - Nhận diện biển số ngầm không block UI.
  - Hiển thị biển số lên `txtInPlate` / `txtOutPlate`.
  - Phát thông báo qua `AppNotificationService` và ghi log Serilog.
  - Tự động truy vấn thông tin chủ xe/đơn vị từ CSDL MongoDB để hiển thị lên màn hình.
- [x] Viết bộ Unit Tests toàn diện trong `tests/PhuXuanParkingSystem.Tests/Services/AnprTests.cs`.
- [x] Chạy kiểm thử tự động `dotnet test` đảm bảo 100% Passed (85/85 tests passed).
- [x] Commit theo chuẩn Conventional Commits và push lên nhánh `task/009-anpr-vietnam-plate-recognition`.

---

## 4. Lưu Ý Kỹ Thuật
- Bộ nhớ Unmanaged: `IProcessor` và `ISimpleLPR` cần được bọc cẩn thận và gọi `Dispose()` khi tắt ứng dụng để tránh rò rỉ RAM (0 unmanaged memory leak).
- Độ trễ luồng UI: Việc nhận diện biển số luôn thực hiện bất đồng bộ (`Task.Run` hoặc `async/await`) để đảm bảo không làm giật khung hình Live Stream của 4 camera.
