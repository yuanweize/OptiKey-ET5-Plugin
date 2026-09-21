[English](ADR-005-reconnect-state-machine.md) | [简体中文](ADR-005-reconnect-state-machine.zh-CN.md)

# ADR-005: 重连状态机与回调生命周期 (Reconnect State Machine and Callback Lifecycle)

## 状态 (Status)
已采纳 (Accepted)

## 上下文 (Context)
在真实的无障碍辅助环境中，眼动仪硬件经常面临物理和系统层面的中断：
- USB 线缆被触碰、松动或重新拔插；
- 笔记本/PC 发生睡眠、休眠或唤醒循环；
- Windows Tobii 后台服务重启或驱动更新；
- 用户临时移动到摄像头的追踪视野之外。

如果在发生断开连接时插件崩溃或陷入无限死循环，用户将失去对计算机的控制并且无法呼叫求助。相反，以固定间隔高频轮询又会消耗不必要的 CPU 和电量。

## 决策 (Decision)
1. **显式状态模型 (Explicit State Model)**：
   生命周期由包含以下状态的显式状态机控制：
   - `Created`：已初始化，无后台线程，不访问硬件。
   - `Starting`：正在解析运行时、初始化 API、枚举设备。
   - `Connected`：眼动仪已连接，注视点流处于活动状态，正常传递注视点。
   - `Reconnecting`：注视流丢失或设备断开；退避等待重试。
   - `Stopping`：已请求停止；正在取消后台工作线程并取消订阅流。
   - `Stopped`：工作线程已停止，资源已清理，可根据需要重新启动。
   - `Disposed`：终态；所有句柄已释放，不允许进一步操作。

   *错误信息存储为结构化的 `ErrorInfo`，而不是衍生更多细分状态。*

2. **带指数退避与抖动的单一重连循环**：
   - 断开连接触发转换为 `Reconnecting` 状态。
   - 重连间隔遵循公式：
     $$T_{\text{wait}} = \min(T_{\text{max}}, T_{\text{base}} \times 2^{\text{retryCount}}) + \text{jitter}$$
     其中 $T_{\text{base}} = 500\text{ ms}$, $T_{\text{max}} = 10000\text{ ms}$，$\text{jitter} \in [0, 250\text{ ms}]$。
   - 防止长时间断开时的 USB 总线风暴或高 CPU 占用。
   - 任何时刻仅允许一个活动的重连循环（通过 `CancellationToken` 和状态门互锁）。

3. **事件驱动的回调等待 (无热轮询)**：
   后台捕获线程使用回调泵模型（如 `tobii_wait_for_callbacks`）而非盲目 `Thread.Sleep(30)`。
   - 当样本可用时，本地线程立即被唤醒。
   - 调用 `tobii_device_process_callbacks` 触发注册的委托。
   - `wait_for_callbacks` 设置有限超时（如 200ms），以便周期性检查 `CancellationToken.IsCancellationRequested`，确保快速关闭（<200ms）。

4. **释放保证 (Disposal Guarantees)**：
   - `Dispose()` 立即向 `CancellationTokenSource` 发出取消信号。
   - 本地句柄在同步锁保护下安全释放；如遇本地阻塞则进行卡死工作线程隔离。
   - `Dispose()` 之后不触发任何回调。
   - `Dispose()` 之后不发起任何重连尝试。

## 后果 (Consequences)
- 从 USB 拔插、驱动重启和系统休眠/唤醒中高度敏捷地自动恢复。
- 在空闲或等待注视点样本时极低 CPU 占用。
- 在关闭或快速启停序列中杜绝竞争条件。
