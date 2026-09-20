# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Complete architecture and component boundary definition (ADR-001 through ADR-007).
- Upstream OptiKey contract pinning (`OPTIKEY_CONTRACT_REF`) targeting stable v4.2.2.
- Hardware-independent parameterless constructor for `ET5PointService` ensuring fail-safe instantiation.
- Strict whitelist-based Tobii runtime locator with PE64 architecture verification.
- Explicit lifecycle state machine (`Created`, `Starting`, `Connected`, `Reconnecting`, `Stopping`, `Stopped`, `Disposed`).
- Resilient exponential backoff reconnect policy with jitter.
- Physical screen pixel coordinate mapping pipeline with multi-DPI consistency.
- Isolated synthetic gaze test harness (`OptiKey.ET5.Plugin.Synthetic`) isolated from production releases.
- Standalone hardware diagnostic utility (`ET5Diagnostics.exe`) and PowerShell launcher (`diagnose.ps1`).
- Dual-matrix Windows CI build pipeline with automated release ZIP verification.
