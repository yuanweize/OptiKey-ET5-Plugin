[English](AGENTS.md) | [简体中文](AGENTS.zh-CN.md)

# AGENTS.md — 自主与协作编程智能体仓库守则 (Repository Mandates for Autonomous & Pair-Programming Agents)

> [!IMPORTANT]
> 任何操作本仓库的自动化智能体 (Agent)、大语言模型 (LLM) 助手或开发者，在提议或提交修改之前，**必须**完整阅读并严格遵守以下 20 项不可逾越的守则。

## 20 条不可妥协的铁律 (The 20 Non-Negotiable Mandates)

1. **先读 AGENTS.md**：所有智能体在修改任何代码或文档之前，必须完整阅读本文件和 [`docs/development/AGENT_SOP.zh-CN.md`](docs/development/AGENT_SOP.zh-CN.md)。
2. **严禁虚报测试通过**：未检查确切的执行输出、运行用例数和退出状态码之前，严禁声称测试已通过。
3. **未处理异常零容忍**：若测试日志中出现进程崩溃、`Unhandled Exception:` 或 `AccessViolationException`，哪怕 CI 显示绿色也绝不视为合格。
4. **分支与 PR 规范**：始终在开发分支（`dev/...` 或 `chore/...`）上工作并通过 Pull Request 提交合并。严禁直接向 `main` 分支推送。
5. **双语文档严格对齐**：必须保持英文（`*.md`）与简体中文（`*.zh-CN.md`）文档之间 1:1 双向同步与相互链接。
6. **面向用户的变更必须更新文档**：任何影响用户体验、配置、安装的变更，必须立即同步更新两份 README 和用户指南。
7. **源代码是唯一事实真相**：以源代码和当前有效测试的行为为权威准则，切勿依赖过时的文档或陈旧历史记录。
8. **绝不打包 Tobii 专有二进制文件**：严禁打包、重新分发、提交或绑定任何专有 Tobii DLL（如 `tobii_stream_engine.dll`）。
9. **严格的隐私不变性**：严禁持久化、记录到日志或网络传输原始注视点坐标或设备识别信息。仅在内存中即时分发。
10. **保持无参构造函数契约**：`ET5PointService` 的无参构造函数是 OptiKey 插件加载器所必须的契约，绝不能抛出异常或提前初始化非托管硬件。
11. **必须通过完整 Windows CI**：所有 PR 必须全绿通过完整的 Windows x64 CI 矩阵（包括上游最新与锁定稳定契约）。
12. **审核安装包清单**：每个发布资产包必须经过自动化审计，确保不含任何多余或专有的 DLL。
13. **双语同步更新 CHANGELOG**：在 `CHANGELOG.md` 和 `CHANGELOG.zh-CN.md` 中同步记录所有显著的用户可见或架构变更。
14. **双语同步更新 STATUS**：在 [`docs/project/STATUS.md`](docs/project/STATUS.md) 和 [`docs/project/STATUS.zh-CN.md`](docs/project/STATUS.zh-CN.md) 中同步跟踪里程碑进展与就绪状态。
15. **双语发布说明**：所有 GitHub Release 摘要必须同时包含完整的英文和简体中文内容。
16. **生产环境杜绝静默合成回退**：本地硬件或运行时失效时，生产代码严禁静默回退到模拟或合成注视点，必须抛出错误事件通知用户。
17. **绝不凭空臆想本地 ABI**：严格遵循 [`src/OptiKey.ET5.Plugin/Runtime/Interop/TobiiStreamEngineBinding.cs`](src/OptiKey.ET5.Plugin/Runtime/Interop/TobiiStreamEngineBinding.cs) 和 [`docs/research/ABI_PROVENANCE.zh-CN.md`](docs/research/ABI_PROVENANCE.zh-CN.md) 中已验证的 C 声明。
18. **尊重既有架构决策**：在设计新组件或创建新 ADR 之前，必须先检索并审查既有的架构决策记录（[`docs/adr/`](docs/adr/)）。
19. **原子且富有含义的提交**：保持 commit 小而清晰，并遵循 Conventional Commits 规范（`feat:`, `fix:`, `docs:`, `ci:`）。
20. **及时清理废弃分支**：PR 合并后，立即删除本地及远端已合并的特性分支。

---

有关详尽的标准操作规程，请参阅：
- [Agent Standard Operating Procedure (English)](docs/development/AGENT_SOP.md)
- [智能体标准操作规范 (简体中文)](docs/development/AGENT_SOP.zh-CN.md)
