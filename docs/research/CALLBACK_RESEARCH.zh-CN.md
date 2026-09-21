[English](CALLBACK_RESEARCH.md) | [简体中文](CALLBACK_RESEARCH.zh-CN.md)

# 原生回调处理与停机生命周期研究 (Callback Research)

> **文档状态**：核心技术规范  
> **研究范畴**：Tobii Stream Engine 回调分发机制、停机有界性与释放后使用（Use-After-Free）根治方案

---

## 1. 核心挑战：原生阻塞等待与停机有界性

在传统的 Tobii Stream Engine 集成模式中，注视点流通常按以下逻辑循环分发：

```c
while (is_running) {
    tobii_error_t error = tobii_wait_for_callbacks(device);
    if (error == TOBII_ERROR_NO_ERROR) {
        tobii_device_process_callbacks(device);
    }
}
```

### 进程内（In-Process）调用潜在故障模式：
1. `tobii_wait_for_callbacks(device)` 仅接受设备句柄，官方 API 没有公开的超时参数。
2. 当传感器未接收到注视数据时（如用户闭眼、视线离开屏幕或设备拔出），该工作线程可能无限期挂起在原生驱动调用中。
3. 当 OptiKey 请求停止服务（`Stop()`）时，托管代码即便触发了 `CancellationToken`，原生线程在底层等待状态下也无法感知托管取消信号。
4. 若托管代码实施了超时强制返回（如 `workerThread.Join(2000)`）：
   - 超时后若直接调用 `tobii_device_destroy()` 释放设备或销毁全局上下文，挂起的原生线程随后被唤醒访问已销毁内存，将立即引发致命的内存访问冲突崩溃（`STATUS_ACCESS_VIOLATION`，0xC0000005），直接导致 OptiKey 崩溃闪退。

---

## 2. 严格的架构生命周期契约

为彻底解决该问题，本插件在 [TobiiGazeProvider.cs](../../src/OptiKey.ET5.Plugin/Runtime/TobiiGazeProvider.cs) 与 [ICallbackPump.cs](../../src/OptiKey.ET5.Plugin/Core/ICallbackPump.cs) 中建立了严格的生命周期停机保护状态机：

```
+-------------------------------------------------------------+
|                     RequestStop()                           |
+-------------------------------------------------------------+
                               |
                               v
+-------------------------------------------------------------+
|                     Join(stopTimeout)                       |
+-------------------------------------------------------------+
           |                                       |
     (超时限额内成功 Join)                     (达到超时限额仍未退出)
           |                                       |
           v                                       v
+-----------------------+              +-----------------------+
| 状态 = Stopped        |              | 状态 = TimedOut       |
| 安全执行原生清理：     |              | 记录 CRITICAL 告警    |
| - UnsubscribeGaze()   |              | 标记 STUCK_WORKER     |
| - DisconnectDevice()  |              | 严格跳过所有原生句柄  |
| - runtime.Dispose()   |              | 释放与析构操作！       |
+-----------------------+              +-----------------------+
```

---

## 3. 回调驱动策略选型

本项目提供了双模式实现，并在生产中默认采用 **轮询模式 (ProcessOnlyPollingPump)**：

1. **`ProcessOnlyPollingPump` (生产默认方案)**：
   - 依赖可中断的 `ManualResetEventSlim.Wait(pollIntervalMs)` 与 `tobii_device_process_callbacks`。
   - 托管层等待通过信号可实现瞬时中断响应，超时受 CI 严格测试保护；原生底层回调执行耗时由环境与驱动决定。
2. **`WaitAndProcessCallbackPump` (传统等待方案)**：
   - 采用 `tobii_wait_for_callbacks`。
   - 严格辅以 `STUCK_WORKER` 隔离保护机制，超时绝不执行非法非托管释放。
