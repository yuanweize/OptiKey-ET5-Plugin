[English](README.md) | [简体中文](README.zh-CN.md)

# OptiKey-ET5-Plugin

[![Windows CI](https://github.com/yuanweize/OptiKey-ET5-Plugin/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/yuanweize/OptiKey-ET5-Plugin/actions/workflows/build-and-test.yml)
[![Release](https://img.shields.io/github/v/release/yuanweize/OptiKey-ET5-Plugin?include_prereleases)](https://github.com/yuanweize/OptiKey-ET5-Plugin/releases)
[![License: GPL-3.0-only](https://img.shields.io/badge/License-GPL--3.0--only-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%20x64-lightgrey.svg)](docs/COMPATIBILITY.md)
[![Privacy: Zero Telemetry](https://img.shields.io/badge/Privacy-Zero%20Telemetry-green.svg)](PRIVACY.md)

Open-source **Tobii Eye Tracker 5 (ET5)** input plugin for **OptiKey 4.x**.

Designed for individuals with ALS/MND, locked-in syndrome, and severe motor disabilities who rely on assistive computer access and augmentative and alternative communication (AAC).

- **Operating System**: Windows 10 / 11 (64-bit)
- **Host Application**: OptiKey 4.x
- **Hardware**: Tobii Eye Tracker 5
- **Zero Bundled Binaries**: Uses the user's locally installed Tobii Experience runtime
- **Strict Privacy**: Zero telemetry, zero gaze data logging or storage
- **Clean Architecture**: Interruptible callback pump with bounded shutdown protection

---

## Requirements

Before using this plugin, ensure your system has:

1. **Windows 10 or 11 (x64)**.
2. **Tobii Eye Tracker 5** connected to your primary display.
3. **Official Tobii Experience** software installed from [Tobii Gaming](https://gaming.tobii.com/getstarted/) with display setup and user calibration successfully completed.
4. **OptiKey 4.x** installed from [OptiKey Releases](https://github.com/OptiKey/OptiKey/releases).

---

## Quick Start & Installation

The standard user experience requires zero manual compiling or DLL copying:

```
Install Tobii Experience & Calibrate ET5
                 ↓
Launch OptiKey -> Open Management Console
                 ↓
Navigate to "Pointing & Selecting"
                 ↓
Click "Find more eye tracker options online"
                 ↓
Select "Tobii Eye Tracker 5" -> Click Install
                 ↓
Select as Active Source -> Start Gaze Control
```

### Manual Installation (Alternative)

If installing from a released ZIP archive:
1. Download `OptiKey-ET5-Plugin-v0.1.0.zip` from [GitHub Releases](https://github.com/yuanweize/OptiKey-ET5-Plugin/releases).
2. Exit OptiKey if it is running.
3. Extract `OptiKey.ET5.Plugin.dll` into your OptiKey plugins directory:
   ```text
   %APPDATA%\OptiKey\OptiKey\Plugins\
   ```
4. Start OptiKey.

---

## First-Time Setup in OptiKey

1. Press `Alt + M` or click the menu button to open the **Management Console**.
2. Go to the **Pointing & Selecting** tab.
3. Set **Pointing device** to:
   ```text
   Tobii Eye Tracker 5 (ET5)
   ```
4. Click **OK** to save. OptiKey will connect to your Eye Tracker 5 and start tracking your gaze immediately.

---

## Multi-Device & Advanced Configuration

For single-tracker setups (the vast majority of users), configuration is entirely automatic: the plugin safely binds to your single calibrated Tobii device.

If you have multiple Tobii devices connected, the plugin strictly refuses silent auto-selection to prevent accidentally controlling the wrong device. In that case, you can configure your preferred device index in:

```text
%APPDATA%\OptiKey-ET5-Plugin\et5-plugin.config
```

Example configuration:
```ini
# Auto-select if exactly 1 device is detected (default: true)
AutomaticDeviceSelection=true

# For multi-device setups, select device 0 or 1:
PreferredDeviceIndex=0

# Callback pump strategy: Polling (default) or WaitAndProcess
CallbackStrategy=Polling
```

---

## Troubleshooting

- **Plugin does not appear in OptiKey**:
  Verify that `OptiKey.ET5.Plugin.dll` is located in `%APPDATA%\OptiKey\OptiKey\Plugins\`.
- **"Tobii Runtime Not Found" error**:
  Ensure the official Tobii Experience or Tobii Service is running. Check Windows Services (`services.msc`) to verify `Tobii Service` is Running.
- **Red "Disconnected" state in OptiKey**:
  Unplug and reconnect your ET5 USB cable, then re-open OptiKey.
- **Diagnostic Tool**:
  If you encounter issues, run `ET5Diagnostics.exe` included with the release to inspect local runtime discovery, signature verification, and device enumeration.

For detailed diagnostic workflows, see the [Troubleshooting Guide](docs/TROUBLESHOOTING.md).

---

## Privacy & Safety Invariant

- **Volatile Processing Only**: Gaze coordinates are processed in real-time memory solely to position the OptiKey pointer and trigger dwell selections.
- **Zero Storage**: Coordinates, eye images, and device URLs are **never saved to disk or temporary files**.
- **Zero Telemetry**: No analytics, telemetry, or remote network requests exist in this codebase.
- **Fail-Safe Isolation**: Native worker threads run bounded shutdown loops to protect OptiKey against unmanaged host crashes.

Review our full [Privacy Policy](PRIVACY.md) and [Security Architecture](SECURITY.md).

---

## Support & Contributing

- **Report an Issue**: Open a ticket using our [Issue Templates](https://github.com/yuanweize/OptiKey-ET5-Plugin/issues/new/choose).
- **Contributing**: Read our [Contributing Guidelines](CONTRIBUTING.md) and [Agent SOP](docs/AGENT_SOP.md).
- **Documentation Parity**: All documentation is maintained in both English and Simplified Chinese per our [Documentation Policy](docs/DOCUMENTATION_POLICY.md).

---

## Technical & Architecture Documentation

For engineers, researchers, and maintainers:

- [Architecture Overview (ADR-001)](docs/adr/ADR-001-architecture-overview.md)
- [Code Audit Report (September 2026)](docs/CODE_AUDIT_2026-09.md)
- [Tobii Runtime Isolation & Security (ADR-004)](docs/adr/ADR-004-tobii-runtime-isolation-and-security.md)
- [In-Process vs. RuntimeHost Architecture (ADR-009)](docs/adr/ADR-009-in-process-vs-runtime-host-architecture.md)
- [Hardware & Runtime Compatibility Matrix](docs/COMPATIBILITY.md)
- [Hardware Validation Protocol](docs/HARDWARE_VALIDATION.md)
- [Legal & Third-Party Notices](LEGAL.md)

---

## License

This project is licensed under the **GNU General Public License v3.0 only** ([GPL-3.0-only](LICENSE)).
No proprietary Tobii software, binaries, or headers are bundled or redistributed.
