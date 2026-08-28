# 05: PlateCameraAdapter - Implement PingAsync & Forward Events

**What to build:** Implement PingAsync (TCP connect) và forward OnConnectionStateChanged event trong PlateCameraAdapter. Adapter này wrap PlateCameraService và expose cho DeviceHealthManager.

**Blocked by:** 02 (PlateCameraService events), 04 (IDeviceAdapter interface)

**Status:** ready-for-agent

## Acceptance criteria

- [ ] PlateCameraAdapter forward `OnConnectionStateChanged` từ PlateCameraService
- [ ] PlateCameraAdapter.IsStreaming returns `_cameraService.IsStreaming`
- [ ] PlateCameraAdapter.PingAsync ping TCP đến `_cameraService.Config.Ip:Port`
- [ ] PingAsync trả về true nếu connect thành công, false nếu fail/timeout
