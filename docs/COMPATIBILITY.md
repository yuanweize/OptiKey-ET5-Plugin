# Compatibility Matrix and Environment Requirements

## Target Hardware
- **Tobii Eye Tracker 5 (ET5)**: Primary supported hardware.
- **IS50 Platform Eye Trackers**: Not verified by this repository.
- **Legacy Tobii Trackers (Eye Tracker 4C, EyeX)**: Not verified and not an intended target.

## Host Application Compatibility
OptiKey introduced external eye-tracker plugin discovery and loading via `DllLoader` in **OptiKey 4.1.0**.

| OptiKey Version | Compatibility Status | Notes |
| :--- | :--- | :--- |
| **OptiKey < 4.0** | ❌ Unsupported | Does not feature external plugin architecture. |
| **OptiKey 4.0.x** | ⚠️ Limited | Early pre-release plugin architecture; unstable discovery. |
| **OptiKey 4.1.0** | ✅ Supported | First stable release with full `EyeTrackerPlugins` directory support. |
| **OptiKey 4.1.1** | ⚠️ Unverified | No exact-version loader test is present in this repository. |
| **OptiKey 4.2.0** | ⚠️ Unverified | No exact-version loader test is present in this repository. |
| **OptiKey 4.2.1** | ⚠️ Unverified | No exact-version loader test is present in this repository. |
| **OptiKey 4.2.2** | ⚠️ Contract reference | Used for the pinned contract build; physical loader compatibility is unverified. |
| **OptiKey 4.x (main)** | 🔄 Continuous CI | Tested in CI on every push to detect upstream contract shifts. |

## Supported Operating Systems
- **Windows 10/11 x64**: Targeted by the build configuration; supported OS versions and editions are not hardware-verified.
- **Architecture**: **x64 Only**. 32-bit (x86) and ARM64 Windows are NOT supported due to native Tobii driver constraints.

## Software Prerequisites
1. **Tobii Experience**:
   Installed and running from the Microsoft Store or official Tobii setup installer.
   Tobii background service (`Tobii.Service.exe`) must be active.
2. **.NET Framework 4.6 or higher**:
   Built-in on Windows 10 and 11.
3. **Calibrated Display**:
   The user must complete display calibration within the Tobii Experience application before launching OptiKey.
