[English](ADR-008-host-runtime-dependencies.md) | [简体中文](ADR-008-host-runtime-dependencies.zh-CN.md)

# ADR-008: 宿主提供的运行时依赖项 (Host-Provided Runtime Dependencies)

## 状态 (Status)
已采纳，用于 CI 及发布准备 (Accepted for CI and release preparation)

## 决策 (Decision)
发布 ZIP 仅包含 `OptiKey.ET5.Plugin.dll` 和 `LICENSE`。它不重新分发 log4net 或 System.Reactive 程序集。

插件仍针对锁定的 System.Reactive 2.2.5 引用进行编译，因为 OptiKey `IPointService` 契约使用了 `System.Reactive.Timestamped<T>`。打包加载器测试套件从代表 OptiKey 安装的宿主依赖项目录解析这些程序集，而不是从插件 ZIP 中解析。

移除 log4net 消除了插件本地程序集绑定冲突和已知存在漏洞的包依赖。日志记录统一采用 .NET 标准库的 `System.Diagnostics.TraceSource`，无需额外的插件特定运行时依赖包。

## 依据与局限性 (Evidence and Limitation)
net46 打包加载器测试套件是此决策的兼容性门禁。它必须成功加载生成的准确 ZIP 文件，以及包含固定 Contracts 和 Rx 程序集的宿主依赖项目录。缺少宿主依赖项会导致显式故意失败。
