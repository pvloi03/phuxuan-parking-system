# CLAUDE.md — PhuXuanParkingSystem Agent Guide

> **Mục đích**: Hướng dẫn agent đọc và thay đổi codebase một cách nhất quán.

---

## Agent Skills

### Issue tracker

GitHub Issues (pvloi03/phuxuan-parking-system). Xem `docs/agents/issue-tracker.md`.

### Domain docs

Single-context layout. Xem `docs/agents/domain.md`.

---

## Context Pointers

| Pointer | Trigger | Target |
|---------|---------|--------|
| **domain-model** | Entity, Value Object, Enum, business rule | [CONTEXT.md](CONTEXT.md) |
| **hardware-layer** | SDK, P/Invoke, Camera, Controller, ZKTeco, Hikvision, NST | [docs/DOCS.md §2-3](docs/DOCS.md) |
| **web-stack** | API Controller, React page, JWT, RBAC | [docs/DOCS.md §8-9](docs/DOCS.md) |
| **winforms-stack** | FrmMain, FrmDeviceMonitor, WinForms UI | [docs/DOCS.md §5-7](docs/DOCS.md) |

---

## Architecture

```
PhuXuanParkingSystem/
├── PhuXuanParkingSystem.slnx          # Solution (x86 + AnyCPU)
├── PhuXuanParkingSystem.Domain/        # Class Library: Entities, VOs, Enums (ZERO deps)
├── PhuXuanParkingSystem.Api/           # ASP.NET Core: REST API + JWT auth
├── PhuXuanParkingSystem.Web/            # React + Redux Toolkit + TanStack Query
├── PhuXuanParkingSystem/              # WinForms .NET 4.8 x86: Live Monitor
├── PhuXuanParkingSystem.LicenseTool/  # WinForms .NET 8: License key generator
└── tests/                             # xUnit + FluentAssertions
```

**Platform constraint**: WinForms và SDK native bắt buộc **x86**. Không thay đổi `PlatformTarget`.

---

## Domain Language

- **ParkingSession** (Aggregate Root): lượt xe vào-ra. Factory methods: `CheckIn()`, `CheckOut()`, `CreateUnmatchedOut()`.
- **PlateNumber** (VO): chuẩn hóa biển số VN. Static methods: `Clean()`, `FormatDisplay()`.
- **ImageStoragePath** (VO): đường dẫn ảnh UNC/local. Custom BSON serializer.
- **DeviceType** enum: `PlateCamera=1`, `OverviewCamera=2`, `Controller=3`.
- **LicenseInfo**: bản quyền RSA-3072. Fields: `MaxLanes`, `MaxCameras`, `MaxControllers`.

→ **Luôn dùng domain language trong code và comments. Không dùng HPParkingSystem.**

---

## Key Patterns

### Repository Pattern
```csharp
// IRepository<T> generic interface
IRepository<ParkingSession> _sessionRepo;
await _sessionRepo.AddAsync(entity);
await _sessionRepo.FindAsync(filter, sort, skip, take);
await _sessionRepo.DeleteAsync(id); // Soft delete (IsDeleted=true)
```

### API Response Wrapper
```csharp
return Ok(ApiResponse<T>.Ok(data, "message"));
return BadRequest(ApiResponse.Fail("error"));
```

### Logging
```csharp
AppLogger.Information("message", "SourceContext");
AppLogger.Error(ex, "error", "SourceContext");
```

---

## Codebase Conventions

### C# Naming
- **PascalCase** cho mọi identifier
- **Private fields**: `_camelCase` (underscore prefix)
- **Async suffix**: `Async` cho method async
- **Interface prefix**: `I` (IRepository, IParkingLaneService)

### File Organization
- **Entities**: `PhuXuanParkingSystem.Domain/Models/Entities/*.cs`
- **Controllers**: `PhuXuanParkingSystem.Api/Controllers/*.cs`
- **WinForms**: `PhuXuanParkingSystem/Forms/*.cs`
- **Services**: `PhuXuanParkingSystem/Services/*/*.cs`

### MongoDB
- Collection name = pluralized entity name (Humanizer)
- Filter luôn thêm `IsDeleted == false` trừ khi cố ý query recycle bin
- Dùng `Builders<T>.Filter` với compound filters

---

## Soft Delete

Tất cả entities kế thừa `BaseEntity` có `IsDeleted`, `DeletedAt`. Delete = soft delete:
```csharp
await _repo.DeleteAsync(id); // Sets IsDeleted=true, DeletedAt=now
```

Recycle bin endpoints trong `RecycleBinController.cs` với restore/batch restore.

---

## Device Connection Flow

1. `FrmMain.LoadConfigurations()` → query MongoDB `Lanes` (IsActive=true)
2. `DeviceConfigService` resolve Device IDs → `Device` entities
3. `CameraConnectAsync()` / `ControllerConnectAsync()` song song với `Task.WhenAll`
4. Debounce radar events với `_lockDebounce` object

---

## License System (Task-022)

- **RSA-3072** digital signature + WMI hardware fingerprint
- `LicenseManager` kiểm tra khi FrmMain khởi động
- Footer label màu: 🟢 >15 ngày, 🟡 ≤15 ngày, 🔴 hết hạn
- `LicenseExpiredForm` block UI khi chưa kích hoạt
- Quota enforcement: `MaxLanes`, `MaxCameras`, `MaxControllers` trong API

---

## Testing

```bash
dotnet test tests/PhuXuanParkingSystem.Tests/
dotnet test tests/PhuXuanParkingSystem.Api.Tests/
```

Test patterns:
- FluentAssertions: `result.Should().BeEquivalentTo(expected)`
- Mock: Moq library
- Test naming: `MethodName_Scenario_ExpectedBehavior`

---

## Build & Run

```bash
# Build solution
dotnet build PhuXuanParkingSystem.slnx -c Debug

# Run WinForms (x86)
dotnet run --project PhuXuanParkingSystem/PhuXuanParkingSystem.csproj

# Run API
dotnet run --project PhuXuanParkingSystem.Api/PhuXuanParkingSystem.Api.csproj

# Run License Tool
dotnet run --project PhuXuanParkingSystem.LicenseTool/PhuXuanParkingSystem.LicenseTool.csproj
```

---

## Gotchas

1. **WinForms SDK dependencies**: Hikvision, NST, ZKTeco DLLs phải nằm cùng folder với executable.
2. **MongoDB connection**: Connection string trong `App.config` hoặc env variable `MongoDb_ConnectionString`.
3. **JWT secret**: `Jwt__Secret` trong API `appsettings.json` — không hardcode.
4. **GDI handles**: Luôn `Dispose()` ảnh trước khi gán mới, tránh leak.
5. **Thread safety**: UI updates phải dùng `InvokeRequired`/`BeginInvoke`.
