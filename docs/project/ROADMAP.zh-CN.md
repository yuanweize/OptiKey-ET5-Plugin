[English](ROADMAP.md) | [简体中文](ROADMAP.zh-CN.md)

# 项目路线图 (Project Roadmap)

## 第一阶段：仓库基础设施与架构规范 (已完成)
- [x] 配置包含 `optikey-plugin` 官方主题的 GitHub 仓库。
- [x] 确立 GPL-3.0-only 开源协议、安全策略、隐私声明与行为准则。
- [x] 建立上游契约锁定机制（`OPTIKEY_CONTRACT_REF`）。
- [x] 撰写系统性架构决策记录（ADR-001 至 ADR-009）。
- [x] 编写法律声明（[`LEGAL.md`](../policies/LEGAL.zh-CN.md)）与运行时研究报告（[`RUNTIME_RESEARCH.md`](../research/RUNTIME_RESEARCH.zh-CN.md)）。
- [x] 搭建双矩阵 Windows CI 工作流流水线。

## 第二阶段：插件骨架与状态机实现 (已完成)
- [x] CI 中动态编译上游 `JuliusSweetland.OptiKey.Contracts.dll` 契约库。
- [x] 具备安全延迟激活机制的无参 `ET5PointService` 实现。
- [x] 完善的生命周期状态机 `GazeServiceStateMachine`。
- [x] 隔离的合成注视点测试套件与反射加载器测试。

## 第三阶段：核心流控与容错恢复 (已完成)
- [x] 带抖动因子的指数退避重连策略（`ExponentialBackoffReconnectPolicy`）。
- [x] 覆盖完整多 DPI 缩放特性的屏幕坐标映射器（`DisplayCoordinateMapper`）。
- [x] 全量测试套件通过（124 项测试 100% 通过）。

## 第四阶段：Tobii 运行时适配与安全加固 (已完成)
- [x] 基于白名单与 PE64 结构校验的安全 `TobiiRuntimeLocator`。
- [x] 严格内存安全的 Tobii Stream Engine 原生互操作绑定。
- [x] 引入可中断回调泵生命周期（`ICallbackPump`）与有界停机安全隔离机制。
- [x] 单设备自动连接与多设备防误控安全机制。
- [x] 2026年9月全面代码安全审计，根除后台线程异常与并发死锁。

## 第五阶段：发布工程与双语文档 (已完成)
- [x] 独立的硬件诊断与配置检测工具 `ET5Diagnostics.exe`。
- [x] CI Release ZIP 自动化打包与白名单安全静态审计。
- [x] 自动化 SHA-256 校验和生成。
- [x] 1:1 中英双语文档对齐与 CI 自动化校验脚本。
- [x] 建立仓库级智能体规范（`AGENTS.md`）与 Agent SOP。

## 第六阶段：首发公开发布与社区反馈 (进行中)
- [x] 首发公开发布版本 `v0.1.0`。
- [ ] 收集社区真实 ET5 硬件用户的无障碍使用体验与兼容性反馈。
- [ ] 根据反馈持续打磨 Windows 10/11 多显示器与物理拔插边缘场景。
- [ ] 推进至功能完备的稳定正式版（`v0.2.0` / `v1.0.0`）。
