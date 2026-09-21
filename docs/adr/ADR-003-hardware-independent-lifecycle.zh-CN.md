[English](ADR-003-hardware-independent-lifecycle.md) | [简体中文](ADR-003-hardware-independent-lifecycle.zh-CN.md)

# ADR-003: 独立于硬件的生命周期与延迟激活 (Hardware-Independent Lifecycle and Lazy Activation)

## 状态 (Status)
已采纳 (Accepted)

## 上下文 (Context)
OptiKey 的 `DllLoader` 通过临时 AppDomain 中的反射发现并测试可用插件：
```csharp
public static bool IsValidPlugin(string dllFilePath)
{
    var assemblyLoader = (AssemblyLoader)DllLoader.GetTempDomain().CreateInstanceAndUnwrap(...);
    bool available = assemblyLoader.LoadAssemblyAndConfirmType(dllFilePath);
    DllLoader.ResetTempDomain();
    return available;
}
```
当用户选择眼动仪时，OptiKey 直接实例化服务：
```csharp
dllService = (IPointService)Activator.CreateInstance(typeToLoad);
```
关键在于，`Activator.CreateInstance()` 调用无参默认构造函数。如果构造函数尝试：
- 连接 USB 硬件；
- 定位或加载本地库（`tobii_stream_engine.dll`）；
- 检查驱动服务或枚举设备；
- 在缺少硬件时抛出异常；

那么 `Activator.CreateInstance` 将抛出 `TargetInvocationException`。在 OptiKey 中，这将直接导致失败：
`OptiKey.Properties.Resources.EYETRACKER_DLL_INSTALLED_BUT_NOT_INSTANTIATED`
导致插件无法使用，甚至导致发现扫描崩溃。

此外，OptiKey 仅在服务开始捕获注视点时附加到 `Point` 事件：
```csharp
public event EventHandler<Timestamped<Point>> Point
{
    add
    {
        pointEvent += value;
        StartStream();
    }
    remove
    {
        pointEvent -= value;
        if (pointEvent == null)
        {
            StopStream();
        }
    }
}
```

## 决策 (Decision)
1. **零副作用构造函数 (Zero-Side-Effect Constructor)**：
   `ET5PointService` 的默认构造函数必须完全独立于硬件且无副作用。它仅初始化内存数据结构、日志通道并将状态机转换为 `Created`。绝不触碰本地 DLL、文件系统路径或硬件设备。
2. **延迟运行时初始化 (Lazy Runtime Initialization)**：
   硬件检测、运行时 DLL 解析、设备上下文创建和连接发起推迟到：
   - 监听器附加到 `Point` 事件，或
   - 显式调用生命周期 `Start()` 方法。
3. **优雅的错误通知 (Graceful Error Notification)**：
   如果在延迟启动期间硬件或运行时驱动不可用，插件绝不能使进程崩溃。相反，它应转换为 `Stopped` 或 `Reconnecting`，并通过 `Error` 事件（`INotifyErrors.Error`）向用户发出清晰的诊断描述。
4. **幂等资源释放 (Idempotent Disposal)**：
   可以在任何生命周期阶段（甚至在构造后尚未连接时）调用 `Dispose()`。它能安全取消后台循环、释放本地句柄、取消回调订阅并转入 `Disposed` 状态。多次调用 `Dispose()` 完全安全且无副作用。

## 后果 (Consequences)
- `Activator.CreateInstance()` 始终可靠成功。
- OptiKey 的插件发现和启动程序绝不会因缺少硬件而崩溃。
- 运动受限用户将收到清晰的诊断错误提示，而不是未处理的程序崩溃异常。
