# 09: FrmMain Integration - Basic Setup

**What to build:** Tích hợp DeviceHealthManager vào FrmMain. Đăng ký cameras và controller, subscribe state change events, thay thế AutoConnectAllAsync để dùng DeviceHealthManager.

**Blocked by:** 08 (DeviceHealthManager)

**Status:** ready-for-agent

## Acceptance criteria

- [ ] FrmMain khởi tạo DeviceHealthManager
- [ ] LoadConfigurationsFromDbAsync đăng ký devices với DeviceHealthManager
- [ ] AutoConnectAllAsync gọi DeviceHealthManager.ConnectAllAsync()
- [ ] FrmMain subscribe OnStateChanged event
- [ ] HandleDeviceStateChanged cập nhật camera state variables
- [ ] InvalidateCameraPanels() được gọi khi state thay đổi
