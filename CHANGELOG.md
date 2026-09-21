[English](CHANGELOG.md) | [简体中文](CHANGELOG.zh-CN.md)

# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-09-21

### Added
- Complete open-source Tobii Eye Tracker 5 (ET5) input provider for OptiKey 4.x.
- Automatic single-device candidate selection enabling seamless out-of-the-box user experience.
- Strict multi-device safety guard preventing accidental connection to unintended Tobii hardware.
- Dynamic runtime discovery searching official Tobii Service, installation paths, and registry entries.
- Authenticode code-signing verification (`WinVerifyTrust`) enforcing Tobii Subject Name validity before DLL loading.
- Interruptible `ICallbackPump` architecture (`ProcessOnlyPollingPump` and `WaitAndProcessCallbackPump`) with bounded shutdown protection.
- Stuck-worker isolation preventing native use-after-free crashes during teardown.
- Standalone hardware diagnostic tool (`ET5Diagnostics.exe`).
- Dual-matrix Windows x64 CI pipeline validating against both pinned stable and upstream-main OptiKey contracts.
- 1:1 bilingual documentation parity across all user manuals, architectural records, and project policies.
- Comprehensive code audit (`docs/CODE_AUDIT_2026-09.md`) resolving all P0/P1 lifecycle and concurrency issues.

### Security
- Zero Tobii proprietary binaries bundled in Git or packaged release archives.
- Strict privacy invariant: volatile memory processing only, zero gaze coordinates or device URLs stored or transmitted.
