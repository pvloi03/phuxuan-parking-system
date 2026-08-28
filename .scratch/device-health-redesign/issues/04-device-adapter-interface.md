# 04: Update IDeviceAdapter Interface

**What to build:** Mở rộng IDeviceAdapter interface với 3 thành phần mới: IsStreaming property, PingAsync method, OnConnectionStateChanged event. Interface này là contract giữa SDK adapters và DeviceHealthManager.

**Blocked by:** 01 (DeviceConnectionState enum)

**Status:** ready-for-agent

## Acceptance criteria

- [ ] IDeviceAdapter có `bool IsStreaming { get; }` property
- [ ] IDeviceAdapter có `Task<bool> PingAsync(int timeoutMs = 2000, CancellationToken ct = default)` method
- [ ] IDeviceAdapter có `event EventHandler<DeviceConnectionState>? OnConnectionStateChanged` event
