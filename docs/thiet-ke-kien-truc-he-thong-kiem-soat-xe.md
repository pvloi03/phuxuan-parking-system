# Tài liệu thiết kế kiến trúc — Hệ thống kiểm soát xe ra/vào (LPR)

## 1. Tổng quan dự án

Hệ thống nhận dạng biển số ô tô tự động, kiểm soát ra/vào tại 1 làn vào và 1 làn ra, chạy hoàn toàn **local trong mạng LAN** (không phụ thuộc internet/cloud). Hệ thống ghi nhận lịch sử ra/vào theo thời gian thực, thống kê số lượt ra/vào của từng xe theo ngày. Hệ thống **không điều khiển barrier** — chỉ ghi nhận và giám sát, không tự động chặn/mở.

### 1.1. Đối tượng nghiệp vụ cần quản lý

| Đối tượng | Mô tả |
|---|---|
| Công ty | Đơn vị chủ quản, có thể có nhiều nhà thầu/khách liên quan... |
| Nhà thầu | Đơn vị/công ty đối tác có người và xe ra vào thường xuyên... |
| Làn xe | Làn vào, làn ra — mỗi làn gắn với 1 bộ thiết bị ngoại vi... |
| Thiết bị ngoại vi | Camera, access controller, cảm biến radar... |
| User | Tài khoản đăng nhập quản lý Web Admin... |
| Người dùng (Person) | Cán bộ nhân viên công ty, khách tham quan, người của nhà thầu, người lạ... |
| Vehicle | Xe (gắn với biển số nhận diện được)... |
| AccessEvent | 1 lượt xe đi qua cổng (vào hoặc ra), có ảnh, thời gian, biển số... |

### 1.2. Ba thành phần triển khai

1. **WinForm Client** (.NET Framework 4.8, build x86/32-bit) — chạy tại vị trí làn, giao tiếp trực tiếp SDK phần cứng
2. **MongoDB Server** — máy chủ vật lý lưu dữ liệu, kèm file share (SMB) lưu ảnh lịch sử ra/vào
3. **Web Admin** — quản lý toàn hệ thống, báo cáo thống kê

### 1.3. Thiết bị ngoại vi

| Thiết bị | Số lượng | Vai trò | SDK |
|---|---|---|---|
| Controller ZKTeco C3-200 | 1 bộ | Nhận tín hiệu từ radar, điều phối trigger | Pull SDK của hãng |
| Camera Hikvision (toàn cảnh) | 2 (vào + ra) | Chụp ảnh toàn cảnh xe | SDK Hikvision |
| Camera NST (soi biển số) | 2 (vào + ra) | Chụp cận cảnh biển số | SDK NST |
| Cảm biến radar | 2 (vào + ra) | Phát hiện xe đến, cắm vào controller, bắn log để trigger chụp | — |

**Ràng buộc kỹ thuật quan trọng:** WinForm Client bắt buộc build x86 (32-bit) do các SDK phần cứng (ZKTeco, Hikvision, NST) chỉ có bản 32-bit.

---

## 2. Bài toán nhận dạng biển số (LPR)

### 2.1. Quyết định thiết kế

Camera NST là IP camera thường, **không có engine LPR tích hợp sẵn** — việc nhận diện biển số phải thực hiện bằng phần mềm, dùng **PaddleOCR**.

**Vấn đề kỹ thuật:** PaddleOCR (qua PaddleInference native engine) chỉ phân phối bản **x64** chính thức. Một tiến trình .NET x86 (WinForm Client) không thể load native DLL x64 trong cùng process (`BadImageFormatException`). Vì vậy **không thể chạy PaddleOCR in-process trực tiếp trong WinForm**.

### 2.2. Giải pháp: LPR Windows Service riêng biệt

- PaddleOCR được đóng gói thành **1 Windows Service chạy ngầm** (background service, tự khởi động cùng Windows, không có UI), build x64, chạy trên **cùng máy vật lý** với WinForm Client tại mỗi làn.
- Service expose REST API nội bộ qua `http://127.0.0.1:<port>/api/v1/lpr/recognize` (HTTP + JSON, giao tiếp loopback, không qua mạng ngoài).
- WinForm Client gọi API này để lấy kết quả biển số, không cần biết bên trong dùng PaddleOCR hay engine gì khác.

**Lý do chọn REST HTTP thay vì gRPC/named pipe:**
- Đơn giản, dễ debug/test độc lập
- Không phụ thuộc công nghệ (PaddleOCR service viết Python thì REST là lựa chọn tự nhiên)
- Tần suất gọi thấp (1 xe qua = 1 request) nên không cần tối ưu hiệu năng cao như gRPC

**Pipeline nhận diện biển số (bên trong LPR Service):**
```
Ảnh chụp từ camera NST
  → Phát hiện vùng biển số (Detection model)
  → Cắt (crop) vùng biển số
  → PaddleOCR đọc ký tự trên vùng đã cắt
  → Chuẩn hóa chuỗi biển số theo định dạng VN
  → Trả kết quả JSON: { plateNumber, confidence, processingTimeMs }
```

**Lợi ích kiến trúc:** Tách LPR ra khỏi WinForm là đúng tinh thần Clean Architecture — mỗi thành phần một trách nhiệm, độc lập ngôn ngữ/công nghệ (WinForm là .NET, LPR Service có thể là Python), dễ thay đổi/nâng cấp model nhận diện sau này mà không đụng đến WinForm Client.

---

## 3. Kiến trúc hạ tầng tổng thể

### 3.1. Bốn khối thành phần

**Tại mỗi làn (vào/ra) — máy vật lý tại chỗ:**
- **WinForm Client** (.NET Fx 4.8, x86) — UI liveview 2 làn + form quản lý thiết bị cho kỹ thuật viên; giao tiếp trực tiếp SDK phần cứng (ZKTeco, Hikvision, NST)
- **LPR Windows Service** (PaddleOCR, x64, chạy ngầm) — expose REST API nội bộ qua localhost

**Máy Server trung tâm:**
- **MongoDB** — lưu toàn bộ dữ liệu nghiệp vụ
- **File Share (SMB)** — lưu ảnh lịch sử ra/vào, cả WinForm Client và Web Admin cùng truy cập

**Web Admin:**
- Chạy trong LAN, kết nối trực tiếp MongoDB + file share

### 3.2. Giao tiếp giữa các thành phần

- **WinForm Client ↔ LPR Service**: REST API qua HTTP loopback (127.0.0.1), cùng máy vật lý
- **WinForm Client ↔ MongoDB**: đọc/ghi trực tiếp qua MongoDB driver, không qua API trung gian
- **Web Admin ↔ MongoDB**: đọc/ghi trực tiếp qua MongoDB driver, không qua API trung gian
- **WinForm Client ↔ Web Admin**: không giao tiếp trực tiếp với nhau — chỉ gián tiếp qua việc cùng đọc/ghi chung MongoDB và file share ảnh

**Lưu ý thiết kế quan trọng:** Vì WinForm Client và Web Admin không có API trung gian mà cùng thao tác trực tiếp trên MongoDB, để tránh trùng lặp business logic ở 2 nơi, các rule nghiệp vụ quan trọng (chuẩn hóa biển số, tính toán 1 "lượt" ra/vào...) nên đặt trong 1 **Shared Domain Library** (class library .NET) dùng chung giữa 2 ứng dụng, thay vì viết lặp lại logic ở cả 2 phía.

### 3.3. Luồng nghiệp vụ: 1 xe đi qua cổng

1. **Radar** phát hiện xe đến → bắn tín hiệu vào **Controller ZKTeco C3-200**
2. Controller ghi nhận sự kiện (log tín hiệu xe đến)
3. **WinForm Client** poll/nhận event từ Controller (qua Pull SDK)
4. WinForm trigger chụp ảnh — gọi đồng thời **Camera Hikvision** (toàn cảnh) và **Camera NST** (biển số)
5. Camera trả ảnh về cho WinForm (2 ảnh: toàn cảnh + biển số)
6. WinForm gửi ảnh biển số sang **LPR Service** qua HTTP localhost
7. LPR Service xử lý OCR, trả về biển số + độ tin cậy (confidence)
8. WinForm xác định hướng (vào/ra theo làn), đối chiếu với danh sách Person nếu có, hiển thị lên UI liveview theo thời gian thực
9. WinForm ghi bản ghi **AccessEvent** (biển số, thời gian, hướng, đường dẫn ảnh) vào **MongoDB**, đồng thời copy ảnh vào **file share**
10. **Web Admin** đọc trực tiếp cùng MongoDB để hiển thị lịch sử ra/vào, tính số lượt/xe/ngày, xuất báo cáo thống kê

**Đặc điểm quan trọng:** Vì hệ thống không có barrier, đây là luồng **ghi nhận/giám sát thuần túy** — không có bước "quyết định cho phép hay từ chối xe đi qua". Mọi lượt xe qua cổng đều được ghi nhận.

---

## 4. Nguyên lý Clean Architecture áp dụng

### 4.1. Bốn tầng kiến trúc (từ trong ra ngoài)

Nguyên tắc cốt lõi — **The Dependency Rule**: mọi phụ thuộc chỉ được trỏ **vào trong**, không bao giờ ngược lại. Lớp trong cùng (Domain) không biết gì về MongoDB, HTTP, hay SDK phần cứng cụ thể.

| Tầng | Vai trò | Ví dụ trong dự án |
|---|---|---|
| **1. Domain** (trong cùng) | Entity và business rule thuần túy, không phụ thuộc gì cả | `Vehicle`, `Person`, `AccessEvent`, `Lane`, `Device` |
| **2. Application** | Use case cụ thể + Port (interface trừu tượng) — định nghĩa "cần gì" chứ không biết "làm bằng gì" | `RecordVehicleAccessUseCase`, `RecognizePlateUseCase`; Port: `IAccessEventRepository`, `ILicensePlateRecognizer`, `IHardwareEventListener` |
| **3. Interface Adapters** | Chuyển đổi dữ liệu giữa Use Case và thế giới bên ngoài | Controllers (nhận sự kiện từ SDK), Presenters (định dạng dữ liệu hiển thị UI hoặc trả JSON) |
| **4. Infrastructure** (ngoài cùng) | Cài đặt cụ thể — nơi **duy nhất** được phép biết về SDK, HTTP, MongoDB | `MongoAccessEventRepository`, `ZKTecoDeviceAdapter`, `HikvisionCameraAdapter`, `PaddleOcrHttpClient` |

### 4.2. Áp dụng cụ thể cho WinForm Client

Cấu trúc project WinForm Client chia thành 4 tầng:

**Domain layer**
- Entity: `Vehicle`, `AccessEvent`, `Lane`
- Không phụ thuộc bất kỳ thư viện ngoài nào (không MongoDB driver, không SDK)

**Application layer**
- Use case điều phối luồng nghiệp vụ (VD: xử lý 1 lượt xe qua cổng)
- Port (interface) trừu tượng:
  - `IHardwareEventListener` — lắng nghe sự kiện từ controller/radar
  - `ICameraCapture` — chụp ảnh (không biết là Hikvision hay NST)
  - `ILicensePlateRecognizer` — nhận diện biển số (không biết là PaddleOCR hay engine khác)
  - `IAccessEventRepository` — lưu trữ bản ghi (không biết là MongoDB hay DB khác)

**Presentation (UI) layer**
- Form liveview: hiển thị camera làn vào/ra, thông tin người và xe khi đi qua cổng
- Form quản lý thiết bị: dành cho kỹ thuật viên, cấu hình/kiểm tra thiết bị ngoại vi

**Infrastructure layer** — nơi duy nhất chứa code SDK cụ thể, mỗi adapter implement 1 Port tương ứng:
- `ZKTecoDeviceAdapter` — implement `IHardwareEventListener`, dùng Pull SDK C3-200
- `HikvisionCameraAdapter` — implement `ICameraCapture`, dùng Hikvision SDK
- `NstCameraAdapter` — implement `ICameraCapture`, dùng NST SDK
- `LprHttpClientAdapter` — implement `ILicensePlateRecognizer`, gọi HTTP tới LPR Windows Service
- `MongoAccessEventRepository` — implement `IAccessEventRepository`, dùng MongoDB driver
- `FileShareImageStore` — lưu ảnh vào file share SMB

**Composition Root** (`Program.cs` hoặc điểm khởi tạo ứng dụng):
- Nơi duy nhất "biết" Port nào nối với Adapter nào, đăng ký qua Dependency Injection

### 4.3. Lợi ích thực tế của cách tổ chức này

- **Thay đổi phần cứng dễ dàng:** Nếu sau này đổi camera Hikvision sang hãng khác, chỉ cần viết Adapter mới implement cùng `ICameraCapture`, không đụng đến Use Case hay UI.
- **Test được mà không cần phần cứng thật:** Có thể viết `FakeDeviceAdapter` implement cùng interface để giả lập radar/controller, giúp test luồng nghiệp vụ độc lập với thiết bị vật lý.
- **Đổi engine LPR không ảnh hưởng WinForm:** Vì WinForm chỉ biết `ILicensePlateRecognizer`, đổi từ gọi LPR Service (PaddleOCR) sang bất kỳ engine nào khác chỉ cần đổi `LprHttpClientAdapter`.
- **Tránh trùng lặp logic khi đổi hạ tầng lưu trữ:** Nếu sau này đổi từ MongoDB sang DB khác, chỉ cần viết Repository mới implement `IAccessEventRepository`.

---

## 5. Các quyết định thiết kế đã chốt (Decision Log)

| # | Vấn đề | Quyết định | Lý do |
|---|---|---|---|
| 1 | Loại hệ thống | Kiểm soát ra/vào bãi xe, 1 làn vào + 1 làn ra | Theo yêu cầu dự án |
| 2 | Vị trí thực hiện LPR | PaddleOCR chạy trong 1 Windows Service riêng, không in-process trong WinForm | PaddleInference chỉ có bản x64, WinForm bắt buộc x86 do ràng buộc SDK phần cứng khác → không thể chạy chung tiến trình |
| 3 | Giao tiếp WinForm ↔ LPR Service | REST API (HTTP + JSON) qua localhost | Đơn giản, dễ debug, đủ nhanh cho tần suất 1 xe/lượt, độc lập công nghệ giữa .NET và Python |
| 4 | Giao tiếp WinForm ↔ Web Admin | Không có API riêng, cả hai cùng đọc/ghi trực tiếp MongoDB | Theo yêu cầu, phù hợp quy mô 1 làn vào/1 làn ra; bù lại bằng Shared Domain Library để tránh trùng lặp logic |
| 5 | Có barrier tự động không | Không có barrier — hệ thống chỉ ghi nhận/giám sát | Theo yêu cầu dự án |
| 6 | Radar kết nối vào đâu trước | Radar → Controller ZKTeco C3-200 → WinForm nhận event | Theo cấu hình phần cứng thực tế đã lắp đặt |
| 7 | LPR Service chạy dạng gì | Windows Service chạy ngầm, tự khởi động cùng hệ thống | Đảm bảo luôn sẵn sàng, tách vòng đời khỏi WinForm Client |
| 8 | Kiến trúc phần mềm | Clean Architecture (Domain → Application → Interface Adapters → Infrastructure) | Cô lập SDK phần cứng đa dạng (ZKTeco, Hikvision, NST, PaddleOCR) khỏi business logic, dễ thay đổi/mở rộng về sau |

---

## 6. Việc còn cần thiết kế tiếp

Tài liệu này mới dừng ở mức kiến trúc tổng thể và kiến trúc WinForm Client. Các phần sau **chưa được thiết kế chi tiết**, cần các buổi làm việc tiếp theo:

- Domain model chi tiết (thuộc tính từng Entity, Value Object)
- Thiết kế MongoDB schema (Collection, Index)
- Chi tiết Use Case Application layer (sequence gọi giữa các Port)
- Kiến trúc chi tiết LPR Windows Service (pipeline detection + OCR)
- Kiến trúc Web Admin (API layer, báo cáo thống kê)
- Thiết kế Shared Domain Library dùng chung giữa WinForm và Web Admin
- Xử lý lỗi, retry, đồng bộ dữ liệu khi mất kết nối mạng/thiết bị
- Bảo mật (xác thực User đăng nhập Web Admin, phân quyền)

**Lưu ý:** Tài liệu này chỉ chứa nội dung thiết kế kiến trúc, chưa bao gồm code cài đặt cụ thể — phần logic code và tối ưu sẽ được bàn ở giai đoạn sau.
