# 07: ZKTecoDeviceAdapter - Implement PingAsync & IsStreaming

**What to build:** Implement PingAsync và IsStreaming property trong ZKTecoDeviceAdapter. IsStreaming = đang nhận ReadLog từ controller.

**Blocked by:** 04 (IDeviceAdapter interface)

**Status:** ready-for-agent

## Acceptance criteria

- [ ] ZKTecoDeviceAdapter.IsStreaming = true khi đang nhận log, false khi không
- [ ] ZKTecoDeviceAdapter.PingAsync ping TCP đến controller IP:Port (mặc định 4370)
- [ ] Có event OnConnectionStateChanged cho state changes
