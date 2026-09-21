[English](CONTRIBUTING.md) | [简体中文](CONTRIBUTING.zh-CN.md)

# 贡献指南 (Contributing)

感谢您参与开源辅助交流与无障碍软件的贡献。您的每一份改进，都将切实帮助渐冻症（ALS/MND）及重度运动障碍人士获得独立沟通的能力。

## 贡献者强制性规范

在提交代码前，请通读 [`AGENTS.zh-CN.md`](../AGENTS.zh-CN.md) 与 [智能体标准操作规范](../docs/development/AGENT_SOP.zh-CN.md)。所有贡献者（无论是人类开发者还是 AI 智能体）必须恪守以下底线：

1. **严禁提交专有二进制文件**：任何情况下均不得提交来自 Tobii 的 `.dll`、`.lib`、`.sys` 或封闭专有头文件。
2. **严禁记录注视点数据**：绝对不得记录、保存、打印或外发任何原始注视点坐标与设备特征。
3. **保持 OptiKey 插件契约**：`ET5PointService` 必须保持公共无参构造函数，构造阶段严禁早期分配非托管资源。
4. **严格双语文档同步**：任何文档修改必须同步更新英文（`*.md`）与简体中文（`*.zh-CN.md`）。
5. **严禁生产代码静默降级为模拟数据**：底层错误必须真实上报，绝不在生产路径中静默回落到测试用虚拟眼动桩。

## Pull Request 工作流程

1. Fork 仓库并从 `main` 创建开发分支：
   ```bash
   git checkout -b dev/your-feature-name
   ```
2. 遵循现有 C# 编码规范及 [`docs/adr/`](../docs/adr/) 中的架构决策实现代码。
3. 在 `tests/OptiKey.ET5.Plugin.Tests/` 下添加或更新相应的单元测试。
4. 运行双语文档对齐检查脚本：
   ```powershell
   pwsh .\tools\scripts\check-doc-sync.ps1
   ```
5. 使用 `PULL_REQUEST_TEMPLATE.md` 模板向 `main` 提交 Pull Request。
6. 确保全量 Windows CI 矩阵全部通过且日志中无任何未处理异常。
