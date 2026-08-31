# Plan: Device Health & LiveView Redesign (WinForms)

## Context

**Problem**: Logic WinForms đang rối, không sync được connection state với stream preview:
1. Camera mất kết nối → SDK tự reconnect → UI không biết
2. Stream Preview không tự restart khi SDK reconnect
3. FrmMain không có event subscription cho camera state changes
4. Không có cơ chế health check đáng tin cậy

**Solution**: Tái thiết kế Device Health layer với:
- **Ping-based Health Check** - Chủ động ping IP:Port định kỳ (không trust SDK callbacks)
- **Separation of Concerns** - SDK (low-level) vs Application (high-level)
- **Event-driven Architecture** - UI subscribe events khi state thay đổi
- **DeviceHealthManager** - Singleton quản lý tất cả devices

**Health Check Flow**:
```
Ping thành công + Device Disconnected → Auto reconnect → Start LiveView
Ping thất bại (3 lần retry) → Device Error → Stop LiveView
Manual restart button → SDK reconnect → Start LiveView
```

---

## Implementation Phases

### Phase 1: Core Infrastructure (Enum + Events)

#### Step 1.1: Create DeviceConnectionState Enum
**File**: `PhuXuanParkingSystem.Domain/Models/Enums/DeviceConnectionState.cs`

```csharp
namespace PhuXuanParkingSystem.Models.Enums
{
    public enum DeviceConnectionState
    {
        Disconnected = 0,
        Connecting = 1,
        Connected = 2,
        Streaming = 3,
        Error = -1
    }
}
```

#### Step 1.2: Add ConnectionStateChanged Event to PlateCameraService
**File**: `PhuXuanParkingSystem/Services/Camera/PlateCameraService.cs`

Changes:
1. Add property: `public bool IsStreaming { get; private set; }`
2. Add event: `public event EventHandler<DeviceConnectionState>? OnConnectionStateChanged;`
3. In LoginAsync success → fire `OnConnectionStateChanged?.Invoke(this, DeviceConnectionState.Connected);`
4. In Logout → fire `OnConnectionStateChanged?.Invoke(this, DeviceConnectionState.Disconnected);`
5. In StartPreview success → set `IsStreaming = true` → fire `OnConnectionStateChanged?.Invoke(this, DeviceConnectionState.Streaming);`
6. In StopPreview → set `IsStreaming = false`

#### Step 1.3: Add ConnectionStateChanged Event to OverviewCameraService
**File**: `PhuXuanParkingSystem/Services/Camera/OverviewCameraService.cs`

Same changes as PlateCameraService.

---

### Phase 2: Update IDeviceAdapter Interface

#### Step 2.1: Update IDeviceAdapter
**File**: `PhuXuanParkingSystem/Services/DeviceHealth/IDeviceAdapter.cs`

Add:
```csharp
/// <summary>
/// TRUE = đang streaming video hoặc đang nhận log (Controller)
/// </summary>
bool IsStreaming { get; }

/// <summary>
/// Ping TCP đến IP:Port của thiết bị
/// </summary>
Task<bool> PingAsync(int timeoutMs = 2000, CancellationToken ct = default);

/// <summary>
/// Event khi trạng thái SDK thay đổi
/// </summary>
event EventHandler<DeviceConnectionState>? OnConnectionStateChanged;
```

---

### Phase 3: Implement PingAsync in Adapters

#### Step 3.1: Add PingAsync to PlateCameraAdapter
**File**: `PhuXuanParkingSystem/Services/DeviceHealth/PlateCameraAdapter.cs`

Add:
```csharp
public event EventHandler<DeviceConnectionState>? OnConnectionStateChanged;

public PlateCameraAdapter(PlateCameraService cameraService)
{
    _cameraService = cameraService ?? throw new ArgumentNullException(nameof(cameraService));
    _cameraService.OnConnectionStateChanged += (s, state) => 
        OnConnectionStateChanged?.Invoke(this, state);
}

public bool IsStreaming => _cameraService.IsStreaming;

public async Task<bool> PingAsync(int timeoutMs = 2000, CancellationToken ct = default)
{
    try
    {
        using var client = new TcpClient();
        var result = await client.ConnectAsync(_cameraService.Config.Ip, _cameraService.Config.Port, ct);
        return result == TcpConnectionState.Success;
    }
    catch
    {
        return false;
    }
}
```

#### Step 3.2: Add PingAsync to OverviewCameraAdapter
**File**: `PhuXuanParkingSystem/Services/DeviceHealth/OverviewCameraAdapter.cs`

Same pattern as PlateCameraAdapter.

#### Step 3.3: Add PingAsync to ZKTecoDeviceAdapter
**File**: `PhuXuanParkingSystem/Services/Controller/ZKTecoDeviceAdapter.cs`

Add IsStreaming, PingAsync, OnConnectionStateChanged (for ReadLog state).

---

### Phase 4: Create DeviceHealthManager

#### Step 4.1: Create DeviceHealthManager Class
**File**: `PhuXuanParkingSystem/Services/DeviceHealth/DeviceHealthManager.cs`

```csharp
public interface IDeviceHealthManager
{
    // State queries
    DeviceConnectionState GetState(string deviceId);
    bool IsStreaming(string deviceId);
    
    // Events
    event EventHandler<DeviceStateChangedEventArgs>? OnStateChanged;
    
    // Registration
    void RegisterDevice(string deviceId, Device device, IDeviceAdapter adapter, IntPtr? previewHandle = null);
    void UnregisterDevice(string deviceId);
    
    // Manual actions
    Task<bool> ConnectAsync(string deviceId);
    Task DisconnectAsync(string deviceId);
    Task<bool> RestartAsync(string deviceId);
    Task StartPreviewAsync(string deviceId);
    Task StopPreviewAsync(string deviceId);
    
    // Bulk operations
    Task ConnectAllAsync();
    Task DisconnectAllAsync();
    
    // Health check control
    void StartHealthCheck(TimeSpan interval);
    void StopHealthCheck();
}

public class DeviceStateChangedEventArgs : EventArgs
{
    public string DeviceId { get; }
    public DeviceConnectionState OldState { get; }
    public DeviceConnectionState NewState { get; }
    public DateTime Timestamp { get; }
}
```

#### Step 4.2: Implement Health Check Timer
Inside DeviceHealthManager:

```csharp
private Timer? _healthCheckTimer;
private readonly ConcurrentDictionary<string, DeviceHealthInfo> _devices = new();
private const int MAX_RETRIES = 3;
private static readonly TimeSpan[] RETRY_DELAYS = { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4) };

public void StartHealthCheck(TimeSpan interval)
{
    _healthCheckTimer = new Timer(async _ => await CheckAllDevicesAsync(), null, interval, interval);
}

private async Task CheckAllDevicesAsync()
{
    foreach (var (deviceId, info) in _devices)
    {
        bool pingOk = await info.Adapter.PingAsync(2000);
        
        if (pingOk)
        {
            if (info.State == DeviceConnectionState.Disconnected || info.State == DeviceConnectionState.Error)
            {
                // Device vừa online → thử reconnect
                await TryReconnectAsync(deviceId);
            }
            else if (info.State == DeviceConnectionState.Connected && !info.IsStreaming)
            {
                // Đã connect nhưng chưa stream → start preview
                await StartPreviewAsync(deviceId);
            }
        }
        else
        {
            if (info.State == DeviceConnectionState.Connected || info.State == DeviceConnectionState.Streaming)
            {
                // Device vừa offline → retry với backoff
                await RetryWithBackoffAsync(deviceId);
            }
        }
    }
}

private async Task RetryWithBackoffAsync(string deviceId)
{
    var info = _devices[deviceId];
    int retryCount = 0;
    
    while (retryCount < MAX_RETRIES)
    {
        bool pingOk = await info.Adapter.PingAsync(2000);
        if (pingOk)
        {
            await TryReconnectAsync(deviceId);
            return;
        }
        
        retryCount++;
        if (retryCount < MAX_RETRIES)
        {
            await Task.Delay(RETRY_DELAYS[retryCount - 1]);
        }
    }
    
    // All retries failed → set Error state
    SetState(deviceId, DeviceConnectionState.Error);
    await StopPreviewAsync(deviceId);
}
```

---

### Phase 5: FrmMain Integration

#### Step 5.1: Update FrmMain Constructor
**File**: `PhuXuanParkingSystem/Forms/FrmMain.cs`

Add:
```csharp
private readonly IDeviceHealthManager _deviceHealthManager;

public FrmMain(..., IDeviceHealthManager? deviceHealthManager = null)
{
    // ... existing code ...
    _deviceHealthManager = deviceHealthManager ?? new DeviceHealthManager();
    _deviceHealthManager.OnStateChanged += HandleDeviceStateChanged;
}
```

#### Step 5.2: Register Devices
**File**: `PhuXuanParkingSystem/Forms/FrmMain.Cameras.cs`

In LoadConfigurationsFromDbAsync, after creating camera instances:
```csharp
// Register cameras với DeviceHealthManager
_deviceHealthManager.RegisterDevice(
    _inPlateCamDeviceId,
    result.InPlateCamera,
    new PlateCameraAdapter(_inPlateCam),
    pnlInPlateVideo.Handle);

_deviceHealthManager.RegisterDevice(
    _inOverviewCamDeviceId,
    result.InOverviewCamera,
    new OverviewCameraAdapter(_inOverviewCam),
    pnlInOverviewVideo.Handle);

// ... similar for out cameras
```

#### Step 5.3: Replace AutoConnectAllAsync
```csharp
private async Task AutoConnectAllAsync()
{
    await LoadConfigurationsFromDbAsync();
    
    // Register devices (from Step 5.2)
    RegisterDevicesWithHealthManager();
    
    // Connect all devices
    await _deviceHealthManager.ConnectAllAsync();
    
    // Start health check every 30 seconds
    _deviceHealthManager.StartHealthCheck(TimeSpan.FromSeconds(30));
}
```

#### Step 5.4: Handle State Change Events
```csharp
private void HandleDeviceStateChanged(object? sender, DeviceStateChangedEventArgs e)
{
    if (InvokeRequired)
    {
        BeginInvoke(() => HandleDeviceStateChanged(sender, e));
        return;
    }
    
    switch (e.DeviceId)
    {
        case var id when id == _inPlateCamDeviceId:
            _inPlateState = e.NewState;
            break;
        case var id when id == _inOverviewCamDeviceId:
            _inOverviewState = e.NewState;
            break;
        // ... similar for other cameras
    }
    
    InvalidateCameraPanels();
}
```

#### Step 5.5: Update DrawVideoPanelStatus
```csharp
private void DrawVideoPanelStatus(Panel pnl, DeviceConnectionState state, string camTitle, string? ip, PaintEventArgs e)
{
    if (state == DeviceConnectionState.Connected || state == DeviceConnectionState.Streaming)
    {
        e.Graphics.DrawRectangle(_penNormalBorder, 0, 0, pnl.Width - 1, pnl.Height - 1);
        return;
    }
    
    // Draw status overlay for other states
    Color bgColor, textColor;
    string statusText;
    
    switch (state)
    {
        case DeviceConnectionState.Connecting:
            bgColor = Color.FromArgb(26, 30, 35);
            textColor = Color.FromArgb(100, 180, 255);
            statusText = "🔄 ĐANG KẾT NỐI...";
            break;
        case DeviceConnectionState.Disconnected:
        case DeviceConnectionState.Error:
        default:
            bgColor = Color.FromArgb(20, 22, 25);
            textColor = Color.FromArgb(235, 75, 75);
            statusText = "❌ MẤT KẾT NỐI";
            break;
    }
    
    // Draw overlay...
}
```

---

### Phase 6: FrmDeviceMonitor Enhancement

#### Step 6.1: Update FrmDeviceMonitor to Use DeviceHealthManager
**File**: `PhuXuanParkingSystem/Forms/FrmDeviceMonitor.cs`

- Subscribe to DeviceHealthManager.OnStateChanged
- Display real-time status in DataGridView
- Add restart button per row
- Show last check time and next check countdown

---

## Files to Create/Modify

| File | Action | Lines |
|------|--------|-------|
| `DeviceConnectionState.cs` | **CREATE** | ~20 |
| `DeviceHealthManager.cs` | **CREATE** | ~250 |
| `PlateCameraService.cs` | MODIFY | +15 |
| `OverviewCameraService.cs` | MODIFY | +15 |
| `IDeviceAdapter.cs` | MODIFY | +10 |
| `PlateCameraAdapter.cs` | MODIFY | +30 |
| `OverviewCameraAdapter.cs` | MODIFY | +30 |
| `ZKTecoDeviceAdapter.cs` | MODIFY | +30 |
| `FrmMain.cs` | MODIFY | +20 |
| `FrmMain.Cameras.cs` | REFACTOR | ~80 |
| `FrmDeviceMonitor.cs` | MODIFY | +50 |

---

## Verification

### Build
```bash
dotnet build PhuXuanParkingSystem.slnx -c Debug
```

### Manual Test Cases

| # | Scenario | Steps | Expected Result |
|---|----------|-------|-----------------|
| 1 | App startup | Run app | All cameras connect → Streams show |
| 2 | Camera disconnect | Unplug camera cable | Panel shows error overlay in <30s |
| 3 | Camera reconnect | Plug cable back | Stream auto-restarts in <60s |
| 4 | Manual restart | Click restart button | SDK reconnects → Stream starts |
| 5 | Multiple offline | Unplug 2 cameras | Only those panels show error |
| 6 | Health check visible | Wait 30s | Status updates in FrmDeviceMonitor |

---

## Open Questions (Resolved)

| Question | Decision |
|----------|----------|
| Ping hay trust SDK? | **Ping-based** - Chủ động ping TCP |
| Retry placement? | **DeviceHealthManager** - Centralized |
| Retry delays? | **1s, 2s, 4s** - Exponential backoff |
| Health check interval? | **30 seconds** |
| Restart button? | **Per device** - In panel + in FrmDeviceMonitor |
