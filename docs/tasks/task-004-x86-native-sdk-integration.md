# Task 004: Tích Hợp Native SDK x86 (ZKTeco + Hikvision + NST)

## 1. Mục tiêu
Xây dựng phiên bản ứng dụng chạy trên nền tảng **32-bit (x86)**, giao tiếp trực tiếp với phần cứng thông qua **Native C++ SDK** của từng hãng:
- **Bộ điều khiển C3-200**: ZKTeco Pull SDK 32-bit (`plcommpro.dll`).
- **Camera Toàn Cảnh**: Hikvision HCNetSDK 32-bit (`HCNetSDK.dll`).
- **Camera Biển Số**: NST NetLib/HISDK 32-bit (`NetLib.dll`, `HISDK.dll`, `HIPlayer.dll`).

---

## 2. Kiến trúc Clean Architecture
```
HPParkingSystem.Domain
        ▲
        │
HPParkingSystem.Application
        │  Ports: ICameraCapture, IHardwareEventListener
        ▲
        │
HPParkingSystem.Infrastructure (x86)
        ├── Adapters/
        │     ├── ZKTecoDeviceAdapter.cs (ZKTeco Pull SDK x86)
        │     ├── HikvisionCameraAdapter.cs (Hikvision HCNetSDK x86)
        │     └── NstCameraAdapter.cs (NST NetLib/HISDK x86)
        └── Native/x86/
              ├── ZKTeco/ (plcommpro.dll, ...)
              ├── Hikvision/ (HCNetSDK.dll, PlayCtrl.dll, HCNetSDKCom/...)
              └── NST/ (NetLib.dll, HISDK.dll, HIPlayer.dll, ...)
        ▲
        │
HPParkingSystem.WinForms (x86)
        └── frmMain.cs (Hiển thị Live Preview qua HWND handle & Snapshot)
```

---

## 3. Danh sách công việc (Checklist)
- [ ] **Giai đoạn 1**: Cấu hình các dự án .NET sang `<PlatformTarget>x86</PlatformTarget>` và copy trọn bộ DLL 32-bit vào thư mục Output.
- [ ] **Giai đoạn 2**: Cập nhật ZKTeco Native P/Invoke cho x86 và kiểm thử kết nối Controller C3-200.
- [ ] **Giai đoạn 3**: Tích hợp Hikvision `HCNetSDK` (Login, RealPlay trực tiếp lên Panel Handle, Capture JPEG).
- [ ] **Giai đoạn 4**: Tích hợp NST `NetLib/HISDK` (Login, Live Preview trực tiếp lên Panel Handle, Capture JPEG).
- [ ] **Giai đoạn 5**: Hoàn thiện `frmMain` x86, kiểm thử hiển thị Live View và tính năng Chụp ảnh tự động / thủ công.
