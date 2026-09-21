# Tobii Stream Engine ABI Provenance

Status: partial and conservative, 2026-09-21.

The repository does not contain Tobii proprietary headers or binaries. The table below records the active managed declarations and the evidence currently available. The official documentation page is an authoritative product entry point, but it does not expose a versioned header in the material independently retrieved for this audit. Therefore most confidence levels remain low until the exact runtime ABI is supplied through a legitimate current Tobii developer channel.

| Symbol | Native signature | Managed signature | Authoritative source | API/version | Verified date | Confidence |
| --- | --- | --- | --- | --- | --- | --- |
| `tobii_api_create` | `tobii_error_t tobii_api_create(tobii_api_t**, tobii_custom_alloc_t const*, tobii_custom_log_t const*)` | `Cdecl tobii_error_t(IntPtr* / out IntPtr, IntPtr, IntPtr)` | [Tobii Stream Engine product integration](https://developer.tobii.com/consumer-eye-trackers/stream-engine/) and local research record | Version not exposed | 2026-09-21 | Low |
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
- `tobii_gaze_point_t`, `tobii_error_t`, `tobii_validity_t`, and `tobii_field_of_use_t` remain declarations requiring versioned-header confirmation before hardware use.
- No Tobii proprietary source or historical wrapper code was copied into this repository.

## Identity status

The provider must not treat `deviceUrls[0]` as proof of ET5 identity. Since the safe device-info ABI is not yet proven, the production path currently fails closed after device creation. A documented identity mechanism must be established before alpha hardware validation.

## Callback shutdown status

The local declaration of `tobii_wait_for_callbacks` has no timeout or cancellation parameter. The authoritative public material retrieved during this audit did not establish whether it is interruptible, what operation wakes it, or whether cross-thread destroy/reconnect is supported. The current worker therefore waits for the worker thread before native destruction, which prevents use-after-free but does not provide a bounded shutdown guarantee. This is a P0 blocker.

The next acceptable implementation must be based on an authoritative Tobii contract for one of:

1. a documented wake/cancel operation;
2. a documented non-blocking callback processing loop; or
3. another documented lifecycle protocol that provides bounded shutdown without destroying a device while a native call is active.
