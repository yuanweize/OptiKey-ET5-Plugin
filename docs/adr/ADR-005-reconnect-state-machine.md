# ADR-005: Reconnect State Machine and Callback Lifecycle

## Status
Accepted

## Context
Eye tracking hardware in real-world accessibility settings experiences frequent physical disruptions:
- USB cables bumped, disconnected, or re-plugged
- Laptop / PC sleep, hibernation, and wake cycles
- Windows Tobii background service restarts or driver updates
- Temporary user repositioning out of camera trackability range

If the plugin crashes or enters an infinite tight loop when a disconnect occurs, the user loses control of their computer and cannot call for assistance. Conversely, hot-polling at fixed intervals consumes unnecessary CPU and power.

## Decision
1. **Explicit State Model**:
   The lifecycle is governed by an explicit state machine with the following states:
   - `Created`: Initialized, no background threads, no hardware access.
   - `Starting`: Resolving runtime, initializing API, enumerating devices.
   - `Connected`: Tracker connected, gaze stream active, delivering gaze points.
   - `Reconnecting`: Stream lost or device detached; waiting before retrying with backoff.
   - `Stopping`: Teardown requested; cancelling background worker, unsubscribing stream.
   - `Stopped`: Worker stopped, resources clean, ready to restart if needed.
   - `Disposed`: Terminal state; all handles released, no further operations allowed.

   *Error information is stored as structured `ErrorInfo` rather than multiplying states.*

2. **Single Reconnect Loop with Exponential Backoff & Jitter**:
   - Disconnection triggers transition to `Reconnecting`.
   - Reconnection interval follows:
     $$T_{\text{wait}} = \min(T_{\text{max}}, T_{\text{base}} \times 2^{\text{retryCount}}) + \text{jitter}$$
     where $T_{\text{base}} = 500\text{ ms}$, $T_{\text{max}} = 10000\text{ ms}$, and $\text{jitter} \in [0, 250\text{ ms}]$.
   - Prevents USB bus saturation or high CPU usage during prolonged disconnects.
   - Only ONE reconnect loop can be active at any given time (concurrency-locked via `CancellationToken` and state gates).

3. **Event-Driven Callback Wait (No Hot Polling)**:
   The background capture thread uses `tobii_wait_for_callbacks([deviceContext])` rather than an arbitrary `Thread.Sleep(30)`.
   - When samples are available, the native thread awakens immediately.
   - It invokes `tobii_device_process_callbacks` to trigger registered delegates.
   - A short timeout (e.g., 200 ms) in `wait_for_callbacks` allows periodic check of `CancellationToken.IsCancellationRequested` so shutdowns remain snappy (<200 ms).

4. **Guarantees Around Disposal**:
   - `Dispose()` immediately signals the `CancellationTokenSource`.
   - Native handles (`deviceContext`, `apiContext`) are released safely under a synchronization lock.
   - No callbacks are fired after `Dispose()`.
   - No reconnect attempts are initiated after `Dispose()`.

## Consequences
- Highly responsive recovery from USB unplugs, driver restarts, and PC sleep/wake.
- Low CPU consumption when idling or waiting for gaze samples.
- Zero race conditions during shutdown or rapid start/stop sequences.
