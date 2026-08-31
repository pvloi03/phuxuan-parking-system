# Task 023: Controller Service Simplification & Refactoring (Ponytail Spec)

## Problem Statement

The Access Controller service subsystem in the WinForms application has accumulated unnecessary abstraction layers and accidental complexity:
- An unnecessary pass-through adapter (`ControllerDeviceAdapter`) acts as a pure delegate wrapper around a single-implementation interface (`IControllerService`).
- `IControllerService` duplicates `IDeviceAdapter` method signatures rather than unifying with the common device architecture.
- The low-level hardware communication driver is directly coupled to UI notification services (`AppNotificationService`), creating side effects during background operations.
- Socket ping operations re-implement timeout logic with manual `Task.WhenAny` and `Task.Delay` instead of leveraging standard platform asynchronous connection overloads.
- Event argument classes and text parsing logic contain verbose boilerplate that obscures core business flow.
- The test suite has out-of-sync test cases referencing non-existent types (`ControllerLogEvent`), failing the build.

## Solution

Streamline the Access Controller subsystem by removing redundant abstractions, utilizing standard framework capabilities, and decoupling hardware event dispatching from UI presentation:
- Make `ControllerService` implement `IDeviceAdapter` directly (or inherit unified contracts), deleting the pass-through adapter.
- Decouple hardware driver from UI notifications by emitting domain events and letting UI listeners dispatch notifications.
- Use native `TcpClient.ConnectAsync` with `CancellationToken` for TCP health check ping.
- Modernize event argument structures to concise positional records.
- Simplify string parsing using standard library `StringSplitOptions.TrimEntries`.
- Fix and align unit tests with the unified event parsing contract.

---

## User Stories

1. As a **Parking Attendant**, I want the Access Controller to reliably detect vehicle arrival and departure events from radar/loop sensors without lag or missed signals, so that lanes operate smoothly.
2. As a **Parking Attendant**, I want device connection status changes to be accurately reflected on the monitoring dashboard, so that I immediately know if the controller goes offline.
3. As a **System Administrator**, I want the controller service to automatically recover from temporary network drops without requiring an application restart, so that parking operations suffer minimal downtime.
4. As a **System Administrator**, I want the health check ping to accurately verify controller TCP reachability without hanging or leaking sockets, so that false disconnect alarms are eliminated.
5. As a **System Integrator**, I want the Controller service to share the same unified `IDeviceAdapter` contract as Cameras and other hardware, so that device health monitoring and lifecycle management are consistent across all device types.
6. As a **Software Developer**, I want to navigate fewer wrapper layers when maintaining hardware controller code, so that bugs can be quickly diagnosed and fixed.
7. As a **Software Developer**, I want hardware drivers to be free of hardcoded UI notification side effects, so that the driver can be tested in isolation and reused across different application hosts.
8. As a **Software Developer**, I want the test suite to compile cleanly and validate real controller event parsing scenarios without broken dependencies, so that CI/CD pipelines remain green.

---

## Implementation Decisions

### 1. Direct Contract Implementation
- Eliminate the standalone pass-through adapter wrapper module.
- Have the core Controller Service directly implement the standard Device Adapter interface, unifying device lifecycle (`IsConnected`, `IsStreaming`, `ConnectAsync`, `DisconnectAsync`, `RestartAsync`, `PingAsync`, `OnConnectionStateChanged`).
- Expose controller-specific capabilities (such as auxiliary sensor event streams) via dedicated events on the service.

### 2. UI Notification Decoupling
- Remove direct calls to UI notification services from inside the low-level SDK driver.
- Hardware driver responsibilities are strictly bounded to: SDK communication, raw log parsing, and triggering high-level events (`OnAuxInputTriggered`, `OnConnectionStateChanged`).
- The UI layer (e.g. Lane Control and Notification handlers) subscribes to these events and triggers notifications as needed.

### 3. Native Asynchronous Socket Operations
- Replace hand-rolled timeout patterns (`Task.WhenAny` + `Task.Delay`) with standard library socket connection overloads that accept `CancellationToken`.

### 4. Compact Data Structures & Parsing
- Convert verbose event argument classes to concise positional records.
- Replace manual line splitting and trimming routines with standard library split options: `StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries`.

---

## Testing Decisions

### Proposed Testing Seams
- **Primary Seam (Highest Public Boundary)**: Controller log parsing and event dispatching (`ParseAndDispatchLog` → `OnAuxInputTriggered`).
- **Device Lifecycle Seam**: `IDeviceAdapter` state transitions (`OnConnectionStateChanged`).

### Test Coverage Strategy
- Unit tests verify log parsing for Lane In (Aux Port 1) and Lane Out (Aux Port 2) triggers (both vehicle arrival `221` / `25` / `1` and vehicle cleared `220`).
- Edge-case testing for malformed CSV lines, truncated log streams, and status broadcast packets (`Bit4=255`).
- No unit test should depend on native SDK binary DLLs or physical hardware devices.

### Prior Art
- Unit tests in `tests/PhuXuanParkingSystem.Tests/` using xUnit and FluentAssertions.

---

## Out of Scope

- Modifying native ZKTeco C-SDK PInvoke signatures (`ZKTecoPullSDK.cs`).
- Adding support for new controller hardware models (e.g. Advantech ADAM / Relay boards).
- Changes to barrier relay opening commands or card swiping logic.

---

## Further Notes

- Net reduction estimated from the Ponytail audit: **~191 lines of code**, 0 added dependencies.
- This refactoring improves maintainability and aligns the Access Controller service with the `DeviceHealthManager` architecture established in the project.
