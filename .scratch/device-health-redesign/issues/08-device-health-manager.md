# 08: Create DeviceHealthManager

**What to build:** Tạo DeviceHealthManager - class trung tâm quản lý health check. Chứa timer ping định kỳ (30s), retry logic với exponential backoff, và fire events khi state thay đổi. Đây là core của kiến trúc mới.

**Blocked by:** 05 (PlateCameraAdapter), 06 (OverviewCameraAdapter), 07 (ZKTecoAdapter)

**Status:** ready-for-agent

## Acceptance criteria

- [ ] DeviceHealthManager có interface IDeviceHealthManager
- [ ] Có RegisterDevice/UnregisterDevice methods
- [ ] Có ConnectAsync/DisconnectAsync/RestartAsync methods
- [ ] Có StartPreviewAsync/StopPreviewAsync methods
- [ ] Có StartHealthCheck(interval) với internal timer
- [ ] Health check ping mỗi 30 giây
- [ ] Retry logic: 3 lần với delays 1s, 2s, 4s
- [ ] Fire OnStateChanged event khi state thay đổi
- [ ] Auto-reconnect khi ping success sau khi offline
- [ ] Stop LiveView khi ping fail sau retries
