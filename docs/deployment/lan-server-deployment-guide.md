# Hướng Dẫn Triển Khai Hệ Thống Bãi Xe Trên Mạng LAN & Máy Chủ Vật Lý

Tài liệu thiết kế và hướng dẫn từng bước cấu hình triển khai thực tế hệ thống **Phú Xuân Parking System** trên mô hình máy chủ vật lý và các máy bốt kiểm soát trong mạng LAN nội bộ (Offline 100%).

---

## 1. Sơ Đồ Kiến Trúc Mạng LAN Nội Bộ

```
                                [ Switch Trung Tâm / Router LAN ]
                               (Dải mạng: 192.168.1.0/24 - Subnet 255.255.255.0)
                                      |
       +------------------------------+-------------------------------+------------------------------+
       |                              |                               |                              |
[ MÁY CHỦ VẬT LÝ ]           [ MÁY BỐT 1 (CỔNG CHÍNH) ]       [ THIẾT BỊ LÀN 1 ]             [ THIẾT BỊ LÀN 2 ]
IP: 192.168.1.254            IP: 192.168.1.21                 IP: 192.168.1.101 (Cam BS Vào)  IP: 192.168.1.103 (Cam BS Ra)
- CSDL MongoDB (Port 27017)  - WinForms App (Làn xe)          IP: 192.168.1.102 (Cam TC Vào)  IP: 192.168.1.104 (Cam TC Ra)
- Web API + Web Admin SPA    - Kết nối Camera & Controller    IP: 192.168.1.201 (Controller)  IP: 192.168.1.202 (Controller)
  (Tiến trình đơn: Cổng 80)  - Lưu ảnh vào máy chủ:           IP: 192.168.1.221 (Radar Vào)   IP: 192.168.1.222 (Radar Ra)
- Ổ đĩa D:\Captures (SMB)      \\192.168.1.254\Captures
```

### Bảng Quy Hoạch IP Tĩnh (Static IP Allocation)

| Thiết bị / Máy tính | Địa chỉ IP Tĩnh | Cổng dịch vụ (Port) | Chức năng |
|----------------------|-----------------|---------------------|-----------|
| **Router / Gateway** | `192.168.1.1` | - | Bộ định tuyến trung tâm |
| **Máy Chủ Vật Lý (Server)** | `192.168.1.254` | **80** (HTTP Web Admin/API), **27017** (MongoDB), **445** (SMB) | Chạy CSDL, Web Admin và lưu trữ ảnh |
| **Máy Bốt 1 (Vào/Ra)** | `192.168.1.21` | - | Máy tính bảo vệ điều khiển làn 1 |
| **Máy Bốt 2 (Dự phòng/Cổng 2)**| `192.168.1.22` | - | Máy tính bảo vệ cổng 2 |
| **Camera Biển Số Làn Vào** | `192.168.1.101` | 8000 (Hikvision SDK) | Đọc biển số xe vào |
| **Camera Toàn Cảnh Làn Vào** | `192.168.1.102` | 8000 | Chụp toàn cảnh xe & tài xế vào |
| **Camera Biển Số Làn Ra** | `192.168.1.103` | 8000 | Đọc biển số xe ra |
| **Camera Toàn Cảnh Làn Ra** | `192.168.1.104` | 8000 | Chụp toàn cảnh xe & tài xế ra |
| **Bộ Điều Khiển Barrier (Controller)** | `192.168.1.201` | 4370 (ZKTeco TCP) | Kích đóng/mở barrier làn vào |
| **Bộ Điều Khiển Barrier Làn Ra** | `192.168.1.202` | 4370 | Kích đóng/mở barrier làn ra |
| **Dải DHCP phụ (Wifi nội bộ)** | `192.168.1.230 - 250` | - | Dành riêng cho điện thoại/laptop kỹ thuật |

---

## 2. Kiến Trúc Tiến Trình Đơn (Monolithic Hosting)

Toàn bộ hệ thống Web Admin (React 19 + Vite) được biên dịch thành HTML/CSS/JS tĩnh và nhúng trực tiếp vào thư mục `wwwroot` của ASP.NET Core Web API:

1. **Không CORS**: Cả Web Admin SPA, REST API (`/api/*`), Realtime Hub (`/hubs/parking`) và Ảnh chụp (`/captures/*`) đều phục vụ từ cùng 1 Host và Port.
2. **Cổng 80 HTTP Tiêu Chuẩn**: Người dùng trong mạng LAN chỉ cần gõ `http://192.168.1.254` trên Chrome/Edge là vào thẳng cổng quản trị, không cần nhớ số cổng.
3. **SPA Fallback & Bảo Vệ 404**: Mọi route giao diện React Router (F5 hoặc truy cập trực tiếp) đều fallback về `index.html`. Riêng các truy cập lỗi vào `/api`, `/captures`, `/hubs` được bảo vệ trả về đúng mã lỗi **HTTP 404 (Not Found)**.
4. **Bảo Mật Swagger**: Trang Swagger UI được tắt tự động trong môi trường Production trên máy chủ LAN để đảm bảo an ninh tối đa.
5. **Đóng Gói Self-Contained win-x64**: Đi kèm toàn bộ .NET 8 Runtime, máy chủ không cần cài thêm bất kỳ SDK hay Runtime nào.
6. **Graceful Shutdown & Background Workers**: 
   - Tích hợp `UseWindowsService()` hỗ trợ xả sạch hàng đợi `AuditLogQueue` khi tắt/khởi động lại máy.
   - Tích hợp `CapturesCleanupBackgroundWorker` quét dọn dẹp các thư mục ảnh cũ quá hạn (mặc định 365 ngày) vào lúc 02:00 AM mỗi ngày.
7. **Nhật Ký Vận Hành (Serilog)**: Tự động cuộn theo ngày `logs/api-yyyyMMdd.log`, lưu trữ 30 ngày gần nhất và giới hạn 10MB/file.

---

## 3. Quy Trình Đóng Gói (Publish Package)

Chỉ cần chạy 1 lệnh duy nhất trên máy phát triển (Dev PC):

```powershell
.\scripts\publish-lan-server.ps1
```

**Kịch bản tự động thực hiện:**
1. Chạy `npm run build` trong `PhuXuanParkingSystem.Web`.
2. Đồng bộ toàn bộ file trong `dist/` vào `PhuXuanParkingSystem.Api\wwwroot`.
3. Chạy `dotnet publish` theo chế độ `Release`, nền tảng `win-x64`, `self-contained true` ra thư mục `publish\server\`.
4. Copy sẵn bộ script cài đặt máy chủ (`setup-lan-server.ps1`, `uninstall-lan-server.ps1`) vào `publish\server\scripts\`.

---

## 4. Quy Trình Cài Đặt Trên Máy Chủ Vật Lý (Server Provisioning)

### Bước 4.1: Cài đặt MongoDB Server
1. Cài đặt MongoDB Community Server (chọn cài đặt dạng Windows Service, tên dịch vụ: `MongoDB`).
2. Mở file `C:\Program Files\MongoDB\Server\X.X\bin\mongod.cfg`:
   ```yaml
   net:
     port: 27017
     bindIp: 0.0.0.0
   security:
     authorization: enabled
   ```
3. Tạo tài khoản ứng dụng trong `mongosh`:
   ```javascript
   use admin;
   db.createUser({
     user: "phuxuan_app",
     pwd: "SecretPassword2026!",
     roles: [
       { role: "readWrite", db: "PhuXuanParkingSystemDb" },
       { role: "dbAdmin", db: "PhuXuanParkingSystemDb" }
     ]
   });
   ```
4. Khởi động lại dịch vụ MongoDB trên Windows Services (`services.msc`).

---

### Bước 4.2: Tạo Thư Mục Captures & SMB Share Thủ Công
1. Tạo thư mục vật lý lưu ảnh trên máy chủ (Khuyên dùng `D:\Captures`).
2. Chia sẻ thư mục qua mạng LAN (Properties -> Sharing -> Advanced Sharing):
   - Đặt tên Share: `Captures` (đường dẫn mạng: `\\192.168.1.254\Captures`).
   - Cấp quyền đọc/ghi (Full Control hoặc Read/Change) cho các máy bốt làn xe.

---

### Bước 4.3: Cài Đặt Web API & Windows Service (1 Click)

1. Sao chép toàn bộ thư mục `publish\server\` sang máy chủ (Ví dụ: `D:\PhuXuanServer`).
2. Mở **PowerShell (Run as Administrator)** tại thư mục `D:\PhuXuanServer\scripts\`.
3. Chạy lệnh cài đặt:

```powershell
.\setup-lan-server.ps1
```

*(Bạn có thể chạy trực tiếp `.\setup-lan-server.ps1` để được script hướng dẫn từng bước tương tác, hoặc truyền trước tham số: `.\setup-lan-server.ps1 -CapturesDir "D:\Captures" -Port 80`).*

**Script `setup-lan-server.ps1` sẽ tự động xử lý linh hoạt:**
- ✅ **Yêu cầu tự nhập thư mục ảnh**: Script sẽ yêu cầu bạn nhập trực tiếp đường dẫn thư mục ảnh (`FolderPath`) trên máy chủ (Ví dụ: `D:\Captures`, `E:\Captures`), hoàn toàn không đoán trước hay hardcode.
- ✅ **Phát hiện xung đột cổng thông minh**: Nếu cổng mặc định (80) đang bị dịch vụ khác (như IIS/W3SVC) chiếm dụng, script sẽ cảnh báo chi tiết tên tiến trình và cho phép bạn **chọn nhập cổng khác ngay lập tức** (Ví dụ: 8080, 5000...) hoặc thử lại.
- ✅ **Tự động mở Windows Firewall**: Tự động mở đúng cổng mạng vừa chọn (80 hoặc cổng mới bạn đã đổi).
- ✅ **Thiết lập Biến Môi Trường cấp hệ thống (Machine)**: Nạp tự động vào `builder.Configuration`.
- ✅ **Đăng ký Windows Service `PhuXuanParkingApi`**: Tự cấu hình tham số `--urls http://0.0.0.0:<Port>` và `--CapturesSettings:FolderPath "<FolderPath>"`, tự phục hồi sau 10 giây nếu gặp sự cố.
- ✅ **Khởi động dịch vụ và kiểm tra trạng thái HTTP**: Xác nhận kết nối và in ra đường link truy cập mạng LAN cho ban quản lý.

---

### Bước 4.3: (Tùy Chọn) Quản Lý Qua NSSM (Non-Sucking Service Manager)

Nếu kỹ thuật viên muốn dùng công cụ giao diện GUI hoặc bọc qua NSSM:
```cmd
:: Cài đặt dịch vụ qua NSSM
nssm install PhuXuanParkingApi "D:\PhuXuanServer\PhuXuanParkingSystem.Api.exe"
nssm set PhuXuanParkingApi AppDirectory "D:\PhuXuanServer"
nssm set PhuXuanParkingApi AppParameters "--urls http://0.0.0.0:80 --CapturesSettings:FolderPath D:\Captures"
nssm set PhuXuanParkingApi Start SERVICE_AUTO_START
nssm set PhuXuanParkingApi DependOnService MongoDB

:: Khởi động dịch vụ
nssm start PhuXuanParkingApi
```

---

## 5. Cấu Hình Trên Các Máy Bốt Kiểm Soát (WinForms Client PCs)

Trên mỗi máy bốt bảo vệ:
1. Đặt IP tĩnh: `192.168.1.21`, Subnet: `255.255.255.0`, Gateway: `192.168.1.1`.
2. Thiết lập Biến Môi Trường bằng PowerShell (Admin) hoặc file `App.config`:
   ```powershell
   [Environment]::SetEnvironmentVariable("MongoDb_ConnectionString", "mongodb://phuxuan_app:SecretPassword2026!@192.168.1.254:27017/PhuXuanParkingSystemDb?authSource=admin&connectTimeoutMS=5000", "Machine")
   [Environment]::SetEnvironmentVariable("CaptureSavePath", "\\192.168.1.254\Captures", "Machine")
   ```
3. Máy bốt tự động ghi ảnh trực tiếp vào máy chủ qua đường dẫn mạng `\\192.168.1.254\Captures\YYYY-MM-DD\...`.
4. Nếu mạng LAN bị sự cố tạm thời, WinForms tự động ghi ảnh vào bộ nhớ đệm cục bộ (`OfflineCaptures/`) và đồng bộ ngầm khi mạng thông lại.

---

## 6. Quy Trình Nâng Cấp & Cập Nhật Hệ Thống (Maintenance & Updates)

### Trường Hợp 1: Chỉ cập nhật giao diện Web Admin
1. Trên máy dev: Chạy `npm run build` trong `PhuXuanParkingSystem.Web`.
2. Copy toàn bộ nội dung trong `dist/` đè vào thư mục `D:\PhuXuanServer\wwwroot\` trên máy chủ.
3. Người dùng trên trình duyệt chỉ cần ấn F5 hoặc Ctrl+F5 để nhận giao diện mới ngay lập tức (không cần dừng Web API).

### Trường Hợp 2: Cập nhật toàn diện (Web API + Web Admin)
1. Trên máy chủ, mở PowerShell (Admin):
   ```powershell
   Stop-Service PhuXuanParkingApi
   ```
2. Copy đè toàn bộ thư mục release mới vào `D:\PhuXuanServer`.
3. Bật lại dịch vụ:
   ```powershell
   Start-Service PhuXuanParkingApi
   ```

### Gỡ Bỏ Dịch Vụ (Khi Cần Di Dời Hoặc Cài Lại)
```powershell
.\scripts\uninstall-lan-server.ps1
```
*(Script sẽ dừng và xóa service sạch sẽ, dữ liệu CSDL MongoDB và ảnh chụp trong `D:\Captures` vẫn được bảo toàn nguyên vẹn).*
