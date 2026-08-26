# Task 021: Nạp Cấu Hình Thiết Bị Camera & Controller Động Theo Làn (Lanes) Từ MongoDB

## 1. Mục Tiêu
Nâng cấp cơ chế nạp cấu hình phần cứng trong ứng dụng WinForms (`PhuXuanParkingSystem`) từ việc hardcode mã thiết bị / App.config sang việc **đọc cấu hình liên kết Làn kiểm soát (`Lane`) - Thiết bị (`Device`) trực tiếp từ MongoDB**.

---

## 2. Bối Cảnh & Phạm Vi
- **Vấn đề trước đây**: `FrmMain.Cameras.cs` nạp cấu hình theo tên `Contains("Vào")` hoặc mã hardcode `CAM-IN-PLT`. Khi người dùng tạo hoặc chỉnh sửa thiết bị/làn trên Web Admin (ví dụ mã `CAM-01`, `CAM-02`), WinForms không map được đúng mà fallback về `App.config`.
- **Giải pháp**:
  1. `FrmMain` inject `IRepository<Lane> _laneRepo` và `IRepository<Device> _deviceRepo`.
  2. Hàm `LoadConfigurationsFromDbAsync()`:
     - Truy vấn các Làn kiểm soát (`Lane`) đang kích hoạt (`IsActive == true && !IsDeleted`).
     - Lấy Làn Vào (`Direction == LaneDirection.In`) ➔ Đọc `PlateCameraDeviceId`, `OverviewCameraDeviceId`, `ControllerDeviceId`.
     - Lấy Làn Ra (`Direction == LaneDirection.Out`) ➔ Đọc `PlateCameraDeviceId`, `OverviewCameraDeviceId`, `ControllerDeviceId`.
     - Tra cứu thông tin chi tiết của các thiết bị này từ `_deviceRepo` để nạp IP, Port, Username, Password cho từng Camera và Controller.
     - Giữ cơ chế Fallback an toàn (nếu Làn chưa gán ID thiết bị, tự động tìm theo `DeviceType` hoặc dùng `App.config`).

---

## 3. Checklist Thực Hiện
- [x] 1. Cập nhật constructor & trường `IRepository<Lane> _laneRepo` trong `FrmMain.cs`.
- [x] 2. Đăng ký `IRepository<Lane>` vào DI trong `Program.cs`.
- [x] 3. Nâng cấp phương thức `LoadConfigurationsFromDbAsync()` trong `FrmMain.Cameras.cs`.
- [x] 4. Biên dịch và kiểm thử WinForms x86 (`0 Error, 0 Warning`).

---

## 4. Chi Tiết Kỹ Thuật

### 4.1. Luồng Nạp Cấu Hình Động

```
LoadConfigurationsFromDbAsync()
│
├── Query Lanes (IsActive == true && !IsDeleted)
│
├── Filter: Direction == LaneDirection.In → InLane
│   └── Read: PlateCameraDeviceId, OverviewCameraDeviceId, ControllerDeviceId
│
├── Filter: Direction == LaneDirection.Out → OutLane
│   └── Read: PlateCameraDeviceId, OverviewCameraDeviceId, ControllerDeviceId
│
└── Resolve Device Details
    └── _deviceRepo.GetByIdAsync(deviceId) → IP, Port, Username, Password, RTSPUrl
```

### 4.2. Cơ Chế Fallback An Toàn

1. **Ưu tiên 1**: `Lane.PlateCameraDeviceId` (MongoDB)
2. **Fallback 1**: Tìm theo `DeviceType` + `LaneId`
3. **Fallback cuối cùng**: `App.config` (legacy)

### 4.3. Các File Chính

| File | Mô tả |
|------|-------|
| `FrmMain.cs` | Constructor inject `IRepository<Lane>`, `IRepository<Device>` |
| `FrmMain.Cameras.cs` | `LoadConfigurationsFromDbAsync()` nạp cấu hình từ MongoDB |
| `Program.cs` | Đăng ký DI container |
| `Lane.cs` | Entity Làn (PlateCameraDeviceId, OverviewCameraDeviceId, ControllerDeviceId) |
| `Device.cs` | Entity Thiết bị (IP, Port, Username, Password, DeviceType) |

---

## 4. Chi Tiết Kỹ Thuật

### 4.1. Luồng Nạp Cấu Hình Động

```
┌─────────────────────────────────────────────────────────────────┐
│                     FrmMain Constructor                          │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ 1. Inject IRepository<Lane>                                │ │
│  │ 2. Inject IRepository<Device>                              │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                 LoadConfigurationsFromDbAsync()                  │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ 1. Query Lanes (IsActive == true && !IsDeleted)            │ │
│  │ 2. Filter: Direction == LaneDirection.In  → InLane         │ │
│  │ 3. Filter: Direction == LaneDirection.Out → OutLane        │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                              │
          ┌───────────────────┴───────────────────┐
          ▼                                       ▼
┌─────────────────────┐               ┌─────────────────────┐
│    InLane Config    │               │   OutLane Config    │
│  - PlateCameraId   │               │  - PlateCameraId   │
│  - OverviewCameraId│               │  - OverviewCameraId│
│  - ControllerId    │               │  - ControllerId    │
└─────────────────────┘               └─────────────────────┘
          │                                       │
          └───────────────────┬───────────────────┘
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│              _deviceRepo.GetByIdAsync(deviceId)                  │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ Resolve: IP, Port, Username, Password, RTSPUrl               │ │
│  │ Build: CameraConfig / ControllerConfig objects              │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│              InitializeCamerasAsync() + ConnectAllAsync()        │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ 1. Create camera services with resolved configs             │ │
│  │ 2. Open live streams                                       │ │
│  │ 3. Start radar listener                                    │ │
│  └─────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

### 4.2. Cơ Chế Fallback An Toàn

```csharp
// 1. Ưu tiên: Lấy từ Lane.DeviceId (MongoDB)
if (!string.IsNullOrEmpty(lane.PlateCameraDeviceId))
{
    var device = await _deviceRepo.GetByIdAsync(lane.PlateCameraDeviceId);
    if (device != null) return BuildCameraConfig(device);
}

// 2. Fallback: Tìm theo DeviceType
var fallbackDevice = await _deviceRepo.FindAsync(
    d => d.DeviceType == DeviceType.CameraPlate && d.LaneId == lane.Id
);

// 3. Fallback cuối cùng: App.config (legacy)
return LoadFromAppConfig(key);
```

### 4.3. Các File Chính

| File | Mô tả |
|------|-------|
| `FrmMain.cs` | Constructor inject IRepository<Lane>, IRepository<Device> |
| `FrmMain.Cameras.cs` | LoadConfigurationsFromDbAsync(), khởi tạo camera với config động |
| `Program.cs` | Đăng ký IRepository<Lane> vào DI container |
| `App.config` | Fallback cuối cùng (legacy) |

### 4.4. Cải Thiện So Với Phiên Bản Trước

| Trước | Sau |
|-------|-----|
| Hardcode `CAM-IN-PLT`, `CAM-OUT-PLT` | Đọc từ `Lane.PlateCameraDeviceId` |
| Tìm theo `Contains("Vào")` | Query theo `LaneDirection.In` |
| Config cố định trong App.config | Config động từ MongoDB |
| Không phản ánh thay đổi từ Web Admin | Đồng bộ real-time khi khởi động lại |

---

## 5. Ghi Chú Triển Khai

- **Commit**: `a1bf6d7` - feat(winforms): dynamically sync camera and controller configs from MongoDB Lanes and Devices
- **Commit**: `230710f` - refactor(winforms): clean up LoadConfigurations and remove obsolete App.config camera settings
