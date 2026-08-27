# TÀI LIỆU KỸ THUẬT HỆ THỐNG QUẢN LÝ BÃI XE THÁI THỤY
**PhuXuanParkingSystem** — Hệ thống Kiểm soát Xe Thông minh (1 Làn Vào - 1 Làn Ra - 4 Camera - 1 Controller - MongoDB)

---

## 📌 MỤC LỤC
1. [Tổng Quan Dự Án](#1-tổng-quan-dự-án)
2. [Kiến Trúc Phần Cứng & Giao Thức Thiết Bị](#2-kiến-trúc-phần-cứng--giao-thức-thiết-bị)
3. [Cấu Trúc Thư Mục & Mã Nguồn](#3-cấu-trúc-thư-mục--mã-nguồn)
4. [Mô Hình Dữ Liệu & MongoDB Driver (Models)](#4-mô-hình-dữ-liệu--mongodb-driver-models)
5. [Thiết Kế Giao Diện (UI/UX) & Luồng Vận Hành](#5-thiết-kế-giao-diện-uiux--luồng-vận-hành)
6. [Tối Ưu Hóa Hiệu Năng & Quản Lý Bộ Nhớ (24/7)](#6-tối-ưu-hóa-hiệu-năng--quản-lý-bộ-nhớ-247)
7. [Cấu Hình Hệ Thống (`App.config`)](#7-cấu-hình-hệ-thống-appconfig)
8. [Hệ Thống Bản Quyền (License System)](#8-hệ-thống-bản-quyền-license-system)

---

## 1. TỔNG QUAN DỰ ÁN

Hệ thống **PhuXuanParkingSystem** là phần mềm quản lý kiểm soát phương tiện ra vào bãi đỗ xe tự động, được xây dựng trên nền tảng **.NET Framework 4.8 / C# WinForms (x86)** kết hợp **MongoDB**.

### Các đặc điểm cốt lõi:
* **Mô hình 2 làn độc lập**: 1 Làn Vào (In-Lane) và 1 Làn Ra (Out-Lane).
* **Hệ thống 4 Camera**: Mỗi làn gồm 1 Camera chụp biển số (NST SDK) + 1 Camera chụp toàn cảnh (Hikvision SDK).
* **Dùng chung 1 Bộ điều khiển (Controller ZKTeco C3-200)**: Tự động phân luồng tín hiệu từ 2 Cảm biến Radar (AUX In #1 cho Làn Vào, AUX In #2 cho Làn Ra).
* **Vận hành tự động hóa**: Khởi động tự động kết nối song song, phát hiện xe bằng Radar, chụp ảnh đồng thời, lưu trữ CSDL MongoDB và hiển thị thông tin chủ xe trực quan.

```
                              BỘ ĐIỀU KHIỂN ZKTECO C3-200
                                  (192.168.1.202:4370)
                                      /          \
                      Cổng AUX #1 (Vào)          Cổng AUX #2 (Ra)
                             │                          │
       ┌─────────────────────┴──────┐            ┌──────┴─────────────────────┐
       │     🔵 LÀN VÀO (IN-LANE)   │            │     🔴 LÀN RA (OUT-LANE)   │
       ├────────────────────────────┤            ├────────────────────────────┤
       │ 1. Cam Biển số Vào (NST)   │            │ 1. Cam Biển số Ra (NST)    │
       │ 2. Cam Toàn cảnh Vào (HIK) │            │ 2. Cam Toàn cảnh Ra (HIK)  │
       │ 3. Radar kích hoạt (221):  │            │ 3. Radar kích hoạt (221):  │
       │    👉 Chụp tự động LÀN VÀO │            │    👉 Chụp tự động LÀN RA  │
       └────────────────────────────┘            └────────────────────────────┘
```

---

## 2. KIẾN TRÚC PHẦN CỨNG & GIAO THỨC THIẾT BỊ

### 2.1. DeviceType Enum (Phân loại thiết bị)
```csharp
public enum DeviceType
{
    PlateCamera = 1,     // Camera chụp ảnh nhận diện biển số xe (ANPR/LPR)
    OverviewCamera = 2,  // Camera chụp ảnh toàn cảnh làn xe
    Controller = 3,      // Bộ điều khiển Barrier & Cảm biến Radar
}
```

### 2.2. Camera Nhận Diện Biển Số (NST SDK)
* **Thư viện Native**: `HISDK.dll`, `HIPlayer.dll`, `NetLib.dll`, `avcodec-54.dll`, `avutil-51.dll` (Build x86).
* **P/Invoke Wrapper**: `PhuXuanParkingSystem.SDK.NST.CHISDK`.
* **Cổng mặc định**: `3000` hoặc `80` qua TCP.
* **DeviceType**: `PlateCamera`
* **Cơ chế**:
  * Live Stream: `HI_SDK_RealPlayExt` gán trực tiếp lên Handle của Panel.
  * Chụp ảnh: `CaptureToFileAsync` ghi trực tiếp file ảnh JPEG chuẩn HD từ SDK.

### 2.3. Camera Chụp Toàn Cảnh (HikVision SDK)
* **Thư viện Native**: `HCNetSDK.dll`, `HCCore.dll`, `PlayCtrl.dll`, `HCNetSDKCom/` (Build x86).
* **P/Invoke Wrapper**: `CHCNetSDK_Library.CHCNetSDK`.
* **Cổng mặc định**: `8000` qua TCP.
* **DeviceType**: `OverviewCamera`
* **Cơ chế**:
  * Live Stream: `NET_DVR_RealPlay_V40` gán trực tiếp lên Handle của Panel.
  * Chụp ảnh: `NET_DVR_CaptureJPEGPicture_NEW` kết hợp Stream bất đồng bộ.

### 2.4. Bộ điều khiển Cảm biến Radar (ZKTeco C3-200 Pull SDK)
* **Thư viện Native**: `plcommpro.dll`, `plcomms.dll`, `pltcpcomm.dll`, `plcommutils.dll` (Build x86).
* **P/Invoke Wrapper**: `PhuXuanParkingSystem.SDK.ZKTeco.ZKTecoPullSDK`.
* **Cổng kết nối**: `4370` TCP.
* **DeviceType**: `Controller`
* **Cơ chế đọc log thời gian thực**:
  * Vòng lặp ngầm `ListenLoopAsync` đọc hàm `GetRTLog` liên tục.
  * Phân tích chuỗi CSV: `Time,Pin,CardNo,DoorID,EventType,InOutState,VerifyMode`.
  * **DoorID (Index 3)**: `1` = Làn Vào, `2` = Làn Ra.
  * **EventType (Index 4)**:
    * `221`: **🟢 CÓ XE** (Radar phát hiện xe vào vùng quét -> Kích hoạt chụp ảnh tự động).
    * `220`: **⚪ HẾT XE** (Xe đã di chuyển qua khỏi vùng quét).

---

## 3. CẤU TRÚC THƯ MỤC & MÃ NGUỒN

```
PhuXuanParkingSystem/
├── PhuXuanParkingSystem.Domain/          # Class Library: Domain Layer (Zero Dependencies)
│   └── Models/
│       ├── Common/
│       │   └── BaseEntity.cs            # Lớp cơ sở (Id ObjectId, CreatedAt, Soft-Delete)
│       ├── Enums/
│       │   ├── VehicleType.cs           # Car=1, Motorcycle=2, Truck=3, Other=99
│       │   ├── ParkingSessionStatus.cs  # Active=1, Completed=2, UnmatchedOut=3, Cancelled=4
│       │   ├── PersonType.cs            # Employee, Contractor, Visitor, VIP, Other
│       │   ├── LaneDirection.cs         # In, Out, Bidirectional
│       │   ├── DeviceType.cs            # PlateCamera=1, OverviewCamera=2, Controller=3
│       │   ├── DeviceStatus.cs          # Connected, Disconnected, Error
│       │   └── UserRole.cs              # SuperAdmin, Manager, Operator
│       ├── ValueObjects/
│       │   ├── PlateNumber.cs           # Chuẩn hóa biển số VN (Clean/FormatDisplay)
│       │   └── ImageStoragePath.cs      # Đường dẫn ảnh UNC + BSON Serializer
│       └── Entities/
│           ├── ParkingSession.cs        # Aggregate Root: Lượt xe vào-ra
│           ├── Vehicle.cs                # Danh mục xe đăng ký
│           ├── Person.cs                 # Người dùng/hành khách
│           ├── Lane.cs                   # Làn kiểm soát (In/Out)
│           ├── Device.cs                # Thiết bị phần cứng (Camera, Controller)
│           ├── Department.cs            # Phòng ban
│           ├── Company.cs               # Công ty/đơn vị thành viên
│           ├── Contractor.cs            # Đơn vị nhà thầu/đối tác
│           ├── User.cs                  # Tài khoản đăng nhận hệ thống
│           └── LicenseInfo.cs          # Bản quyền phần mềm
│
├── PhuXuanParkingSystem.Api/            # Web API (ASP.NET Core)
├── PhuXuanParkingSystem.Web/             # Web Frontend (React)
│
├── PhuXuanParkingSystem/                # WinForms Application (.NET 4.8, x86)
│   ├── SDK/                             # Tầng giao tiếp Driver Native C++
│   │   ├── HikVision/CHCNetSDK.cs       # P/Invoke Hikvision SDK
│   │   ├── NST/CHISDK.cs                # P/Invoke NST SDK
│   │   └── ZKTeco/ZKTecoPullSDK.cs      # P/Invoke ZKTeco Pull SDK
│   ├── Services/                        # Tầng dịch vụ logic thiết bị
│   │   ├── Camera/
│   │   │   ├── CameraConfig.cs         # Cấu hình IP, Port, User, Pass
│   │   │   ├── OverviewCameraService.cs # Service Camera Toàn Cảnh (Hikvision)
│   │   │   └── PlateCameraService.cs    # Service Camera Biển Số (NST)
│   │   └── Controller/
│       ├── AuxTriggerEventArgs.cs        # Model sự kiện Radar (AuxPort, IsActive, Time)
│       └── ZKTecoDeviceAdapter.cs        # Adapter kết nối & đọc log Controller
├── FrmMain.cs                            # Form giao diện chính & Xử lý nghiệp vụ
├── FrmMain.Designer.cs                   # Thiết kế bố cục giao diện WinForms
├── App.config                            # File cấu hình tập trung IP, Port, DB
└── PhuXuanParkingSystem.csproj              # File dự án SDK-style (net48, x86, MongoDB.Driver)
```

---

## 4. MÔ HÌNH DỮ LIỆU & MONGODB DRIVER (MODELS)

Dự án sử dụng thư viện chính thức **`MongoDB.Driver 2.28.0`**:

### 4.1. Entity Lượt Gửi Xe (`ParkingSession.cs`) — Aggregate Root
```csharp
[BsonIgnoreExtraElements]
public class ParkingSession : BaseEntity
{
    // === THÔNG TIN PHƯƠNG TIỆN ===
    public string PlateNumber { get; set; }                          // Biển số (chuẩn hóa)
    public VehicleType VehicleType { get; set; }                      // Car, Motorcycle, Truck, Other
    public ParkingSessionStatus Status { get; set; }                  // Active, Completed, UnmatchedOut, Cancelled

    // === ĐỊNH DANH CHỦ XE ===
    public string? PersonId { get; set; }                             // ID chủ xe (nullable)
    public string? PersonName { get; set; }                          // Tên chủ xe
    public string? CompanyName { get; set; }                         // Tên công ty
    public string? DepartmentName { get; set; }                      // Tên phòng ban
    public PersonType? PersonType { get; set; }                     // Employee, Contractor, Visitor, VIP

    // === LƯỢT VÀO ===
    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime? InTime { get; set; }                           // Thời gian vào
    public string? InLaneName { get; set; }                         // Tên làn vào
    public ImageStoragePath InOverviewImagePath { get; set; }        // Ảnh toàn cảnh vào (Value Object)
    public ImageStoragePath InPlateImagePath { get; set; }          // Ảnh biển số vào (Value Object)

    // === LƯỢT RA ===
    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime? OutTime { get; set; }                           // Thời gian ra
    public string? OutLaneName { get; set; }                        // Tên làn ra
    public ImageStoragePath OutOverviewImagePath { get; set; }       // Ảnh toàn cảnh ra (Value Object)
    public ImageStoragePath OutPlateImagePath { get; set; }         // Ảnh biển số ra (Value Object)

    // === COMPUTED GETTERS ===
    [BsonIgnore]
    public bool IsUnknown => string.IsNullOrWhiteSpace(PersonName);

    [BsonIgnore]
    public TimeSpan? Duration => (InTime.HasValue && OutTime.HasValue && OutTime >= InTime)
        ? OutTime.Value - InTime.Value
        : null;

    // === FACTORY METHODS ===
    public static ParkingSession CheckIn(...)  // Tạo phiên xe vào
    public static ParkingSession CreateUnmatchedOut(...) // Xe ra không có lượt vào
    public void CheckOut(...)                  // Hoàn thành phiên khi xe ra
}
```

### 4.2. Entity Thiết Bị (`Device.cs`)
```csharp
[BsonIgnoreExtraElements]
public class Device : BaseEntity
{
    public string Code { get; set; }                       // Mã thiết bị (CAM-01, CTR-01...)
    public string Name { get; set; }                       // Tên mô tả
    public DeviceType Type { get; set; }                   // PlateCamera=1, OverviewCamera=2, Controller=3

    // === CẤU HÌNH MẠNG ===
    public string IpAddress { get; set; }                   // IP thiết bị
    public int Port { get; set; } = 8000;                  // Port (Hik:8000, NST:3000, ZKTeco:4370)
    public string? UserName { get; set; }                 // Tên đăng nhập
    public string? Password { get; set; }                 // Mật khẩu

    // === TRẠNG THÁI SỨC KHỎE ===
    public DeviceStatus Status { get; set; }               // Connected, Disconnected, Error
    public DateTime? LastHeartbeat { get; set; }           // Heartbeat gần nhất
    public string? ErrorMessage { get; set; }              // Chi tiết lỗi

    // === BEHAVIOR METHODS ===
    public void MarkConnected()
    public void MarkError(string errorMessage)
    public void MarkDisconnected()
}
```

### 4.3. Entity Làn (`Lane.cs`)
```csharp
[BsonIgnoreExtraElements]
public class Lane : BaseEntity
{
    public string Code { get; set; }                       // Mã làn (L01, L02...)
    public string Name { get; set; }                       // Tên làn
    public LaneDirection Direction { get; set; }           // In, Out, Bidirectional
    public bool IsActive { get; set; } = true;             // Trạng thái hoạt động

    // === THAM CHIẾU THIẾT BỊ ===
    public string? OverviewCameraDeviceId { get; set; }    // ID Camera toàn cảnh
    public string? PlateCameraDeviceId { get; set; }      // ID Camera biển số
    public string? ControllerDeviceId { get; set; }       // ID Controller
    public int TriggerAuxPort { get; set; } = 1;          // Cổng Aux (1=Vào, 2=Ra)

    // === NAVIGATION (Runtime, not persisted) ===
    [BsonIgnore]
    public Device? OverviewCamera { get; set; }
    [BsonIgnore]
    public Device? PlateCamera { get; set; }
    [BsonIgnore]
    public Device? Controller { get; set; }
}
```

### 4.4. Entity Bản Quyền (`LicenseInfo.cs`)
```csharp
[BsonIgnoreExtraElements]
public class LicenseInfo : BaseEntity
{
    public string CustomerName { get; set; }              // Tên khách hàng
    public string MachineCode { get; set; }              // Hardware Fingerprint
    public DateTime ExpiryDate { get; set; }             // Ngày hết hạn
    public DateTime IssuedAt { get; set; }              // Ngày cấp
    public string LicenseKey { get; set; }               // RSA-3072 signed key
    public string? Signature { get; set; }               // Digital signature
    public bool IsActive { get; set; } = true;           // Trạng thái hiệu lực

    // === QUOTA LIMITS ===
    public int MaxLanes { get; set; } = 2;              // Số làn tối đa
    public int MaxCameras { get; set; } = 4;            // Số camera tối đa
    public int MaxControllers { get; set; } = 1;        // Số controller tối đa
    public List<string> Features { get; set; }           // Tính năng: ANPR_Vietnam, AutoBarrier...

    // === COMPUTED GETTERS ===
    [BsonIgnore]
    public bool IsPermanent => ExpiryDate.Year >= 2099;

    [BsonIgnore]
    public bool IsExpired => !IsPermanent && DateTime.Now > ExpiryDate;

    [BsonIgnore]
    public int DaysRemaining
    {
        get
        {
            if (IsPermanent) return 99999;
            if (IsExpired) return 0;
            return (int)Math.Ceiling((ExpiryDate - DateTime.Now).TotalDays);
        }
    }

    [BsonIgnore]
    public bool IsValid => IsActive && !IsDeleted && !IsExpired && !string.IsNullOrWhiteSpace(LicenseKey);
}
```

### 4.5. Database Context (`MongoDbContext.cs`)
* Tự động khởi tạo kết nối Singleton: `MongoDbContext.Instance`.
* Cung cấp các Collection: `ParkingSessions`, `Vehicles`, `Persons`, `Departments`, `Companies`, `Lanes`, `Devices`, `Users`, `LicenseInfos`.
* Tự động khởi tạo **Index tìm kiếm siêu tốc** trên trường `PlateNumber`, `InTime`, `Status`.

---

## 5. THIẾT KẾ GIAO DIỆN (UI/UX) & LUỒNG VẬN HÀNH

### 5.1. Bố cục Lưới Chuẩn (`TableLayoutPanel` - Không dùng Splitter)
* **Khung cố định tỉ lệ**: Không có thanh trượt co kéo gây lệch khung hình của nhân viên trực làn.
* **Chia đôi màn hình (50% / 50%)**:
  * **Cột trái**: Làn Vào (Khung Stream Biển số + Toàn cảnh + Ảnh chụp + Thông tin xe vào).
  * **Cột phải**: Làn Ra (Khung Stream Biển số + Toàn cảnh + Ảnh chụp + Thông tin xe ra).
* **Phía dưới (30%)**: 2 Bảng Thông Tin Phương Tiện (Biển số xe chữ lớn, Thời gian, Chủ xe, Đơn vị, Loại xe, Trạng thái).

### 5.2. Chế độ Màn hình & Cố định Khung
* `WindowState = FormWindowState.Maximized`: Luôn khởi động ở kích thước tối đa màn hình.
* `FormBorderStyle = FixedSingle`: Vô hiệu hóa kéo mép thay đổi kích thước, giữ nguyên 2 nút Phóng to / Thu nhỏ / Ẩn Taskbar.

### 5.3. 3 Trạng thái Hiển thị của Khung Camera
1. **🔄 Đang kết nối (`Connecting`)**: Nền xám chì, viền xanh dương 2px, chữ `🔄 ĐANG KẾT NỐI... (IP)`.
2. **🟢 Đã kết nối (`Connected`)**: Mở luồng video trực tiếp từ camera, viền mỏng trung tính.
3. **🔴 Lỗi kết nối (`Failed`)**: Viền đỏ 3px (`#DC3545`), chữ cảnh báo `❌ KHÔNG KẾT NỐI ĐƯỢC (IP)`.

### 5.4. Phím tắt Vận hành Nhanh
* <kbd>Space</kbd> / <kbd>F5</kbd>: Chụp thủ công Làn Vào.
* <kbd>F6</kbd>: Chụp thủ công Làn Ra.
* <kbd>Ctrl</kbd> + <kbd>R</kbd>: Kết nối lại toàn bộ thiết bị.
* <kbd>Ctrl</kbd> + <kbd>O</kbd>: Mở thư mục chứa ảnh chụp (`Captures/`).

---

## 6. TỐI ƯU HÓA HIỆU NĂNG & QUẢN LÝ BỘ NHỚ (24/7)

Hệ thống được thiết kế đặc thù cho trạm bãi xe vận hành liên tục 24/7 với hàng ngàn lượt xe/ngày:

1. **Kết nối 100% Bất đồng bộ & Song song (`Task.WhenAll`)**:
   * Khi mở phần mềm, 4 Camera và Controller C3-200 được kết nối đồng thời trên các Background Thread.
   * Giao diện **không bao giờ bị đơ "Not Responding"** dù có camera bị rút dây mạng hay timeout.
2. **Chụp ảnh Native tốc độ cao (`CaptureToFileAsync`)**:
   * Đẩy thẳng tác vụ nén JPEG xuống DLL Native C/C++ của Camera, không qua trung gian Bitmap GDI+, giảm tải CPU tối đa.
3. **Chống Khóa File (Zero File-Lock)**:
   * Nạp ảnh vào PictureBox thông qua mảng byte `MemoryStream`, đóng stream ngay sau khi nạp để file ảnh trên ổ cứng không bao giờ bị khóa tiến trình.
4. **Giải phóng GDI+ Handle triệt để**:
   * Tự động gọi `Dispose()` cho ảnh cũ trước khi gán ảnh mới, tránh chạm trần giới hạn 10.000 GDI Handles của Windows.
5. **GDI Object Caching**:
   * Toàn bộ Font, Pen, Brush trong sự kiện `Paint` được Cache sẵn, không cấp phát bộ nhớ mới trong từng frame vẽ.

---

## 7. CẤU HÌNH HỆ THỐNG (`App.config`)

Toàn bộ thông số thiết bị và cơ sở dữ liệu được quản lý tập trung trong file `App.config`:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
    <startup>
        <supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8" />
    </startup>
    <appSettings>
        <!-- ================= LƯU TRỮ & DATABASE ================= -->
        <add key="CaptureSavePath" value="Captures" />
        <add key="MongoDb_ConnectionString" value="mongodb://localhost:27017" />
        <add key="MongoDb_DatabaseName" value="PhuXuanParkingSystemDb" />
    </appSettings>
</configuration>
```

> ⚠️ **Lưu ý quan trọng**: Cấu hình Camera và Controller đã được chuyển sang đọc từ **MongoDB** (xem Mục 8). `App.config` chỉ còn chứa cấu hình lưu trữ và database.

---

## 8. NẠP CẤU HÌNH ĐỘNG TỪ MONGODB (Task-020, Task-021)

### 8.1. Tổng Quan

Từ **Task-020** và **Task-021**, hệ thống WinForms đã được nâng cấp để đọc cấu hình thiết bị trực tiếp từ MongoDB thay vì hardcode trong `App.config`.

### 8.2. Luồng Nạp Cấu Hình

```
┌─────────────────────────────────────────────────────────────────┐
│                 LoadConfigurationsFromDbAsync()                   │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ 1. Query Lanes (IsActive == true && !IsDeleted)          │ │
│  │ 2. Filter: Direction == LaneDirection.In  → InLane         │ │
│  │ 3. Filter: Direction == LaneDirection.Out → OutLane        │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                              │                                    │
│          ┌───────────────────┴───────────────────┐                │
│          ▼                                       ▼                │
│  ┌─────────────────────┐               ┌─────────────────────┐   │
│  │    InLane Config    │               │   OutLane Config    │   │
│  │  - PlateCameraId   │               │  - PlateCameraId   │   │
│  │  - OverviewCameraId│               │  - OverviewCameraId│   │
│  │  - ControllerId    │               │  - ControllerId    │   │
│  └─────────────────────┘               └─────────────────────┘   │
│                              │                                    │
│                              ▼                                    │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │  _deviceRepo.GetByIdAsync(deviceId)                         │ │
│  │  → Resolve: IP, Port, Username, Password, RTSPUrl           │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### 8.3. Cơ Chế Fallback An Toàn

1. **Ưu tiên**: Lấy từ `Lane.PlateCameraDeviceId` (MongoDB)
2. **Fallback 1**: Tìm theo `DeviceType` + `LaneId`
3. **Fallback cuối cùng**: `App.config` (legacy - đang loại bỏ dần)

### 8.4. Giám Sát Thiết Bị (Task-020)

**Màn hình `FrmDeviceMonitor`** (`Forms/FrmDeviceMonitor.cs`):

- Thẻ thống kê: Tổng thiết bị, Đang kết nối, Mất kết nối
- Danh sách thiết bị với đèn LED trạng thái (Xanh/Đỏ/Vàng)
- Đo độ trễ (ms), thời gian phản hồi cuối
- Kiểm tra ngay, kiểm tra từng thiết bị, tự động quét theo chu kỳ (10s/30s/1m)
- Phím tắt **F9** để mở nhanh từ `FrmMain`

**Service `DeviceHealthMonitorService`** (`Services/DeviceHealth/DeviceHealthMonitorService.cs`):
- Kiểm tra song song TCP Port connect & ICMP Ping
- Cập nhật `DeviceStatus` và `LastHeartbeat` vào MongoDB
- Đồng bộ real-time với Web Admin `DevicesPage`

### 8.5. Các File Chính

| File | Mô tả |
|------|-------|
| `FrmMain.cs` | Constructor inject IRepository<Lane>, IRepository<Device> |
| `FrmMain.Cameras.cs` | LoadConfigurationsFromDbAsync() |
| `FrmDeviceMonitor.cs` | Màn hình giám sát thiết bị |
| `DeviceHealthMonitorService.cs` | Service kiểm tra & đồng bộ trạng thái |
| `Program.cs` | Đăng ký DI container |

---

## 9. HỆ THỐNG BẢN QUYỀN (LICENSE SYSTEM) — TASK-022

### 9.1. Tổng Quan

Hệ thống bản quyền sử dụng **chữ ký số RSA 3072-bit** kết hợp **Hardware Fingerprint** để bảo vệ phần mềm khỏi vi phạm bản quyền.

### 9.2. Kiến Trúc Bản Quyền

```
┌─────────────────────────────────────────────────────────────────┐
│                    LICENSE SYSTEM ARCHITECTURE                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────┐    ┌─────────────────┐    ┌──────────────┐ │
│  │ LicenseTool.exe │───▶│   LicenseInfo   │◀───│ LicenseCrypto│ │
│  │ (WinForms App)  │    │ (License Key)   │    │ (RSA-3072)   │ │
│  └─────────────────┘    └─────────────────┘    └──────────────┘ │
│           │                     │                     │          │
│           ▼                     ▼                     ▼          │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │                 HardwareFingerprint (WMI)                    │ │
│  │  • CPU Processor ID    • Motherboard Serial                 │ │
│  │  • Disk Serial Number  • BIOS Serial                        │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                                                                  │
│  ┌─────────────────┐    ┌─────────────────┐                     │
│  │ WinForms Client │◀───│ LicenseManager  │                     │
│  │ (FrmMain)       │    │ (Validation)    │                     │
│  └─────────────────┘    └─────────────────┘                     │
│                                                                  │
│  ┌─────────────────┐    ┌─────────────────┐                     │
│  │ Web Admin       │◀───│ LicenseController│                    │
│  │ (LicensePage)   │    │ (API)            │                     │
│  └─────────────────┘    └─────────────────┘                     │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 9.3. License Info Entity (Đã định nghĩa ở Mục 4.4)

### 9.4. Quota Limits (Giới Hạn Bản Quyền)

| Quota | Giá trị mặc định | Mô tả |
|-------|------------------|-------|
| `MaxLanes` | 2 | Số làn xe tối đa (1 vào, 1 ra) |
| `MaxCameras` | 4 | Số camera tối đa (2 biển số + 2 toàn cảnh) |
| `MaxControllers` | 1 | Số bộ điều khiển tối đa |
| `Features` | ANPR_Vietnam, AutoBarrier, DualCameraPerLane | Danh sách tính năng |

### 9.5. Validation Rules

```csharp
public bool IsValid => IsActive && !IsDeleted && !IsExpired && !string.IsNullOrWhiteSpace(LicenseKey);

// Kiểm tra quota trong API:
- LanesController: Kiểm tra MaxLanes
- DevicesController: Kiểm tra MaxCameras, MaxControllers
```

### 9.6. Các File Chính

| File | Mô tả |
|------|-------|
| `LicenseInfo.cs` | Entity bản quyền |
| `LicenseCrypto.cs` | RSA-3072 sign/verify |
| `HardwareFingerprint.cs` | WMI hardware fingerprint |
| `LicenseManager.cs` | WinForms client validation |
| `LicenseController.cs` | Web API endpoints |
| `LicensePage.tsx` | Web Admin UI |
| `LicenseTool/` | WinForms tool tạo license key |

### 9.7. License Status Colors (Footer Label)

| Days Remaining | Màu sắc | Trạng thái |
|---------------|---------|------------|
| > 15 ngày | 🟢 Xanh lá | Bình thường |
| ≤ 15 ngày | 🟡 Cam | Sắp hết hạn |
| ≤ 0 ngày | 🔴 Đỏ | Hết hạn → Hiển thị `LicenseExpiredForm` |

---
*Tài liệu được biên soạn và cập nhật tự động cho dự án PhuXuanParkingSystem.*
