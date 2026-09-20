# ADR-001: Architecture Overview and Component Boundaries

## Status
Accepted

## Context
OptiKey is an assistive communication software designed for individuals with severe motor and speech limitations (e.g., ALS/MND). Users rely on OptiKey as their sole interface to communicate and interact with computers.

Historically, OptiKey supported Tobii eye trackers via built-in code, but native Tobii support was removed in commit `81c88f5` due to licensing and proprietary redistribution restrictions. OptiKey 4.x introduced an external plugin architecture (`DllLoader`, `InstalledPluginsSearch`) that dynamically discovers and loads third-party eye-tracker adapters from GitHub releases carrying the `optikey-plugin` topic.

The goal of this project (`OptiKey-ET5-Plugin`) is to build a robust, open-source, non-proprietary external plugin enabling OptiKey 4.x to utilize the **Tobii Eye Tracker 5 (ET5)** without redistributing proprietary binaries.

## Architectural Requirements
1. **Accessibility-Grade Reliability**: Unhandled crashes or silent failures can strand locked-in users. The plugin must remain resilient under physical disconnects, sleep/resume cycles, and unexpected driver states.
2. **Zero Proprietary Binary Redistribution**: The repository and release assets MUST NOT bundle Tobii proprietary DLLs (`tobii_stream_engine.dll`) or SDK headers. It binds dynamically to the user's existing, locally installed Tobii Experience runtime.
3. **Strict Interface Compliance**: Implements `JuliusSweetland.OptiKey.Contracts.IPointService` compiled against the official upstream contract assembly.
4. **Decoupled Internal Architecture**:
   - `OptiKey.ET5.Plugin.dll`: Production plugin assembly. Contains `ET5PointService`, state machine, coordinate mapping, runtime locator, and Tobii adapter.
   - `OptiKey.ET5.Plugin.Synthetic.dll`: Isolated test harness and synthetic gaze generator. NEVER packaged into production release assets.
   - `ET5Diagnostics.exe` / `diagnose.ps1`: Standalone diagnostic tool sharing identical locator, binding, and enumeration logic.

## Component Diagram
```
+-------------------------------------------------------------------+
|                           OptiKey Core                            |
|             (DllLoader -> Activator.CreateInstance)               |
+-------------------------------------------------------------------+
                                  |
                                  | implements IPointService
                                  v
+-------------------------------------------------------------------+
|                       OptiKey.ET5.Plugin                          |
|                                                                   |
|   +-----------------------------------------------------------+   |
|   |                     ET5PointService                       |   |
|   |   - Lazy initialization (hardware-independent ctor)       |   |
|   |   - Event dispatch: Point, Error                          |   |
|   +-----------------------------------------------------------+   |
|                                 |                                 |
|         +-----------------------+-----------------------+         |
|         v                                               v         |
|   +-------------------------+     +---------------------------+   |
|   | GazeServiceStateMachine |     |   ScreenCoordinateMapper  |   |
|   | - Created / Starting /  |     | - Normalized [0,1] gaze   |   |
|   |   Connected /           |     | - Primary screen physical |   |
|   |   Reconnecting /        |     |   pixels                  |   |
|   |   Stopping / Stopped /  |     | - Multi-DPI consistency   |   |
|   |   Disposed              |     +---------------------------+   |
|   +-------------------------+                                     |
|                 |                                                 |
|                 v                                                 |
|   +-----------------------------------------------------------+   |
|   |                     TobiiGazeProvider                     |   |
|   | - Coordinates background worker & wait_for_callbacks      |   |
|   | - Exponential backoff + jitter reconnect policy           |   |
|   +-----------------------------------------------------------+   |
|                 |                                                 |
|         +-------+-----------------------+                         |
|         v                               v                         |
|   +--------------------------+    +---------------------------+   |
|   |    TobiiRuntimeLocator   |    |    StreamEngineBinding    |   |
|   | - Path whitelisting      |    | - Safe P/Invoke binding   |   |
|   | - Architecture check     |    | - Memory safety guards    |   |
|   | - Signature verification |    |                           |   |
|   +--------------------------+    +---------------------------+   |
+-------------------------------------------------------------------+
                                  |
                                  | P/Invoke dynamic load
                                  v
+-------------------------------------------------------------------+
|                     Tobii Experience Runtime                      |
|                (tobii_stream_engine.dll, x64)                     |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
|                     Tobii Eye Tracker 5 (USB)                     |
+-------------------------------------------------------------------+
```

## Consequences
- The plugin assembly is strictly decoupled from the physical runtime during construction.
- No proprietary binaries are committed, built, or shipped.
- Production and diagnostic tools share exact runtime resolution and communication code, eliminating environment divergence.
