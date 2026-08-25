# TÀI LIỆU KỸ THUẬT HỆ THỐNG QUẢN LÝ BÃI XE THÁI THỤY
**Hệ thống Kiểm soát Xe Thông minh (1 Làn Vào - 1 Làn Ra - 4 Camera - 1 Controller - MongoDB)**

---

## 📌 MỤC LỤC
1. [Tổng Quan Dự Án](#1-tổng-quan-dự-án)
2. [Kiến Trúc Phần Cứng & Giao Thức Thiết Bị](#2-kiến-trúc-phần-cứng--giao-thức-thiết-bị)
3. [Cấu Trúc Thư Mục & Mã Nguồn](#3-cấu-trúc-thư-mục--mã-nguồn)
4. [Mô Hình Dữ Liệu & MongoDB Driver (Models)](#4-mô-hình-dữ-liệu--mongodb-driver-models)
5. [Thiết Kế Giao Diện (UI/UX) & Luồng Vận Hành](#5-thiết-kế-giao-diện-uiux--luồng-vận-hành)
6. [Tối Ưu Hóa Hiệu Năng & Quản Lý Bộ Nhớ (24/7)](#6-tối-ưu-hóa-hiệu-năng--quản-lý-bộ-nhớ-247)
7. [Cấu Hình Hệ Thống (`App.config`)](#7-cấu-hình-hệ-thống-appconfig)

---

## 1. TỔNG QUAN DỰ ÁN

Hệ thống **HPParkingThaiThuy** là phần mềm quản lý kiểm soát phương tiện ra vào bãi đỗ xe tự động, được xây dựng trên nền tảng **.NET Framework 4.8 / C# WinForms (x86)** kết hợp **MongoDB**.

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

### 2.1. Camera Nhận Diện Biển Số (NST SDK)
* **Thư viện Native**: `HISDK.dll`, `HIPlayer.dll`, `NetLib.dll`, `avcodec-54.dll`, `avutil-51.dll` (Build x86).
* **P/Invoke Wrapper**: `HPParkingThaiThuy.SDK.NST.CHISDK`.
* **Cổng mặc định**: `3000` hoặc `80` qua TCP.
* **Cơ chế**:
  * Live Stream: `HI_SDK_RealPlayExt` gán trực tiếp lên Handle của Panel.
  * Chụp ảnh: `CaptureToFileAsync` ghi trực tiếp file ảnh JPEG chuẩn HD từ SDK.

### 2.2. Camera Chụp Toàn Cảnh (HikVision SDK)
* **Thư viện Native**: `HCNetSDK.dll`, `HCCore.dll`, `PlayCtrl.dll`, `HCNetSDKCom/` (Build x86).
* **P/Invoke Wrapper**: `CHCNetSDK_Library.CHCNetSDK`.
* **Cổng mặc định**: `8000` qua TCP.
* **Cơ chế**:
  * Live Stream: `NET_DVR_RealPlay_V40` gán trực tiếp lên Handle của Panel.
  * Chụp ảnh: `NET_DVR_CaptureJPEGPicture_NEW` kết hợp Stream bất đồng bộ.

### 2.3. Bộ điều khiển Cảm biến Radar (ZKTeco C3-200 Pull SDK)
* **Thư viện Native**: `plcommpro.dll`, `plcomms.dll`, `pltcpcomm.dll`, `plcommutils.dll` (Build x86).
* **P/Invoke Wrapper**: `HPParkingThaiThuy.SDK.ZKTeco.ZKTecoPullSDK`.
* **Cổng kết nối**: `4370` TCP.
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
HPParkingThaiThuy/
├── Models/                               # Lớp dữ liệu & Đối tượng quản lý
│   ├── Common/
│   │   └── BaseEntity.cs                 # Lớp cơ sở (Id ObjectId, CreatedAt, Soft-Delete)
│   ├── Enums/
│   │   ├── VehicleType.cs                # Loại xe (Car, Motorcycle, Truck, Bicycle...)
│   │   ├── ParkingSessionStatus.cs       # Trạng thái lượt gửi (Active, Completed, UnmatchedOut)
│   │   ├── PersonType.cs                 # Phân loại (Employee, Contractor, Visitor, VIP)
│   │   ├── LaneDirection.cs              # Chiều làn (In, Out, Bidirectional)
│   │   └── SystemEnums.cs                # DeviceType, DeviceStatus, UserRole
│   ├── ValueObjects/
│   │   ├── PlateNumber.cs                # Chuẩn hóa biển số (loại bỏ ký tự thừa, regex)
│   │   └── ImageStoragePath.cs           # Quản lý đường dẫn ảnh snapshot
│   ├── Entities/
│   │   ├── ParkingSession.cs             # Lượt gửi xe (Biển số, ảnh vào/ra, thời gian, chủ xe)
│   │   ├── Vehicle.cs                    # Danh mục xe đăng ký
│   │   ├── Person.cs                     # Danh mục chủ xe / nhân viên / nhà thầu
│   │   ├── Department.cs                 # Danh mục phòng ban
│   │   ├── OrganizationEntities.cs       # Company, Contractor
│   │   └── SystemEntities.cs             # Lane, Device, User
│   └── Data/
│       └── MongoDbContext.cs             # Quản lý kết nối MongoDB & Khởi tạo Index
├── SDK/                                  # Tầng giao tiếp Driver Native C++
│   ├── HikVision/CHCNetSDK.cs            # P/Invoke Hikvision SDK
│   ├── NST/CHISDK.cs                     # P/Invoke NST SDK
│   └── ZKTeco/
│       ├── CZKPullSDK.cs
│       └── ZKTecoPullSDK.cs              # P/Invoke ZKTeco Pull SDK
├── Services/                             # Tầng dịch vụ logic thiết bị
│   ├── Camera/
│   │   ├── CameraConfig.cs               # Cấu hình IP, Port, User, Pass
│   │   ├── OverviewCameraService.cs      # Service Camera Toàn Cảnh (Hikvision)
│   │   └── PlateCameraService.cs         # Service Camera Biển Số (NST)
│   └── Controller/
│       ├── AuxTriggerEventArgs.cs        # Model sự kiện Radar (AuxPort, IsActive, Time)
│       └── ZKTecoDeviceAdapter.cs        # Adapter kết nối & đọc log Controller
├── FrmMain.cs                            # Form giao diện chính & Xử lý nghiệp vụ
├── FrmMain.Designer.cs                   # Thiết kế bố cục giao diện WinForms
├── App.config                            # File cấu hình tập trung IP, Port, DB
└── HPParkingThaiThuy.csproj              # File dự án SDK-style (net48, x86, MongoDB.Driver)
```

---

## 4. MÔ HÌNH DỮ LIỆU & MONGODB DRIVER (MODELS)

Dự án sử dụng thư viện chính thức **`MongoDB.Driver 2.28.0`**:

### 4.1. Entity Lượt Gửi Xe (`ParkingSession.cs`)
```csharp
[BsonIgnoreExtraElements]
public class ParkingSession : BaseEntity
{
    public string PlateNumber { get; set; }           // Biển số xe đã chuẩn hóa
    public VehicleType VehicleType { get; set; }      // Loại xe (Ô tô, Xe máy...)
    public ParkingSessionStatus Status { get; set; }  // Active / Completed / UnmatchedOut
    
    public string? PersonName { get; set; }           // Tên chủ xe
    public string? DepartmentName { get; set; }       // Phòng ban / Đơn vị

    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime? InTime { get; set; }             // Giờ vào
    public string? InLaneId { get; set; }             // Làn vào
    public string? InOverviewImagePath { get; set; }  // Đường dẫn ảnh toàn cảnh vào
    public string? InPlateImagePath { get; set; }     // Đường dẫn ảnh biển số vào

    [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
    public DateTime? OutTime { get; set; }            // Giờ ra
    public string? OutLaneId { get; set; }            // Làn ra
    public string? OutOverviewImagePath { get; set; } // Đường dẫn ảnh toàn cảnh ra
    public string? OutPlateImagePath { get; set; }    // Đường dẫn ảnh biển số ra

    [BsonIgnore]
    public TimeSpan? Duration => (InTime.HasValue && OutTime.HasValue) ? OutTime.Value - InTime.Value : null;
}
```

### 4.2. Database Context (`MongoDbContext.cs`)
* Tự động khởi tạo kết nối Singleton: `MongoDbContext.Instance`.
* Cung cấp các Collection: `ParkingSessions`, `Vehicles`, `Persons`, `Departments`, `Companies`, `Lanes`, `Devices`...
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
        <!-- ================= LÀN VÀO (IN-LANE) ================= -->
        <add key="In_PlateCam_Ip" value="192.168.1.109" />
        <add key="In_PlateCam_Port" value="80" />
        <add key="In_PlateCam_User" value="admin" />
        <add key="In_PlateCam_Password" value="admin" />

        <add key="In_OverviewCam_Ip" value="192.168.1.61" />
        <add key="In_OverviewCam_Port" value="8000" />
        <add key="In_OverviewCam_User" value="admin" />
        <add key="In_OverviewCam_Password" value="Hoangphat130225" />

        <!-- ================= LÀN RA (OUT-LANE) ================= -->
        <add key="Out_PlateCam_Ip" value="192.168.1.203" />
        <add key="Out_PlateCam_Port" value="80" />
        <add key="Out_PlateCam_User" value="admin" />
        <add key="Out_PlateCam_Password" value="admin" />

        <add key="Out_OverviewCam_Ip" value="192.168.1.62" />
        <add key="Out_OverviewCam_Port" value="8000" />
        <add key="Out_OverviewCam_User" value="admin" />
        <add key="Out_OverviewCam_Password" value="Hoangphat130225" />

        <!-- ================= CONTROLLER DÙNG CHUNG ================= -->
        <add key="Controller_ConnType" value="TCP" />
        <add key="Controller_Ip" value="192.168.1.202" />
        <add key="Controller_Port" value="4370" />
        <add key="Controller_Timeout" value="3000" />
        <add key="Controller_Password" value="" />

        <!-- ================= LƯU TRỮ & DATABASE ================= -->
        <add key="CaptureSavePath" value="Captures" />
        <add key="MongoDb_ConnectionString" value="mongodb://localhost:27017" />
        <add key="MongoDb_DatabaseName" value="HPParkingThaiThuyDb" />
    </appSettings>
</configuration>
```

---
*Tài liệu được biên soạn và cập nhật tự động cho dự án HPParkingThaiThuy.*
