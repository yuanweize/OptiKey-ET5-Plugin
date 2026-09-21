[English](POST_RELEASE_AUDIT_2026-09.md) | [简体中文](POST_RELEASE_AUDIT_2026-09.zh-CN.md)

# Independent Post-v0.1.0 Security, Concurrency, and Architecture Audit

> **Audit Date**: 2026-09-21  
> **Auditor Role**: Independent Principal Software Auditor, Concurrency Reviewer, Security Reviewer, Release Engineer, and OptiKey Integration Reviewer  
> **Baseline Commit**: `8d473333098b7826e7ca172bd5d7c31776b2e76d` (Merged PR #4)  
> **Published Release Target**: `v0.1.0` (ZIP SHA256: `4defb89f7ef20efcbf20a98b712c596c0b6fb1f7b1b2bd0af1b8a917f8b56fd0`)  
> **Target Hardening Branch**: `audit/v0.1.1-hardening`

---

## Executive Summary

An exhaustive independent audit of `OptiKey-ET5-Plugin` post-v0.1.0 was conducted across source code, active tests, CI execution logs, release artifacts, documentation links, and upstream `OptiKey/OptiKey` source code.

Every handoff lead was independently verified against active code and live GitHub resources. Multiple critical and high-severity defects were confirmed, including:
1. **P0 Security Defect**: Runtime locator accepted binaries with bad digests / invalid signatures as long as certificate metadata contained "Tobii".
2. **P0 Concurrency Defect**: `ProcessOnlyPollingPump.Dispose()` and `WaitAndProcessCallbackPump.Dispose()` held `stateLock` while joining worker threads, deadlocking with worker thread shutdown and causing a mandatory 500ms timeout failure on every disposal.
3. **P1 Configuration Precedence Bug**: Configuration file values unexpectedly overwrote environment variable developer overrides for multiple settings.
4. **P1 Lifecycle Defects**: `ET5PointService.Point` event handler `add` triggered startup repeatedly for every subscriber, and `ET5PointService.Dispose()` held `eventLock` across provider disposal.
5. **P1 Documentation & Link Validator Defect**: `check-doc-links.ps1` explicitly ignored `file://` URIs, masking absolute machine paths in documentation; installation guides referenced incorrect OptiKey plugin directories.
6. **P2 Accuracy & Claim Inconsistencies**: Stale fail-closed ABI claims in documentation, inaccurate device identity claims ("ET5 detected"), and overpromised "guaranteed bounded native shutdown" claims.

---

## Findings Matrix

| Finding ID | Severity | Category | Target Component | Status |
| :--- | :--- | :--- | :--- | :--- |
| **SEC-01** | **P0** | Security | `TobiiRuntimeLocator.cs` | Identified / Pending Fix |
| **CONC-01** | **P0** | Concurrency | `ProcessOnlyPollingPump.cs`, `WaitAndProcessCallbackPump.cs` | Identified / Pending Fix |
| **CONF-01** | **P1** | Architecture | `PluginConfiguration.cs` | Identified / Pending Fix |
| **LIFE-01** | **P1** | Lifecycle | `ET5PointService.cs` | Identified / Pending Fix |
| **LIFE-02** | **P1** | Concurrency | `ET5PointService.cs` | Identified / Pending Fix |
| **DOC-01** | **P1** | CI / Docs | `check-doc-links.ps1`, `CALLBACK_RESEARCH.md` | Identified / Pending Fix |
| **DOC-02** | **P1** | Integration | `INSTALLATION.md`, `README.md`, user guides | Identified / Pending Fix |
| **ABI-01** | **P2** | Consistency | `ABI_PROVENANCE.md` | Identified / Pending Fix |
| **ID-01** | **P2** | Accuracy | `README.md`, `STATUS.md`, Release notes | Identified / Pending Fix |
| **SHUT-01** | **P2** | Accuracy | `CALLBACK_RESEARCH.md`, `PluginConfiguration.cs` | Identified / Pending Fix |
| **SOP-01** | **P2** | Documentation | `AGENTS.md`, `AGENTS.zh-CN.md` | Identified / Pending Fix |
| **PRIV-01** | **P2** | Privacy | `TobiiRuntimeLocator.cs`, `TobiiStreamEngineBinding.cs` | Identified / Pending Fix |
| **CI-01** | **P2** | CI / Automation | `.github/workflows/build-and-test.yml` | Identified / Pending Fix |
| **CI-02** | **P2** | CI Supply Chain | Node.js 20 deprecations in GitHub Actions | Documented / Tracked |
| **GOV-01** | **P2** | Governance | GitHub branch protection rules on `main` | Documented / Recommended |

---

## Detailed Findings

### [SEC-01] P0: Runtime Locator Accepts Tampered Binaries with Bad Digest / Invalid Signature

- **File**: `src/OptiKey.ET5.Plugin/Runtime/TobiiRuntimeLocator.cs:97-119`
- **Observed Behavior**: `TobiiRuntimeLocator.LocateRuntime()` only checks `if (!trustResult.SignerMatchesTobii) continue;`. It completely omits checking `trustResult.SignatureStatus == SignatureStatus.Valid` and `trustResult.ChainStatus != ChainStatus.Revoked`. Because `X509Certificate.CreateFromSignedFile` extracts subject metadata from the certificate table even when the file hash is corrupted or modified, a modified or tampered DLL with `TRUST_E_BAD_DIGEST` (`SignatureStatus.HashMismatch`) is accepted and loaded.
- **Expected Behavior**: A binary must strictly satisfy Authenticode signature integrity (`SignatureStatus.Valid`), publisher identity (`SignerMatchesTobii == true`), and not be revoked (`ChainStatus != ChainStatus.Revoked`). Any binary with `HashMismatch`, `Unsigned`, `Error`, or `Revoked` status must be rejected immediately.
- **Reproduction**: Pass a `FakeTrustVerifier` returning `SignatureStatus.HashMismatch` with `SignerMatchesTobii: true` to `TobiiRuntimeLocator`. `LocateRuntime()` returns `IsFound == true` and accepts the binary.
- **User Impact**: Critical security vulnerability. Tampered or malicious binaries mimicking Tobii certificate headers could be loaded into the OptiKey host process.
- **Fix**: Require `trustResult.SignatureStatus == SignatureStatus.Valid && trustResult.SignerMatchesTobii && trustResult.ChainStatus != ChainStatus.Revoked` in `TobiiRuntimeLocator.LocateRuntime()`.
- **Regression Test**: Add tests in `RuntimeTrustVerifierTests.cs` verifying rejection of `SignatureStatus.HashMismatch`, `SignatureStatus.Unsigned`, `SignatureStatus.Error`, and `ChainStatus.Revoked`.
- **Status**: Identified / Pending Fix.

---

### [CONC-01] P0: Callback Pump Dispose Deadlocks with Worker Shutdown by Holding Lock Across Join

- **File**: `src/OptiKey.ET5.Plugin/Runtime/Callbacks/ProcessOnlyPollingPump.cs:204-218`, `src/OptiKey.ET5.Plugin/Runtime/Callbacks/WaitAndProcessCallbackPump.cs:224-237`
- **Observed Behavior**: Both callback pump implementations execute:
  ```csharp
  public void Dispose()
  {
      lock (stateLock)
      {
          RequestStop();
          Join(TimeSpan.FromMilliseconds(500));
          stopEvent.Dispose();
          state = CallbackPumpState.Disposed;
      }
  }
  ```
  Meanwhile, the worker loop at thread exit executes `lock (stateLock) { state = CallbackPumpState.Stopped; }`. The worker thread is blocked waiting for `stateLock` while `Dispose()` holds `stateLock` waiting inside `Join()`. This causes `Join(500)` to always time out after 500ms, transitions pump state to `TimedOut`, and logs a false error.
- **Expected Behavior**: Invariant: Never hold a lock required by a worker while joining that worker. `Dispose()` must snapshot the thread and request stop under lock, release the lock, join the worker outside the lock, and then re-acquire the lock to finalize state and unmanaged resources.
- **Reproduction**: Call `pump.Start(); pump.Dispose();` on a running pump. Measure elapsed time and pump state. Under the buggy implementation, disposal takes >= 500ms and state is `TimedOut`.
- **User Impact**: Host application freeze of 500ms on plugin teardown/switch; erroneous timeout and stuck-worker warnings.
- **Fix**: Refactor `Dispose()` in both pumps to request stop under lock, release lock, join outside lock, and clean up resources under lock.
- **Regression Test**: Add deterministic tests asserting direct `Dispose()` on running pumps exits in < 100ms with state `Disposed`.
- **Status**: Identified / Pending Fix.

---

### [CONF-01] P1: Inconsistent Configuration Precedence and Config File Overwrites Environment

- **File**: `src/OptiKey.ET5.Plugin/Core/PluginConfiguration.cs:96-121, 234-278`
- **Observed Behavior**: `PluginConfiguration.Load()` executed `LoadFromEnvironment` followed by `LoadFromConfigFile`. In `LoadFromConfigFile`, `AutomaticDeviceSelection`, `AllowUnverifiedTobiiDevice`, `CallbackStrategy`, and `PollIntervalMs` unconditionally overwrote environment variables, while `PreferredDeviceIndex` and `PreferredDeviceUrl` were conditionally guarded. This contradicted the documented precedence order (`defaults -> config file -> environment overrides`).
- **Expected Behavior**: Uniform precedence across all configuration fields: defaults applied first, overridden by config file entries, and finally overridden by environment variables.
- **Reproduction**: Set environment variable `ET5_AUTOMATIC_DEVICE_SELECTION=false`, but have config file set `AutomaticDeviceSelection=true`. The config file unexpectedly wins.
- **User Impact**: Developer environment variable overrides fail to take effect if a local config file exists.
- **Fix**: Invert load order to `LoadFromConfigFile(config)` first, then `LoadFromEnvironment(config)` second, and ensure all config file settings apply cleanly.
- **Regression Test**: Add tests covering precedence for every individual configuration setting.
- **Status**: Identified / Pending Fix.

---

### [LIFE-01] P1: ET5PointService.Point Event Handler Starts Provider on Every Subscription Addition

- **File**: `src/OptiKey.ET5.Plugin/ET5PointService.cs:68-77`
- **Observed Behavior**: `public event EventHandler<Timestamped<Point>> Point { add { lock(eventLock) { pointEvent += value; EnsureStarted(); } } }` calls `EnsureStarted()` on every subscription addition, violating the documented 0 -> 1 subscriber start transition.
- **Expected Behavior**: `EnsureStarted()` must only be called when transitioning from 0 subscribers to 1 subscriber (`pointEvent == null` prior to addition).
- **Reproduction**: Subscribe two event handlers sequentially. `EnsureStarted()` is called twice.
- **User Impact**: Redundant startup attempts and potential state churn on multi-subscriber scenarios.
- **Fix**: Check `bool isFirst = (pointEvent == null); pointEvent += value; if (isFirst) EnsureStarted();`.
- **Regression Test**: Add tests verifying `Start()` is called exactly once when multiple subscribers register, and `Stop()` is called only when the last subscriber unregisters.
- **Status**: Identified / Pending Fix.

---

### [LIFE-02] P1: ET5PointService Holds eventLock Across Provider Disposal

- **File**: `src/OptiKey.ET5.Plugin/ET5PointService.cs:189-216`
- **Observed Behavior**: `Dispose()` held `lock (eventLock)` while calling `gazeProvider.Dispose()`. If concurrent gaze point callbacks or error events fired on other threads trying to acquire `eventLock`, lock contention or deadlock risks occurred.
- **Expected Behavior**: Never hold event locks across long-running or blocking provider teardowns.
- **Fix**: Unhook events and clear delegates inside `eventLock`, then call `gazeProvider.Dispose()` outside `eventLock`.
- **Regression Test**: Add test disposing `ET5PointService` concurrently with active callback simulation.
- **Status**: Identified / Pending Fix.

---

### [DOC-01] P1: check-doc-links.ps1 Ignored file:// Links Allowing Machine-Local Links to Pass CI

- **Observed Behavior**: `check-doc-links.ps1` regex explicitly ignored `file://` URIs (`^(https?://|mailto:|#|file://)`). As a result, machine-local absolute links passed CI undetected:
  ```text
  file:///Users/username/repo/...
  ```
  Additionally, these links targeted stale pre-refactor paths.
- **Expected Behavior**: `check-doc-links.ps1` must reject any `file://` or machine-local absolute paths outside code fences:
  ```text
  file://, /Users/, C:\Users\, D:\a\
  ```
- **Reproduction**: Run `check-doc-links.ps1` on current `main`. It passes despite machine-specific links existing.
- **User Impact**: Broken links for external developers; leakage of author's local directory structure in public documentation.
- **Fix**: Update documentation links to valid relative paths; strengthen `check-doc-links.ps1` to fail on `file://` and machine-specific absolute paths.
- **Regression Test**: Add automated checker self-test validating rejection of machine-local paths.
- **Status**: Identified / Pending Fix.

---

### [DOC-02] P1: User Documentation Stated Incorrect OptiKey Plugin Installation Root

- **File**: `docs/user/INSTALLATION.md:27, 45`, `docs/user/INSTALLATION.zh-CN.md:27, 45`, `README.md`, `README.zh-CN.md`
- **Observed Behavior**: Documentation directed users to install manual plugins to `%APPDATA%\OptiKey\OptiKey\Plugins\`.
- **Expected Behavior**: Current upstream OptiKey source (`JuliusSweetland.OptiKey.Core/Services/PluginEngine/EyeTrackerPluginEngine.cs:157-164`) defines the canonical directory as `%APPDATA%\OptiKey\OptiKey\EyeTrackerPlugins\`.
- **Reproduction**: Inspect `EyeTrackerPluginEngine.GetTopLevelPluginDirectory()` in `OptiKey/OptiKey` source code.
- **User Impact**: Manual installation fails completely if users copy files to the documented path because OptiKey does not scan `%APPDATA%\OptiKey\OptiKey\Plugins\`.
- **Fix**: Correct all English and Chinese documentation to `%APPDATA%\OptiKey\OptiKey\EyeTrackerPlugins\`.
- **Status**: Identified / Pending Fix.

---

### [ABI-01] P2: Stale Fail-Closed Claim in ABI_PROVENANCE Contradicted Active Provider

- **File**: `docs/research/ABI_PROVENANCE.md:39`, `docs/research/ABI_PROVENANCE.zh-CN.md:39`
- **Observed Behavior**: Documentation stated: "the production path currently fails closed after enumeration and before device creation." In reality, `TobiiGazeProvider` automatically binds to `deviceUrls[0]` when exactly one device candidate is detected.
- **Expected Behavior**: Documentation must accurately describe active source code behavior.
- **Fix**: Update ABI provenance documentation to describe single-device auto-binding, multi-device safety guards, and explicit device index configuration.
- **Status**: Identified / Pending Fix.

---

### [ID-01] P2: Overstated Device Model Identification Claims in README and Release Notes

- **File**: `README.md:12`, `README.zh-CN.md:12`, `docs/project/STATUS.md`, Release notes
- **Observed Behavior**: Documentation claimed "Eye Tracker 5 detected", whereas code only enumerates device candidates from the Tobii Stream Engine runtime and auto-connects if exactly one candidate exists. The exact hardware model is not verified due to unproven `tobii_get_device_info` ABI.
- **Expected Behavior**: Use precise wording: "single compatible Tobii runtime candidate" rather than asserting hardware model proof.
- **Fix**: Update documentation and logs to precise terminology.
- **Status**: Identified / Pending Fix.

---

### [SHUT-01] P2: Overpromised Native Bounded Shutdown Claims

- **File**: `docs/research/CALLBACK_RESEARCH.md`, `src/OptiKey.ET5.Plugin/Core/PluginConfiguration.cs:14`, `src/OptiKey.ET5.Plugin/Runtime/Callbacks/ProcessOnlyPollingPump.cs:12`
- **Observed Behavior**: Comments and docs asserted "guaranteed bounded native shutdown" for `tobii_device_process_callbacks()`, whereas the native return behavior is empirical and unproven on real ET5 hardware.
- **Expected Behavior**: Clearly separate managed shutdown containment (CI verified) from native Tobii runtime callback return behavior (unproven / runtime-dependent).
- **Fix**: Clarify claims across documentation and code comments.
- **Status**: Identified / Pending Fix.

---

### [SOP-01] P2: AGENTS.md Mandate 17 Referenced Nonexistent Native Binding Filename

- **File**: `AGENTS.md:26`, `AGENTS.zh-CN.md:26`
- **Observed Behavior**: Mandate 17 cited `TobiiStreamEngineNative.cs`. The actual file is `src/OptiKey.ET5.Plugin/Runtime/Interop/TobiiStreamEngineBinding.cs`.
- **Expected Behavior**: All filenames cited in repository mandates must exist.
- **Fix**: Correct the path in both `AGENTS.md` and `AGENTS.zh-CN.md`.
- **Status**: Identified / Pending Fix.

---

### [PRIV-01] P2: Local Profile Path Logging Without Username Sanitization

- **File**: `src/OptiKey.ET5.Plugin/Runtime/TobiiRuntimeLocator.cs:87, 110`, `src/OptiKey.ET5.Plugin/Runtime/Interop/TobiiStreamEngineBinding.cs:173`
- **Observed Behavior**: Probed candidate paths and loaded library paths were logged as raw strings. If a path was located in a user directory (`%LOCALAPPDATA%` or developer explicit path), the user's local username would be exposed in log files.
- **Expected Behavior**: Sanitize user paths by replacing user profile prefixes with `%USERPROFILE%` or `%LOCALAPPDATA%` before logging.
- **Fix**: Implement path sanitization utility and apply to diagnostic log messages.
- **Status**: Identified / Pending Fix.

---

### [CI-01] P2: CI Workflow Push Trigger Omitted Audit Branches

- **File**: `.github/workflows/build-and-test.yml:5`
- **Observed Behavior**: Workflow triggered on `main, dev, 'dev/**', 'chore/**'`, but not `'audit/**'`.
- **Expected Behavior**: Pushing to `audit/**` should trigger the full Windows CI matrix.
- **Fix**: Add `'audit/**'` to the branches trigger list.
- **Status**: Identified / Pending Fix.

---

### [CI-02] P2: Node.js 20 Deprecation Warnings on GitHub Actions Runners

- **File**: `.github/workflows/build-and-test.yml`, `.github/workflows/release.yml`
- **Observed Behavior**: CI runs report Node.js 20 deprecation annotations for `actions/checkout@v4`, `actions/upload-artifact@v4`, `microsoft/setup-msbuild@v2`, `NuGet/setup-nuget@v2`, `darenm/Setup-VSTest@v1.2`.
- **Expected Behavior**: Runners currently force Node 24 execution. Track action upstream releases for official Node 24 support.
- **Status**: Documented / Tracked.

---

### [GOV-01] P2: GitHub Main Branch Protection and Auto-Deletion Disabled

- **File**: GitHub Repository Settings
- **Observed Behavior**: `main` branch has `protected: false`, and `delete_branch_on_merge: false`.
- **Expected Behavior**: Solo-maintainer branch protection rules requiring PRs, passing Windows CI matrix, blocking force pushes and branch deletion, and enabling automatic branch pruning.
- **Status**: Documented / Recommended.

---
