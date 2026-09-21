# OptiKey-ET5-Plugin (Tobii Eye Tracker 5 插件)

[![Windows CI](https://github.com/yuanweize/OptiKey-ET5-Plugin/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/yuanweize/OptiKey-ET5-Plugin/actions/workflows/build-and-test.yml)
[![License: GPL-3.0-only](https://img.shields.io/badge/License-GPL--3.0--only-blue.svg)](LICENSE)
[![OptiKey 兼容性](https://img.shields.io/badge/OptiKey-%3E%3D%204.1.0-brightgreen.svg)](docs/COMPATIBILITY.md)
[![平台](https://img.shields.io/badge/Platform-Windows%20x64-lightgrey.svg)](docs/COMPATIBILITY.md)

[English](README.md) | [简体中文](README.zh-CN.md)

一个专为 **OptiKey 4.x** (>= 4.1.0) 设计的高性能、无专有二进制捆绑的开源外部眼控仪插件，支持直接使用 **Tobii Eye Tracker 5 (ET5)** 作为眼控输入源。

专为重度运动与言语障碍用户（包括渐冻症/肌萎缩侧索硬化症 ALS/MND 闭锁综合征患者）设计，眼控输入是他们日常使用电脑和自主沟通的生命线。

---

## 终极目标：极简无缝的用户体验

```
已安装 Tobii Experience 驱动并完成校准
                 ↓
            启动 OptiKey
                 ↓
     点击“在线查找更多眼控仪选项”
                 ↓
     选择“Tobii Eye Tracker 5”并安装
                 ↓
           等待真实硬件验证
```
*普通终端用户绝不需要手动寻找 DLL 文件、无需配置环境变量、无需编译任何代码。*

---

## 核心设计特性

- **零专有二进制分发**：本仓库和发布包**绝不打包**任何 Tobii 商业闭源 DLL 或 SDK 头文件。插件在用户本地计算机上通过安全白名单和数字签名验证，动态绑定到用户合法安装的 Tobii Experience 驱动运行时。
- **容错型无参实例化构造函数**：严格遵守 OptiKey 的 `Activator.CreateInstance` 反射加载规范。构造函数完全与硬件解耦，零副作用，绝不会在检测或启动阶段使 OptiKey 崩溃。
- **重连状态机**：包含瞬态运行时错误的重连处理；真实断连、休眠恢复和 DPI 行为尚未验证。
- **坐标转换**：提供使用模拟显示指标测试的归一化坐标映射；真实 DPI 和多显示器行为尚未验证。
- **严格隐私保护与零留存**：凝视数据仅在内存中即时用于按键判定，随后立即丢弃。不写盘、不存盘、零网络请求、零数据统计。
- **严禁生产环境静默伪造凝视**：当硬件断开时明确向用户提示错误，模拟凝视桩仅存在于独立测试包中，绝不会在生产环境被静默激活。

---

## 环境要求

1. **操作系统**：Windows 10 64-bit (1903+) 或 Windows 11 64-bit (x64架构)。
2. **硬件设备**：安装在主显示器下方的 Tobii Eye Tracker 5。
3. **软件依赖**：
   - 官方已安装并完成校准的 [Tobii Experience](https://gaming.tobii.com/getstarted/)。
   - [OptiKey](https://github.com/OptiKey/OptiKey/releases) **4.2.2 契约参考版本**。本仓库未验证其他版本。

---

## 安装与使用方法

### 方式一：OptiKey 软件内自动在线安装（推荐）
1. 确保 Tobii Eye Tracker 5 已接入电脑，且在 **Tobii Experience** 中完成了屏幕校准。
2. 启动 **OptiKey**。
3. 打开**管理控制台**（快捷键 `F12` 或点击菜单键） -> **指向与选择**。
4. 将指向源切换为**眼动追踪仪**。
5. 点击**在线查找更多眼动仪选项**。
6. 找到 **Tobii Eye Tracker 5** (`yuanweize/OptiKey-ET5-Plugin`)，点击**安装**。
7. OptiKey 将自动下载并加载插件，即可开始眼动打字。

### 方式二：手动离线安装
1. 从 GitHub [Releases](https://github.com/yuanweize/OptiKey-ET5-Plugin/releases) 页面下载最新的插件包 `OptiKey-ET5-Plugin-v*.zip`。
2. 解压到以下目录：
   ```
   %AppData%\OptiKey\OptiKey\EyeTrackerPlugins\yuanweize\OptiKey-ET5-Plugin\<版本号>\
   ```
3. 在 OptiKey 管理控制台 -> 指向与选择 -> 眼动追踪仪 中，选择 **Tobii Eye Tracker 5**。

---

## 硬件诊断工具

完成原生 ABI 和安全停机的真实硬件验证后，才可使用独立硬件诊断工具排查连接问题：
```powershell
# 运行 PowerShell 诊断脚本
.\tools\HardwareDiagnostics\diagnose.ps1
```
或直接运行 `ET5Diagnostics.exe`。诊断工具会完整检测运行时 DLL 路径、x64 架构、签名合法性、设备连接状态以及采样回调频率（为保护隐私，不会打印任何具体凝视坐标）。

---

## 架构与技术文档

- [架构全景设计 (ADR-001)](docs/adr/ADR-001-architecture-overview.md)
- [上游契约锁定与 Option B 废除 (ADR-002)](docs/adr/ADR-002-upstream-contracts-pinning.md)
- [硬件解耦生命周期与延迟初始化 (ADR-003)](docs/adr/ADR-003-hardware-independent-lifecycle.md)
- [运行时隔离与安全动态加载 (ADR-004)](docs/adr/ADR-004-tobii-runtime-isolation-and-security.md)
- [重连状态机设计 (ADR-005)](docs/adr/ADR-005-reconnect-state-machine.md)
- [测试与模拟凝视桩隔离 (ADR-006)](docs/adr/ADR-006-testing-and-synthetic-gaze-isolation.md)
- [坐标空间语义与高DPI转换 (ADR-007)](docs/adr/ADR-007-coordinate-space.md)
- [Tobii 运行时深度研究报告](docs/RUNTIME_RESEARCH.md)
- [版本兼容性矩阵](docs/COMPATIBILITY.md)
- [真实硬件验证协议与发布门禁](docs/HARDWARE_VALIDATION.md)
- [故障排除指南](docs/TROUBLESHOOTING.md)
- [法律声明与许可来源](LEGAL.md)
- [隐私保护协议](PRIVACY.md)

---

## 开源协议

本项目采用 **GNU General Public License v3.0 only** (GPL-3.0-only) 授权协议。详情参见 [LICENSE](LICENSE)。
所有原始代码均为完全开源、非商业性无偿提供。
