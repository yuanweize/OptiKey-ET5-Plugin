[English](CONTRIBUTING.md) | [简体中文](CONTRIBUTING.zh-CN.md)

# Contributing to OptiKey-ET5-Plugin

Thank you for your interest in contributing to open-source accessibility software. Every improvement helps people with ALS/MND and severe physical disabilities communicate independently.

## Mandatory Contributor Rules

Before contributing, please read [`AGENTS.md`](../AGENTS.md) and [Agent Standard Operating Procedure](../docs/development/AGENT_SOP.md). All contributors (human or AI) must strictly observe the following invariants:

1. **Zero Proprietary Binary Redistribution**: Never commit or pull-request `.dll`, `.lib`, `.sys`, or closed vendor files from Tobii.
2. **Zero Gaze Data Logging**: Never log, store, or transmit raw gaze coordinates or user information.
3. **Preserve OptiKey Contract**: Keep `ET5PointService`'s public parameterless constructor intact with zero unmanaged allocation during construction.
4. **Bilingual Documentation Parity**: Any documentation modification requires updating both English (`*.md`) and Simplified Chinese (`*.zh-CN.md`) files.
5. **No Silent Synthetic Fallbacks**: Production code must never silently switch to synthetic gaze when native runtime errors occur.

## Pull Request Workflow

1. Fork the repository and create a branch from `main`:
   ```bash
   git checkout -b dev/your-feature-name
   ```
2. Implement your changes following existing code conventions and ADR guidelines in [`docs/adr/`](../docs/adr/).
3. Add or update unit tests under `tests/OptiKey.ET5.Plugin.Tests/`.
4. Ensure the documentation sync script passes:
   ```powershell
   pwsh .\tools\scripts\check-doc-sync.ps1
   ```
5. Submit a Pull Request targeting `main` using the provided checklist template in `PULL_REQUEST_TEMPLATE.md`.
6. Ensure all automated Windows CI checks pass with zero unhandled exceptions.
