[English](DOCUMENTATION_POLICY.md) | [简体中文](DOCUMENTATION_POLICY.zh-CN.md)

# Documentation Synchronization Policy

## 1. Core Principle

All human-readable, maintained documentation in this repository **must maintain 1:1 bilingual parity** between English (`*.md`) and Simplified Chinese (`*.zh-CN.md`).

Accessibility and assistive software communities span diverse languages, and this project serves both international and Chinese-speaking accessibility users, clinicians, and engineers. Maintaining complete documentation in both languages is a project requirement.

## 2. File Naming Convention

- English documents: `<FILENAME>.md` (e.g., `README.md`, `SECURITY.md`, `docs/ARCHITECTURE.md`)
- Simplified Chinese documents: `<FILENAME>.zh-CN.md` (e.g., `README.zh-CN.md`, `SECURITY.zh-CN.md`, `docs/ARCHITECTURE.zh-CN.md`)

### Exceptions
- `LICENSE`: The authoritative GNU General Public License v3.0 text must remain unchanged in canonical English. Unofficial explanatory notes may be provided in separate documentation but must never replace the official license file.
- `OPTIKEY_CONTRACT_REF`: Technical reference pinning upstream commit hashes.
- GitHub workflow automation files under `.github/workflows/`, issue forms under `.github/ISSUE_TEMPLATE/`, and `.github/PULL_REQUEST_TEMPLATE.md`.

## 3. Mandatory Header Format

Every paired document must include language switch links at the very top (Line 1):

```markdown
[English](FILENAME.md) | [简体中文](FILENAME.zh-CN.md)
```

For documents located inside subdirectories such as `docs/`:
```markdown
[English](FILENAME.md) | [简体中文](FILENAME.zh-CN.md)
```

## 4. Automated Verification in CI

The script `tools/scripts/check-doc-sync.ps1` runs in automated CI and verifies:
1. Every English document has an exact `.zh-CN.md` counterpart.
2. Every `.zh-CN.md` document has an exact `.md` counterpart.
3. Both files contain the standard bilingual header link.
4. Relative links point to existing files.

Pull Requests that introduce or modify documentation without updating both language files will fail the CI documentation gate.
