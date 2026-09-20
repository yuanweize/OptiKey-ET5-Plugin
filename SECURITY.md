# Security Policy and Vulnerability Disclosure

## Supported Versions
| Version | Supported | Notes |
| :--- | :--- | :--- |
| 1.0.x | Yes | Current development branch |

## Security Hardening in OptiKey-ET5-Plugin
To safeguard vulnerable assistive technology users:
1. **No Generic DLL Searching**: The plugin never loads native DLLs from `%PATH%`, the current directory, or `%TEMP%`.
2. **Strict PE Architecture Verification**: Target libraries must explicitly validate as 64-bit AMD64 PE binaries prior to invoking `LoadLibrary`.
3. **Signer Metadata Inspection**: The current implementation rejects files without Tobii signer metadata, but does not yet perform WinVerifyTrust Authenticode integrity and chain validation. This remains a security blocker.
4. **Clean Parameterless Instantiation**: The plugin constructor executes zero native calls or network operations.

## Reporting a Vulnerability
If you discover a security vulnerability or potential code-execution vector:
1. Do **NOT** disclose the vulnerability in public GitHub issues or forums.
2. Please send a confidential email detailing the issue and reproduction steps to: `yuanweize@users.noreply.github.com`.
3. We will acknowledge receipt within 48 hours and work with you on a private patch before public disclosure.
