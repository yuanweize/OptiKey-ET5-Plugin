[English](CHANGELOG.md) | [简体中文](CHANGELOG.zh-CN.md)

# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.1] - 2026-09-21

### Security
- Enforce strict Authenticode cryptographic signature integrity (`SignatureStatus.Valid`) and non-revocation in `TobiiRuntimeLocator` before accepting candidate binaries (SEC-01).
- Prevent local filesystem username leakage in diagnostic and trace logs via `PathSanitizer` (PRIV-01).

### Fixed
- Eliminate self-induced lock contention and potential deadlocks during callback pump `Dispose()` by releasing `stateLock` prior to joining worker threads (CONC-01).
- Enforce strict configuration precedence: Defaults -> Config File -> Environment Variable Overrides across all 6 configuration properties (CONF-01).
- Correct `ET5PointService.Point` subscription lifecycle so that `gazeProvider.Start()` is invoked strictly on the 0 -> 1 subscriber transition rather than on every subscriber addition (LIFE-01).
- Release `eventLock` before invoking `gazeProvider.Dispose()` in `ET5PointService` to eliminate lock inversion hazards (LIFE-02).
- Correct OptiKey plugin manual installation directory path to `%APPDATA%\OptiKey\OptiKey\EyeTrackerPlugins\OptiKey-ET5-Plugin\` matching current upstream OptiKey source (DOC-02).
- Strengthen `check-doc-links.ps1` to detect and fail on machine-local `file://` URIs and absolute paths outside code fences (DOC-01).
- Fix obsolete native binding file reference in `AGENTS.md` Mandate 17 (SOP-01).

### Changed
- Clarify device candidate selection and ABI documentation: distinguish single compatible Tobii Stream Engine candidates from verified physical ET5 hardware detection (ABI-01, ID-01).
- Accurately qualify callback shutdown claims: distinguish CI-verified managed shutdown timeout containment from runtime-dependent native callback return duration.

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
