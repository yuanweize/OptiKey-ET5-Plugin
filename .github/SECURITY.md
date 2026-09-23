[English](SECURITY.md) | [简体中文](SECURITY.zh-CN.md)

# Security Policy & Vulnerability Disclosure

## Supported Versions

| Version | Supported | Notes |
| :--- | :--- | :--- |
| 0.1.x | Yes | Current active release line |

## Security Hardening in OptiKey-ET5-Plugin

To protect vulnerable assistive technology users and maintain high host application stability:

1. **No Unsafe DLL Search Paths**: The plugin strictly refuses to load native DLLs from `%PATH%`, the working directory, or temporary directories. Only verified absolute paths from official Windows Services and installation roots are considered.
2. **Strict PE64 Architecture Verification**: Candidate libraries must validate as 64-bit AMD64 PE binaries prior to invoking `LoadLibraryExW`.
3. **Authenticode Code-Signing Trust Verification**: Discovered `tobii_stream_engine.dll` binaries are verified using the Win32 `WinVerifyTrust` API (`WINTRUST_ACTION_GENERIC_VERIFY_V2`), verifying file integrity and confirming that the certificate belongs to Tobii AB.
4. **Safe Parameterless Instantiation**: The plugin constructor executes zero native calls or dynamic allocations, ensuring safe reflection loading by OptiKey.
5. **Bounded Shutdown & Memory Isolation**: Native worker threads run with bounded join timeouts. If native threads are stuck, handle cleanup is safely bypassed to avoid use-after-free host crashes.

## Reporting a Vulnerability

If you discover a security vulnerability or code-execution vector:
1. Do **NOT** disclose the issue in public GitHub issues or discussions.
2. Send a confidential report detailing reproduction steps to: `yuanweize@users.noreply.github.com`.
3. I aim to acknowledge valid security reports as soon as practical, investigate the root cause, and coordinate a patch.
