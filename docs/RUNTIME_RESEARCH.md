# Runtime Research and Tobii Integration Specification

> **Document Status**: Partially verified; hardware, installation layout, and legal compatibility remain unverified
> **Deliverable Requirement**: User Review Requirement #15  
> **Target Hardware**: Tobii Eye Tracker 5 (ET5, IS50 series)  
> **Runtime Target**: Windows 10/11 x64 with Tobii Experience

---

## 0. 2026-09-21 Current-source correction

Tobii's current [Streams SDK page](https://www.tobii.com/products/integration/tobii-streams-sdk) advertises Stream Engine Client 7.2, requires a development-license subscription, and explicitly states that Eye Tracker 5 without the `L` is a gaming device that cannot be used for development purposes. The same page's developer-guide and API-reference links did not yield a retrievable versioned contract during this review.

Accordingly, the component names, paths, declarations, and lifecycle discussion below are research leads, not proof of a supported ET5 integration. No active declaration may be promoted to hardware-ready status without a legitimately obtained versioned reference and written Tobii clarification for this accessibility use.

## 1. What Runtime Components Does Tobii Experience Actually Install?

The following component names are historical research leads. They have not been observed on a current ET5 Windows installation in this project:

### A. Kernel and Device Drivers
- **Tobii Eye Tracker 5 Driver (`tobii_usb.sys` / `tobii_sensor.sys`)**:
  Manages low-level USB communication with the IS50 hardware over USB 2.0/3.0.
- **Tobii Virtual Device Drivers**:
  Registers device interfaces in the Windows Device Manager under *Eye Tracker* and *Universal Serial Bus devices*.

### B. Background Services
- **Tobii Service (`Tobii.Service.exe`)**:
  Runs as a Windows NT service (typically Automatic start). Responsible for sensor power management, illumination timing, calibration calculation, and hosting the inter-process IPC communication channels.
- **Tobii Experience Helper Service**:
  Facilitates communication between the UWP / WinUI store application and the Win32 background service.

### C. Client Runtime Libraries
- **`tobii_stream_engine.dll`**:
  The official C API dynamic library that acts as the client library communicating with `Tobii.Service.exe` via local IPC / shared memory.
  - Typical directory:
    - `%ProgramFiles%\Tobii\Tobii Service\tobii_stream_engine.dll`
    - `%ProgramFiles%\Tobii\Tobii Eye Tracker 5\tobii_stream_engine.dll`
    - `%ProgramFiles(x86)%\Tobii\Tobii Eye Tracker 5\x64\tobii_stream_engine.dll`
  - Architecture: **x64 (AMD64)**.

---

## 2. Reliable Resolution and Locating Mechanism for `tobii_stream_engine.dll`

Rather than relying on `%PATH%` or vulnerable relative paths, the plugin uses a deterministic, four-tier resolution pipeline:

1. **Registry Installation Check**: Proposed for the Windows inventory script; the current locator does not implement registry probing.
2. **Fixed High-Integrity Directory Probing**:
   - `C:\Program Files\Tobii\Tobii Service\tobii_stream_engine.dll`
   - `C:\Program Files\Tobii\Tobii Eye Tracker 5\tobii_stream_engine.dll`
   - `C:\Program Files (x86)\Tobii\Tobii Eye Tracker 5\x64\tobii_stream_engine.dll`
   - `C:\Program Files\Tobii\Tobii EyeX Config\tobii_stream_engine.dll`
3. **PE Header Architecture Validation**:
   Open file stream, parse DOS/PE headers, ensure `Machine == 0x8664` (`IMAGE_FILE_MACHINE_AMD64`). Rejects any 32-bit binary immediately.
4. **Digital Signature Check**: The current locator only inspects embedded certificate metadata and does not perform WinVerifyTrust chain/integrity validation. This is not sufficient to claim Authenticode verification.

---

## 3. Current Managed Assumptions for `tobii_stream_engine.dll`

- **Architecture assumption**: `x64` (`AMD64`)
- **Calling-convention assumption**: `__cdecl`; not yet matched to the supported 7.2 contract
- **Subsystem**: Windows C Runtime / Win32
- **CLR Host Compatibility**: Compatible with .NET Framework 4.6+ compiled with `<PlatformTarget>x64</PlatformTarget>`

---

## 4. Public API Documentation Sources and Provenance

Current and historical leads:
- **Current Tobii Streams SDK page**: `https://www.tobii.com/products/integration/tobii-streams-sdk` (advertised client version 7.2; SDK/license access required).
- **Versioned Stream Engine headers**: required evidence, but not obtained during this review.
- **Third-party and historical discrepancy references only**:
  - Historical OptiKey commit `cf841c2` and removal diff in `81c88f5`
  - Talisman / Talon accessibility eye-tracking integrations
  - Beam Eye Tracker documentation and public integration examples

---

## 5. C API Declarations and Minimum Surface Required

The active binding currently contains the following candidate declarations. They are a minimal surface, but they are not hardware-approved until matched to a legitimately obtained supported header. No head pose, user presence, or biometric recording APIs are declared.

### Functions:
1. `tobii_api_create`
   ```c
   tobii_error_t tobii_api_create(tobii_api_t** api, tobii_custom_alloc_t const* custom_alloc, tobii_custom_log_t const* custom_log);
   ```
2. `tobii_api_destroy`
   ```c
   tobii_error_t tobii_api_destroy(tobii_api_t* api);
   ```
3. `tobii_enumerate_local_device_urls`
   ```c
   tobii_error_t tobii_enumerate_local_device_urls(tobii_api_t* api, tobii_device_url_receiver_t receiver, void* user_data);
   ```
4. `tobii_device_create`
   ```c
   tobii_error_t tobii_device_create(tobii_api_t* api, char const* url, tobii_field_of_use_t field_of_use, tobii_device_t** device);
   ```
5. `tobii_device_destroy`
   ```c
   tobii_error_t tobii_device_destroy(tobii_device_t* device);
   ```
6. `tobii_device_reconnect`
   ```c
   tobii_error_t tobii_device_reconnect(tobii_device_t* device);
   ```
7. `tobii_wait_for_callbacks`
   ```c
   tobii_error_t tobii_wait_for_callbacks(size_t device_count, tobii_device_t* const* devices);
   ```
8. `tobii_device_process_callbacks`
   ```c
   tobii_error_t tobii_device_process_callbacks(tobii_device_t* device);
   ```
9. `tobii_gaze_point_subscribe`
   ```c
   tobii_error_t tobii_gaze_point_subscribe(tobii_device_t* device, tobii_gaze_point_callback_t callback, void* user_data);
   ```
10. `tobii_gaze_point_unsubscribe`
    ```c
    tobii_error_t tobii_gaze_point_unsubscribe(tobii_device_t* device);
    ```
11. `tobii_get_device_info`
    ```c
    tobii_error_t tobii_get_device_info(tobii_device_t* device, tobii_device_info_t* device_info);
    ```
12. `tobii_error_message`
    ```c
    char const* tobii_error_message(tobii_error_t error);
    ```

---

## 6. Managed Enum Values Pending Versioned Proof

### `tobii_error_t`
- `TOBII_ERROR_NO_ERROR = 0`
- `TOBII_ERROR_INTERNAL = 1`
- `TOBII_ERROR_INSUFFICIENT_LICENSE = 2`
- `TOBII_ERROR_NOT_SUPPORTED = 3`
- `TOBII_ERROR_NOT_AVAILABLE = 4`
- `TOBII_ERROR_CONNECTION_FAILED = 5`
- `TOBII_ERROR_TIMED_OUT = 6`
- `TOBII_ERROR_ALLOCATION_FAILED = 7`
- `TOBII_ERROR_INVALID_PARAMETER = 8`
- `TOBII_ERROR_CALIBRATION_ALREADY_STARTED = 9`
- `TOBII_ERROR_CALIBRATION_NOT_STARTED = 10`
- `TOBII_ERROR_ALREADY_SUBSCRIBED = 11`
- `TOBII_ERROR_NOT_SUBSCRIBED = 12`
- `TOBII_ERROR_OPERATION_FAILED = 13`
- `TOBII_ERROR_CONFLICTING_API_INSTANCES = 14`
- `TOBII_ERROR_CALIBRATION_BUSY = 15`
- `TOBII_ERROR_CALLBACK_IN_PROGRESS = 16`
- `TOBII_ERROR_TOO_MANY_SUBSCRIBERS = 17`
- `TOBII_ERROR_CONNECTION_FAILED_DRIVER = 18`
- `TOBII_ERROR_UNAUTHORIZED = 19`

### `tobii_validity_t`
- `TOBII_VALIDITY_INVALID = 0`
- `TOBII_VALIDITY_VALID = 1`

### `tobii_field_of_use_t`
- `TOBII_FIELD_OF_USE_INTERACTIVE = 1` (Equivalent to `TOBII_FIELD_OF_USE_STORE_OR_TRANSFER_FALSE`)
- `TOBII_FIELD_OF_USE_ANALYTICAL = 2` (Requires analytical license)

---

## 7. What Parts Remain Engineering Inference?

1. **Exact Tobii Experience installation layout**:
   All current hard-coded candidates are hypotheses until the inventory script records a real supported installation. The locator must not add guessed paths as facts.
2. **Tobii Service startup latency after system boot**:
   Reconnect timing and service behavior have no current hardware evidence. The managed backoff policy exists, but recovery is unverified.

---

## 8. Current Licensing Analysis: What is Permitted vs. Unconfirmed?

### Not established by this repository:
- Distributing original open-source C# code under GPL-3.0-only.
- Referencing public, documented C function signatures and structs for runtime dynamic linking.
- Whether the consumer ET5 and current Tobii policies permit this assistive use.
- Whether the interactive field of use is available for this exact runtime/device combination.
- Whether any current Tobii agreement permits public open-source AAC/accessibility use with the non-`L` Eye Tracker 5. Current official pages instead create an explicit blocker requiring Tobii clarification.

### Project constraints:
- **Zero Redistribution**: The reviewed Git tree and audited CI package do not redistribute `tobii_stream_engine.dll`, `.lib`, `.h`, or any Tobii binary.
- **No Reverse Engineering**: We do NOT decompile or crack proprietary Tobii binaries.
- **No Data Storage or Transmission**: We adhere to the interactive field-of-use contract; no gaze tracking data is recorded, aggregated, or transmitted across the network.
