[English](STATUS.md) | [简体中文](STATUS.zh-CN.md)

# Project Status & Readiness

**Status Date**: 2026-09-21  
**Current Version**: `v0.1.0` (Released)  
**Current Active Branch**: `main` (clean repository)

This document is the canonical readiness dashboard. It distinguishes automated Windows CI validation from physical hardware validation and legal positioning.

---

## Readiness Matrix

| Domain | Status | Evidence & Details |
|:---|:---|:---|
| **Windows CI Matrix** | **VERIFIED** | CI Runs `35611105483`, `35611746478`, and Release run `35612044235` all succeeded across both `upstream-main` and `pinned-stable` configurations. |
| **Automated Test Suite** | **VERIFIED** | **124 tests executed, 124 passed, 0 failed**. Complete test runner logs verified with zero unhandled exceptions, zero process crashes, and zero `NullReferenceException`s. |
| **OptiKey Plugin Loader** | **VERIFIED** | The x64 .NET Framework 4.6 loader harness successfully instantiated `ET5PointService` via parameterless constructor and verified zero early unmanaged allocation. |
| **Release ZIP Static Audit** | **VERIFIED** | Package contains exclusively `LICENSE` and `OptiKey.ET5.Plugin.dll` (SHA256: `4defb89f7ef20efcbf20a98b712c596c0b6fb1f7b1b2bd0af1b8a917f8b56fd0`). Zero proprietary Tobii DLLs, static libraries, headers, or synthetic test harnesses are packaged. |
| **Callback Pump Lifecycle** | **VERIFIED** | Integrated `ICallbackPump` architecture (`ProcessOnlyPollingPump` and `WaitAndProcessCallbackPump`) with bounded shutdown and stuck-worker isolation. |
| **Ordinary User Flow** | **VERIFIED** | Automatic single-device candidate selection enabled by default for seamless plug-and-play. Multi-device safety guard strictly refuses silent connection if multiple devices are present. |
| **Runtime Discovery & Trust** | **VERIFIED** | Dynamic discovery locates installed `tobii_stream_engine.dll` from official services and registry; Authenticode `WinVerifyTrust` validates Tobii code signature. |
| **Zero Proprietary Bundling** | **VERIFIED** | Zero Tobii binaries or SDK archives are redistributed. Relies entirely on the user's locally installed, officially calibrated Tobii Experience. |
| **Privacy Invariant** | **VERIFIED** | Coordinates processed strictly in volatile memory. Zero persistence, zero gaze logging, zero network transmission. |
| **Documentation Parity** | **VERIFIED** | Strict 1:1 English/Simplified Chinese documentation parity verified via `../../tools/scripts/check-doc-sync.ps1`. |
| **Hardware Certification** | **UNVERIFIED** | Verified via synthetic end-to-end simulation pipelines; formal certified hardware sessions on physical ET5 devices pending broad community feedback. |

---

## Release Verdict

**Readiness Verdict**: **v0.1.0 RELEASED**

The initial public release `v0.1.0` has been successfully tagged, verified through full CI gates, and published to GitHub Releases with verified package checksums and bilingual release notes.
