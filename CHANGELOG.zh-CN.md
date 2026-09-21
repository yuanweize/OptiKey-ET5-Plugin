[English](CHANGELOG.md) | [简体中文](CHANGELOG.zh-CN.md)

# 更新日志 (Changelog)

本文件记录本项目的所有重要变更。

格式参考 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)，
并遵循 [语义化版本 2.0.0](https://semver.org/lang/zh-CN/) 规范。

## [0.1.1] - 2026-09-21

### 安全与隐私
- 在 `TobiiRuntimeLocator` 中强制执行严格的 Authenticode 数字签名完整性校验（`SignatureStatus.Valid`）与非吊销检查，杜绝因仅校验 Subject 造成加载篡改或未签名二进制的风险 (SEC-01)。
- 新增 `PathSanitizer` 脱敏工具，杜绝诊断与跟踪日志中泄露用户本地账户名与个人文件目录 (PRIV-01)。

### 缺陷修复
- 重构回调泵 `Dispose()`，在执行工作线程 `Join()` 前提前释放 `stateLock`，彻底消除由于跨线程锁争用引发的自诱导停机死锁风险 (CONC-01)。
- 统一配置加载优先级：明确遵循“默认配置 -> 配置文件 -> 环境变量覆盖”的严格层级，覆盖全部 6 个配置项 (CONF-01)。
- 修复 `ET5PointService.Point` 事件订阅生命周期缺陷，确保仅在订阅者计数经历 0 -> 1 转换时启动提供者，避免每次订阅均重复调用 `Start()` (LIFE-01)。
- 在 `ET5PointService.Dispose()` 中将 `gazeProvider.Dispose()` 移至 `eventLock` 作用域外执行，消除锁倒置隐患 (LIFE-02)。
- 修正所有文档中的 OptiKey 插件手动安装路径为 `%APPDATA%\OptiKey\OptiKey\EyeTrackerPlugins\OptiKey-ET5-Plugin\`，与当前 OptiKey 上游源码保持严格一致 (DOC-02)。
- 强化 `check-doc-links.ps1` 校验器，严格拦截代码块之外的所有 `file://` 链接与本地机器绝对路径 (DOC-01)。
- 修复 `AGENTS.md` 准则 17 中已失效的旧原生绑定文件名引用 (SOP-01)。

### 变更与规范
- 规范化设备候选绑定与 ABI 文档表述：清晰区分单一兼容 Tobii Stream Engine 运行时候选与经过认证的实体 ET5 硬件检测 (ABI-01, ID-01)。
- 严谨界定回调停机事实：清晰区分 CI 验证的托管层停机超时保护与依赖原生运行时的非托管回调耗时。

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
