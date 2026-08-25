# Đặc Tả Layout Kiến Trúc Dự Án — HPParkingSystem

Tài liệu này đặc tả chi tiết cách bố trí, phân tầng và tổ chức codebase của hệ thống kiểm soát xe ra/vào **HPParkingSystem** theo nguyên lý **Clean Architecture**, dựa trên [thiet-ke-kien-truc-he-thong-kiem-soat-xe.md](file:///c:/Users/ADMIN/source/repos/HPParkingSystem/docs/thiet-ke-kien-truc-he-thong-kiem-soat-xe.md).

---

## 1. Nguyên Tắc Cốt Lõi (The Dependency Rule)

Tất cả các phụ thuộc mã nguồn chỉ được phép trỏ **từ ngoài vào trong**:

```
[ Presentation / WinForms (x86) ]
               │
               ▼
[ Infrastructure (Adapters: SDKs, Mongo, LPR HTTP) ]
               │
               ▼
[ Application (Use Cases, Ports/Interfaces) ]
               │
               ▼
[ Domain (Entities, Value Objects, Domain Events) ] (Zero Dependencies)
```

- **Domain Layer**: Lõi của hệ thống, hoàn toàn không phụ thuộc vào bất kỳ framework, UI, database hay SDK phần cứng nào.
- **Application Layer**: Điều phối luồng nghiệp vụ qua các Use Cases, định nghĩa các Ports (Interfaces trừu tượng). Chỉ phụ thuộc vào Domain.
- **Infrastructure Layer**: Cài đặt cụ thể các Port interfaces: Giao tiếp SDK C++ của thiết bị (Hikvision, NST, ZKTeco C3-200), HTTP Client gọi LPR Windows Service (x64), kết nối MongoDB và SMB File Storage.
- **Presentation / WinForms Layer**: Giao diện người dùng WinForms (.NET Framework 4.8, x86), quản lý Liveview 2 làn, cấu hình thiết bị và là **Composition Root** (nơi duy nhất cấu hình Dependency Injection).

---

## 2. Bố Cục Thư Mục & Phân Tầng Codebase

```
HPParkingSystem/
├── HPParkingSystem.slnx                            # Solution file tổng
├── AGENTS.md                                       # Quy chuẩn dự án & Clean Architecture rules
├── src/
│   ├── HPParkingSystem.Domain/                     # Project Class Library: Core Domain
│   │   ├── Entities/                               # Đối tượng nghiệp vụ có định danh (Id)
│   │   │   ├── Vehicle.cs                          # Thông tin xe (Biển số, Loại xe, Chủ sở hữu)
│   │   │   ├── Person.cs                           # Người (Cán bộ nhân viên, Nhà thầu, Khách)
│   │   │   ├── Contractor.cs                       # Đơn vị nhà thầu / Công ty liên quan
│   │   │   ├── Lane.cs                             # Cấu hình làn (Làn Vào / Làn Ra)
│   │   │   ├── Device.cs                           # Thiết bị ngoại vi (Camera, Controller, Sensor)
│   │   │   └── AccessEvent.cs                      # Lượt ra/vào (Thời gian, Biển số, Ảnh, Trạng thái)
│   │   ├── ValueObjects/                           # Đối tượng giá trị bất biến (Immutable)
│   │   │   ├── PlateNumber.cs                      # Chuẩn hóa biển số VN (Format, Regex, Type)
│   │   │   ├── ImageStoragePath.cs                 # Đường dẫn ảnh UNC / Local
│   │   │   └── RecognitionResult.cs                # Biển số đọc được + Độ tin cậy (Confidence)
│   │   ├── Enums/                                  # Định nghĩa kiểu liệt kê
│   │   │   ├── LaneDirection.cs                    # In (Vào), Out (Ra)
│   │   │   ├── VehicleType.cs                      # Car4Seats, Car7Seats, Truck, Van, Unknown
│   │   │   ├── DeviceType.cs                       # HikvisionCam, NstCam, ZKTecoController, RadarSensor
│   │   │   ├── DeviceStatus.cs                     # Connected, Disconnected, Error
│   │   │   └── PersonType.cs                       # Employee, ContractorWorker, Visitor, Unknown
│   │   └── Events/                                 # Domain Events
│   │       ├── VehicleArrivedDomainEvent.cs        # Sự kiện cảm biến phát hiện xe
│   │       └── VehicleAccessLoggedDomainEvent.cs   # Sự kiện ghi nhận hoàn tất lượt ra/vào
│   │
│   ├── HPParkingSystem.Application/                # Project Class Library: Use Cases & Ports
│   │   ├── Common/                                 # Cấu trúc dùng chung trong Application
│   │   │   ├── IUseCase.cs                         # Base UseCase interface
│   │   │   ├── Result.cs                           # Result wrapper pattern (Success/Failure)
│   │   │   └── Exceptions/                         # Business / Application Exceptions
│   │   ├── Ports/                                  # Interfaces trừu tượng (Ports)
│   │   │   ├── Hardware/
│   │   │   │   └── IHardwareEventListener.cs       # Lắng nghe sự kiện từ Controller C3-200/Radar
│   │   │   ├── Camera/
│   │   │   │   └── ICameraCapture.cs               # Giao tiếp camera chụp ảnh (Toàn cảnh/Biển số)
│   │   │   ├── Lpr/
│   │   │   │   └── ILicensePlateRecognizer.cs      # Nhận diện biển số từ mảng byte ảnh
│   │   │   └── Persistence/
│   │   │       ├── IAccessEventRepository.cs       # Đọc/ghi lịch sử ra vào
│   │   │       ├── IPersonRepository.cs            # Tra cứu thông tin người/xe
│   │   │       ├── IDeviceRepository.cs            # Quản lý cấu hình thiết bị
│   │   │       └── IImageStore.cs                  # Lưu/tải file ảnh từ File Share SMB
│   │   ├── UseCases/                               # Xử lý nghiệp vụ chính
│   │   │   ├── VehicleAccess/
│   │   │   │   ├── ProcessVehicleEntryUseCase.cs   # Luồng xử lý xe vào (Trigger -> Chụp -> OCR -> Lưu)
│   │   │   │   └── ProcessVehicleExitUseCase.cs    # Luồng xử lý xe ra
│   │   │   ├── DeviceManagement/
│   │   │   │   ├── CheckDeviceStatusUseCase.cs     # Kiểm tra trạng thái kết nối phần cứng
│   │   │   │   └── ReconnectDeviceUseCase.cs       # Thử kết nối lại thiết bị khi mất tín hiệu
│   │   │   └── Query/
│   │   │       ├── GetRecentAccessEventsUseCase.cs # Lấy danh sách sự kiện gần nhất
│   │   │       └── CountDailyVehicleUseCase.cs     # Đếm số lượt xe theo ngày
│   │   └── Models/                                 # DTOs & View Models trung gian
│   │       ├── Requests/                           # Request DTOs
│   │       ├── Responses/                          # Response DTOs
│   │       └── ViewModels/                         # Dữ liệu chuẩn bị cho UI hiển thị
│   │
│   ├── HPParkingSystem.Infrastructure/             # Project Class Library: Adapters & Concrete SDK
│   │   ├── Adapters/
│   │   │   ├── ZKTeco/
│   │   │   │   ├── Native/                         # P/Invoke plcommpro.dll (x86)
│   │   │   │   └── ZKTecoDeviceAdapter.cs          # Cài đặt IHardwareEventListener
│   │   │   ├── Hikvision/
│   │   │   │   ├── Native/                         # P/Invoke HCNetSDK.dll (x86)
│   │   │   │   └── HikvisionCameraAdapter.cs       # Cài đặt ICameraCapture cho Cam toàn cảnh
│   │   │   ├── Nst/
│   │   │   │   ├── Native/                         # P/Invoke NST Camera SDK (x86)
│   │   │   │   └── NstCameraAdapter.cs             # Cài đặt ICameraCapture cho Cam biển số
│   │   │   └── Lpr/
│   │   │       └── LprHttpClientAdapter.cs         # Cài đặt ILicensePlateRecognizer (Gọi LPR x64)
│   │   ├── Persistence/
│   │   │   ├── MongoDb/
│   │   │   │   ├── MongoDbContext.cs               # Quản lý kết nối MongoDB Driver
│   │   │   │   ├── MongoAccessEventRepository.cs   # Implement IAccessEventRepository
│   │   │   │   └── MongoPersonRepository.cs        # Implement IPersonRepository
│   │   │   └── FileStorage/
│   │   │       └── SmbFileShareImageStore.cs       # Implement IImageStore (SMB File Share)
│   │   └── Logging/
│   │       └── FileAppLogger.cs                    # Ghi log ứng dụng hàng ngày
│   │
│   └── HPParkingSystem.WinForms/                   # Project WinForms (.NET 4.8, x86): UI & Composition Root
│       ├── Program.cs                              # Composition Root (Cấu hình ServiceCollection DI)
│       ├── App.config                              # Cấu hình IP MongoDB, LPR Port, Đường dẫn File Share
│       ├── Views/
│       │   ├── MainForm.cs                         # Container chính / Menu điều hướng
│       │   ├── LiveMonitorView.cs                  # Màn hình Liveview 2 làn (Vào/Ra)
│       │   ├── DeviceStatusView.cs                 # Màn hình quản lý trạng thái thiết bị ngoại vi
│       │   └── HistoryQueryView.cs                 # Màn hình tra cứu lịch sử sự kiện
│       ├── Controls/                               # Custom UserControls
│       │   ├── LaneMonitorControl.cs               # Control hiển thị 1 làn (2 video + info panel)
│       │   ├── CameraVideoBox.cs                   # Control render live stream/ảnh từ SDK
│       │   ├── PlateResultCard.cs                  # Thẻ hiển thị biển số, confidence, ảnh crop
│       │   └── DeviceStatusIndicator.cs            # Badge đèn LED trạng thái (Xanh/Đỏ/Vàng)
│       └── Presenters/                             # Presenter / MVVM Controller cho View
│           ├── LiveMonitorPresenter.cs             # Xử lý cập nhật real-time lên LiveMonitorView
│           └── DeviceStatusPresenter.cs            # Cập nhật bảng sức khỏe thiết bị
│
├── docs/                                           # Tài liệu kiến trúc, layout & tasks
│   ├── thiet-ke-kien-truc-he-thong-kiem-soat-xe.md
│   ├── layout-kien-truc-du-an.md                   # File này
│   ├── layout-giao-dien-winforms.md                # Đặc tả Layout UI WinForms
│   └── tasks/                                      # Hệ thống file quản lý task
│
└── SDKDemo/                                        # Demo và header file SDK từ các hãng
```

---

## 3. Quy Ước Ràng Buộc Kỹ Thuật (Build & Architecture Rules)

1. **Ràng buộc Platform x86 (32-bit)**:
   - Tất cả các project (`Domain`, `Application`, `Infrastructure`, `WinForms`) phải được cấu hình `PlatformTarget: x86` hoặc `AnyCPU` với `Prefer32Bit: true`.
   - Lý do: Native DLLs của Hikvision, NST và ZKTeco C3-200 là bản 32-bit.
2. **Không phụ thuộc ngược (No Inverted Dependencies)**:
   - `Domain` không tham chiếu bất kỳ project nào.
   - `Application` chỉ tham chiếu `Domain`.
   - `Infrastructure` tham chiếu `Application` và `Domain`.
   - `WinForms` tham chiếu `Infrastructure`, `Application`, `Domain` (chỉ tại Composition Root trong `Program.cs` để đăng ký DI).
3. **Quy tắc cô lập SDK phần cứng**:
   - Mọi hàm P/Invoke `[DllImport]` bắt buộc nằm trong thư mục `Infrastructure/Adapters/<Hãng>/Native/`.
   - Không để lộ bất kỳ kiểu dữ liệu SDK native nào (như `NET_DVR_DEVICEINFO_V30`, `HI_VEDIO_INFO`, struct của ZKTeco) ra ngoài tầng `Infrastructure`. Toàn bộ dữ liệu được chuyển đổi sang Domain Entities hoặc Application DTOs trước khi trả về.
