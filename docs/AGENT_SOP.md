[English](AGENT_SOP.md) | [简体中文](AGENT_SOP.zh-CN.md)

# Agent Standard Operating Procedure (SOP)

This document prescribes the mandatory operating procedures for any autonomous agent, pair-programming AI, or contributor developing features, resolving issues, or performing releases for `OptiKey-ET5-Plugin`.

## 1. Operating Prerequisites

1. **Read Core Governance First**:
   - Inspect [`AGENTS.md`](../AGENTS.md) at the repository root.
   - Review [`docs/DOCUMENTATION_POLICY.md`](DOCUMENTATION_POLICY.md).
   - Review architectural decisions under [`docs/adr/`](adr/).
2. **Never Commit Directly to `main`**:
   - Always verify current branch state (`git branch -avv`).
   - Use dedicated development branches (`dev/...`).
   - All merges to `main` must occur via Pull Requests.

## 2. Testing & Verification Standard

1. **Verify Exact CI Execution**:
   - Never claim "all tests passed" without inspecting exact test counts, failed counts, and process logs.
   - Both build configurations (`upstream-main` and `pinned-stable`) must be verified.
2. **Zero Tolerance for Unhandled Exceptions**:
   - Test suites must return exit code 0.
   - Test logs must NOT contain `Unhandled Exception:`, `Fatal error`, or `AccessViolationException`.
3. **Preserve OptiKey Plugin Contract**:
   - `ET5PointService` must retain a public parameterless constructor.
   - Hardware detection, native DLL binding, and thread pools must never initialize in the constructor. Initialization must happen lazily upon `Start()`.

## 3. Bilingual Documentation Parity Workflow

1. Whenever creating or updating any `.md` document, simultaneously create or update its `.zh-CN.md` counterpart.
2. Verify both documents include the language switch header:
   ```markdown
   [English](FILENAME.md) | [简体中文](FILENAME.zh-CN.md)
   ```
3. Run `tools/scripts/check-doc-sync.ps1` before submitting any PR.

## 4. Hardware & Safety Invariants

1. **Zero Proprietary Binary Distribution**:
   - Never package `tobii_stream_engine.dll` or any proprietary Tobii software.
   - Use dynamic runtime discovery and authenticode verification of the user's installed Tobii runtime.
2. **Zero Gaze Logging / Transmission**:
   - Gaze coordinates are processed exclusively in volatile memory for point-stream dispatch.
   - Gaze coordinates, user identifiers, or device URLs must NEVER be persisted to disk or emitted to logs.
3. **Bounded Plugin Shutdown**:
   - If unmanaged native worker threads cannot terminate within bounded timeouts, isolate them without releasing active native pointers to prevent access violation crashes.

## 5. Release Checklist

Before tagging or creating a GitHub Release:
- [ ] All 20 mandates in `AGENTS.md` satisfied.
- [ ] Full Windows CI matrix passed.
- [ ] No unhandled exceptions in CI logs.
- [ ] Packaged loader smoke test passed.
- [ ] Bilingual `CHANGELOG.md` and `STATUS.md` updated.
- [ ] Release ZIP integrity verified (no proprietary DLLs).
- [ ] Semantic versioning strictly followed (`v0.1.0` for initial public release).
