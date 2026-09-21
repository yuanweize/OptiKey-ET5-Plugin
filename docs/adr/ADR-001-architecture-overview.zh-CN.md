[English](ADR-001-architecture-overview.md) | [简体中文](ADR-001-architecture-overview.zh-CN.md)

# ADR-001: 系统架构总览与组件边界 (Architecture Overview)

## 状态
已通过 (Accepted)

## 背景
OptiKey 是一款专为严重运动障碍与语言障碍人士（如肌萎缩侧索硬化症 ALS/MND 患者）设计的开源辅助交流软件。闭锁综合征用户完全依赖 OptiKey 作为与外界交流和操控计算机的唯一通道。

OptiKey 4.x 引入了外部插件架构（`DllLoader`、`InstalledPluginsSearch`），通过带有 `optikey-plugin` 主题的 GitHub Release 动态发现并装载第三方眼动仪插件。

本项目（`OptiKey-ET5-Plugin`）的目标在于打造一款健壮、完全开源、零专有文件捆绑的外部插件，使 OptiKey 4.x 能够无缝支持 **Tobii Eye Tracker 5 (ET5)**。

## 核心架构原则
1. **无障碍级极高可靠性**：未处理的进程崩溃或隐性失效将导致重度残障用户与外界失联。插件在硬件拔插、休眠唤醒与驱动异常状态下必须具备高度容错与自愈能力。
2. **零专有二进制分发**：代码库与发布资产绝对不包含 Tobii 专有 DLL（`tobii_stream_engine.dll`）或闭源头文件，纯粹通过动态链接方式调用用户本机已安装的官方 Tobii 运行时。
3. **严格契约合规**：实现 `JuliusSweetland.OptiKey.Contracts.IPointService` 接口，并针对官方上游契约程序集动态编译。
4. **清晰的模块解耦边界**：
   - `OptiKey.ET5.Plugin.dll`：生产插件程序集。包含 `ET5PointService`、状态机、屏幕坐标映射、运行时定位与 Tobii 适配器。
   - `OptiKey.ET5.Plugin.Synthetic.dll`：独立的合成测试桩程序集，绝对不打包进发布压缩包。
   - `ET5Diagnostics.exe`：独立的命令行诊断与环境检测工具。
