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
IP: 192.168.1.10             IP: 192.168.1.21                 IP: 192.168.1.101 (Cam BS Vào)  IP: 192.168.1.103 (Cam BS Ra)
- MongoDB Database           - WinForms App                   IP: 192.168.1.102 (Cam TC Vào)  IP: 192.168.1.104 (Cam TC Ra)
- Web API + Web Admin        - Kết nối Camera & Barrier       IP: 192.168.1.201 (Controller)  IP: 192.168.1.202 (Controller)
- Ổ D:\Captures (SMB Share)  - Lưu ảnh vào \\192.168.1.10     IP: 192.168.1.221 (Radar Vào)   IP: 192.168.1.222 (Radar Ra)
```

### Bảng Quy Hoạch IP Tĩnh (Static IP Allocation)

| Thiết bị / Máy tính | Địa chỉ IP Tĩnh | Cổng dịch vụ (Port) | Chức năng |
|----------------------|-----------------|---------------------|-----------|
| **Router / Gateway** | `192.168.1.1` | - | Bộ định tuyến trung tâm |
| **Máy Chủ Vật Lý (Server)** | `192.168.1.10` | 27017 (Mongo), 5005 (Web), 445 (SMB) | Chạy CSDL, Web Admin và lưu trữ ảnh |
| **Máy Bốt 1 (Vào/Ra)** | `192.168.1.21` | - | Máy tính bảo vệ điều khiển làn |
| **Máy Bốt 2 (Dự phòng/Cổng phụ)** | `192.168.1.22` | - | Máy tính bảo vệ cổng 2 (nếu có) |
| **Camera Biển Số Làn Vào** | `192.168.1.101` | 8000 (Hikvision SDK) | Đọc biển số xe vào |
| **Camera Toàn Cảnh Làn Vào** | `192.168.1.102` | 8000 | Chụp toàn cảnh xe & tài xế vào |
| **Camera Biển Số Làn Ra** | `192.168.1.103` | 8000 | Đọc biển số xe ra |
| **Camera Toàn Cảnh Làn Ra** | `192.168.1.104` | 8000 | Chụp toàn cảnh xe & tài xế ra |
| **Bộ Điều Khiển Barrier (Controller)** | `192.168.1.201` | 4370 (ZKTeco TCP) | Kích đóng/mở barrier |
| **Radar Phát Hiện Xe** | `192.168.1.221` | Cổng I/O hoặc Aux | Kích hoạt chụp ảnh tự động |
| **Dải DHCP phụ (Wifi nội bộ)** | `192.168.1.230 - 254` | - | Dành riêng cho điện thoại/laptop kỹ thuật |

---

## 2. Cấu Hình Trên Máy Chủ Vật Lý (Central Server)

### Bước 2.1: Cấu hình CSDL MongoDB Server
1. Cài đặt MongoDB Community Server phiên bản 6.0 hoặc 7.0 (chọn chạy dạng Windows Service).
2. Mở file cấu hình `C:\Program Files\MongoDB\Server\X.X\bin\mongod.cfg`:
   ```yaml
   # Cho phép lắng nghe từ tất cả máy trong mạng LAN
   net:
     port: 27017
     bindIp: 0.0.0.0
   
   # Bật xác thực bảo mật
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

### Bước 2.2: Tạo Thư Mục Lưu Trữ Ảnh & Chia Sẻ Mạng (Windows SMB / UNC Path)
1. Trên máy chủ, tạo thư mục lưu ảnh chuyên dụng trên ổ đĩa dữ liệu (không nên lưu ổ C hệ điều hành):
   - Đường dẫn: `D:\Captures`
2. Chia sẻ thư mục qua mạng LAN:
   - Chuột phải thư mục `D:\Captures` -> Chọn **Properties** -> Tab **Sharing** -> Bấm **Advanced Sharing**.
   - Tích chọn **Share this folder**, đặt Share name là `Captures`.
   - Bấm **Permissions** -> Cấp quyền **Full Control** (hoặc Read + Change) cho tài khoản Windows sử dụng chung trong mạng LAN (hoặc tạo user riêng tên `parking_client` / `Everyone` trong LAN cô lập).
3. Kiểm tra từ máy bốt: Mở File Explorer trên máy bốt gõ `\\192.168.1.10\Captures` và thử tạo một file text để kiểm tra quyền đọc/ghi.

---

### Bước 2.3: Cấu Hình Web API Bằng Lệnh CLI (Không Cần Sửa File `appsettings.json`)

Hệ thống .NET 8 hỗ trợ ghi đè mọi cấu hình thông qua **tham số dòng lệnh (CLI Arguments)** với độ ưu tiên cao nhất, giúp kỹ thuật viên không cần mở file `appsettings.json` tránh rủi ro gõ sai định dạng JSON.

#### Lựa chọn Cổng Web API / Web Admin:
- **Cổng 80 (Khuyên dùng 100% trong LAN)**:
  - Đây là cổng HTTP tiêu chuẩn của trình duyệt web.
  - **Lợi ích**: Mọi người trong mạng LAN (kế toán, bảo vệ, quản lý) chỉ cần mở Chrome/Edge gõ thẳng địa chỉ IP:
    👉 `http://192.168.1.10` (rất dễ nhớ, không cần phải gõ thêm số cổng `:5005` hay `:8080`).
- **Cổng 5005**: Dùng nếu trên máy chủ cổng 80 đã bị ứng dụng khác sử dụng (truy cập qua `http://192.168.1.10:5005`).

#### Lệnh Chạy Web API Trực Tiếp Bằng CLI:
```cmd
PhuXuanParkingSystem.Api.exe --urls "http://0.0.0.0:80" --CapturesFolder "D:\Captures" --ConnectionStrings:MongoDb "mongodb://phuxuan_app:SecretPassword2026!@127.0.0.1:27017/PhuXuanParkingSystemDb?authSource=admin"
```

---

### Bước 2.4: Đóng Gói Web Admin & Cài Đặt Windows Service Bằng Lệnh CLI (Tự Động 100%)

Để Web API tự động phục vụ Web Admin và tự khởi động cùng Windows 24/7, thực hiện hoàn toàn bằng các lệnh CLI:

1. **Build và copy Web Admin vào Web API (chạy 1 lần trên máy build)**:
   ```cmd
   cd PhuXuanParkingSystem.Web
   npm run build
   xcopy /E /I /Y dist ..\PhuXuanParkingSystem.Api\wwwroot
   ```

2. **Cài đặt Web API làm Windows Service bằng lệnh CLI (sử dụng NSSM)**:
   Mở Command Prompt (Admin) và chạy lần lượt các lệnh sau:
   ```cmd
   :: 1. Tạo dịch vụ trỏ vào file exe
   nssm install PhuXuanParkingApi "D:\PhuXuanServer\PhuXuanParkingSystem.Api.exe"

   :: 2. Đặt thư mục làm việc
   nssm set PhuXuanParkingApi AppDirectory "D:\PhuXuanServer"

   :: 3. Truyền toàn bộ cấu hình vào Service qua CLI (Cổng 80, Đường dẫn ảnh, CSDL)
   nssm set PhuXuanParkingApi AppParameters "--urls http://0.0.0.0:80 --CapturesFolder D:\Captures --ConnectionStrings:MongoDb mongodb://phuxuan_app:SecretPassword2026!@127.0.0.1:27017/PhuXuanParkingSystemDb?authSource=admin"

   :: 4. Cấu hình tự động khởi động cùng Windows
   nssm set PhuXuanParkingApi Start SERVICE_AUTO_START

   :: 5. Khởi động dịch vụ ngay lập tức
   nssm start PhuXuanParkingApi
   ```

3. **Khi cần thay đổi cấu hình (ví dụ đổi cổng sang 5005 hoặc đổi mật khẩu DB)**:
   Chỉ cần gõ 1 lệnh CLI cập nhật tham số và restart dịch vụ:
   ```cmd
   nssm set PhuXuanParkingApi AppParameters "--urls http://0.0.0.0:5005 --CapturesFolder D:\Captures --ConnectionStrings:MongoDb mongodb://phuxuan_app:NewPassword!@127.0.0.1:27017/PhuXuanParkingSystemDb?authSource=admin"
   nssm restart PhuXuanParkingApi
   ```

---

### Bước 2.5: Mở Cổng Tường Lửa (Windows Firewall) Trên Máy Chủ
Chạy lệnh PowerShell (Admin) trên máy chủ:
```powershell
# 1. Cổng MongoDB
New-NetFirewallRule -DisplayName "PhuXuan_MongoDB" -Direction Inbound -LocalPort 27017 -Protocol TCP -Action Allow

# 2. Cổng Web API & Web Admin
New-NetFirewallRule -DisplayName "PhuXuan_Web_API" -Direction Inbound -LocalPort 5005 -Protocol TCP -Action Allow

# 3. Cổng Windows File Sharing (SMB)
New-NetFirewallRule -DisplayName "PhuXuan_SMB_Share" -Direction Inbound -LocalPort 445 -Protocol TCP -Action Allow
```

---

## 3. Cấu Hình Trên Các Máy Bốt Kiểm Soát (Lane Client PCs)

Trên mỗi máy tính bốt bảo vệ chạy ứng dụng WinForms:
1. Đặt IP tĩnh cho máy bốt: `192.168.1.21`, Subnet: `255.255.255.0`, Gateway: `192.168.1.1`.
2. Mở file cấu hình `PhuXuanParkingSystem.exe.config` (hoặc `App.config` trước khi build):
   ```xml
   <?xml version="1.0" encoding="utf-8" ?>
   <configuration>
       <appSettings>
           <add key="LogLevel" value="Warning" />
           <add key="Log_Path" value="Logs/app-.log" />
           
           <!-- ================= LƯU TRỮ ẢNH TẬP TRUNG TRÊN SERVER ================= -->
           <!-- Trỏ trực tiếp vào thư mục chia sẻ mạng UNC của Máy Chủ -->
           <add key="CaptureSavePath" value="\\192.168.1.10\Captures" />
           
           <!-- Thư mục lưu tạm cục bộ khi mất mạng LAN máy chủ -->
           <add key="CaptureFallbackLocalPath" value="C:\Captures_Local_Temp" />
           
           <!-- ================= KẾT NỐI CSDL MONGODB MÁY CHỦ ================= -->
           <add key="MongoDb_ConnectionString" value="mongodb://phuxuan_app:SecretPassword2026!@192.168.1.10:27017/PhuXuanParkingSystemDb?authSource=admin&amp;connectTimeoutMS=5000" />
           <add key="MongoDb_DatabaseName" value="PhuXuanParkingSystemDb" />
           
           <!-- ================= ANPR TỐI ƯU ================= -->
           <add key="Anpr_MaxImageWidth" value="1280" />
       </appSettings>
   </configuration>
   ```

---

## 4. Cơ Chế Chống Chịu Lỗi Mạng LAN (Offline Fallback & Auto-Sync)

Để bốt kiểm soát không bao giờ bị nghẽn làn khi mạng LAN hoặc máy chủ khởi động lại:
1. **Khi xe vào/ra chụp ảnh**:
   - WinForms kiểm tra đường dẫn `\\192.168.1.10\Captures`.
   - Nếu kết nối bình thường: Lưu trực tiếp vào máy chủ `\\192.168.1.10\Captures\YYYY-MM-DD\...`.
   - Nếu mạng LAN bị ngắt (Timeout > 1 giây): Hệ thống tự động ghi ảnh vào `C:\Captures_Local_Temp\YYYY-MM-DD\...` tại máy bốt và cho phép mở barrier bình thường.
2. **Tiến trình đồng bộ ngầm (Background Worker)**:
   - Định kỳ 1-2 phút, WinForms kiểm tra nếu có ảnh trong `C:\Captures_Local_Temp` và mạng LAN đã thông lại thì tự động di chuyển ảnh về `\\192.168.1.10\Captures` và cập nhật lại đường dẫn trong CSDL.

---

## 5. Chính Sách Bảo Trì Dung Lượng Ổ Cứng Định Kỳ

- **Dung lượng ước tính**: 1 lượt xe ~ 400KB - 500KB ảnh (ảnh nén JPEG chất lượng 80%).
  - 1.000 lượt xe/ngày = ~500 MB/ngày = ~15 GB/tháng = ~180 GB/năm.
  - Với ổ cứng HDD 1TB-2TB, máy chủ có thể lưu trữ lịch sử ảnh từ **3 đến 5 năm** liên tục.
- **Tiến trình tự động dọn dẹp (Background Cleanup Service)**:
  - Cấu hình trên API chạy định kỳ vào 02:00 sáng mỗi ngày, tự động quét và xóa các thư mục ảnh cũ hơn thời hạn quy định (mặc định 365 ngày).
- **Sao lưu CSDL MongoDB (Backup Script)**:
  - Tạo một file batch script `backup_mongo.bat` trên máy chủ, cấu hình Windows Task Scheduler chạy hàng tuần:
    ```cmd
    mongodump --uri="mongodb://phuxuan_app:SecretPassword2026!@127.0.0.1:27017/PhuXuanParkingSystemDb?authSource=admin" --out="D:\MongoBackups\%date:~-4,4%%date:~-10,2%%date:~-7,2%"
    ```
