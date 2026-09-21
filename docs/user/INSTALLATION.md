[English](INSTALLATION.md) | [简体中文](INSTALLATION.zh-CN.md)

# Installation and First-Time Setup Guide

This guide describes how to install, configure, and activate the **OptiKey-ET5-Plugin** for Tobii Eye Tracker 5 in OptiKey 4.x.

---

## 1. Prerequisites

Before installing the plugin, ensure the following prerequisites are met:

1. **Operating System**: Windows 10 or Windows 11 (64-bit edition).
2. **Hardware**: Tobii Eye Tracker 5 connected directly to a USB port (USB 2.0/3.0) and mounted to your calibrated primary display.
3. **Tobii Experience Software**:
   - Download and install the official Tobii Experience from [Tobii Gaming Getting Started](https://gaming.tobii.com/getstarted/).
   - Ensure the Tobii Service is running and user calibration is complete. Test that gaze tracking works within the Tobii Experience app.
4. **OptiKey 4.x**:
   - Install the latest stable release of OptiKey from [OptiKey GitHub Releases](https://github.com/OptiKey/OptiKey/releases).

> [!IMPORTANT]
> The plugin uses your locally installed Tobii runtime (`tobii_stream_engine.dll`). You do **not** need to compile any code, download vendor SDKs, or manually copy DLLs into system folders.

---

## 2. Installation Methods

### Method A: Online Plugin Installation (Recommended)

OptiKey 4.x supports discovering plugins online directly through GitHub topic tags:

1. Launch OptiKey.
2. Open the **Management Console** (press `Alt + M` or click the menu icon).
3. Navigate to the **Pointing & Selecting** tab.
4. Click **Find more eye tracker options online**.
5. Select **Tobii Eye Tracker 5 (ET5)** from the list and click **Install**.
6. Restart OptiKey if prompted.

### Method B: Manual Installation via Release ZIP

If installing in an offline or air-gapped environment:

1. Download the latest `OptiKey-ET5-Plugin-vX.Y.Z.zip` from [GitHub Releases](https://github.com/yuanweize/OptiKey-ET5-Plugin/releases).
2. Verify the package SHA256 checksum against `SHA256SUMS.txt`.
3. Close OptiKey if it is running.
4. Extract the ZIP contents to your user eye tracker plugins folder under a dedicated child directory:
   ```text
   %APPDATA%\OptiKey\OptiKey\EyeTrackerPlugins\OptiKey-ET5-Plugin\
   ```
   *(Note: OptiKey scans `%APPDATA%\OptiKey\OptiKey\EyeTrackerPlugins\` recursively for plugin DLLs; creating the dedicated subfolder keeps plugins organized and avoids file collisions).*
5. Launch OptiKey.

---

## 3. First-Time Setup in OptiKey

1. In OptiKey, open the **Management Console** (`Alt + M`).
2. Go to **Pointing & Selecting**.
3. In the **Pointing device** dropdown, select:
   ```text
   Tobii Eye Tracker 5 (ET5)
   ```
4. Click **OK** to save settings.
5. OptiKey will immediately connect to your Eye Tracker 5 and start streaming normalized gaze points.

---

## 4. Multi-Device Configuration (Optional)

For single-tracker setups, device selection is completely automatic.

If your system has multiple Tobii eye trackers attached, the plugin refuses silent connection to device index 0 for user safety. To configure a specific device, create or edit:

```text
%APPDATA%\OptiKey-ET5-Plugin\et5-plugin.config
```

Example configuration file:

```ini
# Automatically select if exactly 1 device is found (default: true)
AutomaticDeviceSelection=true

# Device index for multi-device setups (0, 1, etc.)
PreferredDeviceIndex=0

# Callback strategy: Polling (default) or WaitAndProcess
CallbackStrategy=Polling
```

---

## 5. Next Steps & Troubleshooting

- For common setup issues, calibration errors, or missing runtime warnings, refer to the [Troubleshooting Guide](TROUBLESHOOTING.md).
- To verify display resolutions and supported OS versions, see [Compatibility Matrix](COMPATIBILITY.md).
