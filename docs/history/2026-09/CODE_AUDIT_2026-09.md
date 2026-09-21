[English](CODE_AUDIT_2026-09.md) | [简体中文](CODE_AUDIT_2026-09.zh-CN.md)

# Comprehensive Code Audit & Hardening Report (September 2026)

## Executive Summary

As part of the finalization and first-release phase of `v0.1.0`, a comprehensive code and security audit was performed across all production files in the `OptiKey-ET5-Plugin` repository. The objective was to eliminate all process-crashing races, enforce OptiKey plugin loader invariants, prevent native use-after-free conditions, verify strict privacy invariants, and validate ordinary-user out-of-the-box operation.

All identified **P0** (Critical / Release Blocker) and **P1** (High / Functionality Blocker) issues have been fully resolved and verified via automated Windows x64 CI and unit/integration test suites.

---

## Findings & Remediation Matrix

| ID | Severity | Category | File | Description & Impact | Resolution Status |
|:---|:---|:---|:---|:---|:---|
| **AUD-01** | **P0** | Lifecycle / Threading | `TobiiGazeProvider.cs` | Background `NullReferenceException` in `WorkerLoop` when dereferencing mutable `cts` after `Dispose()` or Stop race. | **FIXED**: Captured CancellationToken into immutable stack local variable per loop generation; isolated token from mutable instance fields. |
| **AUD-02** | **P0** | Concurrency / Deadlock | `TobiiGazeProvider.cs` | Lock inversion deadlock in `Stop()`: holding `lifecycleLock` while calling `workerThread.Join()` while worker's `finally` block requested `lifecycleLock`. | **FIXED**: Refactored `Stop()` and `Dispose()` to release `lifecycleLock` prior to joining threads and pump instances, eliminating the deadlock. |
| **AUD-03** | **P0** | Usability / Default Path | `TobiiGazeProvider.cs`, `PluginConfiguration.cs` | Out-of-the-box default configuration refused all hardware connections due to conflation of developer strict mode with normal single-device connection. | **FIXED**: Introduced scientific candidate selection policy: if exactly one Tobii device candidate exists and the ET5 plugin is selected, auto-bind without manual config. Multiple devices still strictly refuse silent binding. |
| **AUD-04** | **P1** | Device Selection | `TobiiGazeProvider.cs` | When user configured an explicit `PreferredDeviceUrl` that did not exist in enumeration, code fell through to single-device automatic selection. | **FIXED**: Enforced immediate failure and explicit error event if a configured `PreferredDeviceUrl` cannot be located in enumeration. |
| **AUD-05** | **P1** | Memory Safety / Native Teardown | `TobiiGazeProvider.cs` | Native callback pump timeout during shutdown did not flag `STUCK_WORKER`, allowing `runtime.Dispose()` to proceed and risking native access violations. | **FIXED**: Unified callback pump join timeout into clean-shutdown verification. Timed-out pumps enter `STUCK_WORKER` state, bypassing native handle release. |
| **AUD-06** | **P2** | Compilation / Diagnostics | `ET5Diagnostics/Program.cs` | Unreachable code compiler warning in diagnostic utility loop. | **FIXED**: Removed unreachable code block and cleaned up loop control flow; zero compiler warnings across production projects. |
| **AUD-07** | **P2** | Test Hygiene | `SyntheticEndToEndTests.cs` | Unused fake events triggered compiler warnings in test assemblies. | **FIXED**: Explicitly suppressed intentional fake interface members with `#pragma warning disable CS0067`. |
| **AUD-08** | **P2** | Privacy & Security | `PluginConfiguration.cs`, `TobiiGazeProvider.cs` | Potential accidental disclosure of device URLs in diagnostic logs. | **FIXED**: Verified and enforced URL redaction across all log statements (`logger.Info("Configured explicit PreferredDeviceUrl (redacted for privacy).")`). |

---

## Detailed Audit Breakdown by Domain

### 1. Lifetime, Nullability & Memory Safety
- **OptiKey Contracts**: `ET5PointService` preserves the parameterless public constructor contract. No native DLL loading, thread pools, or hardware handles are allocated during constructor execution.
- **Unmanaged Memory & GC Pinning**: `tobii_gaze_point_callback_t` delegate is pinned to a class instance field (`nativeGazeCallback`) preventing premature garbage collection while registered with the unmanaged Tobii C runtime.
- **Dispose Idempotency**: All classes implementing `IDisposable` (`TobiiGazeProvider`, `ET5PointService`, `WaitAndProcessCallbackPump`, `ProcessOnlyPollingPump`) are fully idempotent and safe against repeated disposal calls.

### 2. Concurrency & Thread Safety
- **Thread Synchronization**: State machine transitions are guarded by `GazeServiceStateMachine.syncLock`. Provider lifecycle mutations are synchronized using `lifecycleLock`.
- **Join Without Lock**: Thread and callback pump `Join()` operations are executed strictly outside critical sections to prevent thread starvation and deadlocks.
- **Worker Generation Tracking**: Worker threads verify their assigned `generation` against `this.lifecycleGeneration` upon waking, ensuring obsolete worker loops exit immediately if a new `Start()` is initiated.

### 3. Native ABI & Discovery Security
- **Dynamic P/Invoke**: Native function pointers are bound dynamically via Win32 `LoadLibrary` / `GetProcAddress`. No hardcoded static P/Invoke imports exist.
- **Authenticode Verification**: Candidate `tobii_stream_engine.dll` files discovered from local services or program directories are validated using `WinVerifyTrust` and subject to Tobii Subject Name verification prior to dynamic loading.
- **No Bundled Binaries**: Verified that zero proprietary Tobii DLLs, static libraries, or binaries are checked into Git or included in release ZIP distributions.

### 4. Privacy Invariants
- Gaze point coordinates (`position_x`, `position_y`, `timestamp_us`) are processed purely in volatile memory and immediately dispatched to OptiKey's `IPointService` interface.
- Gaze coordinates, user identity, eye images, and device URLs are never written to disk, temporary files, or log streams.
- No network sockets or telemetry endpoints are utilized.
