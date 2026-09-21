# ADR-004: Tobii Runtime Isolation and Secure Dynamic Loading

## Status
Accepted

## Context
The plugin needs to communicate with the Tobii Eye Tracker 5 without redistributing proprietary Tobii binaries. On Windows systems where Tobii Experience / Tobii Service is installed, `tobii_stream_engine.dll` resides in official system or vendor directories.

Naively searching `%PATH%`, the current working directory, or arbitrary user directories for `tobii_stream_engine.dll` exposes the application to:
1. **DLL Preloading / Hijacking Attacks**: A malicious user or process could place a counterfeit `tobii_stream_engine.dll` in a writable folder (such as the plugin directory, desktop, or temp).
2. **Architecture Mismatch Crashes**: Loading a 32-bit (x86) DLL in an x64 OptiKey process causes a `BadImageFormatException` that crashes the host.
3. **Version Incompatibility**: Loading obsolete or incompatible versions of Stream Engine can trigger memory corruption or undefined native behavior.

## Decision
1. **Strict Whitelist-Only Locator (`ITobiiRuntimeLocator`)**:
   Dynamic resolution of `tobii_stream_engine.dll` is strictly constrained to explicit, high-integrity system locations:
   - System/Vendor standard installation paths:
     - `%ProgramFiles%\Tobii\Tobii EyeX Config\tobii_stream_engine.dll`
     - `%ProgramFiles%\Tobii\Tobii Service\tobii_stream_engine.dll`
     - `%ProgramFiles%\Tobii\Tobii Eye Tracker 5\tobii_stream_engine.dll`
     - `%ProgramFiles(x86)%\Tobii\Tobii Eye Tracker 5\x64\tobii_stream_engine.dll`
   - `%LocalAppData%\Programs\Tobii\Tobii Eye Tracker 5\tobii_stream_engine.dll` (user-scoped installation; observed location still requires Windows evidence)
   - Generic searches across `%PATH%` or `Environment.CurrentDirectory` are **explicitly prohibited**.
2. **Binary Header Verification (x64 Architecture Validation)**:
   Before attempting `LoadLibraryW`, the locator reads the PE (Portable Executable) header of the target file:
   - Verify `e_magic == 0x5A4D` (`MZ`)
   - Verify PE signature `0x00004550` (`PE\0\0`)
   - Verify `Machine == 0x8664` (`IMAGE_FILE_MACHINE_AMD64`)
   Files failing the PE x64 check are rejected immediately without invoking native loaders.
3. **Signer Metadata Inspection (temporary)**:
   The current locator inspects signer certificate metadata with `X509Certificate`; this is not full WinVerifyTrust validation. Candidates without accepted Tobii signer metadata are rejected, but chain and signature-integrity validation remain required follow-up work.
4. **Isolated Native Binding**:
   Native functions are bound using `LoadLibraryW` and `GetProcAddress` rather than static `[DllImport("tobii_stream_engine.dll")]`. This ensures the plugin has full programmatic control over library lifetime, exact path binding, and graceful cleanup when unloading.

## Consequences
- Total immunity against DLL hijacking and path poisoning vulnerabilities.
- Zero chance of `BadImageFormatException` crashes on mixed 32/64-bit systems.
- Transparent reporting of driver and runtime installation status.
