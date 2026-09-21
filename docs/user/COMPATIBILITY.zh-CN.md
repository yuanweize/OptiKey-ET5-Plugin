[English](COMPATIBILITY.md) | [简体中文](COMPATIBILITY.zh-CN.md)

# 兼容性矩阵与运行环境要求 (Compatibility Matrix)

## 目标硬件
- **Tobii Eye Tracker 5 (ET5)**：核心目标硬件。在无专有源码捆绑的前提下，进行完全合规的洁净室互操作技术适配。
- **IS50 平台眼动仪**：ET5 所采用的底层传感器硬件平台。
- **早期 Tobii 设备（Eye Tracker 4C, EyeX）**：未作专门测试，非首发主要支持目标。

---

## 宿主应用兼容性
OptiKey 从 **4.1.0** 版本开始引入了基于 `DllLoader` 的外部眼动插件动态发现与加载机制。

| OptiKey 版本 | 兼容性状态 | 说明 |
| :--- | :--- | :--- |
| **OptiKey < 4.0** | ❌ 不支持 | 尚未具备外部插件架构。 |
| **OptiKey 4.0.x** | ⚠️ 有限支持 | 早期预览版插件接口，加载机制不稳定。 |
| **OptiKey 4.1.0 - 4.2.1** | ⚠️ 待社区验证 | 支持外部插件，需实际宿主测试。 |
| **OptiKey 4.2.2** | 兼容（锁定契约参考） | 锁定稳定版构建目标（`pinned-stable`），通过全量 CI 自动化验证。 |
| **OptiKey upstream `main`** | 兼容（动态上游参考） | 上游最新主线构建目标（`upstream-main`），通过 CI 动态矩阵自动化验证。 |

### 契约验证保证
- 构造阶段零原生加载：无参构造函数不尝试加载底层 DLL 或分配硬件句柄。
- 契约接口完整实现：实现 `IPointService` 与 `IGazeService`，严格遵循宿主生命周期状态规范。

---

## 支持的操作系统与架构
- **Windows 10 / Windows 11 64位**：必备环境。
- **系统架构**：**仅限 x64**。受底层驱动限制，32位（x86）及 ARM64 架构将被 PE 校验严格拦截。
- **运行时环境**：**.NET Framework 4.6 或更高版本**（Win 10/11 系统原生内置）。

---

## 原生运行时发现与安全校验管道

### 动态探测管道
插件使用 `CompositeRuntimeDiscovery` 聚合多源定位 `tobii_stream_engine.dll`：
1. **开发者显式覆盖**：通过配置文件或环境变量 `OPTIKEY_ET5_RUNTIME_PATH` 显式指定。
2. **标准安装路径**：扫描 64 位 Program Files 常见路径（如 `%ProgramW6432%\Tobii\Tobii Eye Tracker\`）。
3. **Windows 注册表**：检索标准卸载信息注册表项（`HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall` 等）。
4. **Windows 服务配置**：从注册的 Tobii 后台服务二进制路径提取目录（`Tobii Service`）。

### 四级校验门禁管道
每个探测到的 DLL 候选文件必须通过以下四道门禁才能安全装载：
1. **文件系统完整性**：绝对路径校验与存在性检查。
2. **静态 PE 架构结构验证**：解析 COFF 头，确保符合 `IMAGE_FILE_MACHINE_AMD64` (0x8664)。
3. **数字签名信任验证 (Authenticode WinVerifyTrust)**：
   - 调用 Win32 `WinVerifyTrust` 验证文件散列完整性与签名有效性。
   - 验证证书主题名称包含 `"Tobii"`。
4. **动态导出符号能力探测**：
   - `RuntimeCapabilities` 动态探查导出函数符号。
   - 验证核心符号（`tobii_api_create` 等）、设备符号（`tobii_device_create` 等）及注视点订阅符号。
   - 根据符号支持度自适应选择回调泵驱动策略（`Polling` 轮询或 `WaitAndProcess` 等待模式）。
