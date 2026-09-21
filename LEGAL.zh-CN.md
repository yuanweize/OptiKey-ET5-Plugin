[English](LEGAL.md) | [简体中文](LEGAL.zh-CN.md)

# 法律声明与授权源流 (Legal Notice & Licensing Provenance)

> **文档状态**：有效  
> **适用项目**：OptiKey-ET5-Plugin  
> **核实日期**：2026年9月

---

## 1. 已确认事实 (VERIFIED FACT)

1. **OptiKey 核心开源协议**：
   OptiKey 遵循 GNU 通用公共许可证第 3 版（GPL-3.0）。为 `OptiKey-ET5-Plugin` 编写的所有原始代码均基于 GPL-3.0-only 协议分发。
2. **OptiKey 移除 Tobii 支持的历史背景**：
   上游 OptiKey 原先包含内置的 Tobii Dynavox 支持。在 2023年7月的提交（`81c88f5a`）中，OptiKey 维护团队移除了内置 Tobii 支持，原因是代码库曾直接捆绑了 `tobii_stream_engine.dll` 及 Tobii 专有开发授权协议，而该协议禁止在第三方开源软件包中分发这些二进制文件。
3. **零专有二进制分发**：
   本项目 Git 仓库和 CI 发布包中**绝对不包含**任何 Tobii 专有二进制文件（`tobii_stream_engine.dll`、`.lib` 等）、头文件或专有 SDK 压缩包。
4. **独立编写的 P/Invoke 原生互操作声明**：
   所有的 C# 原生互操作类型声明均为独立编写，未复制任何受版权保护的 Tobii SDK 官方样例或历史专有封装代码。
5. **注视点数据合规处理**：
   技术实现上固定选用 `TOBII_FIELD_OF_USE_INTERACTIVE`（即时交互模式），设计上完全不存储、不外发任何眼动注视点数据。

---

## 2. 工程与法律推论 (ENGINEERING INFERENCE)

1. **基于本地系统安装的动态运行时绑定**：
   当用户在个人计算机上安装官方 Tobii Experience 软件时，即与 Tobii AB 确立了在工作站上执行该软件和驱动程序的终端用户协议。第三方无障碍工具在此本地环境中动态调用已安装的官方运行时 DLL，其性质与第三方读屏软件调用操作系统标准辅助功能 API 完全一致。
2. **洁净室接口隔离 (Clean Room)**：
   本插件和构建分发产物均不含任何专有二进制或头文件，实现了干净的洁净室开发隔离。
3. **OptiKey 契约边界**：
   编译时引用公开的契约程序集（`JuliusSweetland.OptiKey.Contracts.dll`）属于技术性构建依赖，插件自身源码完全保持 GPL-3.0-only 开源。

---

## 3. 商标与免责声明

- "Tobii", "Tobii Eye Tracker 5", "Tobii Dynavox", "PCEye", "Tobii Experience" 均为 Tobii AB 的注册商标。
- "OptiKey" 版权归 OPTIKEY LTD 及 Julius Sweetland 所有。
- "Windows" 为微软公司（Microsoft Corporation）的注册商标。
- 本项目中的所有名称引用仅用于技术兼容性说明与客观功能描述，绝不代表 Tobii AB 或相关公司的官方背书。
