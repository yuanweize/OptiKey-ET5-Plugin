# Project Status

Status date: 2026-09-21

This is the canonical readiness summary. It distinguishes Windows CI evidence from native-runtime, hardware, and legal evidence.

| Area | Status | Evidence |
| --- | --- | --- |
| Main baseline | MERGED | PR [#1](https://github.com/yuanweize/OptiKey-ET5-Plugin/pull/1) merged the reviewed hardening history into `main` at `e02b55142fbe8291aea4f1ff5656e61cf96a7135`. |
| Development branch | ACTIVE | `dev/et5-runtime` was created from the verified main merge commit. Native-runtime work must remain off `main`. |
| Windows CI | VERIFIED | Main run [35582811667](https://github.com/yuanweize/OptiKey-ET5-Plugin/actions/runs/35582811667) succeeded for pinned-stable and upstream-main. |
| Test execution | VERIFIED | Downloaded TRX artifacts for both jobs report `total=26 executed=26 passed=26 failed=0`. These are managed/synthetic tests, not hardware tests. |
| Package generation | VERIFIED | The pinned-stable job generated `OptiKey-ET5-Plugin-v0.0.0-ci.24.zip`. |
| ZIP static audit | VERIFIED | The downloaded archive contains exactly `LICENSE` and `OptiKey.ET5.Plugin.dll`; its SHA256 is `0f79a1758c46b7a7917c027b0dcf5509751042db6bb13a95f8ccf9cef7484b9e`. |
| Packaged loader | VERIFIED | The x64 .NET Framework 4.6 harness loaded the exact package, found one `IPointService`, instantiated and disposed it, and confirmed that construction did not load `tobii_stream_engine.dll`. |
| Proprietary content | VERIFIED ABSENT FROM REVIEWED TREE/PACKAGE | The reviewed Git tree and main CI package contain no Tobii DLL, library, header, SDK archive, or copied proprietary source. |
| Tobii ABI | BLOCKED | The active declarations have not been matched to a legitimately obtained, versioned authoritative header. Tobii currently advertises Stream Engine Client 7.2, but the linked API reference is not publicly retrievable and SDK access requires a development license. |
| Device identification | FAIL-CLOSED | Production enumerates but refuses to connect because ET5 identity cannot yet be established through a proven ABI. |
| Callback shutdown | BLOCKED | The current wait declaration exposes no managed cancellation parameter. Stop waits for the worker and therefore avoids timed cleanup, but completion is not bounded. |
| Runtime loading | PARTIAL | Absolute-path `LoadLibraryExW` is used and current-directory/PATH discovery is not implemented. Full transitive dependency control and WinVerifyTrust validation are not proven. |
| Authenticode trust | NOT IMPLEMENTED | The locator inspects embedded signer metadata only. It does not establish signature integrity, chain trust, revocation status, or trusted Tobii identity. |
| Real Tobii runtime | UNVERIFIED | No current Tobii installation has been inventoried on a Windows ET5 system. |
| ET5 hardware | UNVERIFIED | No physical ET5 session has been performed. |
| DPI and multi-monitor behavior | UNVERIFIED | Only synthetic coordinate tests exist. |
| Sleep/resume and reconnect | UNVERIFIED | No hardware lifecycle evidence exists. |
| Legal/API permission | BLOCKED PENDING TOBII CLARIFICATION | Tobii's current Streams SDK page says Eye Tracker 5 without the `L` is a gaming device that cannot be used for development. Current Tobii licensing material also requires a separate development/commercial agreement and identifies a Medical Use option; a published limited SDLA explicitly lists AAC as Medical Use. |
| License file | VERIFIED | `LICENSE` is the GPL-3.0-only text with SHA256 `fb981668c18a279e285fc4d83fba1e836cc84dd4daa73c9697d3cfd2d8aca6e0`. |

## Release decision

Readiness verdict: **NOT READY**.

There is no Git tag or GitHub Release. The CI artifact is evidence for build and packaging only; it is not an end-user release. Do not rely on this software as the sole communication method.

## Current authoritative Tobii leads

- [Tobii Streams SDK](https://www.tobii.com/products/integration/tobii-streams-sdk): current SDK positioning, Stream Engine Client 7.2, supported integration hardware, and development-license requirement.
- [Tobii software-development license overview](https://www.tobii.com/products/integration/tobii-sdk-license): development and distribution require the applicable Tobii agreement; Medical Use is a separate option.
- [Tobii SDLA version 2.0](https://developer.tobii.com/vr/sdla/): the published limited license excludes Medical Use and expressly gives AAC as an example.

These links establish a blocker, not legal advice or permission.
