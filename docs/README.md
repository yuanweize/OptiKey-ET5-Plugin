[English](README.md) | [简体中文](README.zh-CN.md)

# OptiKey-ET5-Plugin Documentation Hub

Welcome to the documentation repository for the **OptiKey-ET5-Plugin**. This directory is structured by audience and purpose to provide clear navigation for assistive technology users, clinicians, open-source contributors, and research engineers.

---

## 1. User Guide (`docs/user/`)

Targeted at everyday AAC users, caregivers, and accessibility clinicians:

- **[Installation and Setup Guide](user/INSTALLATION.md)**: Step-by-step instructions for installing the plugin via OptiKey or manual ZIP archive.
- **[Troubleshooting Guide](user/TROUBLESHOOTING.md)**: Solutions for common issues including driver discovery, multi-device selection, and calibration loss.
- **[Hardware and OS Compatibility](user/COMPATIBILITY.md)**: Supported Windows versions, display aspect ratios, and High-DPI scaling configurations.

---

## 2. Project Status & Roadmap (`docs/project/`)

Canonical source of truth on current development milestones and readiness:

- **[Project Status & Readiness Dashboard](project/STATUS.md)**: Live verification status across CI matrices, test suites, and hardware readiness.
- **[Project Roadmap](project/ROADMAP.md)**: Phased development plan spanning initial release, multi-display support, and future research.

---

## 3. Policies & Legal (`docs/policies/`)

Licensing, third-party intellectual property, and strict biometric privacy boundaries:

- **[Legal Disclosures & Provenance](policies/LEGAL.md)**: Clean-room implementation statements, Tobii IP boundaries, and safe harbor declarations.
- **[Biometric Privacy Invariant](policies/PRIVACY.md)**: Zero-telemetry, volatile memory processing only, zero gaze coordinate logging guarantee.
- **[Third-Party Notices](policies/THIRD_PARTY_NOTICES.md)**: Upstream attributions for OptiKey, Microsoft Reactive Extensions, and Tobii public declarations.
- **[LICENSE](../LICENSE)**: Authoritative GNU General Public License v3.0 text (located in repository root).

---

## 4. Development & Maintainer SOP (`docs/development/`)

Guidelines for software engineers and autonomous AI agents:

- **[Agent Standard Operating Procedure](development/AGENT_SOP.md)**: Complete step-by-step workflows for development, verification, and pull requests.
- **[Documentation Synchronization Policy](development/DOCUMENTATION_POLICY.md)**: 1:1 English/Simplified Chinese documentation parity requirements.
- **[Hardware Validation Protocol](development/HARDWARE_VALIDATION.md)**: Verification methodology for automated testing and real physical Tobii hardware.
- **[Repository Mandates (AGENTS.md)](../AGENTS.md)**: The 20 non-negotiable core invariants (located in repository root).

---

## 5. Technical Research (`docs/research/`)

Deep technical documentation on Tobii native interfaces and Windows platform integration:

- **[ABI Provenance & Calling Conventions](research/ABI_PROVENANCE.md)**: C-level struct layouts, 64-bit alignment, and P/Invoke declarations.
- **[Runtime Discovery Specification](research/RUNTIME_RESEARCH.md)**: PE64 binary inspection, Authenticode `WinVerifyTrust` verification, and registry discovery paths.
- **[Callback Pump Architecture](research/CALLBACK_RESEARCH.md)**: Event-driven native callback pumping, bounded shutdown timeouts, and stuck-worker isolation.

---

## 6. Architecture Decision Records (`docs/adr/`)

Formal decision records detailing the technical evolution of the codebase:

- **[ADR-001: Architecture Overview](adr/ADR-001-architecture-overview.md)**
- **[ADR-002: Upstream Contracts Pinning](adr/ADR-002-upstream-contracts-pinning.md)**
- **[ADR-003: Hardware-Independent Lifecycle](adr/ADR-003-hardware-independent-lifecycle.md)**
- **[ADR-004: Tobii Runtime Isolation and Security](adr/ADR-004-tobii-runtime-isolation-and-security.md)**
- **[ADR-005: Reconnect State Machine](adr/ADR-005-reconnect-state-machine.md)**
- **[ADR-006: Testing and Synthetic Gaze Isolation](adr/ADR-006-testing-and-synthetic-gaze-isolation.md)**
- **[ADR-007: Coordinate Space Semantics](adr/ADR-007-coordinate-space.md)**
- **[ADR-008: Host-Provided Runtime Dependencies](adr/ADR-008-host-runtime-dependencies.md)**
- **[ADR-009: In-Process vs RuntimeHost Isolation](adr/ADR-009-in-process-vs-runtime-host-architecture.md)**

---

## 7. Historical Audits & Archives (`docs/history/`)

Archived technical assessments, audit logs, and milestone records:

- **[September 2026 Code Audit](history/2026-09/CODE_AUDIT_2026-09.md)**: Comprehensive source code audit, concurrency fixes, and safety mitigations prior to `v0.1.0`.
- **[September 2026 Hardening Audit](history/2026-09/AUDIT_2026-09.md)**: Initial runtime hardening, package boundary, and CI verification notes.
