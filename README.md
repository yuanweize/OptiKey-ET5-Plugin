# OptiKey-ET5-Plugin

[![Windows CI](https://github.com/yuanweize/OptiKey-ET5-Plugin/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/yuanweize/OptiKey-ET5-Plugin/actions/workflows/build-and-test.yml)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)
[![OptiKey Compatibility](https://img.shields.io/badge/OptiKey-%3E%3D%204.1.0-brightgreen.svg)](docs/COMPATIBILITY.md)
[![Platform](https://img.shields.io/badge/Platform-Windows%20x64-lightgrey.svg)](docs/COMPATIBILITY.md)

[English](README.md) | [简体中文](README.zh-CN.md)

An open-source, non-proprietary external eye-tracker plugin that enables **OptiKey 4.x** (>= 4.1.0) to directly use the **Tobii Eye Tracker 5 (ET5)** as an eye-gaze point source.

Designed specifically for individuals with severe motor and speech disabilities (including ALS/MND locked-in users) who depend on eye tracking for daily computer access and communication.

---

## The Vision: Seamless User Experience

```
Tobii Experience Installed
         ↓
Launch OptiKey (>= 4.1.0)
         ↓
"Find more eye tracker options online"
         ↓
Select "Tobii Eye Tracker 5" & Click Install
         ↓
Works Immediately!
```
*End users do not need to hunt for DLL files, configure developer paths, or compile code.*

---

## Key Highlights

- **Zero Proprietary Binary Redistribution**: Does NOT bundle or redistribute any closed-source Tobii binaries or SDK headers. It safely and dynamically links to the user's legitimately installed local Tobii Experience runtime.
- **Fail-Safe Parameterless Instantiation**: Conforms strictly to OptiKey's `Activator.CreateInstance` loader requirements. The plugin constructor is completely hardware-independent and never crashes OptiKey.
- **Resilient Reconnection State Machine**: Automatically recovers from USB disconnects, driver restarts, and PC sleep/wake cycles with jittered exponential backoff.
- **Pixel-Accurate Coordinate Transformation**: Maps normalized eye-gaze coordinates directly to primary display physical pixels, ensuring accurate hit-testing across 100%, 125%, 150%, and 200% Windows display scaling.
- **Strict Privacy Guarantees**: Coordinates are processed in volatile memory in real time solely to select keys. Zero local gaze persistence; zero network transmission; zero telemetry.
- **No Silent Fallback to Simulated Gaze**: If hardware is detached, the plugin cleanly notifies the user. Simulated gaze is isolated in a separate testing package and will never activate in production.

---

## Requirements

1. **Operating System**: Windows 10 x64 (1903+) or Windows 11 x64.
2. **Hardware**: Tobii Eye Tracker 5 mounted to the primary monitor.
3. **Software**:
   - [Tobii Experience](https://gaming.tobii.com/getstarted/) installed and calibrated.
   - [OptiKey](https://github.com/OptiKey/OptiKey/releases) version **4.1.0 or newer** (tested on 4.1.1, 4.2.0, 4.2.2).

---

## Installation & Usage

### Method A: Automated In-App Installation (Recommended)
1. Ensure your Tobii Eye Tracker 5 is plugged in and calibrated in **Tobii Experience**.
2. Start **OptiKey**.
3. Open the **Management Console** (press `F12` or click the menu button) -> **Pointing & Selecting**.
4. Set the Pointing source to **Eye tracker**.
5. Click **Find more eye tracker options online**.
6. Select **Tobii Eye Tracker 5** (`yuanweize/OptiKey-ET5-Plugin`) and click **Install**.
7. OptiKey will download the verified plugin package into its plugins folder and immediately begin tracking.

### Method B: Manual Installation
1. Download the latest release package (`OptiKey-ET5-Plugin-v*.zip`) from the [Releases](https://github.com/yuanweize/OptiKey-ET5-Plugin/releases) page.
2. Extract the archive into:
   ```
   %LocalAppData%\OptiKey\OptiKey\EyeTrackerPlugins\yuanweize\OptiKey-ET5-Plugin\<version>\
   ```
3. In OptiKey Management Console -> Pointing & Selecting -> Eye tracker, choose **Tobii Eye Tracker 5**.

---

## Diagnostic Utility

If you encounter connection difficulties, a standalone hardware diagnostic tool is provided:
```powershell
# Run the PowerShell diagnostic helper
.\tools\HardwareDiagnostics\diagnose.ps1
```
Or run `ET5Diagnostics.exe` directly. It checks runtime detection, x64 architecture, driver state, device enumeration, and callback delivery without logging private gaze coordinates.

---

## Documentation and Architecture

- [Architecture Overview (ADR-001)](docs/adr/ADR-001-architecture-overview.md)
- [Upstream Contracts Pinning (ADR-002)](docs/adr/ADR-002-upstream-contracts-pinning.md)
- [Hardware-Independent Lifecycle (ADR-003)](docs/adr/ADR-003-hardware-independent-lifecycle.md)
- [Runtime Security and Dynamic Loading (ADR-004)](docs/adr/ADR-004-tobii-runtime-isolation-and-security.md)
- [Reconnect State Machine (ADR-005)](docs/adr/ADR-005-reconnect-state-machine.md)
- [Testing & Synthetic Gaze Isolation (ADR-006)](docs/adr/ADR-006-testing-and-synthetic-gaze-isolation.md)
- [Coordinate Space Semantics (ADR-007)](docs/adr/ADR-007-coordinate-space.md)
- [Runtime Research Deliverable](docs/RUNTIME_RESEARCH.md)
- [Compatibility Matrix](docs/COMPATIBILITY.md)
- [Hardware Validation Protocol](docs/HARDWARE_VALIDATION.md)
- [Troubleshooting Guide](docs/TROUBLESHOOTING.md)
- [Legal Notice & Licensing Provenance](LEGAL.md)
- [Privacy Policy](PRIVACY.md)

---

## License

This project is licensed under the **GNU General Public License v3.0** (GPL-3.0). See [LICENSE](LICENSE) for details.
All original code is free and open-source.
