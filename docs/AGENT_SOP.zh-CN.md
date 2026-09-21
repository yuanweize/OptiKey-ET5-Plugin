[English](AGENT_SOP.md) | [简体中文](AGENT_SOP.zh-CN.md)

# 智能体标准操作规范 (Agent SOP)

本规范为任何参与 `OptiKey-ET5-Plugin` 仓库开发、问题修复、架构重构或版本发布的自主智能体（AI Agent）、结对编程助理和代码贡献者规定了强制性操作流程。

## 1. 操作前提与分支规范

1. **优先通读核心规范**：
   - 检查仓库根目录的 [`AGENTS.md`](../AGENTS.md)。
   - 了解 [`docs/DOCUMENTATION_POLICY.zh-CN.md`](DOCUMENTATION_POLICY.zh-CN.md) 双语对齐要求。
   - 检索 [`docs/adr/`](adr/) 下的历史架构决策记录（ADR）。
2. **严禁直接向 `main` 提交代码**：
   - 始终确认当前分支状态（`git branch -avv`）。
   - 在专门的开发分支（如 `dev/...`）上工作。
   - 所有向 `main` 分支的合并必须通过严格评审的 Pull Request 完成。

## 2. 测试与验证标准

1. **严格核实 CI 实际执行详情**：
   - 严禁在未检查具体测试计数、失败项和控制台日志的情况下断言“测试通过”。
   - 必须通过两个构建矩阵任务（`upstream-main` 与 `pinned-stable`）。
2. **对未处理异常（Unhandled Exception）零容忍**：
   - 测试套件必须以退出代码 0 成功结束。
   - 测试运行日志中**绝对不得出现** `Unhandled Exception:`、`Fatal error` 或 `AccessViolationException`。
3. **维护 OptiKey 插件契约**：
   - `ET5PointService` 必须保持无参构造函数（parameterless constructor）。
   - 严禁在构造函数中探测硬件、加载原生 DLL 或启动线程。所有资源初始化必须延迟到 `Start()` 中进行。

## 3. 双语对齐与文档维护工作流

1. 任何创建或修改 `.md` 文档的操作，必须同步创建或更新其对应的 `.zh-CN.md` 文件。
2. 确保成对的文档第一行包含标准双语导航头：
   ```markdown
   [English](FILENAME.md) | [简体中文](FILENAME.zh-CN.md)
   ```
3. 提交 PR 之前运行 `tools/scripts/check-doc-sync.ps1` 校验。

## 4. 硬件与安全红线

1. **绝对不得分发专有二进制文件**：
   - 严禁打包、分发或提交 `tobii_stream_engine.dll` 及任何 Tobii 专有组件。
   - 仅通过动态运行时发现与代码签名信任校验（Authenticode WinVerifyTrust）加载用户本机安装的官方运行时。
2. **严禁记录、持久化或传输注视点坐标**：
   - 注视点数据仅在易失性内存中处理并向 OptiKey 派发。
   - 坐标、用户身份信息、设备硬件 URL 严禁落盘或输出到日志中。
3. **有界插件停机保护**：
   - 原生工作线程若无法在超时内停止，必须安全隔离并跳过释放活动原生句柄，坚决防止内存访问越界崩溃（AccessViolationException）。

## 5. 发布检查清单

在打标签或创建 GitHub Release 之前必须满足：
- [ ] 严格遵守 `AGENTS.md` 20 条不可动摇之原则。
- [ ] Windows CI 矩阵全部通过。
- [ ] CI 日志中无未处理异常。
- [ ] 打包产物在 OptiKey 加载器冒烟测试中通过。
- [ ] 双语 `CHANGELOG.md` 和 `STATUS.md` 已更新。
- [ ] Release ZIP 包完整性审计通过（无专有组件混入）。
- [ ] 遵循语义化版本号（首个公开发布必须为 `v0.1.0`）。
