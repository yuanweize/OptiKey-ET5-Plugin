# Tobii Stream Engine ABI Provenance

Status: blocked and conservative, 2026-09-21.

The repository does not contain Tobii proprietary headers or binaries. The table below records the active managed declarations and the evidence currently available. Tobii's current product page identifies **Tobii Stream Engine Client Version 7.2**, but the linked API-reference URLs returned no retrievable reference during this review, and Tobii states that access to the current SDK requires a development-license subscription. No declaration below has therefore been upgraded to production confidence.

The same current product page states that Tobii Eye Tracker 5 without the `L` is a gaming device and cannot be used for development purposes. ABI research may continue, but the project must not activate a hardware path or claim permission unless Tobii provides a written supported path for this accessibility use.

| Symbol | Native signature | Managed signature | Authoritative source | API/version | Verified date | Confidence |
| --- | --- | --- | --- | --- | --- | --- |
| `tobii_api_create` | `tobii_error_t tobii_api_create(tobii_api_t**, tobii_custom_alloc_t const*, tobii_custom_log_t const*)` | `Cdecl tobii_error_t(IntPtr* / out IntPtr, IntPtr, IntPtr)` | [Current Tobii Streams SDK product page](https://www.tobii.com/products/integration/tobii-streams-sdk) identifies the API family/version but does not expose this declaration; local research record only | Advertised client 7.2; declaration not version-matched | 2026-09-21 | Low |
| `tobii_api_destroy` | `tobii_error_t tobii_api_destroy(tobii_api_t*)` | `Cdecl tobii_error_t(IntPtr)` | Same source | Version not exposed | 2026-09-21 | Low |
| `tobii_enumerate_local_device_urls` | `tobii_error_t tobii_enumerate_local_device_urls(tobii_api_t*, receiver, void*)` | `Cdecl tobii_error_t(IntPtr, receiver, IntPtr)` | Same source | Version not exposed | 2026-09-21 | Low |
| `tobii_device_create` | `tobii_error_t tobii_device_create(tobii_api_t*, char const*, tobii_field_of_use_t, tobii_device_t**)` | `Cdecl tobii_error_t(IntPtr, string, enum, out IntPtr)` | Same source | Version not exposed | 2026-09-21 | Low |
| `tobii_device_destroy` | `tobii_error_t tobii_device_destroy(tobii_device_t*)` | `Cdecl tobii_error_t(IntPtr)` | Same source | Version not exposed | 2026-09-21 | Low |
| `tobii_device_reconnect` | `tobii_error_t tobii_device_reconnect(tobii_device_t*)` | `Cdecl tobii_error_t(IntPtr)` | Same source | Version not exposed | 2026-09-21 | Low |
| `tobii_wait_for_callbacks` | `tobii_error_t tobii_wait_for_callbacks(size_t, tobii_device_t* const*)` | `Cdecl tobii_error_t(IntPtr, IntPtr[])` | Same source and local signature cross-check | Version not exposed | 2026-09-21 | Medium |
| `tobii_device_process_callbacks` | `tobii_error_t tobii_device_process_callbacks(tobii_device_t*)` | `Cdecl tobii_error_t(IntPtr)` | Same source | Version not exposed | 2026-09-21 | Low |
| `tobii_gaze_point_subscribe` | `tobii_error_t tobii_gaze_point_subscribe(tobii_device_t*, callback, void*)` | `Cdecl tobii_error_t(IntPtr, callback, IntPtr)` | Same source | Version not exposed | 2026-09-21 | Low |
| `tobii_gaze_point_unsubscribe` | `tobii_error_t tobii_gaze_point_unsubscribe(tobii_device_t*)` | `Cdecl tobii_error_t(IntPtr)` | Same source | Version not exposed | 2026-09-21 | Low |
| `tobii_error_message` | `char const* tobii_error_message(tobii_error_t)` | `Cdecl IntPtr(tobii_error_t)` | Same source | Version not exposed | 2026-09-21 | Low |
| `tobii_get_device_info` | `tobii_error_t tobii_get_device_info(tobii_device_t*, tobii_device_info_t*)` | Not bound in active production code | No independently verified current layout | Disabled | 2026-09-21 | None |
| `tobii_device_clear_callback_buffers` | `tobii_error_t tobii_device_clear_callback_buffers(tobii_device_t*)` | Not bound | No independently verified current source retrieved | Not used | 2026-09-21 | None |

## Structs and enums

- `tobii_device_info_t` remains intentionally unresolved. The previous fixed-size managed string struct and all `TryGetDeviceInfo` production interface code were removed because its field layout and sizes were not proven.
- Every active delegate is declared `CallingConvention.Cdecl` in managed code. The 7.2 calling convention is not yet independently proven.
- The three managed enums use the C# default 32-bit signed underlying type. Native enum width and every numeric value still require confirmation against the exact supported header/toolchain contract.
- `tobii_gaze_point_t` uses `LayoutKind.Sequential` with default managed packing and fields `Int64`, enum, `Single`, `Single`. Native field order, alignment, packing, and total size are not yet proven for the supported runtime.
- The device-URL and device-create string marshalling contract is not yet proven for the supported runtime.
- `tobii_gaze_point_t`, `tobii_error_t`, `tobii_validity_t`, and `tobii_field_of_use_t` must not be treated as hardware-ready declarations until those checks are complete.
- No Tobii proprietary source or historical wrapper code was copied into this repository.

## Identity status

The provider must not treat `deviceUrls[0]` as proof of ET5 identity. Since the safe device-info ABI is not yet proven, the production path currently fails closed after enumeration and before device creation. A documented identity mechanism and a supported API/license path must be established before hardware validation.

## Callback shutdown status

The local declaration of `tobii_wait_for_callbacks` has no timeout or cancellation parameter. The authoritative public material retrieved during this audit did not establish whether it is interruptible, what operation wakes it, or whether cross-thread destroy/reconnect is supported. The current worker therefore waits for the worker thread before native destruction, which prevents use-after-free but does not provide a bounded shutdown guarantee. This is a P0 blocker.

The next acceptable implementation must be based on an authoritative Tobii contract for one of:

1. a documented wake/cancel operation;
2. a documented non-blocking callback processing loop; or
3. another documented lifecycle protocol that provides bounded shutdown without destroying a device while a native call is active.

## Current-source retrieval result

- [Tobii Streams SDK](https://www.tobii.com/products/integration/tobii-streams-sdk) advertises Stream Engine Client 7.2 and gates SDK delivery behind a development license.
- Its public links labelled developer guide and API reference resolved to empty/404 landing responses during verification on 2026-09-21; they did not provide a versioned header or callable contract.
- Third-party wrappers and historical OptiKey code are discrepancy leads only. They are not authoritative ABI provenance and must not be copied into the active binding.

Exact next evidence required: a legitimately obtained versioned 7.2 header/reference, its governing license, and written Tobii confirmation that the non-`L` Eye Tracker 5 may be used for this open-source accessibility input scenario.
