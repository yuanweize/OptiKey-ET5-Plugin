[English](HARDWARE_VALIDATION.md) | [简体中文](HARDWARE_VALIDATION.zh-CN.md)

# Hardware Validation Protocol and Release Gate

## Overview
Because this software serves users with severe motor disabilities (such as ALS/MND) who depend on eye tracking for life-essential communication, **no standard release will be tagged until rigorous hardware validation on physical Tobii Eye Tracker 5 devices is completed**.

Until all checks in this protocol pass, builds are distributed strictly as **GitHub Pre-releases** labeled with `-alpha` or `-beta`. OptiKey's internal plugin search explicitly ignores pre-releases, protecting vulnerable users from experimental builds.

---

## Pre-Hardware Diagnostic Tool
Before connecting OptiKey or installing the plugin binary, run the non-invasive diagnostic inventory script to inspect the host environment:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\HardwareDiagnostics\inventory-tobii-runtime.ps1
```

The script inspects:
- Standard installation directories, uninstall registry hives, and Windows services
- COFF architecture headers (`AMD64` 64-bit verification)
- Authenticode signature and digital certificate subject (`Tobii AB`)
- Exported symbol capability table (`tobii_api_*`, `tobii_device_*`, `tobii_gaze_*`, `tobii_*_callbacks`)
- Privacy guarantee: device URLs, serial numbers, and local usernames (`%USERPROFILE%`) are automatically sanitized.

---

## Hardware Validation Checklist

### 1. Environment & Baseline Setup
- [ ] Windows 10 x64 physical machine tested
- [ ] Windows 11 x64 physical machine tested
- [ ] Official Tobii Eye Tracker 5 hardware mounted below screen
- [ ] Official Tobii Experience software installed
- [ ] Tobii display setup and user profile calibration completed

### 2. Runtime Discovery & Security Verification
- [ ] Run `.\tools\HardwareDiagnostics\inventory-tobii-runtime.ps1`
- [ ] Confirm runtime detected at valid path (`tobii_stream_engine.dll`)
- [ ] Confirm binary signature verified (`Issuer: Tobii AB`, `SignatureStatus: Valid`)
- [ ] Confirm x64 PE architecture validated (`IMAGE_FILE_MACHINE_AMD64`)
- [ ] Confirm device enumerated (Model: Eye Tracker 5 / IS50, Serial redacted)

### 3. Coordinate Accuracy & Multi-DPI Verification
- [ ] Display resolution: 1920x1080 @ 100% DPI
  - Point selection on all 4 corners and center of OptiKey keyboard is accurate
- [ ] Display resolution: 1920x1080 @ 125% DPI
  - Verify key hit detection matches visual gaze target without drift
- [ ] Display resolution: 2560x1440 @ 150% DPI
  - Verify key hit detection remains pixel-accurate
- [ ] Display resolution: 3840x2160 (4K) @ 200% DPI
  - Verify scaling does not clip coordinate space

### 4. Physical Disturbance & Recovery (Resilience)
- [ ] **USB Unplug / Re-plug Test**:
  - While OptiKey is running and typing, unplug the ET5 USB cable.
  - Verify OptiKey stays responsive (no freeze/crash), transitions to Reconnecting.
  - Re-plug the USB cable.
  - Verify gaze automatically restores within 3 seconds without restarting OptiKey.
- [ ] **PC Sleep / Resume Test**:
  - Put Windows to sleep while OptiKey is active.
  - Wake Windows.
  - Verify gaze stream resumes automatically.
- [ ] **Tobii Service Restart Test**:
  - Restart `Tobii Service` via `net stop "Tobii Service"` and `net start "Tobii Service"`.
  - Verify plugin reconnects cleanly.
- [ ] **Bounded Shutdown / Stuck-Worker Test**:
  - Unplug USB during active gaze stream and immediately close OptiKey.
  - Verify OptiKey terminates cleanly within bounded timeout (< 3s) without hanging or crashing with `AccessViolationException`.

### 5. Endurance & Stability (Soak Test)
- [ ] **30-Minute Continuous Gaze Typing Soak Test**:
  - Run continuous gaze typing session for 30 minutes.
  - Check memory footprint: No managed or unmanaged memory leaks (working set < 80MB).
  - Verify CPU usage is < 2% average.
- [ ] **2-Hour Extended Idle & Burst Test**:
  - Leave OptiKey open for 2 hours with periodic gaze input.
  - Verify zero unhandled exceptions, zero deadlocks.

---

## Release Sign-off Table
| Validation Gate | Target | Result | Validator | Date |
| :--- | :--- | :--- | :--- | :--- |
| CI Build & Tests (pinned-stable) | 100% Pass | PASS | GitHub Actions | 2026-09-21 |
| CI Build & Tests (upstream-main) | 100% Pass | PASS | GitHub Actions | 2026-09-21 |
| ZIP Asset Scan | Clean (no Tobii/Test DLLs) | PASS | CI Package Script | 2026-09-21 |
| Reflection Loader Smoke Test | 1 IPointService | PASS | OptiKeyLoaderTest | 2026-09-21 |
| Physical ET5 Test | Win 10/11 Passed | PENDING | Physical Tester | -- |
| 2h Soak Test | 0 Leaks / 0 Crashes | PENDING | Physical Tester | -- |
