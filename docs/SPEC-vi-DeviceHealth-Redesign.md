# SPEC: Thiết Kế Lại Tầng Device Health & LiveView (WinForms)

> **Phiên bản**: 1.0  
> **Ngày**: 2026-08-28  
> **Ngôn ngữ**: Tiếng Việt  
> **Trạng thái**: Draft

---

## 1. Mô Tả Vấn Đề

### 1.1 Vấn đề hiện tại

| # | Vấn đề | Tác động |
|---|--------|----------|
| 1 | Logic WinForms rối, phân tán trong nhiều file | Khó bảo trì, khó test |
| 2 | Trạng thái kết nối camera không đồng bộ với LiveView | UI hiển thị sai trạng thái |
| 3 | SDK tự reconnect nhưng UI không biết | Panel vẫn đen dù camera đã online |
| 4 | Không có cơ chế health check đáng tin cậy | Phụ thuộc vào SDK callback (không đáng tin) |

### 1.2 Nguyên nhân gốc

- Code xử lý camera trong FrmMain.Cameras.cs quá phức tạp
- Không có tầng trung gian quản lý state
- Trust SDK callbacks thay vì ping chủ động
- Không có event-driven architecture cho UI updates

---

## 2. Giải Pháp

### 2.1 Nguyên tắc thiết kế

| Nguyên tắc | Mô tả |
|------------|-------|
| **Ping-based Health Check** | Không tin vào SDK callbacks, chủ động ping IP:Port định kỳ |
| **Separation of Concerns** | SDK (low-level) vs Application (high-level) |
| **Event-driven** | UI subscribe events từ DeviceHealth layer |
| **Deterministic** | State machine rõ ràng, biết được chính xác trạng thái |

### 2.2 Kiến trúc tổng quan

```
┌─────────────────────────────────────────────────────────────────┐
│                    WINFORMS LAYER                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                    FrmMain                               │   │
│   │   - UI Controls (Panels, Labels)                         │   │
│   │   - Subscribe to DeviceState events                      │   │
│   │   - Update UI khi nhận events                           │   │
│   └─────────────────────┬───────────────────────────────────┘   │
│                         │                                       │
├─────────────────────────┼───────────────────────────────────────┤
│                         ▼                                       │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │              DeviceHealthManager                         │   │
│   │   - Quản lý state của tất cả devices                    │   │
│   │   - Ping định kỳ từng device                            │   │
│   │   - Fire events khi state thay đổi                      │   │
│   │   - Retry logic khi kết nối thất bại                     │   │
│   └─────────────────────┬───────────────────────────────────┘   │
│                         │                                       │
├─────────────────────────┼───────────────────────────────────────┤
│                         ▼                                       │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │              DeviceAdapter Layer                         │   │
│   │   - PlateCameraAdapter (NST SDK)                         │   │
│   │   - OverviewCameraAdapter (Hikvision SDK)                │   │
│   │   - ControllerAdapter (ZKTeco SDK)                      │   │
│   │   - Cung cấp: Connect, Disconnect, Restart, Ping         │   │
│   └─────────────────────┬───────────────────────────────────┘   │
│                         │                                       │
├─────────────────────────┼───────────────────────────────────────┤
│                         ▼                                       │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │                    SDK Layer                             │   │
│   │   - CHISDK (NST) - Camera biển số                       │   │
│   │   - CHCNetSDK (Hikvision) - Camera toàn cảnh            │   │
│   │   - ZKTecoPullSDK - Controller                          │   │
│   │   - Chỉ xử lý: Init, Connect, LiveView, Disconnect      │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 3. User Stories

### 3.1 Device Health Monitoring

1. **Là người vận hành**, tôi muốn thấy trạng thái kết nối của tất cả thiết bị (camera vào, camera ra, controller) trên màn hình chính, để tôi biết thiết bị nào đang online/offline.

2. **Là người vận hành**, tôi muốn hệ thống tự động kiểm tra kết nối thiết bị mỗi 30 giây, để tôi không phải thủ công kiểm tra.

3. **Là người vận hành**, tôi muốn thấy cảnh báo khi thiết bị mất kết nối, để tôi kịp thời phản ứng.

### 3.2 LiveView Synchronization

4. **Là người vận hành**, tôi muốn LiveView tự động dừng khi camera mất kết nối, để tránh panel đen hiển thị sai.

5. **Là người vận hành**, tôi muốn LiveView tự động khôi phục khi camera kết nối lại, để tôi không phải thao tác thủ công.

6. **Là người vận hành**, tôi muốn nhấn nút "Khởi động lại" để reset kết nối camera, để khắc phục nhanh khi có sự cố.

### 3.3 Controller & Radar

7. **Là người vận hành**, tôi muốn Controller (C3-200) được giám sát kết nối như camera, để đảm bảo radar hoạt động.

8. **Là người vận hành**, tôi muốn ReadLog từ Controller tự động dừng/khởi động theo trạng thái kết nối, để không có ghost events.

### 3.4 Recovery Scenarios

9. **Là người vận hành**, khi mất mạng LAN, tôi muốn hệ thống hiển thị "Mất kết nối" ngay, để tôi biết vấn đề.

10. **Là người vận hành**, khi khôi phục mạng, tôi muốn hệ thống tự kết nối lại trong vòng 60 giây, để không cần can thiệp thủ công.

11. **Là người vận hành**, khi camera bị treo (không ping được nhưng chưa timeout), tôi muốn có tùy chọn "Force Restart", để reset hardware.

---

## 4. State Machine

### 4.1 Device Connection State

```
                    ┌──────────────────┐
                    │   Disconnected   │ ← Trạng thái ban đầu
                    └────────┬─────────┘
                             │
                             │ Ping thành công
                             │ HOẶC User click "Kết nối"
                             ▼
                    ┌──────────────────┐
              ┌────▶│    Connecting   │
              │     └────────┬─────────┘
              │              │
              │    Thành công│    Thất bại
              │              ▼
              │     ┌──────────────────┐
              │     │    Connected     │──────┐
              │     └────────┬─────────┘      │
              │              │                │ Start LiveView
              │              │ Start LiveView │ HOẶC Start ReadLog
              │              ▼                │
              │     ┌──────────────────┐      │
              │     │    Streaming     │◀─────┘
              │     └────────┬─────────┘
              │              │
              │              │ Stop/Error/Ping fail
              │              ▼
              └────── Disconnected
```

### 4.2 Trạng thái và Hành động tương ứng

| Trạng thái | LiveView | ReadLog | UI Label |
|------------|----------|---------|----------|
| `Disconnected` | Stop | Stop | Đỏ: "Mất kết nối" |
| `Connecting` | Stop | Stop | Vàng: "Đang kết nối..." |
| `Connected` | Start | Start | Xanh: "Đã kết nối" |
| `Streaming` | Running | Running | Xanh: "Hoạt động" |
| `Error` | Stop | Stop | Đỏ: "Lỗi" |

---

## 5. Kiến trúc Chi tiết

### 5.1 Component Responsibilities

#### DeviceHealthManager (NEW)
- **Singleton** được inject vào FrmMain
- Chứa timer cho periodic ping (30 giây)
- Quản lý state machine cho mỗi device
- Retry logic với exponential backoff
- Fire events khi state thay đổi

#### IDeviceAdapter (EXISTING - MODIFY)
```csharp
public interface IDeviceAdapter
{
    // Trạng thái hiện tại
    bool IsConnected { get; }
    bool IsStreaming { get; }  // THÊM MỚI
    
    // Kết nối
    Task<bool> ConnectAsync(Device device, CancellationToken ct = default);
    Task DisconnectAsync();
    
    // Khởi động lại
    Task<bool> RestartAsync(Device device, CancellationToken ct = default);
    
    // Ping (TCP connection test)
    Task<bool> PingAsync(int timeoutMs = 2000, CancellationToken ct = default);
    
    // Events
    event EventHandler<ConnectionState>? OnStateChanged;
}
```

#### PlateCameraAdapter / OverviewCameraAdapter (MODIFY)
- Implement `IsStreaming` property
- Implement `PingAsync()` method
- Forward SDK events qua `OnStateChanged`

#### ControllerAdapter (ZKTeco) (EXISTING)
- Implement `IsStreaming` (cho ReadLog)
- Implement `PingAsync()`
- Handle ReadLog start/stop

### 5.2 Ping Strategy

```csharp
// Ping thực hiện TCP connection attempt đến IP:Port
public async Task<bool> PingAsync(int timeoutMs = 2000, CancellationToken ct = default)
{
    try
    {
        using var client = new TcpClient();
        var result = await client.ConnectAsync(device.IpAddress, device.Port, ct);
        return result == TcpConnectionState.Success;
    }
    catch
    {
        return false;
    }
}
```

### 5.3 Retry Logic

```
┌─────────────────────────────────────────────────────────────┐
│                     Retry Flow                               │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│   Ping Fail ──▶ Retry 1 (sau 1 giây) ──▶ Ping Fail         │
│                                              │               │
│                                              ▼               │
│                                    Retry 2 (sau 2 giây)     │
│                                              │               │
│                                              ▼               │
│                                    Retry 3 (sau 4 giây)     │
│                                              │               │
│                                    ┌─────────┴─────────┐   │
│                                    │                    │   │
│                              Ping Fail            Ping Success
│                                    │                    │
│                                    ▼                    ▼
│                            Set Error State      Reconnect
│                            Stop LiveView/Log    Start LiveView/Log
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 6. User Interface

### 6.1 FrmMain - Camera Panels

```
┌─────────────────────────────────────────────────────────────────┐
│  ┌─────────────────────┐    ┌─────────────────────┐           │
│  │ Camera Biển Số Vào  │    │ Camera Toàn Cảnh Vào│           │
│  │ ┌─────────────────┐ │    │ ┌─────────────────┐ │           │
│  │ │                 │ │    │ │                 │ │           │
│  │ │   LIVE VIEW     │ │    │ │   LIVE VIEW     │ │           │
│  │ │   (Video)       │ │    │ │   (Video)       │ │           │
│  │ │                 │ │    │ │                 │ │           │
│  │ └─────────────────┘ │    │ └─────────────────┘ │           │
│  │ 🔴 IP: 192.168.1.11 │    │ 🟢 IP: 192.168.1.12│           │
│  │    [⟳ Khởi động lại] │    │    [⟳ Khởi động lại]│           │
│  └─────────────────────┘    └─────────────────────┘           │
│                                                                 │
│  ┌─────────────────────┐    ┌─────────────────────┐           │
│  │ Camera Biển Số Ra    │    │ Camera Toàn Cảnh Ra │           │
│  │ ┌─────────────────┐ │    │ ┌─────────────────┐ │           │
│  │ │   LIVE VIEW     │ │    │ │   LIVE VIEW     │ │           │
│  │ │   (Video)       │ │    │ │   (Video)       │ │           │
│  │ │                 │ │    │ │                 │ │           │
│  │ └─────────────────┘ │    │ └─────────────────┘ │           │
│  │ 🟡 IP: 192.168.1.13 │    │ 🔴 IP: 192.168.1.14 │           │
│  │    [⟳ Khởi động lại] │    │    [⟳ Khởi động lại]│           │
│  └─────────────────────┘    └─────────────────────┘           │
│                                                                 │
│  🟢 = Online, 🟡 = Connecting, 🔴 = Offline/Error               │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 6.2 Device Status Overlay

Khi device `Disconnected` hoặc `Error`:
```
┌─────────────────────────┐
│                         │
│   🔴 MẤT KẾT NỐI       │
│                         │
│   Camera Biển Số Vào    │
│   IP: 192.168.1.11      │
│                         │
│   [⟳ Khởi động lại]    │
│                         │
└─────────────────────────┘
```

### 6.3 FrmDeviceMonitor (Enhanced)

```
┌─────────────────────────────────────────────────────────────────┐
│  Device Monitor                                    [⟳ Refresh] │
├─────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ Device         │ Type    │ IP           │ Status │ Actions │ │
│  ├─────────────────────────────────────────────────────────────┤ │
│  │ CAM-01         │ Plate   │ 192.168.1.11 │ 🟢     │ [⟳]    │ │
│  │ CAM-02         │ Overview│ 192.168.1.12 │ 🟢     │ [⟳]    │ │
│  │ CAM-03         │ Plate   │ 192.168.1.13 │ 🟡     │ [⟳]    │ │
│  │ CAM-04         │ Overview│ 192.168.1.14 │ 🔴     │ [⟳]    │ │
│  │ CTRL-01        │ Ctrl    │ 192.168.1.202│ 🟢     │ [⟳]    │ │
│  └─────────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Last check: 10:30:45  │  Next check: 10:31:15  │  Interval: 30s│
└─────────────────────────────────────────────────────────────────┘
```

---

## 7. Implementation Decisions

### 7.1 Modules cần tạo/sửa

| Module | Type | Mô tả |
|--------|------|-------|
| `DeviceConnectionState` | **NEW Enum** | Disconnected, Connecting, Connected, Streaming, Error |
| `DeviceHealthManager` | **NEW Class** | Singleton quản lý health check, state, retry |
| `IDeviceAdapter.PingAsync()` | **NEW Method** | TCP ping đến IP:Port |
| `IDeviceAdapter.IsStreaming` | **NEW Property** | Trạng thái LiveView/ReadLog |
| `IDeviceAdapter.OnStateChanged` | **NEW Event** | Event khi SDK state thay đổi |
| `PlateCameraService` | MODIFY | Expose events, IsStreaming state |
| `OverviewCameraService` | MODIFY | Expose events, IsStreaming state |
| `FrmMain.Cameras.cs` | REFACTOR | Sử dụng DeviceHealthManager |
| `FrmDeviceMonitor` | MODIFY | Hiển thị real-time status |

### 7.2 Health Check Cycle

```csharp
public class DeviceHealthManager
{
    private readonly Timer _pingTimer;
    private readonly TimeSpan _pingInterval = TimeSpan.FromSeconds(30);
    
    public DeviceHealthManager()
    {
        _pingTimer = new Timer(PingAllDevices, null, _pingInterval, _pingInterval);
    }
    
    private async void PingAllDevices(object? state)
    {
        foreach (var device in _registeredDevices)
        {
            bool pingOk = await device.Adapter.PingAsync();
            
            if (pingOk && device.State == ConnectionState.Disconnected)
            {
                // Device vừa online → thử reconnect
                await TryReconnectAsync(device);
            }
            else if (!pingOk && device.State == ConnectionState.Connected)
            {
                // Device vừa offline → retry
                await RetryWithBackoffAsync(device);
            }
            else if (!pingOk && device.State == ConnectionState.Connected)
            {
                // Ping fail liên tục → set Error
                SetState(device, ConnectionState.Error);
                StopLiveView(device);
            }
        }
    }
}
```

### 7.3 LiveView Sync Rule

```csharp
// Khi state thay đổi → tự động sync LiveView
public void OnStateChanged(object sender, ConnectionState newState)
{
    var device = (Device)sender;
    
    switch (newState)
    {
        case ConnectionState.Connected:
        case ConnectionState.Streaming:
            StartLiveView(device);
            break;
            
        case ConnectionState.Disconnected:
        case ConnectionState.Error:
            StopLiveView(device);
            break;
    }
}
```

---

## 8. Testing Decisions

### 8.1 Test Strategy

| Loại test | Mô tả | Ví dụ |
|-----------|-------|-------|
| **Unit Test** | Test DeviceHealthManager logic | Ping success/fail, retry logic, state transitions |
| **Integration Test** | Test với mock SDK | Mock camera connect/disconnect |
| **Manual Test** | Test thực tế với hardware | Rút cable, restore, verify behavior |

### 8.2 Test Cases quan trọng

1. **PingFail_Streaming**: Disconnect cable → Ping fail → LiveView stops
2. **PingSuccess_AfterFail**: Restore cable → Ping success → Auto reconnect
3. **ManualRestart**: Click restart → SDK reconnect → LiveView starts
4. **MultipleDevices**: Một camera fail, camera khác vẫn hoạt động
5. **ControllerOffline**: Controller mất kết nối → Radar events không có

---

## 9. Out of Scope

| Item | Lý do |
|------|-------|
| **WPF Migration** | Giữ WinForms, chỉ refactor internal |
| **Cloud/MQTT integration** | Chỉ local health check |
| **Multi-site** | Chỉ single site deployment |
| **Automatic failover** | Không có backup device |

---

## 10. Ghi chú thêm

### 10.1 Đã xác định

- ✅ Ping-based approach (không trust SDK callbacks)
- ✅ Retry với exponential backoff (3 lần: 1s, 2s, 4s)
- ✅ Health check interval: 30 giây
- ✅ Restart button cho mỗi device
- ✅ LiveView sync với connection state

### 10.2 Cần verify

- [ ] Hikvision SDK có callback khi reconnect không? (Dù đã quyết định ping-based)
- [ ] Timeout cho ping? Đề xuất: 2 giây
- [ ] Số lần retry tối đa? Đề xuất: 3 lần

---

## 11. Tài liệu liên quan

- [SPEC gốc](./SPEC-device-health-redesign.md)
- [Implementation Plan](./plans/device-health-redesign-implementation.md)
- [CONTEXT.md](../CONTEXT.md) - Domain vocabulary
