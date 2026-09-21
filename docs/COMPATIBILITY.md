# Compatibility Matrix and Environment Requirements

## Target Hardware
- **Tobii Eye Tracker 5 (ET5)**: Intended target only; not currently supported or hardware-validated. Current Tobii material also creates an unresolved API/license blocker for the non-`L` device.
- **IS50 Platform Eye Trackers**: Not verified by this repository.
- **Legacy Tobii Trackers (Eye Tracker 4C, EyeX)**: Not verified and not an intended target.

## Host Application Compatibility
OptiKey introduced external eye-tracker plugin discovery and loading via `DllLoader` in **OptiKey 4.1.0**.

| OptiKey Version | Compatibility Status | Notes |
| :--- | :--- | :--- |
| **OptiKey < 4.0** | ❌ Unsupported | Does not feature external plugin architecture. |
| **OptiKey 4.0.x** | ⚠️ Limited | Early pre-release plugin architecture; unstable discovery. |
| **OptiKey 4.1.0** | ⚠️ Unverified | External plugin support is a research lead; no exact-version packaged-loader test is present. |
| **OptiKey 4.1.1** | ⚠️ Unverified | No exact-version loader test is present in this repository. |
| **OptiKey 4.2.0** | ⚠️ Unverified | No exact-version loader test is present in this repository. |
| **OptiKey 4.2.1** | ⚠️ Unverified | No exact-version loader test is present in this repository. |
| **OptiKey 4.2.2** | ⚠️ Contract reference | Used for the pinned contract build; physical loader compatibility is unverified. |
| **OptiKey upstream `main`** | 🔄 Contract CI only | The plugin rebuilds and runs managed tests against current upstream Contracts. This is not an installed-host or hardware compatibility test. |

## Supported Operating Systems
- **Windows 10/11 x64**: Targeted by the build configuration; supported OS versions and editions are not hardware-verified.
- **Architecture**: **x64 Only**. 32-bit (x86) and ARM64 Windows are NOT supported due to native Tobii driver constraints.

## Software Prerequisites
1. **Tobii Experience**:
   Intended future prerequisite. Actual installed products, services, registry entries, and runtime paths require inventory on a real ET5 Windows system.
2. **.NET Framework 4.6 or higher**:
   Built-in on Windows 10 and 11.
3. **Calibrated Display**:
   The user must complete display calibration within the Tobii Experience application before launching OptiKey.
