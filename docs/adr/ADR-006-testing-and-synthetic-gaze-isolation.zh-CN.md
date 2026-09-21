[English](ADR-006-testing-and-synthetic-gaze-isolation.md) | [简体中文](ADR-006-testing-and-synthetic-gaze-isolation.zh-CN.md)

# ADR-006: 测试与合成注视点隔离 (Testing and Synthetic Gaze Isolation)

## 状态 (Status)
已采纳 (Accepted)

## 上下文 (Context)
在自动化测试、CI 运行以及缺少物理 Tobii Eye Tracker 5 硬件的开发者工作站中，合成注视点数据对于验证以下内容至关重要：
- OptiKey 加载器兼容性（`DllLoader.IsValidPlugin`, `TryInstantiatePointSource`）；
- 坐标变换精度；
- 状态机流转；
- 事件冒泡与过滤。

然而，在生产环境中，如果物理眼动仪不可用或未校准，**静默回退到合成注视点是灾难性的**。依赖眼动的运动受限用户会突然看到光标不受控制地移动或发生幽灵按键，导致困惑、沮丧以及失去对通信工具的控制。

## 决策 (Decision)
1. **生产环境绝不静默回退 (Never Fallback Silently in Production)**：
   生产插件（`OptiKey.ET5.Plugin.dll`）不包含任何合成追踪器回退逻辑。如果硬件或运行时缺失，插件显式转入 `Stopped` 或 `Reconnecting` 状态，并触发 `Error` 事件指明硬件不可用。
2. **独立程序集承载合成注视点提供者**：
   合成注视点提供者（`SyntheticGazeProvider`）完全存放在独立的项目/程序集中：
   `src/OptiKey.ET5.Plugin.Synthetic/OptiKey.ET5.Plugin.Synthetic.csproj`。
   该程序集仅由单元/集成测试和诊断工具引用，**坚决排除**在生产发布 ZIP 资产之外。
3. **CI 发布资产扫描与校验**：
   CI 流水线必须解压最终发布 ZIP 并断言：
   - 恰好包含一个实现 `IPointService` 的 DLL；
   - 不含任何测试程序集（`*.Tests.dll`, `*.Synthetic.dll`）；
   - 不含任何专有 Tobii 二进制文件（`tobii_stream_engine.dll`）；
   - 不含重复的 `JuliusSweetland.OptiKey.Contracts.dll`；
   - 不含调试 PDB 文件。
4. **精确的 OptiKey 反射加载测试**：
   测试套件包含一个自动化测试（`OptiKeyLoaderReflectionTests`），针对生成的发布构件执行与 OptiKey 的 `DllLoader.cs` 完全一致的反射逻辑：
   - 将程序集加载到单独的临时 `AppDomain` 中；
   - 验证 `t.GetInterfaces().Contains(typeof(IPointService))`；
   - 验证 `Activator.CreateInstance(typeToLoad)` 成功且无异常；
   - 验证 `((IDisposable)instance).Dispose()` 清理干净。

## 后果 (Consequences)
- 合成注视点绝不可能泄漏到终端用户生产环境中。
- 100% 验证发布包完全符合 OptiKey 加载器的预期。
- 安全、确定且无硬件依赖的 CI 测试。
