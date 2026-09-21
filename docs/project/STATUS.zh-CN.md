[English](STATUS.md) | [简体中文](STATUS.zh-CN.md)

# 项目就绪状态与里程碑 (Project Status)

- **更新日期**：2026-09-21
- **当前已发布版本**：`v0.1.0`（不可变历史发布基准）
- **目标补丁版本**：`v0.1.1`（发布候选版本已验证 / Release Candidate Validated）
- **当前活动分支**：`audit/v0.1.1-hardening`（准备合并至 `main` 的 PR #5）

本文件为官方就绪状态看板。用于清晰区分自动化 Windows CI 验证事实、实体硬件实测进展、托管运行时超时隔离机制与原生回调执行边界、以及法律合规依据。

---

## 就绪状态评估矩阵

| 评估维度 | 状态 | 证据分类与技术详情 |
|:---|:---|:---|
| **Windows CI 构建矩阵** | **CI 已验证 (CI VERIFIED)** | 在 Windows Server 2022 环境下同时通过 `upstream-main` 与 `pinned-stable` 两组契约配置。发布后加固构建（如 `35631685399`）全部步骤顺利通过。 |
| **自动化测试套件** | **单元与集成测试已验证 (UNIT/INTEGRATION VERIFIED)** | 扩展后的测试套件全面覆盖运行时签名完整性校验、非阻塞回调泵释放、配置优先级矩阵、订阅生命周期转换、坐标映射及端到端仿真，0 失败且无任何未处理异常。 |
| **OptiKey 插件加载器** | **集成与包级已验证 (INTEGRATION/PACKAGE VERIFIED)** | x64 .NET Framework 4.6 加载器测试成功通过无参构造函数反射实例化 `ET5PointService`，并验证构造阶段零非托管早期分配。 |
| **Release ZIP 静态审计** | **发布包已验证 (PACKAGE VERIFIED)** | 发布压缩包仅严格包含 `LICENSE` 与 `OptiKey.ET5.Plugin.dll`（v0.1.0 SHA256: `4defb89f7ef20efcbf20a98b712c596c0b6fb1f7b1b2bd0af1b8a917f8b56fd0`）。绝无任何专有 DLL、静态库、头文件或测试桩组件混入。 |
| **回调泵停机生命周期** | **仿真集成已验证 (SYNTHETIC INTEGRATION VERIFIED)** | 托管层停机超时与原生卡死工作线程隔离机制已在仿真测试下全面通过；重构移除了跨线程 Join 持有状态锁的死锁风险。*注：Tobii 原生回调内部执行耗时依赖系统运行时，尚未获得独立证明。* |
| **普通用户开箱即连流程** | **仿真集成已验证 (SYNTHETIC INTEGRATION VERIFIED)** | 默认启用单设备候选自动直连，实现零配置即插即用；检测到多设备候选时严格拒绝静默连接。*注：当前仅枚举并绑定唯一的兼容 Tobii Stream Engine 运行时候选；精确识别 ET5 物理型号仍需硬件内省接口。* |
| **运行时发现与数字签名** | **单元与系统级已验证 (UNIT/SYSTEM VERIFIED)** | 动态探测系统自动从系统服务和注册表定位官方 `tobii_stream_engine.dll`。Authenticode 校验强制验证签名完整性（`SignatureStatus.Valid`）、非吊销以及 Tobii 签名主体身份，并明确区分签名有效性与根证书信任链。 |
| **零专有二进制分发** | **源码与包级已验证 (SOURCE/PACKAGE VERIFIED)** | 绝对不分发或捆绑任何 Tobii 二进制文件，完全依赖用户本机合法安装并校准好的官方 Tobii Experience 软件环境。 |
| **注视隐私不变量** | **源码与测试已验证 (SOURCE/TEST VERIFIED)** | 数据仅在易失性内存中实时流转，绝不进行本地注视点存储、日志记录或任何形式的网络遥测；诊断日志中已增加用户个人目录脱敏过滤。 |
| **文档对齐与链接有效性** | **CI 与脚本已验证 (CI/SCRIPT VERIFIED)** | 通过 `tools/scripts/check-doc-sync.ps1` 校验全量 Markdown 文件的 1:1 中英双语对齐；通过 `tools/scripts/check-doc-links.ps1` 严格拦截代码块外的所有 `file://` 或机器绝对路径。 |
| **硬件实测认证** | **待社区反馈 (UNVERIFIED)** | 目前已通过端到端合成管道与宿主加载验证；实体 ET5 硬件认证期待广大用户与 AAC 社区实机反馈。 |
| **Tobii ABI 权威溯源** | **部分已验证 (PARTIALLY VERIFIED)** | Stream Engine 绑定已在函数 ABI 与测试替身下验证兼容；官方 C 头文件由于版权保护仍处于闭源状态。 |

---

## 发布决议

**就绪状态结论**：**v0.1.0 已发布 / v0.1.1 发布候选版本已验证 (v0.1.0 RELEASED / v0.1.1 RELEASE CANDIDATE VALIDATED)**

- 历史版本 `v0.1.0` 保留在 GitHub Releases 上且保持不可变，以维护密码学发布完整性。
- 针对发布后独立审计发现的关键缺陷（SEC-01、CONC-01、CONF-01、LIFE-01、LIFE-02、DOC-01、DOC-02），已在 PR #5 中通过完整 Windows CI 矩阵及发布演练验证，准备合并入 `main` 并打标发布 `v0.1.1`。
