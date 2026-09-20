# Compatibility Matrix and Environment Requirements

## Target Hardware
- **Tobii Eye Tracker 5 (ET5)**: Primary supported hardware.
- **IS50 Platform Eye Trackers**: Compatible via standard Tobii Stream Engine IS50 driver endpoints.
- **Legacy Tobii Trackers (Eye Tracker 4C, EyeX)**: May function if supported by the installed Tobii Experience runtime, but ET5 is the primary target.

## Host Application Compatibility
OptiKey introduced external eye-tracker plugin discovery and loading via `DllLoader` in **OptiKey 4.1.0**.

| OptiKey Version | Compatibility Status | Notes |
| :--- | :--- | :--- |
| **OptiKey < 4.0** | ❌ Unsupported | Does not feature external plugin architecture. |
| **OptiKey 4.0.x** | ⚠️ Limited | Early pre-release plugin architecture; unstable discovery. |
| **OptiKey 4.1.0** | ✅ Supported | First stable release with full `EyeTrackerPlugins` directory support. |
| **OptiKey 4.1.1** | ✅ Verified Baseline | Tested baseline release. Verified plugin reflection loading. |
| **OptiKey 4.2.0** | ✅ Supported | Full compatibility with `JuliusSweetland.OptiKey.Contracts`. |
| **OptiKey 4.2.1** | ✅ Supported | Maintained ABI parity. |
| **OptiKey 4.2.2** | ✅ Pinned Target | Current pinned reference release (`OPTIKEY_CONTRACT_REF`). |
| **OptiKey 4.x (main)** | 🔄 Continuous CI | Tested in CI on every push to detect upstream contract shifts. |

## Supported Operating Systems
- **Windows 10 64-bit (x64)**: Version 1903 (Build 18362) or later.
- **Windows 11 64-bit (x64)**: All editions (21H2, 22H2, 23H2, 24H2+).
- **Architecture**: **x64 Only**. 32-bit (x86) and ARM64 Windows are NOT supported due to native Tobii driver constraints.

## Software Prerequisites
1. **Tobii Experience**:
   Installed and running from the Microsoft Store or official Tobii setup installer.
   Tobii background service (`Tobii.Service.exe`) must be active.
2. **.NET Framework 4.6 or higher**:
   Built-in on Windows 10 and 11.
3. **Calibrated Display**:
   The user must complete display calibration within the Tobii Experience application before launching OptiKey.
