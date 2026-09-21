[English](CHANGELOG.md) | [简体中文](CHANGELOG.zh-CN.md)

# 更新日志 (Changelog)

本文件记录本项目的所有重要变更。

格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)，
并遵循 [语义化版本 2.0.0](https://semver.org/lang/zh-CN/) 规范。

## [0.1.0] - 2026-09-21

### 新增功能
- 适用于 OptiKey 4.x 的完整开源 Tobii Eye Tracker 5 (ET5) 输入提供者实现。
- 单设备自动发现与直连策略，为普通用户提供零配置“开箱即连”体验。
- 多设备安全防误控机制，检测到多台硬件时严格拒绝静默连接候选 0。
- 动态运行时探测系统，自动搜索官方 Tobii Service、程序安装目录及注册表。
- 数字签名验证（Authenticode `WinVerifyTrust`），在加载 DLL 前强制校验 Tobii 签名有效性。
- 可中断的 `ICallbackPump` 回调泵架构（支持 `ProcessOnlyPollingPump` 轮询与 `WaitAndProcessCallbackPump`），具备有界停机保护。
- 原生卡死工作线程安全隔离机制，停机超时时跳过非托管释放，坚决防止内存释放后使用（Use-After-Free）崩溃。
- 独立的硬件与环境诊断工具（`ET5Diagnostics.exe`）。
- 双矩阵 Windows x64 CI 构建与测试流水线，同时验证锁定稳定版与上游最新版 OptiKey 契约。
- 全量文档 1:1 中英双语对齐，覆盖所有操作手册、架构决策记录（ADR）及项目治理规范。
- 2026年9月全面代码安全审计（`docs/CODE_AUDIT_2026-09.zh-CN.md`），彻底根除所有 P0/P1 并发与生命周期竞态缺陷。

### 安全与隐私
- 源代码库及发布压缩包中绝对不捆绑任何 Tobii 专有 DLL 二进制文件。
- 严格注视隐私不变量：纯易失性内存即时处理，绝不存储、记录或外发任何注视点坐标与设备硬件地址。
