[English](STATUS.md) | [简体中文](STATUS.zh-CN.md)

# Project Status & Readiness

- **Status Date**: 2026-09-21
- **Current Published Version**: `v0.1.0` (Historical Released Baseline)
- **Patch Candidate**: `v0.1.1` (Release Candidate Validated)
- **Active PR**: PR #5 (`audit/v0.1.1-hardening` -> `main`)

This document is the canonical readiness dashboard. It explicitly distinguishes automated Windows CI validation from physical hardware validation, runtime containment from native execution bounds, and legal positioning.

---

## Readiness Matrix

| Domain | Status | Evidence Classification & Details |
|:---|:---|:---|
| **Windows CI Matrix** | **CI VERIFIED** | Validated across both `upstream-main` and `pinned-stable` configurations on Windows Server 2022. Post-v0.1.0 hardening runs (`35631685399`) passed all build and test steps. |
| **Automated Test Suite** | **UNIT / INTEGRATION VERIFIED** | Expanded test suite covers runtime trust integrity, non-blocking callback disposal, configuration precedence, subscription transitions, coordinate mapping, and synthetic E2E streaming with zero failures and zero unhandled exceptions. |
| **OptiKey Plugin Loader** | **INTEGRATION / PACKAGE VERIFIED** | The x64 .NET Framework 4.6 loader harness successfully instantiates `ET5PointService` via parameterless constructor with zero early unmanaged allocation. |
| **Release ZIP Static Audit** | **PACKAGE VERIFIED** | Release archives strictly contain `LICENSE` and `OptiKey.ET5.Plugin.dll` (v0.1.0 SHA256: `4defb89f7ef20efcbf20a98b712c596c0b6fb1f7b1b2bd0af1b8a917f8b56fd0`). Zero proprietary Tobii DLLs, static libraries, headers, or synthetic test harnesses are packaged. |
| **Callback Pump Lifecycle** | **SYNTHETIC INTEGRATION VERIFIED** | Managed shutdown timeout and stuck-worker isolation are fully verified under simulated conditions. Disposal deadlocks resolved (locks released prior to worker join). *Note: Native Tobii callback return duration remains runtime dependent and not independently proven.* |
| **Ordinary User Flow** | **SYNTHETIC INTEGRATION VERIFIED** | Automatic single-device candidate selection enabled by default for seamless plug-and-play. Multi-device safety guard strictly refuses silent connection if multiple candidates exist. *Note: Identifies a single compatible Tobii Stream Engine runtime candidate; exact physical ET5 model confirmation requires hardware introspection.* |
| **Runtime Discovery & Trust** | **UNIT / SYSTEM VERIFIED** | Dynamic discovery locates installed `tobii_stream_engine.dll` from official services and registry. Authenticode verification enforces signature integrity (`SignatureStatus.Valid`), non-revocation, and Tobii signer identity. Distinct from full root chain trust. |
| **Zero Proprietary Bundling** | **SOURCE & PACKAGE VERIFIED** | Zero Tobii binaries or SDK archives are redistributed. Relies entirely on the user's locally installed, officially calibrated Tobii Experience software. |
| **Privacy Invariant** | **SOURCE & TEST VERIFIED** | Coordinates processed strictly in volatile memory. Zero persistence, zero gaze logging, zero network transmission. Local user filesystem paths sanitized in diagnostic logs. |
| **Documentation & Link Integrity** | **CI / SCRIPT VERIFIED** | Strict 1:1 English/Simplified Chinese documentation parity verified via `tools/scripts/check-doc-sync.ps1`. Zero machine-local `file://` or absolute paths outside code fences verified via `tools/scripts/check-doc-links.ps1`. |
| **Hardware Certification** | **UNVERIFIED** | Verified via synthetic end-to-end simulation pipelines; formal certified hardware sessions on physical ET5 devices pending community feedback. |
| **Authoritative Tobii ABI Provenance** | **PARTIALLY VERIFIED** | Stream Engine bindings verified against functional ABI and synthetic mocks; authoritative upstream C headers remain proprietary and closed-source. |

---

## Release Verdict

**Readiness Verdict**: **v0.1.0 RELEASED / v0.1.1 RELEASE CANDIDATE VALIDATED**

- Historical `v0.1.0` remains published on GitHub Releases and is intentionally unmodified to preserve cryptographic release integrity.
- Critical hardening fixes for post-release audit findings (SEC-01, CONC-01, CONF-01, LIFE-01, LIFE-02, DOC-01, DOC-02) have been validated on PR #5 across full Windows CI matrices and release dry-run packaging prior to merging into main and tagging `v0.1.1`.
