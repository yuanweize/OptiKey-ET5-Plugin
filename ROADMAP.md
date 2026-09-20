# Project Roadmap

## Phase 1: Repository Infrastructure & Architecture (Current)
- [x] GitHub repository setup with official `optikey-plugin` topics.
- [x] GPL-3.0 License, Security, Privacy, and Code of Conduct policies.
- [x] Upstream contract reference pinning (`OPTIKEY_CONTRACT_REF`).
- [x] Comprehensive Architectural Decision Records (ADR-001 through ADR-007).
- [x] Legal notice (`LEGAL.md`) and runtime research deliverable (`docs/RUNTIME_RESEARCH.md`).
- [x] Dual-target Windows CI workflow setup.

## Phase 2: Plugin Skeleton & State Machine
- [x] Upstream `JuliusSweetland.OptiKey.Contracts.dll` dynamic compilation in CI.
- [x] Fail-safe `ET5PointService` implementation with lazy activation.
- [x] Comprehensive `GazeServiceStateMachine`.
- [x] Isolated `SyntheticGazeProvider` test harness.
- [x] Exact OptiKey reflection loader unit test.

## Phase 3: Core Logic & Resilience
- [x] Exponential backoff reconnect policy with jitter.
- [x] `ScreenCoordinateMapper` with full DPI-scaling coverage.
- [x] Comprehensive test suites (100% pass on synthetic scenarios).

## Phase 4: Tobii Runtime Adapter
- [x] Secure `TobiiRuntimeLocator` with whitelist and PE64 validation.
- [x] Memory-safe P/Invoke bindings for Tobii Stream Engine.
- [x] `TobiiGazeProvider` integrating `tobii_wait_for_callbacks`.
- [x] Informative diagnostic error reporting.

## Phase 5: Hardware Diagnostics & Release Packaging
- [x] Console diagnostic utility `ET5Diagnostics.exe` and `diagnose.ps1`.
- [x] CI Release ZIP scanner (ensures zero proprietary binaries, zero test DLLs).
- [x] Automated SHA256 checksum generation.

## Phase 6: Hardware Testing & Release Gates
- [ ] Community Alpha Release (`v1.0.0-alpha.1`) as GitHub Pre-release.
- [ ] Physical ET5 validation across Windows 10/11 physical machines.
- [ ] 30-minute and 2-hour soak testing on live hardware.
- [ ] Promote to official General Availability release (`v1.0.0`) once hardware validation sign-off is complete.
