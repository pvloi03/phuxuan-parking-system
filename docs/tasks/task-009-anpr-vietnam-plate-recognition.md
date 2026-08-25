# Task 009: Module Nhận Diện Biển Số Tự Động (ANPR / OCR) Chuyên Sâu Cho Biển Số Xe Việt Nam

## 1. Mục Tiêu
Triển khai hệ thống nhận dạng biển số xe tự động (**Automatic Number Plate Recognition - ANPR**) chạy 100% Offline trên mạng LAN, tốc độ cực cao (< 30ms trên CPU), đạt độ chính xác > 98% nhờ tối ưu hóa đặc thù cho các quy chuẩn biển số xe tại Việt Nam.

---

## 2. Các Cải Tiến Trọng Tâm Đã Triển Khai

### 2.1. Thuật Toán Sửa Lỗi Ngữ Nghĩa Vị Trí (Positional Semantic Correction)
Biển số Việt Nam có quy tắc cấu trúc ký tự cố định theo từng vị trí. Module tự động phát hiện và chuyển đổi các lỗi nhầm lẫn quang học kinh điển của AI:
- **Vị trí Mã Tỉnh (2 ký tự đầu - Bắt buộc là SỐ):**
  - Chuyển `O`, `D`, `Q`, `U` $\rightarrow$ `0`
  - Chuyển `I`, `L`, `T`, `J` $\rightarrow$ `1`
  - Chuyển `Z` $\rightarrow$ `2`, `E` $\rightarrow$ `3`, `A` $\rightarrow$ `4`, `S` $\rightarrow$ `5`, `G` $\rightarrow$ `6`, `B` $\rightarrow$ `8`, `P/q` $\rightarrow$ `9`
  - *Ví dụ:* `OI-H1` $\rightarrow$ `01-H1`, `SD-A1` $\rightarrow$ `50-A1`.
- **Vị trí Ký Tự Seri (Ký tự thứ 3 - Bắt buộc là CHỮ CÁI):**
  - Chuyển `8` $\rightarrow$ `B`, `0` $\rightarrow$ `D`, `1` $\rightarrow$ `L`, `5` $\rightarrow$ `S`, `2` $\rightarrow$ `Z`.
  - *Ví dụ:* `298-123.45` $\rightarrow$ `29B-123.45`.
- **Vị trí Đuôi Số (4-5 chữ số cuối - Bắt buộc là SỐ):**
  - Chuyển các chữ cái bị nhầm thành chữ số tương ứng: `5IF-I23.4S` $\rightarrow$ `51F-123.45`.

---

### 2.2. Xử Lý Đa Dạng Biển Số Việt Nam & Bộ Lọc Blacklist
- **Biển 1 Dòng (Ô tô dài, xe tải):** `51F-123.45`, `30A-999.99`, `17B-123.45`, `80A-123.45`, `51LD-123.45`, `29A-1234`.
- **Biển 2 Dòng (Xe máy, Ô tô vuông):**
  - Tự động ghép cặp dòng trên (Mã tỉnh + Seri: `17-B1`, `29-H1`, `59-X2`) và dòng dưới (`123.45`, `678.90`) theo tọa độ không gian Y/X hợp lý $\rightarrow$ `17B1-123.45`.
- **Bộ lọc Blacklist chữ thương hiệu & Decal dán xe:**
  - Tự động loại bỏ chữ quảng cáo camera góc rộng hay quét phải: `HONDA`, `YAMAHA`, `TAXI`, `MAILINH`, `GRAB`, `BE`, `AIRBLADE`, `VISION`, `LEAD`, `SH`, `WAVE`, `SIRIUS`, `INNOVA`, `VIOS`, `FORTUNER`...
  - Tự động loại bỏ số điện thoại in trên thân xe (`\d{10,}`).

---

### 2.3. Cơ Chế Cooldown 2 Lớp (Chống Rung Radar & Chống Nhận Diện Trùng Lặp)
- **Lớp 1 (Same-lane Cooldown ~1.8s):** Ngăn chặn trường hợp cảm biến Radar bị rung hoặc xe đứng lâu trong vùng quét kích hoạt chụp liên tục.
- **Lớp 2 (Duplicate-plate Cooldown ~3.0s):** Ngăn chặn việc ghi nhận lặp lại cùng một biển số trong tích tắc.

---

### 2.4. Tối Ưu Bộ Nhớ In-Memory & Tốc Độ Siêu Nhanh
- Sử dụng `SkiaSharp` giải mã byte[] và trích xuất ảnh crop biển số trong RAM (0% lock file ổ cứng, 0% rò rỉ GDI handle).
- Tích hợp Engine AI **PaddleOCR v5** chạy CPU cục bộ, độc lập đa luồng (mỗi làn 1 instance riêng, 0 lock chéo).

---

## 3. Cấu Trúc Mã Nguồn

| File | Chức năng |
| :--- | :--- |
| `PhuXuanParkingSystem/Models/ValueObjects/AnprResult.cs` | Kết quả nhận diện (`LicensePlate`, `Confidence`, `PlateCropBytes`, `ProcessTimeMs`...) |
| `PhuXuanParkingSystem/Models/ValueObjects/OcrTextBlock.cs` | Khối chữ phát hiện từ AI (`Text`, `Score`, `BoundingBox`...) |
| `PhuXuanParkingSystem/Services/ANPR/VietnamLicensePlateParser.cs` | Phân tích cấu trúc biển số VN, Positional Correction, Blacklist |
| `PhuXuanParkingSystem/Services/ANPR/IAnprService.cs` | Giao diện chuẩn cho dịch vụ ANPR |
| `PhuXuanParkingSystem/Services/ANPR/RapidOcrAnprService.cs` | Dịch vụ ANPR AI PaddleOCR v5 |
| `PhuXuanParkingSystem/Services/ANPR/AnprLaneCoordinator.cs` | Bộ điều phối nhận diện đa làn kèm Cooldown 2 lớp |
| `PhuXuanParkingSystem/FrmMain.cs` | Tự động điền biển số vào ô `txtInPlate` / `txtOutPlate` khi chụp ảnh |

---

## 4. Kết Quả Kiểm Thử (Unit Tests)
- **Tổng số test:** **89/89 Tests Passed 100%** (19 bài test ANPR mới bao gồm kiểm thử biển 1 dòng, biển 2 dòng, Positional Correction, Blacklist và Coordinator Cooldown).