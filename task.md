# OptiKey-ET5-Plugin — 任务跟踪

## Phase 1: 仓库基础设施
- [x] 创建 GitHub 仓库 `yuanweize/OptiKey-ET5-Plugin`
- [x] 设置 topics: `optikey-plugin`, `optikey`, `tobii`, `eye-tracking`, `accessibility` 等
- [x] 初始化 .gitignore, LICENSE (GPL-3.0-only)
- [x] 创建项目骨架目录结构
- [x] 创建 ADR-001 到 ADR-007
- [x] 创建基础文档框架 (LEGAL.md, PRIVACY.md, THIRD_PARTY_NOTICES.md, COMPATIBILITY.md, HARDWARE_VALIDATION.md, TROUBLESHOOTING.md, ROADMAP.md, CHANGELOG.md)
- [x] 设置 GitHub Actions CI (Windows runner, MSBuild, 双矩阵验证)

## Phase 2: OptiKey 插件骨架
- [x] 在 CI 中从上游 OptiKey 构建 Contracts DLL (`tools/scripts/build-contracts.ps1`)
- [x] 记录 OPTIKEY_CONTRACT_REF (Pin 到 v4.2.2 commit `ebbbef7bbac5e2dab0e255e5a57acf163d515ca7`)
- [x] 实现 ET5PointService (hardware-independent parameterless constructor)
- [x] 实现 SyntheticGazeProvider (独立测试程序集，不进发布包)
- [x] 实现 GazeServiceStateMachine (Created, Starting, Connected, Reconnecting, Stopping, Stopped, Disposed)
- [x] 实现 PluginLogger (log4net 封装，隐私零留存)
- [x] 实现完整的生命周期管理
- [x] 核心单元测试 (StateMachine, ReconnectPolicy, CoordinateMapper, Security, Synthetic E2E)
- [x] OptiKey loader 反射兼容性测试 (精确验证单一 IPointService 实现与无参构造函数)
- [x] Windows CI 跑通验证 (全绿通过，产物生成并已审计)

## Phase 3: 核心逻辑完善
- [x] IReconnectPolicy (指数退避 + 抖动)
- [x] ScreenCoordinateMapper (在 ADR-007 确认后，1080p/1440p/4K 物理像素映射)
- [x] 坐标转换单元测试
- [x] 集成测试 (synthetic end-to-end)

## Phase 4: Tobii 运行时抽象
- [x] 提交 docs/RUNTIME_RESEARCH.md (回答全部 8 个交付物要求)
- [x] ITobiiRuntime / ITobiiRuntimeLocator (安全白名单路径加载，PE64 验证，签名检查)
- [x] Stream Engine P/Invoke 最小 API (独立实现，动态绑定，无专有二进制分发)
- [x] TobiiGazeProvider (阻塞等待 tobii_wait_for_callbacks + 错误通知)
- [x] 设备发现和信息获取
- [x] 友好错误消息
- [x] LEGAL.md 完善 (严格区分 VERIFIED FACT / ENGINEERING INFERENCE / OPEN LEGAL QUESTION)

## Phase 5: 测试和诊断
- [x] HardwareDiagnostics console exe (ET5Diagnostics.exe) + PowerShell wrapper (diagnose.ps1)
- [x] CI ZIP 验证脚本 (`tools/scripts/verify-release-zip.ps1`)（禁止 Tobii/Contracts/test DLL，验证仅 1 个 IPointService）
- [x] Release ZIP 打包脚本 (`tools/scripts/package-release.ps1`)
- [x] SHA256 校验和自动生成

## Phase 6: 文档和打包
- [x] README.md (English)
- [x] README.zh-CN.md (中文)
- [x] 完整 docs/ 目录
- [x] HARDWARE_VALIDATION.md
- [x] TROUBLESHOOTING.md
- [x] CONTRIBUTING.md, CODE_OF_CONDUCT.md, SECURITY.md
- [x] CHANGELOG.md, ROADMAP.md
- [x] PRIVACY.md, THIRD_PARTY_NOTICES.md
