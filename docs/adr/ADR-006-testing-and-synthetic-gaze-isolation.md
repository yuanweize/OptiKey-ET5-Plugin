[English](ADR-006-testing-and-synthetic-gaze-isolation.md) | [简体中文](ADR-006-testing-and-synthetic-gaze-isolation.zh-CN.md)

# ADR-006: Testing and Synthetic Gaze Isolation

## Status
Accepted

## Context
During automated testing, CI runs, and developer workstations without physical Tobii Eye Tracker 5 hardware, simulated gaze data is essential to verify:
- OptiKey loader compatibility (`DllLoader.IsValidPlugin`, `TryInstantiatePointSource`)
- Coordinate transformation accuracy
- State machine transitions
- Event bubbling and filtering

However, in production, if a physical eye tracker becomes unavailable or uncalibrated, **silently falling back to synthetic gaze is catastrophic**. A locked-in user relying on eye gaze would suddenly see a cursor moving erratically or phantom key presses occurring without their control, causing confusion, frustration, and potential loss of communication control.

## Decision
1. **Never Fallback Silently in Production**:
   The production plugin (`OptiKey.ET5.Plugin.dll`) contains NO synthetic tracker fallback. If hardware or runtime is absent, the plugin explicitly transitions to `Stopped` or `Reconnecting` and fires an `Error` event indicating hardware is unavailable.
2. **Dedicated Assembly for Synthetic Provider**:
   The synthetic provider (`SyntheticGazeProvider`) is housed exclusively in a separate project/assembly:
   `src/OptiKey.ET5.Plugin.Synthetic/OptiKey.ET5.Plugin.Synthetic.csproj`.
   This assembly is referenced by unit/integration tests and diagnostic tools, but is **categorically excluded** from production release ZIP assets.
3. **CI Release Asset Scanning and Validation**:
   The CI pipeline must unpack the final release ZIP and assert:
   - Contains exactly one DLL that implements `IPointService`.
   - Contains NO test assemblies (`*.Tests.dll`, `*.Synthetic.dll`).
   - Contains NO proprietary Tobii binaries (`tobii_stream_engine.dll`).
   - Contains NO duplicate `JuliusSweetland.OptiKey.Contracts.dll` unless an explicit verified decision is made.
   - Contains NO debug PDB files (unless specifically packaged in a symbols archive).
4. **Exact OptiKey Reflection Loading Test**:
   The test suite includes an automated test (`OptiKeyLoaderReflectionTests`) that executes the exact reflection logic found in OptiKey's `DllLoader.cs` against the generated release artifact:
   - Loads assembly into a separate temporary `AppDomain`.
   - Verifies `t.GetInterfaces().Contains(typeof(IPointService))`.
   - Verifies `Activator.CreateInstance(typeToLoad)` succeeds without throwing.
   - Verifies `((IDisposable)instance).Dispose()` cleans up cleanly.

## Consequences
- Impossible for synthetic gaze to leak into end-user production environments.
- 100% verification that release packages conform to OptiKey's loader expectations.
- Safe, deterministic CI testing without hardware dependencies.
