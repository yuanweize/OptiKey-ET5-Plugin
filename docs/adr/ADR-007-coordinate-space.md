# ADR-007: Coordinate Space Semantics and Mapping Pipeline

## Status
Accepted

## Context
A frequent source of critical bugs in eye-tracking assistive software is coordinate space mismatch across high-DPI displays (e.g., 100%, 125%, 150%, 200% Windows scaling) and multi-monitor configurations:
- At 100% DPI, points appear accurate.
- At 125% or 150% DPI, points drift or key hit detection misses entirely.
- In multi-monitor setups, points may be clamped to the primary screen or incorrectly mapped to virtual desktop bounds.

To ensure pinpoint accuracy, we must rigorously trace and specify coordinate semantics across both ends:
1. **Source**: Tobii Stream Engine coordinate output.
2. **Sink**: OptiKey's internal hit-testing and consumption pipeline.

## Comprehensive Trace of Coordinate Semantics

### End A: Tobii Stream Engine Output Semantics
Tobii Stream Engine delivers gaze data via `tobii_gaze_point_t`:
```c
typedef struct tobii_gaze_point_t {
    int64_t timestamp_us;
    tobii_validity_t validity;
    float position[2]; // position[0] = x, position[1] = y
} tobii_gaze_point_t;
```
- **Range**: `position[0]` ($X$) and `position[1]` ($Y$) are normalized 2D floating-point values in the range `[0.0, 1.0]`.
- **Origin**: `(0.0, 0.0)` is the top-left corner of the calibrated display plane; `(1.0, 1.0)` is the bottom-right corner.
- **Reference Frame**: Tobii Eye Tracker 5 calibrates specifically to a single physical display panel (the primary monitor where the hardware is mounted). It does not automatically span a multi-monitor virtual desktop.

### End B: OptiKey Consumption Semantics
How does OptiKey consume the `System.Windows.Point` emitted by `IPointService.Point`?
1. In `src/JuliusSweetland.OptiKey.Core/Observables/PointSources/PointServiceSource.cs`:
   ```csharp
   sequence = Observable.FromEventPattern<Timestamped<Point>>(
           eh => pointGeneratingService.Point += eh,
           eh => pointGeneratingService.Point -= eh)
       .Select(ep => kalmanFilter.Update(ep.EventArgs.Value))
       .Select(tp => tp.Value.ToPointAndKeyValue(PointToKeyValueMap))
   ```
2. In `src/JuliusSweetland.OptiKey.Core/UI/Controls/KeyboardHost.cs` (lines 806-810):
   ```csharp
   var rect = new Rect
   {
       Location = key.PointToScreen(topLeftPoint),
       Size = (Size)key.GetTransformToDevice().Transform((Vector)key.RenderSize)
   };
   pointToKeyValueMap.Add(rect, key.Value);
   ```
   - In WPF, `Visual.PointToScreen(Point)` converts visual coordinates into **device physical screen pixels**.
   - `GetTransformToDevice().Transform(RenderSize)` multiplies logical WPF units (96 DPI) by the DPI scale factor (e.g., 1.25x for 120 DPI), yielding **physical pixel dimensions**.
3. In `src/JuliusSweetland.OptiKey.Core/Observables/PointSources/MousePositionSource.cs`:
   ```csharp
   new Point(Cursor.Position.X, Cursor.Position.Y)
   ```
   `System.Windows.Forms.Cursor.Position` returns Win32 physical screen coordinates.
4. In upstream historical `TobiiPointService.cs`:
   ```csharp
   new Point(Graphics.PrimaryScreenWidthInPixels * gazePoint.position.x,
             Graphics.PrimaryScreenHeightInPixels * gazePoint.position.y)
   ```

**Conclusion**: OptiKey's `IPointService.Point` MUST be emitted in **Physical Screen Pixels** corresponding to the display on which the tracker is calibrated (by default, the primary display).

## Transformation Pipeline
```
+--------------------------------------------------------------------+
|                Tobii Stream Engine (Hardware/Driver)               |
| Normalized: (normX, normY) in [0.0, 1.0]                           |
+--------------------------------------------------------------------+
                                  |
                                  | Native Callback (tobii_gaze_point_t)
                                  v
+--------------------------------------------------------------------+
|               Plugin: ScreenCoordinateMapper                       |
| 1. Sanity check: validity == TOBII_VALIDITY_VALID                  |
| 2. Clamping: Clamp normX, normY to [0.0, 1.0]                      |
| 3. Resolution Query: Win32 GetSystemMetrics or DisplayMetrics      |
|    - PhysicalWidth = GetSystemMetrics(SM_CXSCREEN)                 |
|    - PhysicalHeight = GetSystemMetrics(SM_CYSCREEN)                |
|    - DisplayBounds = (Left, Top, Width, Height)                    |
| 4. Mapping Calculation:                                            |
|    pixelX = DisplayBounds.Left + (normX * DisplayBounds.Width)     |
|    pixelY = DisplayBounds.Top  + (normY * DisplayBounds.Height)    |
+--------------------------------------------------------------------+
                                  |
                                  | Emitted as Timestamped<Point>
                                  v
+--------------------------------------------------------------------+
|                OptiKey: PointToKeyValueMap                         |
| Hit-tested against key.PointToScreen() (Physical Screen Pixels)    |
+--------------------------------------------------------------------+
```

## High-DPI and Multi-Monitor Policy
1. **DPI Awareness**:
   Because the mapper uses the physical pixel dimensions of the target display, the mapping remains invariant across 100%, 125%, 150%, and 200% DPI settings without manual scaling coefficients.
2. **Target Display Selection**:
   In single-monitor setups, the target is the Primary Display (`(0, 0, SM_CXSCREEN, SM_CYSCREEN)`).
   In multi-monitor setups, Tobii ET5 mounts to a specific screen (typically primary). The plugin defaults to the primary screen. Future configuration can allow selecting a secondary monitor rectangle without altering the core math.
3. **Out-of-Bounds Handling**:
   Samples where `validity != TOBII_VALIDITY_VALID` (e.g., eyes closed, user turned away) are discarded and NOT dispatched to OptiKey, preventing spurious cursor jumps.

## Consequences
- Pixel-perfect key selection in OptiKey regardless of Windows DPI scale setting.
- Clean separation of normalized sensor space from host OS display topology.
- Thoroughly testable with deterministic unit test matrices covering multiple display resolutions and aspect ratios (1080p, 1440p, 4K, 16:9, 16:10, 21:9).
