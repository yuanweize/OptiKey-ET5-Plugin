# Privacy Policy and Biometric Data Handling

## Fundamental Principles
Assistive communication software carries the highest level of privacy responsibility. Locked-in users type private personal messages, banking credentials, medical queries, and intimate communications.

The `OptiKey-ET5-Plugin` architecture is built around **Data Minimization** and **Zero Persistence**:

1. **Ephemeral Real-Time Processing Only**:
   - Gaze coordinates received from the Tobii sensor are processed in volatile memory for the sole purpose of immediate on-screen key selection.
   - Coordinates are discarded immediately after hit-testing.
2. **Zero Local Storage of Gaze Data**:
   - The plugin does NOT write raw gaze points, coordinates, heatmaps, or timestamps to disk.
   - Log files record ONLY high-level lifecycle events (e.g., `Connected`, `Reconnecting`, `Disconnected`, error codes). Raw $(X, Y)$ points are **never logged**.
3. **Zero Network Transmission**:
   - The plugin contains no telemetry, analytics, tracking, or network transmission code.
   - It performs zero HTTP/TCP/UDP requests.
4. **Diagnostic Tool Redaction**:
   - The hardware diagnostic tool (`ET5Diagnostics.exe`) reports aggregate sample counts (e.g., `167 samples received in 5s`) and driver version.
   - It **never outputs or logs user gaze coordinates**.
   - Hardware serial numbers are partially redacted in diagnostic logs.
5. **Interactive Field of Use Compliance**:
   - Complies strictly with Tobii's `TOBII_FIELD_OF_USE_INTERACTIVE` mandate: gaze data is consumed exclusively to drive interactive UI controls without storage or transfer.
