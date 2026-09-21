# OptiKey-ET5-Plugin (Tobii Eye Tracker 5 插件)

[![Windows CI](https://github.com/yuanweize/OptiKey-ET5-Plugin/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/yuanweize/OptiKey-ET5-Plugin/actions/workflows/build-and-test.yml)
[![License: GPL-3.0-only](https://img.shields.io/badge/License-GPL--3.0--only-blue.svg)](LICENSE)
[![就绪状态](https://img.shields.io/badge/readiness-NOT%20READY-red.svg)](STATUS.md)
[![平台](https://img.shields.io/badge/Platform-Windows%20x64-lightgrey.svg)](docs/COMPATIBILITY.md)

[English](README.md) | [简体中文](README.zh-CN.md)

一个实验性开源项目，目标是将 **OptiKey 4.x** 与 **Tobii Eye Tracker 5 (ET5)** 连接。当前状态为 **NOT READY**，不可用于真实硬件或终端用户安装。

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

- **零专有二进制分发**：经审查的源码树和 CI 包不包含 Tobii DLL、库、头文件或 SDK 归档。
- **与硬件解耦的构造**：打包加载测试已证明 `Activator.CreateInstance` 可在不加载 `tobii_stream_engine.dll` 的情况下构造并释放点服务。
- **重连状态机**：包含瞬态运行时错误的重连处理；真实断连、休眠恢复和 DPI 行为尚未验证。
- **坐标转换**：提供使用模拟显示指标测试的归一化坐标映射；真实 DPI 和多显示器行为尚未验证。
- **严格隐私保护与零留存**：凝视数据仅在内存中即时用于按键判定，随后立即丢弃。不写盘、不存盘、零网络请求、零数据统计。
- **不静默使用模拟凝视**：模拟凝视位于独立测试程序集，且已从审计过的发布包中排除。

---

## 预期环境要求

1. **操作系统目标**：Windows x64；ET5 及适用 Tobii API 的具体支持版本尚未验证。
2. **硬件目标**：安装在主显示器下方的 Tobii Eye Tracker 5；尚未验证任何真实配置。
3. **软件依赖**：
   - 官方已安装并完成校准的 [Tobii Experience](https://gaming.tobii.com/getstarted/)。
   - [OptiKey](https://github.com/OptiKey/OptiKey/releases) **4.2.2 契约参考版本**。本仓库未验证其他版本。

---

## 安装与使用

当前没有受支持的安装方式，也没有 Git 标签或 GitHub Release。在设备身份、ABI、有界停机、运行时信任、真实硬件行为和 Tobii 许可问题解决前，生产路径会主动拒绝连接。

未来目标流程仍是：安装并校准官方 Tobii 软件、安装 OptiKey、通过 OptiKey 查找插件、选择并使用 ET5。这是项目目标，不是当前功能。

---

## 硬件诊断工具

当前安全的盘点脚本只记录安装元数据，不初始化 Tobii，也不采集凝视数据：
```powershell
.\tools\HardwareDiagnostics\inventory-tobii-runtime.ps1
```
基于回调的 `ET5Diagnostics.exe` 仍保持失败关闭，未批准用于真实硬件。

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
