# Task 001: Khởi Tạo Solution Skeleton (Clean Architecture 4 Layers x86)

## 1. Mục Tiêu
Khởi tạo cấu trúc Solution và phân tầng các project trong Solution theo đúng nguyên lý **Clean Architecture** (x86, .NET Framework 4.8 / .NET Standard), thiết lập reference giữa các project và kiểm tra biên dịch cơ bản.

## 2. Bối Cảnh & Phạm Vi
- **Bối cảnh**: Hệ thống `HPParkingSystem` cần tích hợp nhiều SDK phần cứng (Hikvision, NST, ZKTeco C3-200) là các file thư viện native 32-bit (x86).
- **Phạm vi**:
  - Tạo cấu trúc thư mục `src/` và 4 projects:
    1. `src/HPParkingSystem.Domain` (Class Library)
    2. `src/HPParkingSystem.Application` (Class Library)
    3. `src/HPParkingSystem.Infrastructure` (Class Library)
    4. `src/HPParkingSystem.WinForms` (WinForms App x86)
  - Cập nhật Solution file tổng `HPParkingSystem.slnx` hoặc `HPParkingSystem.sln`.
  - Cấu hình TargetPlatform x86 cho tất cả các project.

## 3. Chi Tiết Thực Hiện

### 3.1. Thiết lập Project References
- `HPParkingSystem.Domain` $\rightarrow$ Không reference project nào.
- `HPParkingSystem.Application` $\rightarrow$ Reference `HPParkingSystem.Domain`.
- `HPParkingSystem.Infrastructure` $\rightarrow$ Reference `HPParkingSystem.Application` và `HPParkingSystem.Domain`.
- `HPParkingSystem.WinForms` $\rightarrow$ Reference `HPParkingSystem.Infrastructure`, `HPParkingSystem.Application`, `HPParkingSystem.Domain`.

### 3.2. Cấu hình MSBuild Platform Target
Trong mỗi file `.csproj`, đảm bảo có cấu hình:
```xml
<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Debug|x86' ">
  <PlatformTarget>x86</PlatformTarget>
  <OutputPath>bin\x86\Debug\</OutputPath>
</PropertyGroup>
<PropertyGroup Condition=" '$(Configuration)|$(Platform)' == 'Release|x86' ">
  <PlatformTarget>x86</PlatformTarget>
  <OutputPath>bin\x86\Release\</OutputPath>
</PropertyGroup>
```

## 4. Checklist Tiến Độ
- [x] Tạo thư mục `src/` và các thư mục con cho từng layer.
- [x] Khởi tạo file `.csproj` cho `HPParkingSystem.Domain`.
- [x] Khởi tạo file `.csproj` cho `HPParkingSystem.Application`.
- [x] Khởi tạo file `.csproj` cho `HPParkingSystem.Infrastructure`.
- [x] Chuyển project WinForms vào `src/HPParkingSystem.WinForms` và cấu hình lại Solution.
- [x] Thiết lập đúng project references theo The Dependency Rule.
- [x] Build thử nghiệm toàn bộ Solution với `Platform=x86` thành công (0 errors, 0 warnings).

## 5. Lưu Ý Kỹ Thuật
- Tuyệt đối không tạo tham chiếu vòng (circular reference).
- Domain layer không được cài đặt bất kỳ NuGet package nào liên quan đến Database (MongoDB) hay UI.
