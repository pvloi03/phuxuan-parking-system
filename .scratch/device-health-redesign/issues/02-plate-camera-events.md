# 02: PlateCameraService - Add Connection Events

**What to build:** Thêm `OnConnectionStateChanged` event và `IsStreaming` property vào PlateCameraService. Fire event khi Login/Logout/StartPreview/StopPreview. Đây là nguồn event cho UI layer.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

## Acceptance criteria

- [ ] PlateCameraService có `public event EventHandler<DeviceConnectionState>? OnConnectionStateChanged;`
- [ ] PlateCameraService có `public bool IsStreaming { get; private set; }`
- [ ] LoginAsync thành công → fire `OnConnectionStateChanged(Connected)`
- [ ] Logout → fire `OnConnectionStateChanged(Disconnected)`
- [ ] StartPreview thành công → set `IsStreaming = true` → fire `OnConnectionStateChanged(Streaming)`
- [ ] StopPreview → set `IsStreaming = false`
