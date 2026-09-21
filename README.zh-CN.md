[English](README.md) | [简体中文](README.zh-CN.md)

# OptiKey-ET5-Plugin

[![Windows CI](https://github.com/yuanweize/OptiKey-ET5-Plugin/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/yuanweize/OptiKey-ET5-Plugin/actions/workflows/build-and-test.yml)
[![Release](https://img.shields.io/github/v/release/yuanweize/OptiKey-ET5-Plugin?include_prereleases)](https://github.com/yuanweize/OptiKey-ET5-Plugin/releases)
[![License: GPL-3.0-only](https://img.shields.io/badge/License-GPL--3.0--only-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%20x64-lightgrey.svg)](docs/COMPATIBILITY.zh-CN.md)
[![Privacy: Zero Telemetry](https://img.shields.io/badge/Privacy-Zero%20Telemetry-green.svg)](PRIVACY.zh-CN.md)

专为 **OptiKey 4.x** 开发的开源 **Tobii Eye Tracker 5 (ET5)** 眼动输入插件。

本项目专为渐冻症（ALS/MND）、闭锁综合征及重度运动障碍人士设计，助力依赖眼动追踪进行日常电脑无障碍操作与辅助交流（AAC）的特殊群体。

- **操作系统**：Windows 10 / 11 (64位)
- **宿主程序**：OptiKey 4.x
- **支持硬件**：Tobii Eye Tracker 5
- **零专有文件捆绑**：动态调用用户本机安装的官方 Tobii Experience 运行时
- **严格隐私保护**：零遥测、零注视点数据存储或外发
- **稳健可靠架构**：可中断的回调泵生命周期与有界停机安全隔离保护

---

## 运行环境要求

在使用本插件前，请确保您的系统满足以下条件：

1. **Windows 10 或 Windows 11 (x64)** 系统。
2. **Tobii Eye Tracker 5** 已正确安装并固定在主显示器底部。
3. 从 [Tobii 官方网站](https://gaming.tobii.com/getstarted/) 下载并安装 **Tobii Experience** 软件，完成屏幕配置与用户眼动校准。
4. 从 [OptiKey 发布页](https://github.com/OptiKey/OptiKey/releases) 安装 **OptiKey 4.x**。

---

## 快速上手与安装流程

普通最终用户无需手动编译源码或复制底层 DLL 文件：

```
安装官方 Tobii Experience 并完成眼动校准
                   ↓
启动 OptiKey -> 打开“管理控制台”
                   ↓
切换到“指向与选择 (Pointing & Selecting)”标签页
                   ↓
点击“在线查找更多眼动仪选项”
                   ↓
找到“Tobii Eye Tracker 5”并点击安装
                   ↓
设为当前输入源 -> 开启眼动无障碍打字与操作
```

### 手动安装方式（备选）

若通过 Release 压缩包手动安装：
1. 从 [GitHub Releases](https://github.com/yuanweize/OptiKey-ET5-Plugin/releases) 下载 `OptiKey-ET5-Plugin-v0.1.0.zip`。
2. 如 OptiKey 正在运行，请先完全退出。
3. 将解压出的 `OptiKey.ET5.Plugin.dll` 复制到 OptiKey 插件目录：
   ```text
   %APPDATA%\OptiKey\OptiKey\Plugins\
   ```
4. 重新启动 OptiKey。

---

## 在 OptiKey 中首次启用设置

1. 按快捷键 `Alt + M` 或点击界面菜单按钮打开 **管理控制台 (Management Console)**。
2. 进入 **指向与选择 (Pointing & Selecting)** 选项卡。
3. 在 **指向设备 (Pointing device)** 下拉菜单中选择：
   ```text
   Tobii Eye Tracker 5 (ET5)
   ```
4. 点击 **确定 (OK)** 保存配置。OptiKey 将自动连接眼动仪并立即接收注视点输入。

---

## 多设备与高级配置

对于单眼动仪用户（占绝大多数情况），插件会自动检测并直连唯一可用的 Tobii 设备，实现**零配置即插即用**。

若您的计算机同时连接了多台 Tobii 硬件，插件为了防止误控，默认会拒绝静默自动连接。此时您可以在配置文件中指定目标设备索引：

```text
%APPDATA%\OptiKey-ET5-Plugin\et5-plugin.config
```

配置文件示例：
```ini
# 仅检测到1台设备时自动连接（默认：true）
AutomaticDeviceSelection=true

# 多设备连接时显式指定使用第几台设备（0 或 1）：
PreferredDeviceIndex=0

# 回调泵策略：Polling（默认轮询模式，停机有保障）或 WaitAndProcess
CallbackStrategy=Polling
```

---

## 常见问题排查 (FAQ)

- **OptiKey 下拉菜单中找不到本插件**：
  请确认 `OptiKey.ET5.Plugin.dll` 文件已放置在 `%APPDATA%\OptiKey\OptiKey\Plugins\` 目录下。
- **提示“Tobii Runtime Not Found”（未找到运行时）**：
  请检查 Tobii 官方驱动与后台服务是否正在运行。可在 Windows 服务（`services.msc`）中确认 `Tobii Service` 处于“正在运行”状态。
- **OptiKey 显示红色的断开（Disconnected）状态**：
  重新拔插 ET5 的 USB 连接线，并重启 OptiKey。
- **诊断工具**：
  如遇到疑难问题，可运行压缩包内的 `ET5Diagnostics.exe` 工具，查看底层运行时发现、签名校验与设备枚举日志。

更多详细排查指引请参阅 [故障排查手册](docs/TROUBLESHOOTING.zh-CN.md)。

---

## 隐私与安全承诺

- **仅在内存中即时处理**：注视点坐标仅用于实时驱动 OptiKey 屏幕光标并触发停留选择。
- **绝对不保存数据**：注视轨迹坐标、眼部特征图像与设备 URL **绝不落盘、绝不保存到任何本地文件**。
- **零网络外发与零遥测**：代码库中完全不包含任何遥测、分析收集或网络上传模块。
- **进程防崩溃保护**：原生线程采用严格超时的有界等待策略，即使底层驱动挂起也不会引发宿主程序访问冲突崩溃。

详情参阅 [隐私策略声明](PRIVACY.zh-CN.md) 与 [安全架构文档](SECURITY.zh-CN.md)。

---

## 技术架构与开发者文档

供研究人员、无障碍工程师和代码维护者参考：

- [系统架构总览 (ADR-001)](docs/adr/ADR-001-architecture-overview.zh-CN.md)
- [全面代码审计报告 (2026年9月)](docs/CODE_AUDIT_2026-09.zh-CN.md)
- [Tobii 运行时隔离与安全性 (ADR-004)](docs/adr/ADR-004-tobii-runtime-isolation-and-security.zh-CN.md)
- [进程内调用与独立宿主进程评估 (ADR-009)](docs/adr/ADR-009-in-process-vs-runtime-host-architecture.zh-CN.md)
- [硬件与运行时兼容性矩阵](docs/COMPATIBILITY.zh-CN.md)
- [硬件实测验证流程指南](docs/HARDWARE_VALIDATION.zh-CN.md)
- [智能体标准操作规范 (Agent SOP)](docs/AGENT_SOP.zh-CN.md)
- [文档双语同步维护策略](docs/DOCUMENTATION_POLICY.zh-CN.md)
- [开源协议与版权声明](LEGAL.zh-CN.md)

---

## 开源协议

本项目采用 **GNU General Public License v3.0 only** ([GPL-3.0-only](LICENSE)) 开源许可证。
发布包中绝不捆绑任何 Tobii 专有 DLL、头文件或二进制组件。
