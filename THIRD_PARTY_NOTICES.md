# Third-Party Notices and Acknowledgements

This project references and builds upon open-source software and community specifications:

## 1. OptiKey
- **Copyright**: Copyright (c) 2014-2026 Julius Sweetland, OPTIKEY LTD
- **License**: GNU General Public License v3.0-only (GPL-3.0-only)
- **Website**: https://github.com/OptiKey/OptiKey
- **Usage**: Plugin architecture interfaces (`JuliusSweetland.OptiKey.Contracts.dll`).

## 2. .NET Reactive Extensions (System.Reactive)
- **Copyright**: .NET Foundation and Contributors
- **License**: MIT License / Apache License 2.0
- **Website**: https://github.com/dotnet/reactive
- **Usage**: Compile-time contract type (`Timestamped<T>`); the release package does not redistribute Rx assemblies because the OptiKey host supplies the pinned Rx runtime.

## 3. Tobii Stream Engine Specification
- **Copyright**: Tobii AB
- **Website**: https://developer.tobii.com/
- **Notice**: C API declarations referenced for runtime dynamic linking under documented public APIs. No proprietary binaries or closed header code are distributed with this software.
