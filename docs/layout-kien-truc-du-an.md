# Đặc Tả Layout Kiến Trúc Dự Án — PhuXuanParkingSystem

Tài liệu này đặc tả chi tiết cách bố trí, phân tầng và tổ chức codebase của hệ thống kiểm soát xe ra/vào **PhuXuanParkingSystem** theo nguyên lý **Clean Architecture**, dựa trên [thiet-ke-kien-truc-he-thong-kiem-soat-xe.md](thiet-ke-kien-truc-he-thong-kiem-soat-xe.md).

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
PhuXuanParkingSystem/
├── PhuXuanParkingSystem.sln                        # Solution file tổng
├── PhuXuanParkingSystem.Domain/                    # Project Class Library: Core Domain (Zero Dependencies)
│   └── Models/
│       ├── Common/
│       │   └── BaseEntity.cs                      # Lớp cơ sở (Id ObjectId, CreatedAt, Soft-Delete)
│       ├── Entities/                              # Đối tượng nghiệp vụ có định danh (Id)
│       │   ├── ParkingSession.cs                 # Lượt xe vào-ra (Aggregate Root)
│       │   ├── Vehicle.cs                         # Thông tin xe (Biển số, Loại xe, Chủ sở hữu)
│       │   ├── Person.cs                          # Người (Cán bộ nhân viên, Nhà thầu, Khách)
│       │   ├── Contractor.cs                      # Đơn vị nhà thầu / Công ty liên quan
│       │   ├── Lane.cs                            # Cấu hình làn (Làn Vào / Làn Ra)
│       │   ├── Device.cs                          # Thiết bị ngoại vi (Camera, Controller)
│       │   ├── Department.cs                       # Phòng ban
│       │   ├── Company.cs                         # Công ty / Đơn vị thành viên
│       │   ├── User.cs                            # Tài khoản đăng nhập hệ thống
│       │   └── LicenseInfo.cs                    # Bản quyền phần mềm
│       ├── ValueObjects/                          # Đối tượng giá trị bất biến (Immutable)
│       │   ├── PlateNumber.cs                     # Chuẩn hóa biển số VN (Clean/FormatDisplay)
│       │   └── ImageStoragePath.cs                # Đường dẫn ảnh UNC / Local + BSON Serializer
│       └── Enums/
│           ├── VehicleType.cs                      # Car=1, Motorcycle=2, Truck=3, Other=99
│           ├── ParkingSessionStatus.cs            # Active=1, Completed=2, UnmatchedOut=3, Cancelled=4
│           ├── PersonType.cs                      # Employee, Contractor, Visitor, VIP, Other
│           ├── LaneDirection.cs                   # In, Out, Bidirectional
│           ├── DeviceType.cs                      # PlateCamera=1, OverviewCamera=2, Controller=3
│           ├── DeviceStatus.cs                    # Connected, Disconnected, Error
│           └── UserRole.cs                         # SuperAdmin, Manager, Operator
│
├── PhuXuanParkingSystem.Api/                      # ASP.NET Core Web API
├── PhuXuanParkingSystem.Web/                       # React Frontend
│
├── PhuXuanParkingSystem/                           # WinForms Application (.NET 4.8, x86)
│   ├── SDK/                                       # P/Invoke Native DLLs (x86)
│   │   ├── HikVision/CHCNetSDK.cs               # Hikvision HCNetSDK.dll
│   │   ├── NST/CHISDK.cs                        # NST HISDK.dll
│   │   └── ZKTeco/ZKTecoPullSDK.cs              # ZKTeco plcommpro.dll
│   ├── Services/                                  # Business Logic Services
│   │   ├── Camera/                              # Camera Services (Overview, Plate)
│   │   ├── Controller/                          # ZKTeco Controller Adapter
│   │   ├── Parking/                             # Parking Session Logic
│   │   ├── DeviceHealth/                         # Device Health Monitor
│   │   └── License/                             # License Manager
│   ├── Forms/                                     # WinForms Forms
│   │   ├── FrmMain.cs                           # Main Live Monitor Form
│   │   ├── FrmDeviceMonitor.cs                  # Device Status Monitor
│   │   └── FrmLicenseExpired.cs                 # License Expired Form
│   └── Program.cs                                 # Composition Root (DI Container)
│
├── PhuXuanParkingSystem.LicenseTool/             # WinForms Tool tạo License Key (RSA-3072)
│
└── tests/
    └── PhuXuanParkingSystem.Tests/               # xUnit Tests
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
   - Mọi hàm P/Invoke `[DllImport]` bắt buộc nằm trong thư mục `SDK/<Hãng>/`.
   - Không để lộ bất kỳ kiểu dữ liệu SDK native nào (như `NET_DVR_DEVICEINFO_V30`, `HI_VEDIO_INFO`, struct của ZKTeco) ra ngoài tầng WinForms. Toàn bộ dữ liệu được chuyển đổi sang Domain Entities hoặc Application DTOs trước khi trả về.
