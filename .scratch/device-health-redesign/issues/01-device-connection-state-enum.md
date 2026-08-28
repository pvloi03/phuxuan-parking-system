# 01: DeviceConnectionState Enum

**What to build:** Tạo enum mới `DeviceConnectionState` với 5 giá trị: Disconnected, Connecting, Connected, Streaming, Error. Enum này là nền tảng cho toàn bộ state machine.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

## Acceptance criteria

- [ ] Enum được tạo trong `PhuXuanParkingSystem.Domain/Models/Enums/`
- [ ] Có đủ 5 giá trị: Disconnected=0, Connecting=1, Connected=2, Streaming=3, Error=-1
- [ ] Enum được reference từ Camera Services và DeviceHealth layer
