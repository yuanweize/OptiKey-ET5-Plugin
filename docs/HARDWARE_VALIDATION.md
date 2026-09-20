# Hardware Validation Protocol and Release Gate

## Overview
Because this software serves users with severe motor disabilities (such as ALS/MND) who depend on eye tracking for life-essential communication, **no standard release will be tagged until rigorous hardware validation on physical Tobii Eye Tracker 5 devices is completed**.

Until all checks in this protocol pass, builds are distributed strictly as **GitHub Pre-releases** labeled with `-alpha` or `-beta`. OptiKey's internal plugin search explicitly ignores pre-releases, protecting vulnerable users from experimental builds.

---

## Hardware Validation Checklist

### 1. Environment & Baseline Setup
- [ ] Windows 10 x64 physical machine tested
- [ ] Windows 11 x64 physical machine tested
- [ ] Official Tobii Eye Tracker 5 hardware mounted below screen
- [ ] Official Tobii Experience software installed
- [ ] Tobii display setup and user profile calibration completed

### 2. Runtime Discovery & Security Verification
- [ ] Run `tools/HardwareDiagnostics/ET5Diagnostics.exe`
- [ ] Confirm runtime detected at valid path (`%ProgramFiles%\Tobii\Tobii Service`)
- [ ] Confirm binary signature verified (Issuer: Tobii AB)
- [ ] Confirm x64 PE architecture validated
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
  - Restart `Tobii.Service` via `net stop "Tobii Service"` and `net start "Tobii Service"`.
  - Verify plugin reconnects cleanly.

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
| CI Build & Tests | 100% Pass | PENDING | GitHub Actions | -- |
| ZIP Asset Scan | Clean (no Tobii/Test DLLs) | PENDING | CI Package Script | -- |
| Reflection Loader | 1 IPointService | PENDING | OptiKeyLoaderTest | -- |
| Physical ET5 Test | Win 10/11 Passed | PENDING | Physical Tester | -- |
| 2h Soak Test | 0 Leaks / 0 Crashes | PENDING | Physical Tester | -- |
