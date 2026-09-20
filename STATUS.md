# Project Status

Status date: 2026-09-21

This is the canonical readiness summary. It is intentionally conservative because this repository has no real ET5 hardware evidence.

| Area | Status | Evidence |
| --- | --- | --- |
| Architecture | Implemented in source | Managed plugin, runtime locator, state machine, and synthetic provider are present. |
| Synthetic tests | Source tests present | NUnit tests cover mapping, state transitions, and synthetic point delivery; they were not executable on this macOS host because `dotnet` is unavailable. |
| Windows CI | Partially verified | Run `35545794228` passed both matrix build/test legs. Latest run `35545932333` passed pinned-stable build/test/package/ZIP audit, but loader smoke failed. |
| Packaged loader | Implemented, failing | Separate-process smoke script is wired into CI and runs against the exact ZIP; the current gate fails and needs repair. |
| Tobii ABI | Partially verified | Several signatures match the local research document, but no authoritative versioned header or hardware run was available. `tobii_get_device_info` is disabled. |
| Real Tobii runtime | UNVERIFIED | No Tobii runtime is installed in this environment. |
| ET5 hardware | UNVERIFIED | No physical ET5 session has been performed. |
| Device identification | FAIL-CLOSED | Production refuses to bind when safe identity inspection is unavailable. |
| DPI and multi-monitor behavior | UNVERIFIED | Only synthetic metric tests exist. |
| Sleep/resume and reconnect | UNVERIFIED | No hardware lifecycle evidence exists. |
| Authenticode trust | UNVERIFIED | Current code must not describe certificate metadata inspection as trust validation. |
| Legal compatibility | AWAITING CLARIFICATION | Tobii policy for this consumer-device assistive use has not been confirmed. |
| License file | CI verified locally | Replaced with the SPDX GPL-3.0-only text; SHA256 is `fb981668c18a279e285fc4d83fba1e836cc84dd4daa73c9697d3cfd2d8aca6e0`. |

## Release decision

Readiness verdict: **NOT READY**.

No GitHub release or tag was created. Stable releases are blocked in the release workflow. A prerelease must still wait for ABI/shutdown review, a passing packaged-loader gate, and a controlled hardware session. Do not rely on this software as the sole communication method.
