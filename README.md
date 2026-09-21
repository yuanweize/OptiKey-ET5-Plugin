[English](README.md) | [简体中文](README.zh-CN.md)

# OptiKey-ET5-Plugin

[![Windows CI](https://github.com/yuanweize/OptiKey-ET5-Plugin/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/yuanweize/OptiKey-ET5-Plugin/actions/workflows/build-and-test.yml)
[![Release](https://img.shields.io/github/v/release/yuanweize/OptiKey-ET5-Plugin?include_prereleases)](https://github.com/yuanweize/OptiKey-ET5-Plugin/releases)
[![License: GPL-3.0-only](https://img.shields.io/badge/License-GPL--3.0--only-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%20x64-lightgrey.svg)](docs/user/COMPATIBILITY.md)
[![Privacy: Zero Telemetry](https://img.shields.io/badge/Privacy-Zero%20Telemetry-green.svg)](docs/policies/PRIVACY.md)

Open-source **Tobii Eye Tracker 5 (ET5)** input plugin for **OptiKey 4.x**.

Designed for individuals with ALS/MND, locked-in syndrome, and severe motor disabilities who rely on assistive computer access and augmentative and alternative communication (AAC).

- **Operating System**: Windows 10 / 11 (64-bit)
- **Host Application**: OptiKey 4.x
- **Hardware**: Tobii Eye Tracker 5
- **Zero Bundled Binaries**: Seamlessly uses the user's locally installed Tobii Experience runtime
- **Strict Privacy Invariant**: Real-time memory processing only; zero gaze logging, persistence, or telemetry
- **Clean Architecture**: Interruptible callback pump with bounded shutdown and stuck-worker isolation

---

## Requirements

Before using this plugin, ensure your workstation satisfies:

1. **Windows 10 or 11 (x64)**.
2. **Tobii Eye Tracker 5** connected to your primary display.
3. **Official Tobii Experience** installed from [Tobii Gaming Getting Started](https://gaming.tobii.com/getstarted/) with display setup and user calibration successfully completed.
4. **OptiKey 4.x** installed from [OptiKey Releases](https://github.com/OptiKey/OptiKey/releases).

For display resolutions, aspect ratios, and DPI scaling information, consult the [Compatibility Guide](docs/user/COMPATIBILITY.md).

---

## Quick Start

The standard user experience requires zero code compilation or manual DLL copying:

```text
Install Tobii Experience & Calibrate ET5
                 ↓
Launch OptiKey -> Open Management Console (Alt + M)
                 ↓
Navigate to "Pointing & Selecting"
                 ↓
Click "Find more eye tracker options online"
                 ↓
Select "Tobii Eye Tracker 5" -> Click Install
                 ↓
Select as Pointing Device -> Start Gaze Control
```

For complete offline installation and multi-device configuration steps, see the **[Installation & Setup Guide](docs/user/INSTALLATION.md)**.

---

## Troubleshooting

- **Plugin not found in OptiKey**: Verify that `OptiKey.ET5.Plugin.dll` is located in `%APPDATA%\OptiKey\OptiKey\Plugins\`.
- **Runtime Not Found**: Ensure official Tobii Experience is installed and the `Tobii Service` is running in `services.msc`.
- **Red Disconnected State**: Unplug and reconnect the Eye Tracker 5 USB cable, then re-open OptiKey.
- **Diagnostic Tool**: Run `tools/HardwareDiagnostics/diagnose.ps1` to inspect runtime discovery, PE architecture, and code signatures.

Detailed diagnostic flows are available in the **[Troubleshooting Guide](docs/user/TROUBLESHOOTING.md)**.

---

## Privacy & Safety

- **Volatile Processing**: Gaze coordinates are processed in volatile memory solely to dispatch real-time pointer events.
- **Zero Storage**: Raw coordinates, eye images, and device URLs are **never saved to disk or transmitted**.
- **Zero Telemetry**: No analytics or remote network calls exist in this codebase.
- **Fail-Safe Worker Isolation**: Bounded shutdown loops protect the OptiKey host application from unmanaged crashes.

Read our full **[Biometric Privacy Policy](docs/policies/PRIVACY.md)** and **[Security Architecture](.github/SECURITY.md)**.

---

## Documentation Navigation

Comprehensive documentation is organized by audience in our **[Documentation Hub](docs/README.md)**:

- **[User Guides](docs/README.md#1-user-guide-docsuser)**: [Installation](docs/user/INSTALLATION.md), [Troubleshooting](docs/user/TROUBLESHOOTING.md), [Compatibility](docs/user/COMPATIBILITY.md)
- **[Project Status](docs/README.md#2-project-status--roadmap-docsproject)**: [Status Dashboard](docs/project/STATUS.md), [Roadmap](docs/project/ROADMAP.md)
- **[Policies & Compliance](docs/README.md#3-policies--legal-docspolicies)**: [Legal & Provenance](docs/policies/LEGAL.md), [Privacy](docs/policies/PRIVACY.md), [Third-Party Notices](docs/policies/THIRD_PARTY_NOTICES.md)
- **[Development & Architecture](docs/README.md#4-development--maintainer-sop-docsdevelopment)**: [Agent SOP](docs/development/AGENT_SOP.md), [Documentation Policy](docs/development/DOCUMENTATION_POLICY.md), [Hardware Protocol](docs/development/HARDWARE_VALIDATION.md), [Architecture Decision Records (ADRs)](docs/README.md#6-architecture-decision-records-docsadr)
- **[Technical Research](docs/README.md#5-technical-research-docsresearch)**: [ABI Provenance](docs/research/ABI_PROVENANCE.md), [Runtime Research](docs/research/RUNTIME_RESEARCH.md), [Callback Research](docs/research/CALLBACK_RESEARCH.md)

---

## Community & Contributing

- **Report Issues**: Submit bug reports via [GitHub Issue Templates](https://github.com/yuanweize/OptiKey-ET5-Plugin/issues/new/choose).
- **Contributing**: Review our [Contributing Guidelines](.github/CONTRIBUTING.md) and [Code of Conduct](.github/CODE_OF_CONDUCT.md).
- **Agent Instructions**: Coding agents must follow [`AGENTS.md`](AGENTS.md).

---

## License

This project is licensed under the **GNU General Public License v3.0 only** ([GPL-3.0-only](LICENSE)).
No proprietary Tobii software, binaries, or headers are bundled or redistributed.
