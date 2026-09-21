[English](ADR-008-host-runtime-dependencies.md) | [简体中文](ADR-008-host-runtime-dependencies.zh-CN.md)

# ADR-008: Host-Provided Runtime Dependencies

Status: Accepted for CI and alpha preparation, 2026-09-21.

## Decision

The release ZIP contains only `OptiKey.ET5.Plugin.dll` and `LICENSE`. It does not redistribute log4net or System.Reactive assemblies.

The plugin still compiles against the pinned System.Reactive 2.2.5 references because the OptiKey `IPointService` contract uses `System.Reactive.Timestamped<T>`. The packaged loader harness resolves those assemblies from a host dependency directory representing the OptiKey installation, not from the plugin ZIP.

## Evidence and limitation

The net46 packaged-loader harness is the compatibility gate for this decision. It must load the exact generated ZIP and a host dependency directory containing the pinned Contracts and Rx assemblies. A missing host dependency is a deliberate failure. This is not proof that every OptiKey release supplies identical bindings; only the pinned Contracts build is in scope until exact-version loader tests exist.

Removing log4net eliminates the plugin-local assembly-binding conflict and the known vulnerable package dependency. Logging uses `System.Diagnostics.TraceSource` with no plugin-specific runtime package.
