[English](README.md) | [简体中文](README.zh-CN.md)

# OptiKey-ET5-Plugin

[![Windows CI](https://github.com/yuanweize/OptiKey-ET5-Plugin/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/yuanweize/OptiKey-ET5-Plugin/actions/workflows/build-and-test.yml)
[![Release](https://img.shields.io/github/v/release/yuanweize/OptiKey-ET5-Plugin?include_prereleases)](https://github.com/yuanweize/OptiKey-ET5-Plugin/releases)
[![License: GPL-3.0-only](https://img.shields.io/badge/License-GPL--3.0--only-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%20x64-lightgrey.svg)](docs/user/COMPATIBILITY.zh-CN.md)
[![Privacy: Zero Telemetry](https://img.shields.io/badge/Privacy-Zero%20Telemetry-green.svg)](docs/policies/PRIVACY.zh-CN.md)

适用于 **OptiKey 4.x** 的开源 **Tobii Eye Tracker 5 (ET5)** 眼动仪输入插件。

专为依赖辅助计算机访问和增强与替代性交流（AAC）的肌萎缩侧索硬化症（ALS/渐冻症）、闭锁综合征及重度运动障碍群体设计。

- **操作系统**：Windows 10 / 11（64 位版本）
- **宿主程序**：OptiKey 4.x
- **硬件设备**：Tobii Eye Tracker 5 眼动仪
- **零专有二进制捆绑**：完全使用用户本机合法安装的官方 Tobii Experience 运行时环境
- **严格的隐私不变量**：仅在易失性内存中实时分发数据，绝不记录、持久化或网络传输注视点坐标
- **高韧性架构设计**：可中断的回调泵机制，具备有界停机超时与原生卡死工作线程安全隔离保护

---

## 运行环境要求 (Requirements)

使用本插件前，请确保满足以下要求：

1. **Windows 10 或 11 (x64)**。
2. **Tobii Eye Tracker 5** 牢固安装在主显示器下方并连接 USB 端口。
3. 从 [Tobii Gaming 开始使用](https://gaming.tobii.com/getstarted/) 安装**官方 Tobii Experience**，并完成显示屏绑定与用户眼动校准。
4. 从 [OptiKey GitHub Releases](https://github.com/OptiKey/OptiKey/releases) 安装 **OptiKey 4.x**。

有关支持的分辨率、宽高比及 DPI 缩放详情，请查阅 [系统与硬件兼容性列表](docs/user/COMPATIBILITY.zh-CN.md)。

---

## 快速起步 (Quick Start)

普通用户安装使用无需编译代码或手动复制 DLL：

```text
安装官方 Tobii Experience 并完成 ET5 眼动校准
                     ↓
启动 OptiKey -> 打开管理控制台 (Alt + M)
                     ↓
切换至 "指向与选择" 选项卡
                     ↓
点击 "联机查找更多眼动仪选项"
                     ↓
选择 "Tobii Eye Tracker 5" -> 点击安装
                     ↓
设为指向设备 -> 开启眼动打字与光标控制
```

有关离线手动 ZIP 安装与多设备配置细节，请参阅完整的 **[安装与配置指南](docs/user/INSTALLATION.zh-CN.md)**。

---

## 常见问题与排错 (Troubleshooting)

- **OptiKey 中未出现本插件**：检查 `OptiKey.ET5.Plugin.dll` 是否已放置在 `%APPDATA%\OptiKey\OptiKey\Plugins\`。
- **提示未找到 Tobii 运行时**：确认官方 Tobii Experience 已安装，且系统服务 `services.msc` 中的 `Tobii Service` 处于运行状态。
- **OptiKey 呈红色断开状态**：重新拔插 ET5 的 USB 接口，并重启 OptiKey。
- **独立硬件诊断工具**：运行 `tools/HardwareDiagnostics/diagnose.ps1` 可自动检测运行时路径、架构校验及数字签名。

详尽的排错流程请参阅 **[故障排除指南](docs/user/TROUBLESHOOTING.zh-CN.md)**。

---

## 隐私与安全 (Privacy & Safety)

- **纯内存即时分发**：注视点坐标仅在内存中即时计算，用于定位 OptiKey 光标与触发驻留点击。
- **零存储**：原始坐标、眼部图像与设备标识**绝不写入磁盘或临时文件**。
- **零遥测**：代码库中绝无任何网络请求、遥测或行为分析组件。
- **故障隔离保护**：有界停机超时机制保护 OptiKey 宿主免受非托管驱动崩溃影响。

阅读完整的 **[生物识别隐私政策](docs/policies/PRIVACY.zh-CN.md)** 与 **[安全架构说明](.github/SECURITY.zh-CN.md)**。

---

## 文档导航中心 (Documentation Hub)

完整的技术、合规与设计文档已统一归类在 **[文档导航中心 (docs/README)](docs/README.zh-CN.md)**：

- **[用户指南](docs/README.zh-CN.md#1-用户指南-user-guide--docsuser)**：[安装与配置](docs/user/INSTALLATION.zh-CN.md)、[故障排除](docs/user/TROUBLESHOOTING.zh-CN.md)、[硬件兼容性](docs/user/COMPATIBILITY.zh-CN.md)
- **[项目状态](docs/README.zh-CN.md#2-项目状态与规划-project--docsproject)**：[就绪状态看板](docs/project/STATUS.zh-CN.md)、[开发路线图](docs/project/ROADMAP.zh-CN.md)
- **[政策与合规](docs/README.zh-CN.md#3-政策与合规-policies--legal--docspolicies)**：[法律与溯源声明](docs/policies/LEGAL.zh-CN.md)、[隐私不变量](docs/policies/PRIVACY.zh-CN.md)、[第三方版权声明](docs/policies/THIRD_PARTY_NOTICES.zh-CN.md)
- **[开发与维护](docs/README.zh-CN.md#4-开发与维护标准规范-development--docsdevelopment)**：[智能体标准操作规范 (AGENT SOP)](docs/development/AGENT_SOP.zh-CN.md)、[双语同步规范](docs/development/DOCUMENTATION_POLICY.zh-CN.md)、[硬件验证规程](docs/development/HARDWARE_VALIDATION.zh-CN.md)、[架构决策记录 (ADRs)](docs/README.zh-CN.md#6-架构决策记录-adrs--docsadr)
- **[核心技术调研](docs/README.zh-CN.md#5-核心技术调研-research--docsresearch)**：[ABI 溯源](docs/research/ABI_PROVENANCE.zh-CN.md)、[运行时探测](docs/research/RUNTIME_RESEARCH.zh-CN.md)、[回调泵架构](docs/research/CALLBACK_RESEARCH.zh-CN.md)

---

## 社区与贡献 (Community & Contributing)

- **问题反馈**：通过 [GitHub Issue 模板](https://github.com/yuanweize/OptiKey-ET5-Plugin/issues/new/choose) 提交 Bug 或功能建议。
- **参与贡献**：阅读 [贡献指南](.github/CONTRIBUTING.zh-CN.md) 与 [行为准则](.github/CODE_OF_CONDUCT.zh-CN.md)。
- **智能体守则**：编程助手须遵守 [`AGENTS.zh-CN.md`](AGENTS.zh-CN.md)。

---

## 许可证 (License)

本项目采用 **GNU 通用公共许可证第 3.0 版 (GPL-3.0-only)** 开源（参阅 [LICENSE](LICENSE)）。
本项目严禁捆绑或重新分发任何 Tobii 专有软件、二进制文件或 C 语言头文件。
