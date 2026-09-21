# OptiKey-ET5-Plugin

[![Windows CI](https://github.com/yuanweize/OptiKey-ET5-Plugin/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/yuanweize/OptiKey-ET5-Plugin/actions/workflows/build-and-test.yml)
[![License: GPL-3.0-only](https://img.shields.io/badge/License-GPL--3.0--only-blue.svg)](LICENSE)
[![Readiness](https://img.shields.io/badge/readiness-NOT%20READY-red.svg)](STATUS.md)
[![Platform](https://img.shields.io/badge/Platform-Windows%20x64-lightgrey.svg)](docs/COMPATIBILITY.md)

[English](README.md) | [简体中文](README.zh-CN.md)

An experimental open-source project intended to connect **OptiKey 4.x** to **Tobii Eye Tracker 5 (ET5)**. It is currently **NOT READY** for hardware use or end-user installation.

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
         Hardware validation pending
```
*End users do not need to hunt for DLL files, configure developer paths, or compile code.*

---

## Key Highlights

- **Zero Proprietary Binary Redistribution**: The reviewed source tree and CI package do not bundle Tobii binaries, libraries, headers, or SDK archives.
- **Hardware-Independent Construction**: The packaged-loader harness confirms that `Activator.CreateInstance` can construct and dispose the point service without loading `tobii_stream_engine.dll`.
- **Reconnection State Machine**: Contains reconnect handling for transient runtime failures. Physical disconnect, sleep/resume, and DPI behavior remain unverified.
- **Coordinate Transformation**: Provides a normalized-to-display mapping tested with synthetic metrics. Physical DPI and multi-monitor behavior remain unverified.
- **Strict Privacy Guarantees**: Coordinates are processed in volatile memory in real time solely to select keys. Zero local gaze persistence; zero network transmission; zero telemetry.
- **No Silent Fallback to Simulated Gaze**: Simulated gaze is isolated in a separate test assembly and is excluded from the audited package.

---

## Intended Requirements

1. **Operating-system target**: Windows x64; exact supported versions remain unverified for ET5 and the applicable Tobii API.
2. **Hardware target**: Tobii Eye Tracker 5 mounted to the primary monitor; no physical configuration has been validated.
3. **Software**:
   - [Tobii Experience](https://gaming.tobii.com/getstarted/) installed and calibrated.
   - [OptiKey](https://github.com/OptiKey/OptiKey/releases) version **4.2.2 contract reference**. Other versions are not verified by this repository.

---

## Installation & Usage

There is no supported installation yet, no Git tag, and no GitHub Release. The production path intentionally refuses to connect until device identity, ABI, callback shutdown, runtime trust, physical hardware behavior, and Tobii permission are resolved.

The intended future user flow remains: install and calibrate official Tobii software, install OptiKey, find the plugin through OptiKey, select it, and use ET5 as gaze input. That flow is a goal, not current functionality.

---

## Diagnostic Utility

The current safe inventory script records installation metadata without initializing Tobii or collecting gaze data:
```powershell
.\tools\HardwareDiagnostics\inventory-tobii-runtime.ps1
```
The callback-based `ET5Diagnostics.exe` remains fail-closed and is not approved for hardware use.

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

This project is licensed under the **GNU General Public License v3.0 only** (GPL-3.0-only). See [LICENSE](LICENSE) for details.
All original code is free and open-source.
