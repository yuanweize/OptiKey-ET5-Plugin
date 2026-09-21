[English](ADR-002-upstream-contracts-pinning.md) | [简体中文](ADR-002-upstream-contracts-pinning.zh-CN.md)

# ADR-002: 上游契约锁定与拒绝自行重新定义 (Upstream Contracts Pinning)

## 状态
已通过 (Accepted)

## 背景
OptiKey 将其外部插件的公共接口定义在独立的程序集项目中：`JuliusSweetland.OptiKey.Contracts`。核心接口包括：
- `JuliusSweetland.OptiKey.Contracts.IPointService`
- `JuliusSweetland.OptiKey.Contracts.INotifyErrors`

在设计构建流水线时，我们评估了两种方案：
- **方案 A**：锁定上游 OptiKey 源码仓库，在 CI 中直接编译 `JuliusSweetland.OptiKey.Contracts.dll`，并将编译出的程序集作为二进制引用依赖。
- **方案 B（明确拒绝）**：在插件工程内部自行声明同名的命名空间、接口与方法签名。

## 决议
**坚决拒绝方案 B。**

在 .NET CLR 中，类型一致性严格由 `[AssemblyQualifiedName]` 决定。如果插件自行定义接口，即使命名空间和方法完全相同，OptiKey 的反射加载器通过 `typeof(IPointService)` 进行类型匹配时依然会判定为不一致，导致报错 `No IPointService implementation found`。

### 契约锁定与 CI 矩阵规则
1. **锁定稳定版本引用**：生产构建严格链接自 `OPTIKEY_CONTRACT_REF` 中锁定的稳定标签（`v4.2.2`，commit `ebbbef7`）。
2. **双目标 CI 矩阵**：
   - Job 1：基于 `PINNED_COMMIT`（`v4.2.2`）构建与测试，作为发布产物的准入基线。
   - Job 2：基于上游 `main` 分支最新 HEAD 动态编译，为上游未来的潜在破坏性变更提供即时预警。
3. **避免冗余捆绑**：由于 OptiKey 宿主进程已在 AppDomain 中加载了该程序集，发布 ZIP 包中无需重复打包该契约 DLL。
