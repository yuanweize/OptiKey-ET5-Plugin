## Pull Request Checklist

Please ensure all items below are satisfied before requesting review or merging:

- [ ] **Windows CI Passed**: Full matrix (`upstream-main` and `pinned-stable`) executed successfully.
- [ ] **Tests Executed**: Total tests > 0, Executed > 0, Failed == 0.
- [ ] **No Unhandled Exceptions**: Verified complete CI test logs contain zero `Unhandled Exception:`, `Fatal error`, or `AccessViolationException`.
- [ ] **Packaged Loader Passed**: OptiKey packaged loader smoke test executed without error.
- [ ] **Zero Tobii Proprietary Binaries**: Audited package contents; no `tobii_stream_engine.dll` or vendor binaries bundled.
- [ ] **English Documentation Updated**: All changes reflected in relevant `.md` files.
- [ ] **Chinese Documentation Parity**: Corresponding `.zh-CN.md` files updated with identical semantic content.
- [ ] **CHANGELOG Updated**: User-visible and architectural changes added to both `CHANGELOG.md` and `CHANGELOG.zh-CN.md`.
- [ ] **STATUS Updated**: Readiness state refreshed in both `STATUS.md` and `STATUS.zh-CN.md`.
- [ ] **Privacy Invariant Preserved**: Confirmed zero persistence, logging, or transmission of raw gaze coordinates or device URLs.
- [ ] **OptiKey Contract Preserved**: `ET5PointService` retains parameterless public constructor without early unmanaged allocation.

## Description of Changes

<!-- Provide a concise summary of the rationale, architecture decisions, and testing performed -->
