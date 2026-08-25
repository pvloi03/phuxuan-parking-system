# Task 012: Chuẩn Hóa Unified API Response & Global Exception Middleware

## 1. Mục Tiêu (Goal)
- Định nghĩa cấu trúc phản hồi chuẩn hóa `ApiResponse<T>` cho toàn bộ endpoints Backend API.
- Xây dựng `GlobalExceptionMiddleware` bắt toàn bộ Exception phát sinh, phân loại mã lỗi HTTP và trả về cấu trúc lỗi chuẩn.
- Đồng bộ hóa các Controller trên Web API, cập nhật tầng API Client trên Frontend React và điều chỉnh các bài kiểm thử `ApiIntegrationTests.cs`.

## 2. Bối Cảnh & Phạm Vi (Scope)
- **Kiến trúc:** Clean Architecture & Standardized API Design.
- **Dự án liên quan:**
  - `PhuXuanParkingSystem.Api` (Backend API & Middleware)
  - `PhuXuanParkingSystem.Web` (Frontend SPA & Axios Interceptors)
  - `tests/PhuXuanParkingSystem.Api.Tests` (Integration Test Suite)

## 3. Checklist Thực Hiện (Progress Checklist)
- [x] **Giai đoạn 1**: Tạo `ApiResponse.cs` và `GlobalExceptionMiddleware.cs` trong `PhuXuanParkingSystem.Api`.
- [x] **Giai đoạn 2**: Đăng ký Middleware trong `Program.cs` và cập nhật toàn bộ Controllers (`AuthController`, `DashboardController`, `ParkingSessionsController`, `VehiclesController`, `PeopleController`, `DepartmentsController`) trả về `ApiResponse.Ok(...)` hoặc `ApiResponse.Fail(...)`.
- [x] **Giai đoạn 3**: Cập nhật Frontend `PhuXuanParkingSystem.Web` (`types`, `apiClient.ts`, services, pages) bóc tách `ApiResponse.data`.
- [x] **Giai đoạn 4**: Cập nhật và bổ sung test cases trong `PhuXuanParkingSystem.Api.Tests` (`Test_1` đến `Test_9`) để xác thực `ApiResponse` và Exception Middleware.
- [x] **Giai đoạn 5**: Kiểm thử tổng thể (`dotnet test` 97/97 pass 100%, `npm run build` 0 errors), cập nhật tài liệu `walkthrough.md`.
