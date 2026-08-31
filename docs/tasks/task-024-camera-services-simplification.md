# Task 024: Camera Services Simplification & Refactoring (Ponytail Spec)

## Problem Statement

The Camera subsystem in the WinForms application (`Services/Devices/Camera`) contains accidental complexity and repetitive boilerplate:
- An unnecessary pass-through wrapper (`CameraDeviceAdapter`) acts as an intermediary between `ICameraService` and `IDeviceAdapter`, duplicating state properties and lifecycle forwarding methods.
- High degree of code duplication between `PlateCameraService` (NST SDK) and `OverviewCameraService` (Hikvision SDK), including configuration state, semaphore concurrency control for snapshots, streaming state tracking, and disposal logic.
- Low-level camera drivers are tightly coupled to the UI notification singleton (`AppNotificationService`), causing intrusive UI notifications and log side effects during automated operations and reconnect attempts.
- A redundant helper utility (`CameraCaptureHelper`) wraps standard asynchronous file I/O with verbose boilerplate rather than utilizing concise platform file writing mechanisms.
- Camera configuration classes are defined with unnecessarily verbose property ceremonies.

## Solution

Streamline and modernize the Camera subsystem by eliminating pass-through adapters, establishing a lightweight base abstraction for common camera lifecycle operations, and decoupling hardware drivers from UI notifications:
- Make `ICameraService` inherit `IDeviceAdapter` directly, allowing `PlateCameraService` and `OverviewCameraService` to be registered and monitored without an adapter wrapper.
- Extract common camera infrastructure (configuration management, snapshot concurrency serialization, streaming status management, and disposable cleanup) into a shared base class (`CameraServiceBase`).
- Decouple hardware drivers from UI notifications: drivers strictly raise `OnConnectionStateChanged` and log diagnostic messages, leaving UI notifications to UI event listeners.
- Simplify snapshot file saving utilities into concise standard library I/O calls.
- Simplify configuration data structures.

---

## User Stories

1. As a **Parking Attendant**, I want camera preview feeds (Main Stream HD) to start reliably and stay synchronized with camera connection states, so that vehicles entering and exiting the lane are clearly visible.
2. As a **Parking Attendant**, I want license plate and panoramic snapshot captures to execute rapidly without locking UI threads or freezing preview streams, so that gate processing is instantaneous.
3. As a **System Administrator**, I want camera health check pings and auto-reconnects to operate in the background without spamming notification banners across the screen, so that system alarms are reserved for actual unresolved hardware failures.
4. As a **System Integrator**, I want Plate Cameras and Overview Cameras to adhere directly to the unified `IDeviceAdapter` contract, so that device health monitoring and health check cycles treat all devices uniformly.
5. As a **Software Developer**, I want shared camera behavior (thread-safe snapshot capture semaphores, connection state management, lifecycle disposal) centralized in one place, so that maintaining or adding new camera drivers is straightforward.
6. As a **Software Developer**, I want camera driver unit tests to execute cleanly in memory without depending on native camera C-SDK DLLs or external file locks, so that the test suite runs fast and reliably.

---

## Implementation Decisions

### 1. Unified Device Adapter Inheritance
- `ICameraService` inherits `IDeviceAdapter` and `IDisposable`.
- The pass-through `CameraDeviceAdapter` wrapper is deleted entirely.
- `DeviceAdapterFactory` registers `ICameraService` directly as an `IDeviceAdapter`.

### 2. Common Camera Base Abstraction (`CameraServiceBase`)
- Introduce an abstract `CameraServiceBase` that implements shared state and mechanics:
  - Configuration properties (`CameraConfig`).
  - Thread-safe snapshot execution via `SemaphoreSlim(1, 1)` with a 5-second safety timeout.
  - Lifecycle state tracking (`IsLoggedIn`, `IsStreaming`, `OnConnectionStateChanged`).
  - Standard `IDeviceAdapter` implementation (`ConnectAsync`, `DisconnectAsync`, `RestartAsync`, `PingAsync`).
  - `Dispose` pattern releasing streaming handles and semaphores.
- Concrete classes (`PlateCameraService`, `OverviewCameraService`) only implement SDK-specific native P/Invoke calls (`LoginNative`, `LogoutNative`, `StartPreviewNative`, `StopPreviewNative`, `CaptureSnapshotNative`).

### 3. UI Notification Decoupling
- Remove direct invocations of `AppNotificationService.NotifySuccess` / `NotifyError` from inside low-level camera drivers.
- Camera status transitions emit `OnConnectionStateChanged` (`Connected`, `Disconnected`, `Streaming`, `Error`).
- The UI layer (e.g. `FrmMain.Cameras.cs` and `FrmDeviceMonitor.cs`) handles status rendering and user notifications.

### 4. Simplified Snapshot I/O & Config
- Modernize file writing logic to use standard `File.WriteAllBytes` / `FileStream` asynchronously.
- Compact configuration structures.

---

## Testing Decisions

### Proposed Testing Seams
- **Primary Public Behavior Seam**: `ICameraService` lifecycle methods (`LoginAsync`, `Logout`, `StartPreview`, `StopPreview`, `CaptureSnapshotAsync`, `CaptureToFileAsync`) and event transitions (`OnConnectionStateChanged`).
- **Device Health Integration Seam**: `IDeviceAdapter` contract methods on camera instances (`PingAsync`, `ConnectAsync`, `RestartAsync`).

### Test Coverage Strategy
- Unit tests verify config binding (`ApplyDeviceConfig`), connection state transitions (`Connected` → `Streaming` → `Disconnected`), snapshot semaphore timeout handling, and file saving using mocked/simulated camera drivers.
- Integration tests verify `DeviceAdapterFactory` registering camera services and `DeviceHealthMonitorService` monitoring cameras without SDK runtime dependencies.

### Prior Art
- Existing unit tests in `tests/PhuXuanParkingSystem.Tests/Services/CameraCaptureServiceTests.cs` and `tests/PhuXuanParkingSystem.Tests/Services/DeviceHealthStreamTests.cs`.

---

## Out of Scope

- Modifying native SDK C-header P/Invoke bindings (`CHISDK.cs` and `CHCNetSDK.cs`).
- Introducing new camera vendor SDKs (e.g. Dahua, Uniview).
- WinForms video rendering panel handle allocation logic (`hPlayWnd`).

---

## Further Notes

- Estimated reduction from Ponytail audit: **~267 lines of boilerplate/duplicate code**, 0 added dependencies.
- Aligns camera services with the unified pattern established in Controller Service (Task 023).
