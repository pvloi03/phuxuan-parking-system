# Task 006: Xây Dựng Hệ Thống Kiểm Thử Tự Động (Unit & Integration Tests)

## 1. Mục Tiêu
Khởi tạo project kiểm thử tự động `tests/HPParkingThaiThuy.Tests` trên nền tảng **xUnit**, **FluentAssertions**, và **Moq**, bao phủ toàn bộ các tầng logic cốt lõi: Value Objects, Entities, Common BaseEntity, ZKTeco Controller Log Parser, Dependency Injection Container, và MongoRepository Collection Naming.

## 2. Bối Cảnh & Phạm Vi
- **Bối cảnh**: Hệ thống bãi đỗ xe vận hành 24/7 đòi hỏi độ tin cậy cao, việc chuẩn hóa biển số xe, tính toán thời gian gửi, phân luồng sự kiện cảm biến và cơ chế Dependency Injection cần được kiểm thử tự động 100% trước khi triển khai thực tế.
- **Phạm vi kiểm thử**:
  - `Value Objects`: Kiểm thử làm sạch, chuẩn hóa biển số xe máy / ô tô Việt Nam (`PlateNumber`).
  - `Entities`: Kiểm thử vòng đời phiên gửi xe `ParkingSession` (`CheckIn`, `CheckOut`, `CreateUnmatchedOut`, tính toán `Duration`, nhận diện `IsUnknown`).
  - `Entities`: Kiểm thử các thuộc tính và khởi tạo của `Person`, `Vehicle`, `Department`, `Lane`, `Device`, `Company`, `Contractor`, `User`.
  - `Common`: Kiểm thử cơ chế Xóa mềm (`MarkDeleted()`, `Restore()`) của `BaseEntity`.
  - `Services / Controller`: Kiểm thử phân tích log sự kiện phần cứng ZKTeco C3-200 RTLog (`ZKTecoLogEvent`), phân biệt cổng Aux In 1 (Làn Vào) và Aux In 2 (Làn Ra), mã sự kiện `221` (Có xe) và `220` (Hết xe).
  - `Dependency Injection`: Kiểm thử `Program.ConfigureServices` đăng ký và resolve thành công `MongoDbContext`, `IRepository<T>` (Open Generic) cho toàn bộ entities và `FrmMain`.
  - `Repositories`: Kiểm thử `MongoRepository<T>` tự động đặt tên Collection theo số nhiều qua thư viện `Humanizer`.

## 3. Checklist Tiến Độ
- [x] Khởi tạo project `tests/HPParkingThaiThuy.Tests/HPParkingThaiThuy.Tests.csproj` (.NET 4.8 / x86).
- [x] Thêm project test vào file Solution `HPParkingThaiThuy.slnx`.
- [x] Viết bộ Unit Tests cho `PlateNumberTests.cs`.
- [x] Viết bộ Unit Tests cho `ParkingSessionTests.cs`.
- [x] Viết bộ Unit Tests cho `BaseEntityTests.cs`.
- [x] Viết bộ Unit Tests cho `EntityTests.cs` (Person, Vehicle, Lane, Device, Department, etc.).
- [x] Viết bộ Unit Tests cho `ZKTecoLogEventTests.cs`.
- [x] Viết bộ Unit Tests cho `DiContainerTests.cs`.
- [x] Viết bộ Unit Tests cho `MongoRepositoryUnitTests.cs`.
- [x] Chạy kiểm thử tự động với `dotnet test` và đạt **100% Passed**.

## 4. Lưu Ý Kỹ Thuật
- TargetFramework và PlatformTarget của project Test phải khớp với project chính (`net48`, `x86`) để đảm bảo không bị xung đột môi trường.
- Các unit tests chạy hoàn toàn in-memory, độc lập với cơ sở dữ liệu thật và phần cứng thật.
