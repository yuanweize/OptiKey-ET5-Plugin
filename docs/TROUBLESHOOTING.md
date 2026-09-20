# Troubleshooting Guide

## Quick Diagnostic Checklist
Before troubleshooting deeper, run the included diagnostic tool:
1. Open PowerShell or Command Prompt.
2. Navigate to the diagnostic tool directory or run:
   ```powershell
   .\tools\HardwareDiagnostics\diagnose.ps1
   ```
3. Check the output summary.

---

## Common Issues and Solutions

### 1. "Tobii Eye Tracker not detected" or Error: TOBII_TRACKER_NOT_FOUND
- **Cause A: Tobii Background Service is stopped.**
  - **Fix**: Open Windows Services (`services.msc`), find **Tobii Service**, right-click and select **Start** or **Restart**.
- **Cause B: USB cable loose or plugged into unpowered hub.**
  - **Fix**: Plug the Tobii Eye Tracker 5 directly into a motherboard USB 3.0/2.0 port on the back of the PC. Avoid unpowered USB splitters.
- **Cause C: Device disabled in Device Manager.**
  - **Fix**: Open Device Manager (`devmgmt.msc`), expand *Eye Tracker* and *Universal Serial Bus controllers*. Ensure no Tobii devices have a yellow exclamation mark.

### 2. "No IPointService implementation found" / Error: EYETRACKER_DLL_NOT_VALID
- **Cause**: An incorrect DLL was selected, or files are missing from the plugin folder.
- **Fix**:
  - Verify that `OptiKey.ET5.Plugin.dll` is placed in:
    `%LocalAppData%\OptiKey\OptiKey\EyeTrackerPlugins\yuanweize\OptiKey-ET5-Plugin\<tag>\`
  - In OptiKey Management Console -> Pointing & Selecting -> Eye tracker, ensure **Tobii Eye Tracker 5** is selected.

### 3. Gaze is Inaccurate or Keys Drift Away at Different Screen Positions
- **Cause A: Uncalibrated Display.**
  - **Fix**: Open **Tobii Experience** from the Start Menu or System Tray. Click **Display Setup** and re-calibrate your eye profile.
- **Cause B: Windows DPI Scaling mismatch.**
  - **Fix**: OptiKey and Tobii ET5 use physical display pixel mapping. Ensure Windows Display Scaling is set to standard presets (100%, 125%, 150%) and that monitor physical resolution is set to its native recommended value.

### 4. Tracker Disconnects After PC Wakes from Sleep
- **Cause**: Windows USB Selective Suspend is powering down the tracker.
- **Fix**:
  1. Open Windows **Power & Sleep Settings** -> **Additional power settings**.
  2. Click **Change plan settings** -> **Change advanced power settings**.
  3. Expand **USB settings** -> **USB selective suspend setting** -> Set to **Disabled**.
  4. Expand **Device Manager** -> **Universal Serial Bus controllers** -> Right-click Tobii USB Hub -> **Properties** -> **Power Management** -> Uncheck *Allow the computer to turn off this device to save power*.

### 5. Plugin Log Files
Plugin runtime logs are appended to:
`%AppData%\OptiKey\OptiKey\Logs\OptiKey.ET5.Plugin.log`
Examine this file to review connection timestamps, reconnect attempts, and error codes.
