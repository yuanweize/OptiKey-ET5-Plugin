[English](POST_RELEASE_AUDIT_2026-09.md) | [简体中文](POST_RELEASE_AUDIT_2026-09.zh-CN.md)

# v0.1.0 发布后独立安全性、并发性与架构审计报告

> **审计日期**：2026-09-21  
> **审计角色**：独立主要软件审计员、并发评审员、安全评审员、发布工程师与 OptiKey 集成评审员  
> **基线提交**：`8d473333098b7826e7ca172bd5d7c31776b2e76d`（已合并 PR #4）  
> **已发布版本目标**：`v0.1.0`（ZIP SHA256: `4defb89f7ef20efcbf20a98b712c596c0b6fb1f7b1b2bd0af1b8a917f8b56fd0`）  
> **目标加固分支**：`audit/v0.1.1-hardening`

---

## 执行摘要

本报告对 `OptiKey-ET5-Plugin` 在 v0.1.0 发布后的源代码、现存测试、CI 执行日志、发布制品、文档链接以及上游 `OptiKey/OptiKey` 源代码进行了详尽的独立审计。

所有交接线索均已对照活跃代码与线上 GitHub 资源进行了独立重现与验证。确认了多项致命（P0）与高危（P1）缺陷，包括：
1. **P0 安全缺陷**：运行时定位器在证书元数据包含 "Tobii" 时，错误接受摘要错误（Bad Digest）或无效签名的篡改二进制文件。
2. **P0 并发缺陷**：`ProcessOnlyPollingPump.Dispose()` 与 `WaitAndProcessCallbackPump.Dispose()` 在等待工作线程 Join 时持有 `stateLock`，与工作线程退出锁死，导致每次释放均触发 500ms 超时失败。
3. **P1 配置优先级缺陷**：配置文件中的键值意外覆盖了环境变量开发者覆盖值。
4. **P1 生命周期缺陷**：`ET5PointService.Point` 事件的 `add` 访问器在每次添加订阅者时均重复调用启动，且 `ET5PointService.Dispose()` 在持有 `eventLock` 的情况下调用提供者释放。
5. **P1 文档与链接校验器缺陷**：`check-doc-links.ps1` 显式忽略了 `file://` 协议，导致文档中的绝对机器路径逃逸了 CI 检查；用户安装文档指向了错误的 OptiKey 插件路径。
6. **P2 准确性与声明不一致**：文档中遗留了早已弃用的枚举后闭合（fail-closed）声明、不准确的设备型号识别声明（“检测到 ET5”），以及过度的“保证有界本地关闭”断言。

---

## 缺陷矩阵

| 缺陷 ID | 严重级别 | 类别 | 目标组件 | 状态 |
| :--- | :--- | :--- | :--- | :--- |
| **SEC-01** | **P0** | 安全 | `TobiiRuntimeLocator.cs` | 已确认 / 待修复 |
| **CONC-01** | **P0** | 并发 | `ProcessOnlyPollingPump.cs`, `WaitAndProcessCallbackPump.cs` | 已确认 / 待修复 |
| **CONF-01** | **P1** | 架构 | `PluginConfiguration.cs` | 已确认 / 待修复 |
| **LIFE-01** | **P1** | 生命周期 | `ET5PointService.cs` | 已确认 / 待修复 |
| **LIFE-02** | **P1** | 并发 | `ET5PointService.cs` | 已确认 / 待修复 |
| **DOC-01** | **P1** | CI / 文档 | `check-doc-links.ps1`, `CALLBACK_RESEARCH.zh-CN.md` | 已确认 / 待修复 |
| **DOC-02** | **P1** | 集成 | `INSTALLATION.zh-CN.md`, `README.zh-CN.md`, 用户指南 | 已确认 / 待修复 |
| **ABI-01** | **P2** | 一致性 | `ABI_PROVENANCE.zh-CN.md` | 已确认 / 待修复 |
| **ID-01** | **P2** | 准确性 | `README.zh-CN.md`, `STATUS.zh-CN.md`, 发布说明 | 已确认 / 待修复 |
| **SHUT-01** | **P2** | 准确性 | `CALLBACK_RESEARCH.zh-CN.md`, `PluginConfiguration.cs` | 已确认 / 待修复 |
| **SOP-01** | **P2** | 规范文档 | `AGENTS.md`, `AGENTS.zh-CN.md` | 已确认 / 待修复 |
| **PRIV-01** | **P2** | 隐私 | `TobiiRuntimeLocator.cs`, `TobiiStreamEngineBinding.cs` | 已确认 / 待修复 |
| **CI-01** | **P2** | CI 自动化 | `.github/workflows/build-and-test.yml` | 已确认 / 待修复 |
| **CI-02** | **P2** | CI 供应链 | GitHub Actions 运行器中的 Node.js 20 弃用警告 | 已记录 / 跟踪 |
| **GOV-01** | **P2** | 仓库治理 | `main` 分支保护规则与自动分支清理 | 已记录 / 建议 |

---

## 详细缺陷分析

### [SEC-01] P0: 运行时定位器接受含错误摘要 / 无效签名的篡改二进制

- **文件**：`src/OptiKey.ET5.Plugin/Runtime/TobiiRuntimeLocator.cs:97-119`
- **观察到的行为**：`TobiiRuntimeLocator.LocateRuntime()` 仅检查了 `if (!trustResult.SignerMatchesTobii) continue;`。它完全忽略了 `trustResult.SignatureStatus == SignatureStatus.Valid` 与 `trustResult.ChainStatus != ChainStatus.Revoked`。由于 `X509Certificate.CreateFromSignedFile` 即使在文件哈希被篡改或损坏时依然会解析证书表，因此一个产生 `TRUST_E_BAD_DIGEST`（`SignatureStatus.HashMismatch`）的被篡改 DLL 会被直接接受并加载。
- **预期行为**：二进制文件必须严格满足 Authenticode 签名完整性（`SignatureStatus.Valid`）、发布者身份（`SignerMatchesTobii == true`）且未被吊销（`ChainStatus != ChainStatus.Revoked`）。任何处于 `HashMismatch`、`Unsigned`、`Error` 或 `Revoked` 状态的二进制必须被立即拒绝。
- **重现方式**：向 `TobiiRuntimeLocator` 传入返回 `SignatureStatus.HashMismatch` 且 `SignerMatchesTobii: true` 的 `FakeTrustVerifier`。`LocateRuntime()` 返回 `IsFound == true` 并接受该二进制。
- **用户影响**：严重安全漏洞。恶意构造或被篡改的二进制文件可能注入 OptiKey 宿主进程。
- **修复方案**：在 `TobiiRuntimeLocator.LocateRuntime()` 中严格要求 `trustResult.SignatureStatus == SignatureStatus.Valid && trustResult.SignerMatchesTobii && trustResult.ChainStatus != ChainStatus.Revoked`。
- **回归测试**：在 `RuntimeTrustVerifierTests.cs` 中增加拒绝 `SignatureStatus.HashMismatch`、`SignatureStatus.Unsigned`、`SignatureStatus.Error` 和 `ChainStatus.Revoked` 的测试。
- **状态**：已确认 / 待修复。

---

### [CONC-01] P0: 回调泵 Dispose 在跨 Join 等待时持有锁导致与工作线程死锁

- **文件**：`src/OptiKey.ET5.Plugin/Runtime/Callbacks/ProcessOnlyPollingPump.cs:204-218`, `src/OptiKey.ET5.Plugin/Runtime/Callbacks/WaitAndProcessCallbackPump.cs:224-237`
- **观察到的行为**：两个回调泵实现均执行：
  ```csharp
  public void Dispose()
  {
      lock (stateLock)
      {
          RequestStop();
          Join(TimeSpan.FromMilliseconds(500));
          stopEvent.Dispose();
          state = CallbackPumpState.Disposed;
      }
  }
  ```
  而工作线程循环在线程退出时执行 `lock (stateLock) { state = CallbackPumpState.Stopped; }`。当 `Dispose()` 持有 `stateLock` 并在 `Join()` 中等待时，工作线程正阻塞在等待 `stateLock`。这导致 `Join(500)` 必然在 500ms 后超时，泵状态转为 `TimedOut` 并记录误报错误。
- **预期行为**：不变性规则：在等待 Join 工作线程时绝不持有工作线程所需的锁。`Dispose()` 必须在锁内快照线程并请求停止，释放锁，在锁外 Join 工作线程，然后重新获取锁以完成状态与非托管资源的清理。
- **重现方式**：在运行中的泵上直接调用 `pump.Start(); pump.Dispose();`。测量耗时与状态。现有缺陷代码下必然耗时 >= 500ms 且状态变为 `TimedOut`。
- **用户影响**：用户切换或关闭插件时 OptiKey 宿主出现 500ms 卡顿，并产生虚假的线程超时与卡死警告。
- **修复方案**：重构两个泵的 `Dispose()`：锁内请求停止，锁外 Join，锁内清理资源。
- **回归测试**：增加确定性测试，断言运行中泵直接 `Dispose()` 在 100ms 内完成且最终状态为 `Disposed`。
- **状态**：已确认 / 待修复。

---

### [CONF-01] P1: 配置优先级不一致且配置文件意外覆盖环境变量

- **文件**：`src/OptiKey.ET5.Plugin/Core/PluginConfiguration.cs:96-121, 234-278`
- **观察到的行为**：`PluginConfiguration.Load()` 先执行 `LoadFromEnvironment` 后执行 `LoadFromConfigFile`。在 `LoadFromConfigFile` 中，`AutomaticDeviceSelection`、`AllowUnverifiedTobiiDevice`、`CallbackStrategy` 与 `PollIntervalMs` 无条件覆盖了环境变量，而 `PreferredDeviceIndex` 与 `PreferredDeviceUrl` 则有条件保留。这违反了文档中声明的优先级（`默认值 -> 配置文件 -> 环境变量覆盖`）。
- **预期行为**：所有配置项具有完全统一的优先级：首先加载默认值，由配置文件覆盖，最终由环境变量覆盖。
- **重现方式**：设置环境变量 `ET5_AUTOMATIC_DEVICE_SELECTION=false`，同时配置文件设为 `AutomaticDeviceSelection=true`。配置文件意外胜出。
- **用户影响**：在存在本地配置文件时，开发者的环境变量覆盖失效。
- **修复方案**：将加载顺序调整为先 `LoadFromConfigFile(config)` 后 `LoadFromEnvironment(config)`，确保配置文件的所有字段正确生效并由环境变量覆盖。
- **回归测试**：针对每一项配置单独编写优先级覆盖测试。
- **状态**：已确认 / 待修复。

---

### [LIFE-01] P1: ET5PointService.Point 事件添加时对每次订阅均触发启动

- **文件**：`src/OptiKey.ET5.Plugin/ET5PointService.cs:68-77`
- **观察到的行为**：`public event EventHandler<Timestamped<Point>> Point { add { lock(eventLock) { pointEvent += value; EnsureStarted(); } } }` 在每次有监听器添加时均调用 `EnsureStarted()`，违反了 0 -> 1 订阅者时启动的契约。
- **预期行为**：仅在订阅者从 0 变为 1 时（添加前 `pointEvent == null`）才调用 `EnsureStarted()`。
- **重现方式**：连续注册两个事件监听器，`EnsureStarted()` 被调用了两次。
- **用户影响**：多订阅者场景下产生多余的启动调用和状态扰动。
- **修复方案**：判断 `bool isFirst = (pointEvent == null); pointEvent += value; if (isFirst) EnsureStarted();`。
- **回归测试**：增加测试验证多次注册仅启动一次，最后一个注销时仅停止一次。
- **状态**：已确认 / 待修复。

---

### [LIFE-02] P1: ET5PointService 在持有 eventLock 时释放提供者

- **文件**：`src/OptiKey.ET5.Plugin/ET5PointService.cs:189-216`
- **观察到的行为**：`Dispose()` 在持有 `lock (eventLock)` 的情况下调用 `gazeProvider.Dispose()`。如果其他线程上的注视点回调或错误回调正尝试获取 `eventLock`，会产生锁倒置与阻塞风险。
- **预期行为**：避免在持有用户/事件锁时执行耗时或阻塞的提供者释放。
- **修复方案**：在 `eventLock` 内部解绑事件并置空委托，退出 `eventLock` 后再调用 `gazeProvider.Dispose()`。
- **回归测试**：增加在活跃回调模拟期间释放 `ET5PointService` 的测试。
- **状态**：已确认 / 待修复。

---

### [DOC-01] P1: check-doc-links.ps1 忽略 file:// 导致机器本地链接逃逸 CI

- **观察到的行为**：`check-doc-links.ps1` 中的正则显式忽略了 `file://` 协议（`^(https?://|mailto:|#|file://)`）。导致机器本地绝对链接逃逸了 CI 检查：
  ```text
  file:///Users/username/repo/...
  ```
  此外，这些链接指向了重构前的过期路径。
- **预期行为**：`check-doc-links.ps1` 必须严格拦截代码块之外的所有 `file://` 或机器绝对路径：
  ```text
  file://, /Users/, C:\Users\, D:\a\
  ```
- **重现方式**：在当前 `main` 上运行 `check-doc-links.ps1`，尽管存在机器本地链接，校验依然成功。
- **用户影响**：外部开发者点击链接失效；公开文档中泄漏了作者本地机器目录。
- **修复方案**：将文档链接修正为相对路径；强化 `check-doc-links.ps1` 拦截 `file://` 和机器绝对路径。
- **回归测试**：增加校验器自测试，验证对机器本地路径的准确拦截。
- **状态**：已确认 / 待修复。

---

### [DOC-02] P1: 用户文档记录了错误的 OptiKey 插件安装目录

- **文件**：`docs/user/INSTALLATION.md:27, 45`, `docs/user/INSTALLATION.zh-CN.md:27, 45`, `README.md`, `README.zh-CN.md`
- **观察到的行为**：文档指引用户手动将插件安装到 `%APPDATA%\OptiKey\OptiKey\Plugins\`。
- **预期行为**：当前上游 OptiKey 源码（`JuliusSweetland.OptiKey.Core/Services/PluginEngine/EyeTrackerPluginEngine.cs:157-164`）定义的标准目录为 `%APPDATA%\OptiKey\OptiKey\EyeTrackerPlugins\`。
- **重现方式**：查阅 `OptiKey/OptiKey` 源码中 `EyeTrackerPluginEngine.GetTopLevelPluginDirectory()`。
- **用户影响**：若用户按照文档将文件复制到 `%APPDATA%\OptiKey\OptiKey\Plugins\`，OptiKey 根本不会扫描该目录，导致手动安装彻底失败。
- **修复方案**：修正所有中英文文档为 `%APPDATA%\OptiKey\OptiKey\EyeTrackerPlugins\`。
- **状态**：已确认 / 待修复。

---

### [ABI-01] P2: ABI_PROVENANCE 中过期的 fail-closed 声明与活跃代码矛盾

- **文件**：`docs/research/ABI_PROVENANCE.md:39`, `docs/research/ABI_PROVENANCE.zh-CN.md:39`
- **观察到的行为**：文档声称：“生产路径在枚举之后、设备创建之前保持失败闭合（fails closed）”。而实际上 `TobiiGazeProvider` 在检测到单个设备候选时会自动绑定并连接 `deviceUrls[0]`。
- **预期行为**：文档必须准确反映当前活跃代码的真实行为。
- **修复方案**：更新 ABI 文档，准确描述单设备自动绑定、多设备安全防护以及显式设备配置策略。
- **状态**：已确认 / 待修复。

---

### [ID-01] P2: README 与发布说明中过度声称设备型号识别

- **文件**：`README.md:12`, `README.zh-CN.md:12`, `docs/project/STATUS.md`, 发布说明
- **观察到的行为**：文档声称“检测到 Eye Tracker 5”，而代码实际上只是从 Tobii Stream Engine 枚举候选设备，并在存在唯一候选时连接。由于 `tobii_get_device_info` ABI 尚未验证，代码并未验证设备具体硬件型号。
- **预期行为**：使用精确表述：“检测到单个兼容的 Tobii 运行时候选设备”，而非断言硬件型号已被证明。
- **修复方案**：更新文档与日志至精确表述。
- **状态**：已确认 / 待修复。

---

### [SHUT-01] P2: 本地有界关闭声明过度承诺

- **文件**：`docs/research/CALLBACK_RESEARCH.md`, `src/OptiKey.ET5.Plugin/Core/PluginConfiguration.cs:14`, `src/OptiKey.ET5.Plugin/Runtime/Callbacks/ProcessOnlyPollingPump.cs:12`
- **观察到的行为**：注释与文档对 `tobii_device_process_callbacks()` 声称“保证有界本地关闭”，而该本地调用的返回行为在实际 ET5 硬件上仍属经验性、未完全验证的课题。
- **预期行为**：明确区分托管层关闭超时控制（CI 已验证）与本地 Tobii 运行时回调返回行为（依赖环境 / 未经验证）。
- **修复方案**：澄清中英文文档与代码注释中的相关表述。
- **状态**：已确认 / 待修复。

---

### [SOP-01] P2: AGENTS.md 准则 17 引用了不存在的本地绑定文件名

- **文件**：`AGENTS.md:26`, `AGENTS.zh-CN.md:26`
- **观察到的行为**：准则 17 引用了 `TobiiStreamEngineNative.cs`。实际文件为 `src/OptiKey.ET5.Plugin/Runtime/Interop/TobiiStreamEngineBinding.cs`。
- **预期行为**：准则中引用的所有文件名必须存在。
- **修复方案**：修正 `AGENTS.md` 与 `AGENTS.zh-CN.md` 中的路径。
- **状态**：已确认 / 待修复。

---

### [PRIV-01] P2: 本地用户目录路径日志未脱敏

- **文件**：`src/OptiKey.ET5.Plugin/Runtime/TobiiRuntimeLocator.cs:87, 110`, `src/OptiKey.ET5.Plugin/Runtime/Interop/TobiiStreamEngineBinding.cs:173`
- **观察到的行为**：探测的候选路径与加载的库路径直接以原始字符串记入日志。若路径位于用户目录中（如 `%LOCALAPPDATA%` 或开发者指定路径），会泄漏本地用户名。
- **预期行为**：在日志输出前脱敏用户目录前缀，替换为 `%USERPROFILE%` 或 `%LOCALAPPDATA%`。
- **修复方案**：引入路径脱敏工具类并应用于诊断日志。
- **状态**：已确认 / 待修复。

---

### [CI-01] P2: CI 工作流 Push 触发器遗漏了审计分支

- **文件**：`.github/workflows/build-and-test.yml:5`
- **观察到的行为**：工作流仅在 `main, dev, 'dev/**', 'chore/**'` 上触发 push CI，未包含 `'audit/**'`。
- **预期行为**：推送至 `audit/**` 分支时应自动触发完整的 Windows CI 矩阵。
- **修复方案**：在分支触发列表中添加 `'audit/**'`。
- **状态**：已确认 / 待修复。

---

### [CI-02] P2: GitHub Actions 运行器报告 Node.js 20 弃用警告

- **文件**：`.github/workflows/build-and-test.yml`, `.github/workflows/release.yml`
- **观察到的行为**：CI 运行日志标注了 `actions/checkout@v4`, `actions/upload-artifact@v4`, `microsoft/setup-msbuild@v2`, `NuGet/setup-nuget@v2`, `darenm/Setup-VSTest@v1.2` 的 Node.js 20 弃用警告。
- **预期行为**：运行器当前已强制使用 Node 24 运行。持续跟踪上游 Action 官方对 Node 24 的升级版本。
- **状态**：已记录 / 跟踪。

---

### [GOV-01] P2: GitHub main 分支保护与自动合并清理未开启

- **文件**：GitHub 仓库设置
- **观察到的行为**：`main` 分支 `protected: false`，且 `delete_branch_on_merge: false`。
- **预期行为**：建立适合单人维护者的分支保护策略（强制 PR、强制 Windows CI 状态检查通过、禁止强制推送与删除、开启合并后自动删除特性分支）。
- **状态**：已记录 / 建议。

---
