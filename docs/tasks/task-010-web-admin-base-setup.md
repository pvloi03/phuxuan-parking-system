# Task 010: Khởi Tạo Nền Tảng Web Admin (Domain Library, ASP.NET Core Web API, React + shadcn/ui)

## 1. Mục Tiêu (Goal)
- Trích xuất thư viện dùng chung `PhuXuanParkingSystem.Domain` chứa toàn bộ Entities, Enums, Value Objects và MongoDB Repositories để cả WinForms và Web API cùng sử dụng.
- Khởi tạo Backend `PhuXuanParkingSystem.Api` (ASP.NET Core Web API .NET 8) cung cấp Swagger, JWT Authentication, SignalR Realtime Hub và Static File Server cho ảnh chụp.
- Khởi tạo Frontend `PhuXuanParkingSystem.Web` (React + Vite + TypeScript + Tailwind CSS + shadcn/ui + Zustand + React Hook Form + Zod + Axios + TanStack Query).

## 2. Bối Cảnh & Phạm Vi (Scope)
- **Kiến trúc:** Clean Architecture & Multi-Platform Centralized System.
- **Dự án liên quan:**
  - `PhuXuanParkingSystem.Domain` (.NET Standard 2.0 / .NET 8 / .NET 4.8 compatible)
  - `PhuXuanParkingSystem` (WinForms trạm kiểm soát)
  - `PhuXuanParkingSystem.Api` (Backend API)
  - `PhuXuanParkingSystem.Web` (Frontend Web Admin SPA)

## 3. Checklist Thực Hiện (Progress Checklist)
- [x] **Giai đoạn 1**: Tạo Class Library `PhuXuanParkingSystem.Domain`, di chuyển Models & Repositories, cập nhật references và kiểm thử 88/88 unit tests vượt qua thành công.
- [x] **Giai đoạn 2**: Tạo Web API `PhuXuanParkingSystem.Api` (.NET 8), cấu hình JWT Auth, Swagger có Bearer Token, MongoDB DI, CORS, SignalR Hub, Static Files cho thư mục ảnh Captures.
- [x] **Giai đoạn 3**: Tạo React App `PhuXuanParkingSystem.Web` (React + Vite + TypeScript + Tailwind CSS + shadcn/ui + Zustand + React Hook Form + Zod + TanStack Query + Axios + SignalR + Recharts).
- [x] **Giai đoạn 4**: Xây dựng màn hình Login, AuthStore, Layout Sidebar + Header, Dashboard với KPI/Biểu đồ lưu lượng, Lịch sử xe ra vào với Modal xem 4 ảnh và Xuất Excel, Quản lý Xe, Quản lý Nhân sự.
- [x] **Giai đoạn 5**: Kiểm thử tổng thể (`dotnet test` 88/88 pass, `dotnet build` 0 errors, `npm run build` 0 errors).
