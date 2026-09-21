[English](TROUBLESHOOTING.md) | [简体中文](TROUBLESHOOTING.zh-CN.md)

# Troubleshooting Guide

## Diagnostic Quick-Start
Before modifying configuration files or driver settings, run the standalone, non-invasive PE diagnostic tool:
1. Open PowerShell on the target machine.
2. Run:
   ```powershell
   .\tools\HardwareDiagnostics\inventory-tobii-runtime.ps1
   ```
3. Check the generated `tobii-runtime-inventory-*.txt` report:
   - Verify that `tobii_stream_engine.dll` is discovered.
   - Confirm PE Machine Architecture is `x64` (`IMAGE_FILE_MACHINE_AMD64`).
   - Check Authenticode signature status and Publisher subject (`Tobii AB`).
   - Confirm required exported symbols (`tobii_api_create`, `tobii_device_create`, `tobii_gaze_point_subscribe`, etc.) are present.

---

## Common Issues and Solutions

### 1. "Tobii Eye Tracker not detected" / Error: TOBII_TRACKER_NOT_FOUND

#### In Strict Mode (Default):
- **Symptom**: Log message shows: `Candidate rejected: device '...' is not a verified Tobii Eye Tracker 5 (Strict Mode).`
- **Cause**: The plugin strictly enforces hardware verification to protect ALS/MND users from unintended operation on non-target devices.
- **Remedy**:
  - Ensure the physical device is a genuine Tobii Eye Tracker 5 connected via USB.
  - If developing or testing on a developer device / evaluation kit, see **Developer Test Mode** below.

#### Developer Test Mode:
- **Symptom**: `Developer Test Mode requires an explicit PreferredDeviceIndex or PreferredDeviceUrl.`
- **Cause**: When `AllowUnverifiedDevices = true`, the plugin deliberately **refuses** to automatically bind to the first available candidate (`candidate 0`) to prevent accidental connection.
- **Remedy**: Configure an explicit device index or URL in `PluginConfiguration`:
  ```csharp
  var config = new PluginConfiguration
  {
      AllowUnverifiedDevices = true,
      PreferredDeviceIndex = 0
  };
  ```

---

### 2. Runtime Security & Verification Failures

#### Signer Validation Failed (`RuntimeSignerValidationFailed`):
- **Symptom**: Log shows `Candidate rejected: Tobii signer verification failed: ... Subject='None'`.
- **Cause**: The discovered `tobii_stream_engine.dll` is unsigned, corrupted, or signed by a publisher other than Tobii.
- **Remedy**:
  - Reinstall the official **Tobii Experience** package from the Microsoft Store or Tobii support.
  - Do NOT attempt to place arbitrary or third-party DLLs in the probing path.

#### Untrusted Root Certificate Warning:
- **Symptom**: Log shows `Notice: Binary signature is valid but certificate chain is UntrustedRoot`.
- **Cause**: The file has a valid digital signature matching Tobii, but the root CA certificate is not installed in the Windows Trusted Root Certification Authorities store (often seen in offline or air-gapped test rigs).
- **Remedy**: The plugin logs a diagnostic warning and proceeds if the signature and publisher are valid. To resolve the warning, connect to Windows Update to refresh the root certificates.

---

### 3. Native Shutdown & Stuck Worker Warning (`STUCK_WORKER`)

- **Symptom**: Warning logged during OptiKey shutdown:
  `Native callback worker did not terminate within ... ms. Abandoning stuck thread to prevent host crash.`
- **Cause**: The underlying native `tobii_wait_for_callbacks` API can block indefinitely when the stream engine stops receiving packets.
- **Safety Mechanism**:
  - Unlike naive wrappers that call `tobii_device_destroy()` or `FreeLibrary()` while the thread is blocked (which causes a fatal `AccessViolationException` in OptiKey), OptiKey-ET5-Plugin marks the pump as `Faulted` / `STUCK_WORKER` and skips native handle destruction.
  - The OptiKey UI remains completely responsive and closes cleanly.
- **Remedy**: If this occurs frequently, check whether USB Selective Suspend is enabled or switch the callback strategy to `ProcessOnlyPollingPump`.

---

### 4. Missing Exported Symbols (`RuntimeMissingRequiredExports`)

- **Symptom**: Plugin fails to initialize with `ET5ErrorCode.RuntimeMissingRequiredExports`.
- **Cause**: An older or stripped version of `tobii_stream_engine.dll` was loaded that lacks core or gaze subscription exports.
- **Remedy**:
  - Run `inventory-tobii-runtime.ps1` to inspect the exported symbol table.
  - Required exports:
    - Core: `tobii_api_create`, `tobii_api_destroy`
    - Device: `tobii_device_create`, `tobii_device_destroy`, `tobii_enumerate_devices`
    - Gaze: `tobii_gaze_point_subscribe`, `tobii_gaze_point_unsubscribe`
  - Update the Tobii driver software to the latest official release.

---

### 5. USB & Hardware Connection Issues

- **Tobii Background Service stopped**:
  - Open `services.msc`, check **Tobii Service**, and start/restart it.
- **USB Power Drops / Hub Issues**:
  - Always plug the Tobii Eye Tracker 5 directly into a motherboard USB 3.0 port. Avoid unpowered USB hubs or keyboard passthroughs.
  - Disable **USB selective suspend** in Windows Power Options.
- **Calibration Drift**:
  - Open **Tobii Experience** and run display setup/calibration before launching OptiKey.
