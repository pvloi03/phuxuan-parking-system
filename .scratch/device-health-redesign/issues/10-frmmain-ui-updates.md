# 10: FrmMain UI - Enhanced Status Display

**What to build:** Cập nhật FrmMain.Cameras.cs để hiển thị đúng trạng thái theo DeviceConnectionState. Panel border colors, status text, restart buttons.

**Blocked by:** 09 (FrmMain Integration)

**Status:** ready-for-agent

## Acceptance criteria

- [ ] DrawVideoPanelStatus xử lý đủ 5 states: Disconnected, Connecting, Connected, Streaming, Error
- [ ] Disconnected/Error: Hiển thị overlay đỏ với text "MẤT KẾT NỐI"
- [ ] Connecting: Hiển thị overlay vàng với text "ĐANG KẾT NỐI..."
- [ ] Connected/Streaming: Border xanh, không overlay
- [ ] Mỗi panel có nút "Khởi động lại" (⟳)
- [ ] Click restart → gọi DeviceHealthManager.RestartAsync()
