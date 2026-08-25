# Đặc Tả Layout Giao Diện WinForms — HPParkingSystem

Tài liệu này đặc tả chi tiết bố cục giao diện người dùng (UI Layout Wireframe), các thành phần điều khiển (UserControls), màu sắc, trạng thái hiển thị và luồng tương tác trên ứng dụng **WinForms Client** của hệ thống **HPParkingSystem**.

---

## 1. Màn Hình Chính: Giám Sát Ra/Vào Trực Tiếp (`LiveMonitorView`)

Màn hình làm việc chính của nhân viên bảo vệ trực cổng, tối ưu cho màn hình độ phân giải từ Full HD (1920x1080) trở lên.

### 1.1. Wireframe Tổng Thể

```
+-------------------------------------------------------------------------------------------------------------------+
| [LOGO] HPPARKING SYSTEM — GIÁM SÁT KIỂM SOÁT XE RA/VÀO    |  Server: [● Connected]  LPR: [● Ready]  | 22/08/2026 14:30:00 |
+-----------------------------------------------------------+-------------------------------------------------------+
|                       LÀN VÀO (ENTRY LANE)                |                    LÀN RA (EXIT LANE)                 |
| +----------------------------+--------------------------+ | +----------------------------+--------------------------+ |
| | [CAM TOÀN CẢNH - HIKVISION]| [CAM BIỂN SỐ - NST]      | | | [CAM TOÀN CẢNH - HIKVISION]| [CAM BIỂN SỐ - NST]      | |
| |                            |                          | | |                            |                          | |
| |       Live Video (16:9)    |     Live Video (16:9)    | | |       Live Video (16:9)    |     Live Video (16:9)    | |
| |                            |                          | | |                            |                          | |
| +----------------------------+--------------------------+ | +----------------------------+--------------------------+ |
| | KẾT QUẢ NHẬN DIỆN XE VÀO GẦN NHẤT                     | | | KẾT QUẢ NHẬN DIỆN XE RA GẦN NHẤT                      | |
| | +----------------+  Biển số: [ 29A - 888.88 ] (98%)   | | | +----------------+  Biển số: [ 30E - 123.45 ] (95%)   | |
| | | Ảnh cắt biển số|  Thời gian: 22/08/2026 14:28:10    | | | | Ảnh cắt biển số|  Thời gian: 22/08/2026 14:29:15    | |
| | | (Crop In-Mem)  |  Chủ xe: Nguyễn Văn An             | | | | (Crop In-Mem)  |  Chủ xe: Trần Thị Bình (Khách)   | |
| | +----------------+  Đơn vị: Cán bộ Nhân viên          | | | +----------------+  Đơn vị: Công ty Xây Dựng ABC      | |
| +-------------------------------------------------------+ | +-------------------------------------------------------+ |
+-----------------------------------------------------------+-------------------------------------------------------+
| BẢNG TRẠNG THÁI THIẾT BỊ NGOẠI VI:                                                                                |
| [Radar Vào: ● OK]  [Radar Ra: ● OK]  [C3-200: ● OK]  [Cam Hik 1: ● OK]  [Cam NST 1: ● OK]  [MongoDB: ● OK]       |
+-------------------------------------------------------------------------------------------------------------------+
| LỊCH SỬ 10 LƯỢT XE GẦN ĐÂY NHẤT (Data Grid View):                                                                 |
| | STT | Thời gian           | Làn   | Biển số       | Loại xe | Đối tượng         | Độ tin cậy | Trạng thái |         |
| | 01  | 22/08/2026 14:29:15 | Ra    | 30E - 123.45  | Ô tô 4c | Khách vãng lai    | 95%        | Đã ghi nhận|         |
| | 02  | 22/08/2026 14:28:10 | Vào   | 29A - 888.88  | Ô tô 7c | Cán bộ nhân viên  | 98%        | Đã ghi nhận|         |
+-------------------------------------------------------------------------------------------------------------------+
```

### 1.2. Phân Rã Các Vùng Chức Năng (Layout Zones)

1. **Header Zone (Thanh tiêu đề & Hệ thống)**:
   - Tên hệ thống và phiên bản.
   - Trạng thái kết nối Server trung tâm MongoDB, LPR Windows Service.
   - Đồng hồ thời gian thực (Giờ:Phút:Giây Ngày/Tháng/Năm).
   - Nút mở màn hình Cấu hình thiết bị & Tra cứu lịch sử.
2. **Lane Dual Monitor Zone (Khu vực giám sát 2 làn song song)**:
   - Chia thành 2 cột cân đối: **Làn Vào (Trái)** và **Làn Ra (Phải)**.
   - Mỗi làn chứa:
     - **Video Panel**: 2 màn hình phụ (1 cho Cam Hikvision toàn cảnh, 1 cho Cam NST soi biển số). Tự động hiển thị khung viền xanh khi cảm biến kích hoạt chụp.
     - **Plate Result Card**: Hiển thị ảnh chụp crop biển số, Biển số to rõ ràng (Font chữ đậm, dễ đọc từ xa), Thanh tiến trình hoặc phần trăm độ tin cậy OCR (Confidence Score), Thông tin đối tượng (Tên chủ xe, đơn vị/nhà thầu).
3. **Hardware Health Status Zone (Thanh trạng thái thiết bị)**:
   - Đèn LED trạng thái màu (Xanh: Bình thường, Đỏ: Mất kết nối, Vàng: Cảnh báo).
   - Hiển thị từng thiết bị: Controller ZKTeco C3-200, Radar Vào/Ra, Camera Hikvision Vào/Ra, Camera NST Vào/Ra, Dịch vụ LPR Service x64, Cơ sở dữ liệu MongoDB.
4. **Recent Events DataGridView Zone (Lưới dữ liệu sự kiện gần đây)**:
   - Bảng hiển thị 10 sự kiện vào/ra gần nhất. Tự động cuộn và cập nhật khi có xe mới đi qua.

---

## 2. Màn Hình Quản Lý & Kiểm Tra Thiết Bị (`DeviceStatusView`)

Dành cho kỹ thuật viên kiểm tra phần cứng hoặc cấu hình lại thông số khi cần:

```
+---------------------------------------------------------------------------------------+
| CẤU HÌNH & KIỂM TRA THIẾT BỊ NGOẠI VI                                                |
+---------------------------------------------------------------------------------------+
|  THIẾT BỊ             | ĐỊA CHỈ IP / CỔNG | TRẠNG THÁI | THAO TÁC                     |
|  ------------------------------------------------------------------------------------  |
|  ZKTeco Controller    | 192.168.1.201:4370| [● Đang nối]| [Ping] [Đọc Log] [Re-connect] |
|  Cam Hik Toàn Cảnh (In)| 192.168.1.101:8000| [● Đang nối]| [Xem Thử] [Chụp Test]        |
|  Cam NST Biển Số (In) | 192.168.1.102:3000| [● Đang nối]| [Xem Thử] [Chụp Test]        |
|  Cam Hik Toàn Cảnh(Out)| 192.168.1.103:8000| [● Đang nối]| [Xem Thử] [Chụp Test]        |
|  Cam NST Biển Số (Out)| 192.168.1.104:3000| [● Đang nối]| [Xem Thử] [Chụp Test]        |
|  LPR Windows Service  | 127.0.0.1:5000    | [● Đang nối]| [Gửi Ảnh Test OCR]           |
|  MongoDB Server       | 192.168.1.100:27017|[● Đang nối]| [Kiểm tra DB]               |
+---------------------------------------------------------------------------------------+
```

---

## 3. Quy Chuẩn Màu Sắc & Trải Nghiệm Người Dùng (UX/UI Rules)

- **Màu nền**: Xám trung tính (`#2B2B2B` hoặc `#F0F2F5`) giúp bảo vệ không bị mỏi mắt khi trực ca dài.
- **Màu nhận diện biển số**:
  - Độ tin cậy $\ge 85\%$: Nền xanh lá nhạt (`#E8F5E9`), chữ xanh đậm (`#2E7D32`).
  - Độ tin cậy $< 85\%$ hoặc biển số chưa rõ: Nền vàng cam (`#FFF3E0`), chữ cam đậm (`#E65100`).
- **Phím tắt hỗ trợ bảo vệ**:
  - `F1`: Chụp cưỡng bức Làn Vào (Manual Trigger Entry).
  - `F2`: Chụp cưỡng bức Làn Ra (Manual Trigger Exit).
  - `F5`: Làm mới trạng thái kết nối thiết bị.
  - `Esc`: Đóng các cửa sổ popup / dialog.
