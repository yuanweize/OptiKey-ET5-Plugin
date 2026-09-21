# Compatibility Matrix and Environment Requirements

## Target Hardware
- **Tobii Eye Tracker 5 (ET5)**: Intended target hardware. Clean-room technical research and interoperability engineering are supported.
- **IS50 Platform Eye Trackers**: Compatible hardware base used by ET5.
- **Legacy Tobii Trackers (Eye Tracker 4C, EyeX)**: Not tested and not the primary design target.

---

## Host Application Compatibility
OptiKey introduced external eye-tracker plugin discovery and loading via `DllLoader` in **OptiKey 4.1.0**.

| OptiKey Version | Compatibility Status | Notes |
| :--- | :--- | :--- |
| **OptiKey < 4.0** | ❌ Unsupported | Does not feature external plugin architecture. |
| **OptiKey 4.0.x** | ⚠️ Limited | Early pre-release plugin architecture; unstable discovery. |
| **OptiKey 4.1.0 - 4.2.1** | ⚠️ Unverified | External plugin support exists; requires physical loader testing. |
| **OptiKey 4.2.2** | ⚠️ Pinned Contract Reference | Pinned reference contracts build target (`pinned-stable`). Verified via CI automated tests. |
| **OptiKey upstream `main`** | 🔄 Dynamic Upstream Reference | Upstream reference contracts build target (`upstream-main`). Verified via CI matrix rebuild. |

### Contract Verification
- Zero-native-loading on instantiation: The parameterless constructor required by OptiKey's plugin loader never attempts native DLL loading or initialization.
- Dynamic interface implementation: Implements `IPointService` and `IGazeService` with full lifecycle states (`Off`, `Initialising`, `Running`, `Calibrating`, `Reconnecting`, `Paused`, `Faulted`).

---

## Supported Operating Systems & Architecture
- **Windows 10/11 64-bit**: Required.
- **Architecture**: **x64 Only**. 32-bit (x86) and ARM64 Windows are strictly rejected by PE validation due to native Tobii driver constraints.
- **Runtime Framework**: **.NET Framework 4.6 or higher** (included natively in Windows 10/11).

---

## Native Runtime Architecture & Discovery

### Discovery Pipeline
The plugin searches for `tobii_stream_engine.dll` dynamically using `CompositeRuntimeDiscovery`, aggregating multiple decoupled sources:
1. **Developer Explicit Override**:
   - Explicit path passed via `PluginConfiguration.CustomProbePaths` or `OPTIKEY_ET5_RUNTIME_PATH` environment variable.
2. **Known Installation Paths**:
   - Default 64-bit Program Files paths (e.g., `%ProgramW6432%\Tobii\Tobii Eye Tracker\tobii_stream_engine.dll`).
3. **Windows Installed Programs Registry**:
   - Standard uninstall registry hives (`HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall` and `HKCU`).
4. **Windows Service Configuration**:
   - Extracts directory candidate from registered Tobii Windows service binary paths (`HKLM\SYSTEM\CurrentControlSet\Services\Tobii Service`).

### 4-Tier Candidate Validation Pipeline
Every discovered candidate must pass 4 verification gates before any code is loaded:
1. **File System Integrity**: Absolute path validation and existence check.
2. **Static PE Architecture Validation**: Reads COFF header to ensure `IMAGE_FILE_MACHINE_AMD64` (0x8664).
3. **Authenticode Trust & Signer Verification**:
   - Uses Win32 `WinVerifyTrust` (`WINTRUST_ACTION_GENERIC_VERIFY_V2`) to verify file integrity and cryptographic signature.
   - Inspects certificate subject to ensure publisher identity contains `"Tobii"`.
   - Distinguishes signature validity, certificate chain status, and publisher identity via `RuntimeTrustResult`.
4. **Dynamic Export Capability Probing**:
   - `RuntimeCapabilities` inspects exported function symbols without hardcoding assumptions.
   - Verifies **Core Required** (`tobii_api_create`, `tobii_api_destroy`), **Device Required** (`tobii_device_create`, `tobii_device_destroy`, `tobii_enumerate_devices`), and **Gaze Required** (`tobii_gaze_point_subscribe`, `tobii_gaze_point_unsubscribe`).
   - Dynamically selects callback pump strategy based on symbol availability (`tobii_wait_for_callbacks`, `tobii_process_callbacks`).

---

## Execution Engine & Resilience

### Decoupled Callback Pumps
- **WaitAndProcessCallbackPump**: Uses blocking `tobii_wait_for_callbacks` followed by `tobii_process_callbacks`.
- **ProcessOnlyPollingPump**: Uses non-blocking `tobii_process_callbacks` in a loop with interruptible wait handle, bypassing `tobii_wait_for_callbacks`.

### Bounded Shutdown & Fault Isolation
- Bounded join on background pump thread prevents UI freezes during OptiKey shutdown.
- **Stuck Native Thread Protection**: If native `tobii_wait_for_callbacks` blocks indefinitely during teardown, the worker thread is abandoned and native destruction calls (`tobii_device_destroy`, `tobii_api_destroy`, `FreeLibrary`) are skipped. This prevents fatal native access violations (`AccessViolationException`) from crashing the OptiKey host.
- **Process Isolation Option**: Out-of-process `RuntimeHost` architecture specification (ADR-009) with binary framing IPC for zero-crash host isolation.

---

## Device Selection & Safety Policy
- **Strict Mode (Default)**:
  - Only officially recognized Tobii Eye Tracker 5 devices are bound.
  - Automatically rejects unverified models or unknown devices.
- **Developer Test Mode**:
  - Requires explicit opt-in: `AllowUnverifiedDevices = true`.
  - Requires explicit device selection: `PreferredDeviceIndex` or `PreferredDeviceUrl` must be specified.
  - **No auto-binding to candidate 0**: Prevents accidental operation on unexpected hardware.
- **Privacy Assurance**:
  - Device URLs and serial numbers are strictly redacted from logs and diagnostic exports.
  - Diagnostic inventory tools sanitize usernames to `%USERPROFILE%`.
