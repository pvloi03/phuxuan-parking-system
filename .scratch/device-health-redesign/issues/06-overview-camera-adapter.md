# 06: OverviewCameraAdapter - Implement PingAsync & Forward Events

**What to build:** Implement PingAsync (TCP connect) và forward OnConnectionStateChanged event trong OverviewCameraAdapter (Hikvision SDK).

**Blocked by:** 03 (OverviewCameraService events), 04 (IDeviceAdapter interface)

**Status:** ready-for-agent

## Acceptance criteria

- [ ] OverviewCameraAdapter forward `OnConnectionStateChanged` từ OverviewCameraService
- [ ] OverviewCameraAdapter.IsStreaming returns `_cameraService.IsStreaming`
- [ ] OverviewCameraAdapter.PingAsync ping TCP đến `_cameraService.Config.Ip:Port`
- [ ] PingAsync trả về true/false
