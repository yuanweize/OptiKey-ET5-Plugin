# ADR-009: Architectural Evaluation: In-Process vs. RuntimeHost Isolation

**Status**: Proposed / Evaluation Baseline (Phase F, 2026-09-21)  
**Context**: Critical reliability evaluation for AAC users (ALS/MND) who depend on uninterrupted eye tracking.

---

## 1. Context and Problem Statement

When integrating Tobii Eye Tracker 5 via the native `tobii_stream_engine.dll`, the plugin faces inherent native risks:
1. **Unbounded Native Blocking**: `tobii_wait_for_callbacks` lacks a documented timeout parameter and cannot be cancelled across the P/Invoke boundary by managed cancellation tokens.
2. **Access Violations and Fault Isolation**: Any unhandled SEH exception, memory corruption, or driver fault inside the Tobii C library or service communication layer directly crashes the host process (`OptiKey.exe`).
3. **Use-After-Free on Shutdown**: If managed code terminates a worker join after a timeout and subsequently calls native teardown (`tobii_device_destroy`, `FreeLibrary`), a delayed wake-up in native code triggers memory access violations.

We must formally evaluate whether the native binding should execute **In-Process** within OptiKey's CLR AppDomain or **Out-of-Process** in a dedicated helper executable (`OptiKey.ET5.RuntimeHost.exe`).

---

## 2. Architectural Comparison Matrix

| Evaluation Criterion | In-Process Architecture | Out-of-Process (`RuntimeHost`) | Evaluation Notes |
| :--- | :--- | :--- | :--- |
| **Crash Containment** | ❌ **Zero Isolation**: Any native `STATUS_ACCESS_VIOLATION` in `tobii_stream_engine.dll` immediately terminates `OptiKey.exe`. | ✅ **Complete Isolation**: A native crash only terminates `RuntimeHost.exe`. OptiKey survives, transitions to `Reconnecting`, and can restart the host. | For AAC users with ALS/MND, a complete OptiKey crash leaves the patient without a voice or nurse call mechanism. |
| **Shutdown Boundedness** | ⚠️ **Conditional**: Requires fail-safe skipping of native deallocation on timeout to prevent use-after-free. | ✅ **Guaranteed (<50ms)**: OptiKey closes the named pipe; if the child process does not exit, OptiKey calls `Process.Kill()`. Safe OS-level reclamation. | In-process can never cleanly free leaked native worker threads that are stuck. |
| **Fault Recovery** | ❌ **Requires Full OptiKey Restart**: A corrupt native state cannot be purged from the host process space without closing OptiKey. | ✅ **Autonomous Process Recycle**: OptiKey detects pipe disconnect, terminates zombie host, spawns fresh `RuntimeHost.exe`, and resumes tracking seamlessly. | Significantly boosts unattended uptime. |
| **Gaze Latency** | ✅ **Direct Call (~0 μs)**: Direct P/Invoke delegate invocation. | ⚖️ **Negligible (~50–150 μs)**: Local Windows Named Pipe IPC overhead is sub-millisecond, far below the tracker's sampling interval (15–30 ms @ 33–66 Hz) and dwell threshold (300–600 ms). | End-user perception is identical. |
| **Complexity** | ✅ **Lower**: Single assembly, direct P/Invoke bindings. | ⚖️ **Moderate**: Requires a small helper console binary and binary IPC framing over named pipes. | Mitigated by clean separation of concerns and pipe test harnesses. |
| **Deployment Footprint** | ✅ **Single DLL**: `OptiKey.ET5.Plugin.dll` | ⚖️ **DLL + Helper EXE**: `OptiKey.ET5.Plugin.dll` + `OptiKey.ET5.RuntimeHost.exe` (~30 KB extra). | No external dependencies; both built from the same solution. |
| **OptiKey Lifetime Isolation** | ❌ **Shared Memory/Handles**: Plugin shares memory, thread pool, and GC cycles with OptiKey UI. | ✅ **Independent Memory Space**: Native allocations and driver buffers reside entirely in the child process. | Zero risk of native memory leaks exhausting OptiKey's 32/64-bit address space. |
| **Diagnostics & Observability** | ⚠️ **Limited**: Crash logs only captured if Windows Error Reporting or WER dump configured. | ✅ **Superior**: Standard input/output/error pipes allow real-time diagnostic stream ingestion into OptiKey logs. | Easier troubleshooting of driver/service errors in user environments. |

---

## 3. Extensible IPC Protocol Design

If `RuntimeHost` is deployed, the IPC protocol between OptiKey and `RuntimeHost.exe` must use local Windows Named Pipes (`\\.\pipe\OptiKey-ET5-{SessionId}`) with structured, versioned binary framing to ensure forward and backward compatibility.

### Security Guarantees:
- **Local named pipe only**: No TCP, no network sockets, no open listening ports.
- **Strict Access Control**: `PipeOptions.Asynchronous`, restricted to the current user's Windows security token (`PipeSecurity`).
- **Privacy Assurance**: No gaze samples or video feeds are ever persisted to disk or sent over a network.

### Extensible Framing Specification:

All messages begin with a fixed 16-byte header:

```
 0                   1                   2                   3
 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                 Protocol Magic (0x4F455435 - "OET5")          |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|       Protocol Version        |          Message Type         |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                        Payload Length                         |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                        Reserved / Flags                       |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
|                                                               |
+                     Payload Data (Variable)                   +
|                                                               |
+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
```

#### Fields:
1. **Magic** (`uint32`): Fixed `0x4F455435` (ASCII representation of `"OET5"`). Rejects rogue or mismatched connections immediately.
2. **Protocol Version** (`uint16`): Current version `0x0001`. Enables evolutionary upgrades without breaking existing clients.
3. **Message Type** (`uint16`):
   - `0x0001` - `Heartbeat / Ping`
   - `0x0002` - `HeartbeatAck / Pong`
   - `0x0010` - `Command_StartGaze`
   - `0x0011` - `Command_StopGaze`
   - `0x0012` - `Command_GetCapabilities`
   - `0x0020` - `Event_CapabilitiesReport`
   - `0x0021` - `Event_GazePoint`
   - `0x0022` - `Event_StatusChanged`
   - `0x0023` - `Event_DeviceError`
4. **Payload Length** (`uint32`): Byte length of the following payload block (0 for parameterless messages).
5. **Reserved / Flags** (`uint32`): Reserved for future streaming compression, encryption flags, or alignment padding.

#### Gaze Point Payload Definition (`Event_GazePoint`, 24 bytes):
```c
struct GazePointPayload {
    int64_t  timestamp_us;     // Microseconds tracker timestamp
    float    normalized_x;     // [0.0, 1.0]
    float    normalized_y;     // [0.0, 1.0]
    uint8_t  validity;         // 1 = Valid, 0 = Invalid
    uint8_t  reserved[7];      // Padding / future eye-specific flags
};
```

---

## 4. Decision Rule and Phased Path Forward

1. **Phase D/E (Current)**:
   - Provide managed abstraction (`ICallbackPump`, `WaitAndProcessCallbackPump`, `ProcessOnlyPollingPump`) with bounded join timeouts and use-after-free skipping.
   - This keeps in-process execution as safe as theoretically possible.
2. **Empirical Gate (Phase F Hardware Testing)**:
   - If physical Tobii Eye Tracker 5 testing reveals that `tobii_wait_for_callbacks` blocks unboundably on disconnect and `ProcessOnlyPollingPump` has unacceptable CPU/sampling artifacts:
     - **`RuntimeHost` will be adopted as the default production architecture.**
   - The minimal latency overhead (~100 μs) is completely negligible compared to human eye fixation (200–400 ms) and OptiKey key dwell times (300–600 ms).
   - The crash isolation and reliable termination guarantees provide unmatched clinical safety for paralyzed AAC users.
