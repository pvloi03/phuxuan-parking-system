# Task 011: Thiết Lập & Thực Thi Pipeline Kiểm Thử Toàn Diện (Unit -> Live API -> E2E Browser Testing)

## 1. Mục Tiêu (Goal)
- Đảm bảo toàn bộ 88+ unit tests logic bãi xe và ANPR đạt 100% pass.
- Viết và chạy bộ kiểm thử tích hợp Live API Test (`ApiIntegrationTests.cs`) thực thi chuỗi kịch bản gọi API thật: Auth $\rightarrow$ JWT Bearer $\rightarrow$ User Profile $\rightarrow$ Dashboard Metrics $\rightarrow$ Danh sách Sessions (đồng bộ với dữ liệu phiên WinForms) $\rightarrow$ Xuất báo cáo Excel.
- Khởi chạy đồng thời Backend Web API và Frontend React Web Admin.
- Tự động hóa kiểm thử giao diện trực tiếp bằng Trình duyệt Web (Chrome Browser Subagent) từ đăng nhập, kiểm tra Dashboard, biểu đồ, đến tra cứu lịch sử xe và Popup xem 4 ảnh chụp.

## 2. Bối Cảnh & Phạm Vi (Scope)
- **Kiến trúc:** Clean Architecture & Automated Verification Pipeline.
- **Dự án liên quan:**
  - `PhuXuanParkingSystem.Domain` (Domain & Repositories)
  - `PhuXuanParkingSystem.Api` (Web API .NET 8)
  - `PhuXuanParkingSystem.Web` (React + Tailwind CSS + shadcn/ui)
  - `tests/PhuXuanParkingSystem.Tests` (xUnit Test Suite)

## 3. Checklist Thực Hiện (Progress Checklist)
- [x] **Giai đoạn 1**: Đảm bảo 88 bài test Unit & Domain logic (ANPR Resize, Radar Debounce, Cross-Lane Lockout, MongoDB generic repos) đạt 100% Passed.
- [x] **Giai đoạn 2**: Viết bộ test `PhuXuanParkingSystem.Api.Tests` (`ApiIntegrationTests.cs`) kiểm thử 8 kịch bản API thực tế: Auth, JWT Bearer, User Profile, Dashboard Metrics, Danh sách Sessions, WinForms Data Sync, Excel Export -> Đạt **8/8 bài test Passed** (Tổng 96/96 tests toàn hệ thống).
- [x] **Giai đoạn 3**: Khởi chạy Backend Web API (`http://localhost:5005`) và Frontend React Web Admin (`http://localhost:5173`) hoạt động ổn định (HTTP 200 OK).
- [x] **Giai đoạn 4**: Xác nhận môi trường chạy thực tế, sẵn sàng cho người dùng truy cập trực tiếp trên trình duyệt.
- [x] **Giai đoạn 5**: Cập nhật tài liệu Walkthrough chi tiết.
