[English](ADR-003-hardware-independent-lifecycle.md) | [简体中文](ADR-003-hardware-independent-lifecycle.zh-CN.md)

# ADR-003: Hardware-Independent Lifecycle and Lazy Activation

## Status
Accepted

## Context
OptiKey's `DllLoader` discovers and tests available plugins through reflection in a temporary AppDomain:
```csharp
public static bool IsValidPlugin(string dllFilePath)
{
    var assemblyLoader = (AssemblyLoader)DllLoader.GetTempDomain().CreateInstanceAndUnwrap(...);
    bool available = assemblyLoader.LoadAssemblyAndConfirmType(dllFilePath);
    DllLoader.ResetTempDomain();
    return available;
}
```
When an eye tracker is selected, OptiKey instantiates the service directly:
```csharp
dllService = (IPointService)Activator.CreateInstance(typeToLoad);
```
Crucially, `Activator.CreateInstance()` invokes the parameterless default constructor. If the constructor attempts to:
- Connect to USB hardware
- Locate or load native libraries (`tobii_stream_engine.dll`)
- Check driver services or enumerate devices
- Throw an exception when hardware is absent

then `Activator.CreateInstance` throws a `TargetInvocationException`. In OptiKey, this causes immediate failure:
`OptiKey.Properties.Resources.EYETRACKER_DLL_INSTALLED_BUT_NOT_INSTANTIATED`
and the plugin is unusable, or worse, discovery scanning fails.

Furthermore, OptiKey attaches to the `Point` event only when the service starts capturing gaze:
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

## Decision
1. **Zero-Side-Effect Constructor**:
   The default constructor of `ET5PointService` MUST be completely hardware-independent and side-effect free. It only initializes memory structures, logging channels, and transitions the state machine to `Created`. It NEVER touches native DLLs, file system paths, or hardware devices.
2. **Lazy Runtime Initialization**:
   Hardware detection, runtime DLL resolution, device context creation, and connection initiation are deferred until:
   - A listener attaches to the `Point` event, or
   - An explicit lifecycle `Start()` method is invoked.
3. **Graceful Error Notification**:
   If hardware or runtime drivers are unavailable during lazy start, the plugin MUST NOT crash the process. Instead, it transitions to `Stopped` or `Reconnecting` and fires the `Error` event (`INotifyErrors.Error`) with clear, diagnostic descriptions for the user.
4. **Idempotent Disposal**:
   `Dispose()` can be called at any lifecycle stage (even immediately after construction before connection). It safely cancels background loops, releases native handles, unsubscribes callbacks, and transitions state to `Disposed`. Multiple calls to `Dispose()` are safe and have no side effects.

## Consequences
- `Activator.CreateInstance()` always succeeds reliably.
- OptiKey's discovery and startup routines never crash due to missing hardware.
- Locked-in users receive clean error diagnostics instead of unhandled application exceptions.
