# Project Status

Status date: 2026-09-21

This is the canonical readiness summary. It is intentionally conservative because this repository has no real ET5 hardware evidence.

| Area | Status | Evidence |
| --- | --- | --- |
| Architecture | Implemented in source | Managed plugin, runtime locator, state machine, and synthetic provider are present. |
| Synthetic tests | Source tests present | NUnit tests cover mapping, state transitions, and synthetic point delivery; they were not executable on this macOS host because `dotnet` is unavailable. |
| Windows CI | Not independently rerun | Workflow exists, but no CI run was executed in this audit. |
| Packaged loader | Incomplete | Reflection simulation exists; a clean-process test using the real OptiKey loader is not present. |
| Tobii ABI | Partially verified | Several signatures match the local research document, but no authoritative versioned header or hardware run was available. `tobii_get_device_info` is disabled. |
| Real Tobii runtime | UNVERIFIED | No Tobii runtime is installed in this environment. |
| ET5 hardware | UNVERIFIED | No physical ET5 session has been performed. |
| Device identification | FAIL-CLOSED | Production refuses to bind when safe identity inspection is unavailable. |
| DPI and multi-monitor behavior | UNVERIFIED | Only synthetic metric tests exist. |
| Sleep/resume and reconnect | UNVERIFIED | No hardware lifecycle evidence exists. |
| Authenticode trust | UNVERIFIED | Current code must not describe certificate metadata inspection as trust validation. |
| Legal compatibility | AWAITING CLARIFICATION | Tobii policy for this consumer-device assistive use has not been confirmed. |
| License file | REVIEW REQUIRED | The checked-in GPL text contains non-canonical inserted Chinese text and needs replacement with the exact selected GPL text. |

## Release decision

Readiness verdict: **NOT READY**.

No GitHub release or tag was created. Stable releases are blocked in the release workflow. A prerelease must still wait for ABI/shutdown review and a controlled hardware session. Do not rely on this software as the sole communication method.
