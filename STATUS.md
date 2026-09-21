# Project Status

Status date: 2026-09-21

This is the canonical readiness summary. It is intentionally conservative because this repository has no real ET5 hardware evidence.

| Area | Status | Evidence |
| --- | --- | --- |
| Architecture | Implemented in source | Managed plugin, runtime locator, state machine, and synthetic provider are present. |
| Build | CI verified | Windows run `35545794228` built both matrix legs; later runs also built the solution successfully. |
| Test project | Compiled | The NUnit test project compiles in the Windows CI build. |
| Test execution | Previously INVALID | Historical VSTest output reported zero tests discovered. The new TRX-enforcing runner is present but requires a new completed run to prove total > 0. |
| Synthetic tests | Source tests present | NUnit tests cover mapping, state transitions, lifecycle, and synthetic point delivery. |
| Windows CI | VERIFIED | Run `35578818780` succeeded for both pinned-stable and upstream-main. |
| Test execution | VERIFIED | Both TRX artifacts report `total=26 executed=26 passed=26 failed=0 skipped=0`. |
| Package generation | VERIFIED | Pinned-stable generated `OptiKey-ET5-Plugin-v0.0.0-ci.21.zip`. |
| ZIP static audit | VERIFIED | Archive contains exactly `LICENSE` and `OptiKey.ET5.Plugin.dll`. |
| Packaged loader | VERIFIED | Net46 x64 harness passed against the exact generated ZIP and pinned Contracts assembly. |
| Tobii ABI | Partially verified | Several signatures match the local research document, but no authoritative versioned header or hardware run was available. `tobii_get_device_info` is disabled. |
| Real Tobii runtime | UNVERIFIED | No Tobii runtime is installed in this environment. |
| ET5 hardware | UNVERIFIED | No physical ET5 session has been performed. |
| Device identification | FAIL-CLOSED | Production refuses to bind when safe identity inspection is unavailable. |
| DPI and multi-monitor behavior | UNVERIFIED | Only synthetic metric tests exist. |
| Sleep/resume and reconnect | UNVERIFIED | No hardware lifecycle evidence exists. |
| Authenticode trust | UNVERIFIED | Current code must not describe certificate metadata inspection as trust validation. |
| Legal compatibility | AWAITING CLARIFICATION | Tobii policy for this consumer-device assistive use has not been confirmed. |
| License file | Verified | Replaced with the SPDX GPL-3.0-only text; SHA256 is `fb981668c18a279e285fc4d83fba1e836cc84dd4daa73c9697d3cfd2d8aca6e0`. |
| log4net | Removed | No plugin PackageReference or release payload entry remains. |
| Rx packaging | Host-provided | Rx 2.2.5 remains a compile-time dependency; Rx DLLs are excluded from the release ZIP and the net46 harness resolves host dependencies. |

## Release decision

Readiness verdict: **NOT READY**.

No GitHub release or tag was created. Stable releases are blocked in the release workflow. A prerelease must still wait for ABI/shutdown review and a controlled hardware session. Do not rely on this software as the sole communication method.
