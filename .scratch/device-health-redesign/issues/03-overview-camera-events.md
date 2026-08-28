# 03: OverviewCameraService - Add Connection Events

**What to build:** Thêm `OnConnectionStateChanged` event và `IsStreaming` property vào OverviewCameraService (Hikvision SDK). Giống pattern với PlateCameraService.

**Blocked by:** None (can start immediately - parallel với #02)

**Status:** ready-for-agent

## Acceptance criteria

- [ ] OverviewCameraService có `public event EventHandler<DeviceConnectionState>? OnConnectionStateChanged;`
- [ ] OverviewCameraService có `public bool IsStreaming { get; private set; }`
- [ ] LoginAsync thành công → fire `OnConnectionStateChanged(Connected)`
- [ ] Logout → fire `OnConnectionStateChanged(Disconnected)`
- [ ] StartPreview thành công → set `IsStreaming = true` → fire `OnConnectionStateChanged(Streaming)`
- [ ] StopPreview → set `IsStreaming = false`
