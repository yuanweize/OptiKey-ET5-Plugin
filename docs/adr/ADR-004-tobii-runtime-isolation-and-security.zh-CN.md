[English](ADR-004-tobii-runtime-isolation-and-security.md) | [简体中文](ADR-004-tobii-runtime-isolation-and-security.zh-CN.md)

# ADR-004: Tobii 运行时隔离与安全动态加载 (Tobii Runtime Isolation and Secure Dynamic Loading)

## 状态 (Status)
已采纳为强化基线；已补充 WinVerifyTrust 与安全加载标志 (Accepted as a hardening baseline; enhanced with WinVerifyTrust and secure load flags)

## 上下文 (Context)
插件需要在不重新分发 Tobii 专有二进制文件的情况下与 Tobii Eye Tracker 5 通信。在安装了 Tobii Experience / Tobii 服务的 Windows 系统上，`tobii_stream_engine.dll` 位于官方系统或供应商目录中。

简单地在 `%PATH%`、当前工作目录或任意用户目录中搜索 `tobii_stream_engine.dll` 会使应用程序面临以下风险：
1. **DLL 预加载/劫持攻击 (DLL Preloading / Hijacking Attacks)**：恶意用户或进程可能会在可写文件夹（如插件目录、桌面或临时目录）中放置伪造的 `tobii_stream_engine.dll`。
2. **架构不匹配崩溃 (Architecture Mismatch Crashes)**：在 x64 OptiKey 进程中加载 32 位 (x86) DLL 会引发 `BadImageFormatException` 并导致宿主进程崩溃。
3. **版本不兼容 (Version Incompatibility)**：加载过时或不兼容的 Stream Engine 版本可能会触发内存损坏或未定义的本地行为。

## 决策 (Decision)
1. **严格白名单定位器 (`ITobiiRuntimeLocator`)**：
   `tobii_stream_engine.dll` 的动态解析严格限制在显式的高可信系统位置：
   - 系统/供应商标准安装路径：
     - `%ProgramFiles%\Tobii\Tobii EyeX Config\tobii_stream_engine.dll`
     - `%ProgramFiles%\Tobii\Tobii Service\tobii_stream_engine.dll`
     - `%ProgramFiles%\Tobii\Tobii Eye Tracker 5\tobii_stream_engine.dll`
     - `%ProgramFiles(x86)%\Tobii\Tobii Eye Tracker 5\x64\tobii_stream_engine.dll`
   - `%LocalAppData%\Programs\Tobii\Tobii Eye Tracker 5\tobii_stream_engine.dll`（用户作用域安装）
   - **严禁**对 `%PATH%` 或 `Environment.CurrentDirectory` 进行全盘搜索。
2. **二进制头校验 (x64 架构验证)**：
   在尝试 `LoadLibraryExW` 之前，定位器读取目标文件的 PE（可移植可执行）头：
   - 验证 `e_magic == 0x5A4D` (`MZ`)
   - 验证 PE 签名 `0x00004550` (`PE\0\0`)
   - 验证 `Machine == 0x8664` (`IMAGE_FILE_MACHINE_AMD64`)
   未通过 PE x64 检查的文件将被立即拒绝，而不调用底层加载器。
3. **签名验证与 Authenticode 信任检查**：
   在加载前，使用 Windows `WinVerifyTrust` API 验证 Authenticode 签名完整性与证书链。无有效 Tobii 签名者的文件将被拒绝加载（测试桩除外）。
4. **绝对路径动态绑定**：
   使用 `LoadLibraryExW`（带 `LOAD_LIBRARY_SEARCH_DLL_LOAD_DIR | LOAD_LIBRARY_SEARCH_SYSTEM32` 安全标志）与 `GetProcAddress` 绑定本地函数，而不是静态 `[DllImport("tobii_stream_engine.dll")]`。顶层 DLL 路径必须为绝对路径。

## 后果 (Consequences)
- 从设计上杜绝了相对路径与泛 `%PATH%` 查找。
- PE 检查在加载前即刻剔除明显的非 AMD64 候选文件。
- 签名验证确保不执行被篡改或伪造的第三方二进制文件。
- 确保 OptiKey 宿主在动态加载 Tobii 运行时时的安全性和稳定性。
