[English](README.md) | [简体中文](README.zh-CN.md)

# OptiKey-ET5-Plugin 文档导航中心 (Documentation Hub)

欢迎查阅 **OptiKey-ET5-Plugin** 项目文档中心。本目录按目标受众与功能用途进行了分类组织，为 AAC 辅助技术用户、临床医护人员、开源贡献者和研发工程师提供清晰的指引。

---

## 1. 用户指南 (User Guide — `docs/user/`)

面向终端无障碍用户、陪护照料人员与辅助技术临床工作者：

- **[安装与首次配置指南](user/INSTALLATION.zh-CN.md)**：通过 OptiKey 联机安装或手动使用发布 ZIP 安装插件的详尽说明。
- **[故障排除指南](user/TROUBLESHOOTING.zh-CN.md)**：解决驱动未找到、多设备连接识别、眼动校准失效等常见故障。
- **[硬件与系统兼容性](user/COMPATIBILITY.zh-CN.md)**：支持的 Windows 系统版本、显示器宽高比与高 DPI 缩放配置。

---

## 2. 项目状态与规划 (Project — `docs/project/`)

记录当前开发里程碑与就绪状态的权威事实源：

- **[项目就绪状态看板](project/STATUS.zh-CN.md)**：包含 Windows CI 矩阵、测试套件、打包加载器及硬件实测进展的权威看板。
- **[项目开发路线图](project/ROADMAP.zh-CN.md)**：涵盖首发版本、多屏支持以及未来演进的分阶段路线规划。

---

## 3. 政策与合规 (Policies & Legal — `docs/policies/`)

软件许可协议、第三方知识产权界限与严格的生物识别数据隐私规范：

- **[法律声明与代码溯源](policies/LEGAL.zh-CN.md)**：净室重写声明、Tobii 知识产权边界与避风港合规说明。
- **[生物识别隐私不变量](policies/PRIVACY.zh-CN.md)**：零遥测、仅限易失性内存处理、绝不记录注视点坐标与设备信息的隐私保证。
- **[第三方版权与技术声明](policies/THIRD_PARTY_NOTICES.zh-CN.md)**：OptiKey、微软 Rx 及 Tobii 公开声明的版权与来源归属。
- **[LICENSE (GPL-3.0)](../LICENSE)**：权威 GNU 通用公共许可证 v3.0 文本（位于仓库根目录）。

---

## 4. 开发与维护标准规范 (Development — `docs/development/`)

面向软件工程师与自动化自主编程智能体 (AI Agent)：

- **[智能体标准操作规范 (AGENT SOP)](development/AGENT_SOP.zh-CN.md)**：包含开发、测试审查、双语对齐与 PR 合并的标准作业流。
- **[双语文档同步规范](development/DOCUMENTATION_POLICY.zh-CN.md)**：中英双语 1:1 严格对齐与超链接格式要求。
- **[硬件实测验证协议](development/HARDWARE_VALIDATION.zh-CN.md)**：自动化流水线与真实 Tobii ET5 硬件上的实测验证规程。
- **[智能体核心铁律 (AGENTS.md)](../AGENTS.zh-CN.md)**：20 条不可妥协的核心守则（位于仓库根目录）。

---

## 5. 核心技术调研 (Research — `docs/research/`)

针对 Tobii 原生 C 接口机制与 Windows 底层交互的深度技术调研：

- **[ABI 溯源与调用约定](research/ABI_PROVENANCE.zh-CN.md)**：C 结构体内存对齐、x64 调用约定及 P/Invoke 签名定义。
- **[运行时探测规范](research/RUNTIME_RESEARCH.zh-CN.md)**：PE64 架构校验、Authenticode `WinVerifyTrust` 签名验签与注册表发现机制。
- **[回调泵架构设计](research/CALLBACK_RESEARCH.zh-CN.md)**：事件驱动唤醒、有界超时停机保护与卡死工作线程安全隔离。

---

## 6. 架构决策记录 (ADRs — `docs/adr/`)

记录系统演进过程中的核心架构决策：

- **[ADR-001: 整体架构概览](adr/ADR-001-architecture-overview.zh-CN.md)**
- **[ADR-002: 上游契约依赖锁定](adr/ADR-002-upstream-contracts-pinning.zh-CN.md)**
- **[ADR-003: 独立于硬件的生命周期与延迟激活](adr/ADR-003-hardware-independent-lifecycle.zh-CN.md)**
- **[ADR-004: Tobii 运行时隔离与安全动态加载](adr/ADR-004-tobii-runtime-isolation-and-security.zh-CN.md)**
- **[ADR-005: 重连状态机与回调生命周期](adr/ADR-005-reconnect-state-machine.zh-CN.md)**
- **[ADR-006: 测试与合成注视点隔离](adr/ADR-006-testing-and-synthetic-gaze-isolation.zh-CN.md)**
- **[ADR-007: 坐标空间语义与映射流水线](adr/ADR-007-coordinate-space.zh-CN.md)**
- **[ADR-008: 宿主提供的运行时依赖项](adr/ADR-008-host-runtime-dependencies.zh-CN.md)**
- **[ADR-009: 架构评估：进程内与 RuntimeHost 隔离](adr/ADR-009-in-process-vs-runtime-host-architecture.zh-CN.md)**

---

## 7. 历史审计与归档报告 (History & Archives — `docs/history/`)

项目重要里程碑的历史独立审计与工程评估归档：

- **[2026 年 9 月代码审计报告](history/2026-09/CODE_AUDIT_2026-09.zh-CN.md)**：在发布 `v0.1.0` 之前对并发竞态、异常处理与生命周期安全进行的全面审计。
- **[2026 年 9 月加固审计报告](history/2026-09/AUDIT_2026-09.zh-CN.md)**：早期运行时加固、打包边界与 CI 验证日志记录。
