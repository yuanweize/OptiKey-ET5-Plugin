[English](CODE_AUDIT_2026-09.md) | [简体中文](CODE_AUDIT_2026-09.zh-CN.md)

# 全面代码审计与加固报告 (2026年9月)

## 执行摘要

作为 `v0.1.0` 最终定型与首发版本的一部分，我们对 `OptiKey-ET5-Plugin` 仓库中的所有生产源码和测试套件进行了全面、深入的代码质量与安全审计。本次审计的重点在于：根除可能导致进程崩溃的竞态条件、严格恪守 OptiKey 插件加载契约、防止原生句柄释放后使用（Use-After-Free）、确保绝对的注视隐私不变量，并实现普通用户“开箱即连”的平滑体验。

所有被标识为 **P0**（严重阻碍发布）与 **P1**（高危功能缺陷）的缺陷均已彻底修复，并通过了 Windows x64 CI 自动化矩阵及端到端模拟测试套件的验证。

---

## 缺陷分类与修复矩阵

| 缺陷编号 | 严重级别 | 分类领域 | 涉及文件 | 问题描述与影响 | 修复状态 |
|:---|:---|:---|:---|:---|:---|
| **AUD-01** | **P0** | 生命周期 / 线程模型 | `TobiiGazeProvider.cs` | 停止或释放时引发竞态，在 `WorkerLoop` 中解引用被注销的可变 `cts` 导致后台 `NullReferenceException` 崩溃。 | **已修复**：为每次循环代数将 `CancellationToken` 捕获到局部栈变量中，实现生命周期与实例字段的彻底解耦。 |
| **AUD-02** | **P0** | 并发模型 / 死锁防御 | `TobiiGazeProvider.cs` | `Stop()` 中发生锁顺序倒置死锁：主线程持有 `lifecycleLock` 调用 `workerThread.Join()`，而工作线程的 `finally` 块正在请求同一把锁。 | **已修复**：重构 `Stop()` 与 `Dispose()`，在进入线程或泵的 `Join()` 超时等待前释放锁，彻底消除了死锁隐患。 |
| **AUD-03** | **P0** | 易用性 / 默认路径 | `TobiiGazeProvider.cs`, `PluginConfiguration.cs` | 开箱默认配置因将严格开发者模式与普通用户连接逻辑混淆，导致默认情况下拒绝所有硬件连接。 | **已修复**：引入科学设备候选策略：当系统仅检测到 1 台可用设备且用户在 OptiKey 中选择了本插件时，自动建立连接；多设备时依然严格拒绝静默连接候选 0。 |
| **AUD-04** | **P1** | 设备选择逻辑 | `TobiiGazeProvider.cs` | 当用户配置了明确的 `PreferredDeviceUrl` 但在当前枚举中未找到时，代码错误地回落（fall-through）至单设备自动选择逻辑。 | **已修复**：强制要求当显式配置的 URL 未找到时立即返回错误并通知失败，禁止隐式回落。 |
| **AUD-05** | **P1** | 内存安全 / 原生析构 | `TobiiGazeProvider.cs` | 原生回调泵在停机超时时未能正确标记 `STUCK_WORKER`，导致后续调用 `runtime.Dispose()`，可能引发原生内存访问冲突（AccessViolation）。 | **已修复**：将回调泵超时纳入干净停机检验标准。超时的回调泵同样被判定为 `STUCK_WORKER`，自动跳过原生句柄析构以保护宿主进程。 |
| **AUD-06** | **P2** | 编译告警 / 诊断工具 | `ET5Diagnostics/Program.cs` | 诊断工具循环中存在不可达代码（unreachable code）编译器警告。 | **已修复**：移除了冗余逻辑，优化了控制流；生产项目实现零编译器警告。 |
| **AUD-07** | **P2** | 测试代码规范 | `SyntheticEndToEndTests.cs` | 测试用模拟桩中未使用的接口事件产生编译器告警。 | **已修复**：使用 `#pragma warning disable CS0067` 进行了显式针对性抑制。 |
| **AUD-08** | **P2** | 隐私与脱敏 | `PluginConfiguration.cs`, `TobiiGazeProvider.cs` | 诊断日志中可能意外记录原始设备 URL。 | **已修复**：全面检查并落实了 URL 隐私脱敏机制（日志输出固定为 `(redacted for privacy)`）。 |

---

## 重点领域审计细则

### 1. 生命周期、空安全与内存安全
- **OptiKey 契约要求**：`ET5PointService` 保持了无参公共构造函数契约。构造函数执行期间绝不加载原生 DLL、不初始化线程池、不分配非托管句柄，所有操作延迟到 `Start()`。
- **非托管委托固定**：`tobii_gaze_point_callback_t` 委托牢固绑定至类实例成员（`nativeGazeCallback`），防止在被非托管 C 运行时持有期间被 GC 意外回收。
- **Dispose 幂等性**：所有实现 `IDisposable` 的核心类型均实现了标准 Dispose 模式，多次重复调用完全幂等安全。

### 2. 并发模型与线程安全
- **线程同步边界**：状态机迁移受 `GazeServiceStateMachine.syncLock` 保护，提供线程安全的状态流转；提供者生命周期由 `lifecycleLock` 同步。
- **锁外 Join 准则**：所有涉及阻塞等待的 `Join()` 操作必须严格在关键区（critical section）外部执行，杜绝死锁与线程饥饿。
- **工作代数追踪**：工作线程在每次唤醒时均核对其所属的 `generation` 与当前的 `lifecycleGeneration`，确保旧代线程在发起新的 `Start()` 时立即退出。

### 3. 原生 ABI 与运行时发现安全
- **动态 P/Invoke**：原生函数指针均通过 Win32 `LoadLibrary` / `GetProcAddress` 动态解析与装载，不存在任何静态硬编码依赖。
- **数字签名验证**：从本地服务或系统目录发现的 `tobii_stream_engine.dll` 候选文件，在动态加载前必须通过 `WinVerifyTrust` 及 Tobii 签名者身份校验。
- **绝不捆绑专有文件**：全面排查确认 Git 源码库及打包发布的 ZIP 中绝对没有附带任何 Tobii 专有 DLL 或二进制文件。

### 4. 注视隐私不变量
- 注视点坐标（`position_x`, `position_y`, `timestamp_us`）仅在易失性内存中处理，并直接派发给 OptiKey 的 `IPointService` 接口。
- 坐标、用户身份、眼部图像与硬件 URL 严禁落盘、严禁输出至日志文件或临时文件。
- 不引入任何外部网络连接、遥测套件或分析上报代码。
