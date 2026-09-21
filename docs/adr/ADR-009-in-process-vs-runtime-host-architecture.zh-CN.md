[English](ADR-009-in-process-vs-runtime-host-architecture.md) | [简体中文](ADR-009-in-process-vs-runtime-host-architecture.zh-CN.md)

# ADR-009: 架构评估：进程内与 RuntimeHost 进程隔离 (Architectural Evaluation: In-Process vs. RuntimeHost Isolation)

**状态 (Status)**：评估基准与预备架构 (Proposed / Evaluation Baseline)  
**上下文 (Context)**：对于依赖不间断眼动追踪的 AAC 用户（渐冻症/运动神经元疾病患者），系统稳定性直接关乎人身安全与求助通道。

---

## 1. 背景与问题阐述 (Context and Problem Statement)

通过本地 `tobii_stream_engine.dll` 集成 Tobii Eye Tracker 5 时，插件面临固有的本地代码风险：
1. **无界本地阻塞**：`tobii_wait_for_callbacks` 在某些断开场景下可能无超时参数，且无法跨 P/Invoke 边界被托管取消令牌直接打断。
2. **访问违规与故障隔离**：Tobii C 库内部或与服务通信层的任何未处理 SEH 异常、内存损坏或驱动故障都会直接导致宿主进程（`OptiKey.exe`）异常退出。
3. **关闭时的释放后使用 (Use-After-Free)**：如果托管代码在超时后终止 join，随后调用本地清理（`tobii_device_destroy`, `FreeLibrary`），本地代码的延迟唤醒可能会触发致命的内存访问违规。

我们必须严格评估本地绑定应在 OptiKey 的 CLR AppDomain **进程内 (In-Process)** 执行，还是在专用的辅助进程（`OptiKey.ET5.RuntimeHost.exe`）中**跨进程 (Out-of-Process)** 运行。

---

## 2. 架构对比矩阵 (Architectural Comparison Matrix)

| 评估维度 | 进程内架构 (In-Process) | 进程隔离架构 (RuntimeHost) | 说明 |
| :--- | :--- | :--- | :--- |
| **崩溃隔离 (Crash Containment)** | ❌ **零隔离**：`tobii_stream_engine.dll` 中的任何本地违规直接终止 `OptiKey.exe`。 | ✅ **完全隔离**：本地崩溃仅终止 `RuntimeHost.exe`。OptiKey 保持存活，转入 `Reconnecting` 并可自动重启辅助进程。 | 对渐冻症患者而言，OptiKey 彻底崩溃会导致其失去求助和呼叫护士的能力。 |
| **关闭确定性 (Shutdown Boundedness)** | ⚠️ **有条件保证**：超时时必须跳过本地释放，以避免引发释放后使用违规。 | ✅ **强保证 (<50ms)**：OptiKey 关闭命名管道；若子进程超时未退出，调用 `Process.Kill()` 由操作系统安全回收。 | 进程内无法安全终止卡死的本地工作线程。 |
| **故障自愈恢复 (Fault Recovery)** | ❌ **必须重启整个 OptiKey**：已损坏的本地状态在不关闭 OptiKey 的情况下无法从宿主空间彻底清除。 | ✅ **自动进程回收**：OptiKey 检测到管道断开后，终结僵死进程并启动新实例，无缝恢复追踪。 | 极大提升无人值守运行时间。 |
| **注视点延迟 (Gaze Latency)** | ✅ **直接调用 (~0 μs)**：直接 P/Invoke 委托分发。 | ⚖️ **微秒级 (~50–150 μs)**：本地 Windows 命名管道 IPC 开销远低于眼动仪采样周期（15–30 ms）和停留时间（300–600 ms）。 | 用户感知完全一致。 |
| **架构复杂度 (Complexity)** | ✅ **较低**：单程序集直接绑定。 | ⚖️ **中等**：需要独立辅助控制台二进制和命名管道 IPC 二进制帧编解码。 | 通过测试套件与关注点分离进行管控。 |
| **部署体积 (Footprint)** | ✅ **单 DLL**：`OptiKey.ET5.Plugin.dll` | ⚖️ **DLL + 辅助 EXE**：增加约 30 KB。 | 无第三方外部依赖；由同一解决方案编译。 |
| **内存与句柄隔离** | ❌ **共享空间**：与 OptiKey UI 共享内存、线程池和 GC。 | ✅ **完全独立**：本地驱动缓冲区完全限定在子进程内。 | 零原生内存泄漏耗尽宿主地址空间的风险。 |

---

## 3. 可扩展 IPC 协议设计 (Extensible IPC Protocol Design)

当部署 `RuntimeHost` 时，OptiKey 与 `RuntimeHost.exe` 之间的 IPC 协议使用本地 Windows 命名管道（`\\.\pipe\OptiKey-ET5-{SessionId}`），并采用结构化二进制帧以确保前后兼容性。

### 安全保证：
- **仅限本地命名管道**：无 TCP，无网络套接字，无开放监听端口。
- **严格访问控制**：`PipeOptions.Asynchronous`，严格限制为当前 Windows 用户的安全令牌。
- **隐私保证**：绝不向磁盘持久化注视点数据，亦不进行网络传输。

### 二进制帧格式 (16 字节头部)：
- **Magic** (`uint32`): `0x4F455435` ("OET5")
- **Protocol Version** (`uint16`): `0x0001`
- **Message Type** (`uint16`):
  - `0x0001`: Ping
  - `0x0002`: Pong
  - `0x0010`: StartGaze
  - `0x0011`: StopGaze
  - `0x0021`: Event_GazePoint (24 字节载荷)
- **Payload Length** (`uint32`)
- **Reserved / Flags** (`uint32`)

---

## 4. 实施路径 (Implementation Path)

1. **当前发布基线 (v0.1.0)**：
   - 生产环境中集成 `ICallbackPump`（包含 `WaitAndProcessCallbackPump`），并实施明确的有界超时与卡死隔离（`STUCK_WORKER`），确保如果本地线程超时则放弃释放本地句柄以防崩溃。
2. **后续演进 (v0.2.0+)**：
   - 在复杂多设备或特定驱动环境下，可平滑激活 `RuntimeHost` 架构作为外置高隔离引擎。
