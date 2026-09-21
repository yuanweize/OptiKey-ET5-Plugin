[English](DOCUMENTATION_POLICY.md) | [简体中文](DOCUMENTATION_POLICY.zh-CN.md)

# 文档双语同步维护策略 (Documentation Policy)

## 1. 核心原则

本仓库中所有需要维护的面向开发者和用户的说明文档，**必须严格保持英文（`*.md`）与简体中文（`*.zh-CN.md`）1:1 语义对等与同步更新**。

辅助技术与无障碍交流（AAC）社区遍布全球，涵盖众多国际与中文使用者、研究者和临床工程师。保持完整、同步的双语文档是本项目的高优先级质量要求。

## 2. 文件命名规范

- 英文文档：`<FILENAME>.md`（例如：`README.md`, `SECURITY.md`, `docs/ARCHITECTURE.md`）
- 简体中文文档：`<FILENAME>.zh-CN.md`（例如：`README.zh-CN.md`, `SECURITY.zh-CN.md`, `docs/ARCHITECTURE.zh-CN.md`）

### 例外说明
- `LICENSE`：权威的 GNU 通用公共许可证第 3 版（GPL-3.0）文本必须保持标准英文，不得篡改或用翻译版本替代。中文的说明仅能作为非正式辅助文档存在。
- `OPTIKEY_CONTRACT_REF`：技术性上游契约提交 SHA 锁定文件。
- `.github/workflows/` 下的 CI 自动化工作流文件、`.github/ISSUE_TEMPLATE/` 下的工单模板以及 `.github/PULL_REQUEST_TEMPLATE.md` PR 模板。

## 3. 强制语言切换导航头

所有成对的双语文档必须在第一行（Line 1）放置标准的双语切换链接：

```markdown
[English](FILENAME.md) | [简体中文](FILENAME.zh-CN.md)
```

对于位于 `docs/` 或子目录下的文档：
```markdown
[English](FILENAME.md) | [简体中文](FILENAME.zh-CN.md)
```

## 4. CI 自动化双语对齐检查

CI 工作流中集成了自动化检查脚本 `tools/scripts/check-doc-sync.ps1`，自动校验：
1. 每一个英文字符文档均有同名的 `.zh-CN.md` 配对文件。
2. 每一个 `.zh-CN.md` 中文文档均有同名的 `.md` 配对文件。
3. 双方文件顶部均具备有效的双语切换链接。
4. 文档内的相对路径链接指向实际存在的文件。

任何仅修改单语言文档或遗漏中文翻译的 Pull Request 均会被 CI 文档门禁拦截。
