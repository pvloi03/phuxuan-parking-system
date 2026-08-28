# 11: FrmDeviceMonitor Enhancement

**What to build:** Nâng cấp FrmDeviceMonitor để hiển thị real-time status từ DeviceHealthManager. Thêm restart buttons, last check time, countdown timer.

**Blocked by:** 08 (DeviceHealthManager)

**Status:** ready-for-agent

## Acceptance criteria

- [ ] FrmDeviceMonitor subscribe OnStateChanged events
- [ ] DataGridView hiển thị real-time status của mỗi device
- [ ] Icon màu: 🟢 Connected/Streaming, 🟡 Connecting, 🔴 Disconnected/Error
- [ ] Mỗi row có nút Restart
- [ ] Footer hiển thị: "Last check: HH:mm:ss | Next check: HH:mm:ss | Interval: 30s"
- [ ] Manual refresh button
