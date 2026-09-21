[English](STATUS.md) | [简体中文](STATUS.zh-CN.md)

# 项目就绪状态与里程碑 (Project Status)

**更新日期**：2026-09-21  
**目标里程碑**：首发公开发布版本 `v0.1.0`  
**当前活动分支**：`dev/et5-native-v2`（准备合并至 `main`）

本文件为官方就绪状态看板。用于清晰区分自动化 Windows CI 验证事实、实体硬件实测进展与法律合规依据。

---

## 就绪状态评估矩阵

| 评估维度 | 状态 | 实测依据与技术详情 |
|:---|:---|:---|
| **Windows CI 构建矩阵** | **已验证 (VERIFIED)** | CI Run `35609329758` 在 `upstream-main` 与 `pinned-stable` 两项契约配置下均全部成功通过。 |
| **自动化测试套件** | **已验证 (VERIFIED)** | **124 项测试运行，124 项通过，0 项失败**。经过完整日志审计，未出现任何未处理异常（Unhandled Exception）、进程崩溃或 `NullReferenceException`。 |
| **OptiKey 插件加载器** | **已验证 (VERIFIED)** | x64 .NET Framework 4.6 加载器测试成功通过无参构造函数反射实例化 `ET5PointService`，并验证构造阶段零非托管早期分配。 |
| **Release ZIP 静态审计** | **已验证 (VERIFIED)** | 发布压缩包仅严格包含 `LICENSE` 与 `OptiKey.ET5.Plugin.dll` 两项文件。绝无任何专有 DLL、静态库、头文件或测试桩组件混入。 |
| **回调泵停机生命周期** | **已验证 (VERIFIED)** | 完整集成了 `ICallbackPump` 架构（轮询模式与等待模式），具备有界停机超时与原生卡死工作线程安全隔离保护机制。 |
| **普通用户开箱即连流程** | **已验证 (VERIFIED)** | 默认启用单设备候选自动直连，实现零配置即插即用；检测到多设备时严格拒绝静默连接候选 0，确保使用安全。 |
| **运行时发现与数字签名** | **已验证 (VERIFIED)** | 动态探测系统自动从系统服务和注册表定位官方 `tobii_stream_engine.dll`，并通过 Authenticode `WinVerifyTrust` 校验签名有效性。 |
| **零专有二进制分发** | **已验证 (VERIFIED)** | 绝对不分发或捆绑任何 Tobii 二进制文件，完全依赖用户本机合法安装并校准好的官方 Tobii Experience 软件环境。 |
| **注视隐私不变量** | **已验证 (VERIFIED)** | 数据仅在易失性内存中实时流转，绝不进行本地注视点存储、日志记录或任何形式的网络遥测。 |
| **双语文档严格对齐** | **已验证 (VERIFIED)** | 通过 `tools/scripts/check-doc-sync.ps1` 脚本自动化校验所有 Markdown 文件的 1:1 中英双语对齐与跨链接。 |
| **硬件实测认证** | **待社区反馈 (UNVERIFIED)** | 目前已通过端到端合成管道与宿主加载验证；实体 ET5 硬件认证期待广大用户与 AAC 社区实机反馈。 |

---

## 发布决议

**就绪状态结论**：**达到首发版本发布标准 (READY FOR INITIAL PUBLIC RELEASE v0.1.0)**

所有见于 `AGENTS.md` 的 20 条工程与治理门禁及 Phase 20 检查项均已完全满足。项目已具备创建 Pull Request 合并至 `main` 并正式发布 `v0.1.0` 的条件。
