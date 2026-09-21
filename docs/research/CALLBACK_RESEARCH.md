[English](CALLBACK_RESEARCH.md) | [简体中文](CALLBACK_RESEARCH.zh-CN.md)

# Callback Processing Research & Strategy Evidence

> **Document Status**: Active research specification (Phase E)  
> **Date**: 2026-09-21  
> **Scope**: Tobii Stream Engine callback mechanics, shutdown boundedness, and use-after-free prevention

---

## 1. The Core Problem: Potential Unbounded Native Wait

In standard Tobii Stream Engine sample code and historical integrations, the gaze streaming loop typically follows this pattern:

```c
while (is_running) {
    tobii_error_t error = tobii_wait_for_callbacks(device);
    if (error == TOBII_ERROR_NO_ERROR) {
        tobii_device_process_callbacks(device);
    }
}
```

### The In-Process Failure Mode:
1. `tobii_wait_for_callbacks(device)` takes only the `device` handle; it has no publicly documented timeout parameter.
2. If no gaze data arrives (e.g. tracker disconnected, user looks away, sleep mode), the thread can remain indefinitely suspended inside native driver/service code.
3. When OptiKey requests `Stop()`, the managed layer can signal a stop flag or cancellation token, but the native thread inside `tobii_wait_for_callbacks` does NOT inspect CLR cancellation tokens.
4. If managed code performs a bounded join (e.g. `workerThread.Join(2000)`):
   - When the timeout expires, the managed caller unblocks to avoid hanging OptiKey.
   - **CRITICAL RISK**: If the managed code subsequently calls `tobii_device_destroy()` or `tobii_api_destroy()`, or unloads the DLL, the stuck native thread may subsequently wake up and dereference destroyed memory. This results in an immediate `STATUS_ACCESS_VIOLATION` (0xC0000005) or hard process termination, crashing OptiKey.

---

## 2. Architectural Lifecycle Contract (Enforced in Phase D)

To prevent native memory corruption and use-after-free crashes, the plugin enforces the following strict lifecycle contract in [TobiiGazeProvider.cs](../../src/OptiKey.ET5.Plugin/Runtime/TobiiGazeProvider.cs) and [WaitAndProcessCallbackPump.cs](../../src/OptiKey.ET5.Plugin/Runtime/Callbacks/WaitAndProcessCallbackPump.cs):

```
+-------------------------------------------------------------+
|                     RequestStop()                           |
+-------------------------------------------------------------+
                              |
                              v
+-------------------------------------------------------------+
|                     Join(stopTimeout)                       |
+-------------------------------------------------------------+
           |                                       |
  (Joined within timeout)                (Timed out after limit)
           |                                       |
           v                                       v
+-----------------------+              +-----------------------+
| State = Stopped       |              | State = TimedOut      |
| Safe to call:         |              | LOG CRITICAL WARNING  |
| - UnsubscribeGaze()   |              | State = Error         |
| - DisconnectDevice()  |              | SKIP ALL NATIVE       |
| - runtime.Dispose()   |              | CLEANUP/DEALLOCATION! |
+-----------------------+              +-----------------------+
```

---

## 3. Evaluation of Candidate Strategies

Three architectural paths are available:

### PATH A: Wait + Wake/Cancel (Official Signal Mechanism)
- **Concept**: Native API provides a wake function (e.g. `tobii_device_cancel_wait` or signaling an internal event).
- **Current Evidence**:
  - Inspection of probed exports (`tobii_api_create`, `tobii_enumerate_local_device_urls`, `tobii_device_create`, `tobii_device_destroy`, `tobii_gaze_point_subscribe`, `tobii_wait_for_callbacks`, `tobii_device_process_callbacks`, `tobii_device_reconnect`) reveals **no** dedicated cancel/wake export in standard Stream Engine client headers.
  - Calling `tobii_gaze_point_unsubscribe()` from another thread may or may not wake `tobii_wait_for_callbacks()`. This requires empirical testing on real ET5 hardware.
- **Verdict**: Unproven. Cannot rely on Path A without hardware confirmation.

### PATH B: Process-Only Polling Loop (`ProcessOnlyPollingPump`)
- **Concept**: Bypasses `tobii_wait_for_callbacks()` entirely. Uses `tobii_device_process_callbacks()` on a periodic polling interval (e.g. 5ms) managed via CLR event wait.
- **Advantages**:
  - `stopEvent.Set()` unblocks the managed wait handle immediately (< 1ms).
  - Managed timeout and cancellation containment verified in CI; avoids indefinite blocking in `tobii_wait_for_callbacks`.
- **Open Empirical Questions**:
  - Does `tobii_device_process_callbacks()` return immediately when no callbacks are queued, returning `TOBII_ERROR_TIMED_OUT` or `TOBII_ERROR_NO_ERROR`?
  - Does skipping `tobii_wait_for_callbacks()` increase CPU usage slightly (e.g. 0.5-1% on modern multicore CPU)?
- **Verdict**: Viable fallback for in-process execution, implemented in [ProcessOnlyPollingPump.cs](../../src/OptiKey.ET5.Plugin/Runtime/Callbacks/ProcessOnlyPollingPump.cs).

### PATH C: RuntimeHost Out-of-Process Architecture (`RuntimeHostIsolated`)
- **Concept**: Dedicated lightweight helper executable (`OptiKey.ET5.RuntimeHost.exe`) that loads `tobii_stream_engine.dll` and streams gaze samples to OptiKey over a local Windows Named Pipe.
- **Advantages**:
  - **Crash Containment**: A native crash (AccessViolation, SEH exception) in Tobii driver/DLL crashes only the host process, never OptiKey.
  - **Bounded Shutdown**: If native wait hangs, OptiKey closes the pipe and terminates the helper process (`Process.Kill()`). Zero risk to OptiKey's process space.
  - **Clean CLR / Native Lifecycle Boundary**: No unmanaged delegates held inside the OptiKey AppDomain.
  - **Zero Security Surface**: Local named pipe only, strictly anonymous/local, no TCP, no network, no gaze logging.
- **Verdict**: Formally evaluated in ADR-009 as a prime candidate for production-grade robustness.

---

## 4. Hardware Verification Checklist (To Be Executed on ET5 Setup)

When testing with physical Tobii Eye Tracker 5 hardware:
1. Probe return codes of `tobii_device_process_callbacks()` when called without `tobii_wait_for_callbacks()`.
2. Measure CPU utilization of `ProcessOnlyPollingPump` at 5ms and 10ms poll intervals.
3. Test whether `tobii_gaze_point_unsubscribe()` wakes up a blocked `tobii_wait_for_callbacks()` call.
4. Measure IPC latency over Windows Named Pipe to determine if RuntimeHost introduces any perceptible gaze latency.
