[English](AGENTS.md) | [简体中文](AGENTS.zh-CN.md)

# AGENTS.md — Repository Mandates for Autonomous & Pair-Programming Agents

> [!IMPORTANT]
> Any automated agent, LLM assistant, or developer operating on this repository **MUST** read and strictly follow the 20 mandates below before proposing or committing changes.

## The 20 Non-Negotiable Mandates

1. **Read AGENTS.md First**: Every agent must read this file and [`docs/development/AGENT_SOP.md`](docs/development/AGENT_SOP.md) before making any code or documentation edits.
2. **Never Falsely Claim Tests Passed**: Never assert tests passed without inspecting exact run output, execution counts, and exit codes.
3. **Zero Tolerance for Unhandled Exceptions**: Never accept a green CI status if logs contain process crashes, `Unhandled Exception:`, or `AccessViolationException`.
4. **Branch & PR Policy**: Always work on development branches (`dev/...` or `chore/...`) and submit Pull Requests. Direct commits to `main` are strictly forbidden.
5. **Bilingual Documentation Parity**: Maintain strict 1:1 synchronization between English (`*.md`) and Simplified Chinese (`*.zh-CN.md`) documentation pairs with cross-links.
6. **User-Facing Changes Require Doc Updates**: Any change affecting user experience, configuration, or installation must immediately update both README files and user guides.
7. **Source Code Is Ground Truth**: Source code and active tests define authoritative behavior. Do not rely on stale documentation or historical notes over active code.
8. **Never Bundle Tobii Binaries**: Absolutely never package, redistribute, commit, or bundle proprietary Tobii DLLs (`tobii_stream_engine.dll`).
9. **Strict Privacy Invariant**: Never persist, log, or transmit raw gaze coordinates or user identity. Local memory only for immediate pointing dispatch.
10. **Preserve Parameterless Constructor**: The `ET5PointService` parameterless constructor contract is required by the OptiKey plugin loader and must never throw or initialize unmanaged hardware directly.
11. **Mandatory Full Windows CI**: All PRs must pass the full Windows x64 CI matrix (both upstream-main and pinned-stable contracts).
12. **Verify Package Inventory**: Every release package must be audited to ensure zero extraneous or proprietary DLLs are present.
13. **Update CHANGELOG Bilingually**: Record every notable user-facing or architectural change in both `CHANGELOG.md` and `CHANGELOG.zh-CN.md`.
14. **Update STATUS Bilingually**: Track milestone progress and readiness in both [`docs/project/STATUS.md`](docs/project/STATUS.md) and [`docs/project/STATUS.zh-CN.md`](docs/project/STATUS.zh-CN.md).
15. **Bilingual Release Notes**: All GitHub Release summaries must provide complete English and Simplified Chinese sections.
16. **No Silent Synthetic Fallbacks**: Production code must never silently fall back to mock or synthetic gaze if native hardware fails. Errors must be surfaced.
17. **Do Not Hallucinate Native ABI**: Adhere strictly to verified native C declarations and documented signatures in [`src/OptiKey.ET5.Plugin/Runtime/Interop/TobiiStreamEngineBinding.cs`](src/OptiKey.ET5.Plugin/Runtime/Interop/TobiiStreamEngineBinding.cs) and [`docs/research/ABI_PROVENANCE.md`](docs/research/ABI_PROVENANCE.md).
18. **Respect Architectural Decisions**: Check existing Architecture Decision Records ([`docs/adr/`](docs/adr/)) before designing new components or creating new ADRs.
19. **Atomic, Descriptive Commits**: Keep commits small, well-scoped, and formatted according to Conventional Commits (`feat:`, `fix:`, `docs:`, `ci:`).
20. **Prune Obsolete Branches**: Delete merged feature branches both locally and remotely immediately after PR completion.

---

For the comprehensive operating procedure, refer to:
- [Agent Standard Operating Procedure (English)](docs/development/AGENT_SOP.md)
- [智能体标准操作规范 (简体中文)](docs/development/AGENT_SOP.zh-CN.md)
