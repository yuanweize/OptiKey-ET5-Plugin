[English](TROUBLESHOOTING.md) | [简体中文](TROUBLESHOOTING.zh-CN.md)

# 故障排查手册 (Troubleshooting Guide)

## 诊断工具快速上手
在修改任何配置文件或驱动设置前，推荐先运行独立的无侵入式诊断工具：
1. 在目标计算机上打开 PowerShell。
2. 运行：
   ```powershell
   .\tools\HardwareDiagnostics\inventory-tobii-runtime.ps1
   ```
   或运行插件包附带的控制台诊断工具 `ET5Diagnostics.exe`。
3. 检查生成的诊断输出：
   - 确认是否探测到 `tobii_stream_engine.dll`。
   - 确认 PE 机器架构是否为 `x64` (`IMAGE_FILE_MACHINE_AMD64`)。
   - 确认 Authenticode 签名状态及发布者主题（`Tobii AB`）。
   - 确认必需的导出函数符号完整可用。

---

## 常见问题与解决办法

### 1. 提示“未检测到眼动仪”或连接失败

#### 单设备自动连接流程：
- **正常行为**：若计算机仅连接了 1 台 ET5，插件默认会自动绑定并建立连接。
- **排查步骤**：
  - 确认眼动仪 USB 已牢固插入计算机（建议使用主板直连的 USB 3.0 接口，避免使用供电不足的拓展坞）。
  - 打开官方 **Tobii Experience** 应用，确认眼动仪在官方软件中能够正常识别并处于激活状态。

#### 多设备环境防误控保护：
- **现象**：日志提示 `Multiple Tobii device candidates detected... Automatic selection refused`。
- **原因**：当系统检测到多台 Tobii 硬件时，插件为防止误操作，严禁静默盲连。
- **解决办法**：在 `%APPDATA%\OptiKey-ET5-Plugin\et5-plugin.config` 中指定要使用的设备编号：
  ```ini
  PreferredDeviceIndex=0
  ```

---

### 2. 运行时安全验证与签名异常

#### 签名验证失败 (`RuntimeSignerValidationFailed`)：
- **现象**：日志记录 `Candidate rejected: Tobii signer verification failed`。
- **原因**：发现的 DLL 未签名、已损坏或被非官方程序篡改。
- **解决办法**：
  - 从 Tobii 官网重新安装正版 **Tobii Experience**。
  - 切勿手动从第三方网站下载未知 DLL 放入扫描路径。

#### 根证书不受信任警告 (`UntrustedRoot`)：
- **现象**：提示签名有效但证书链不受信任。
- **原因**：设备处于离线无网状态，Windows 根证书库尚未更新 Tobii 的根 CA。
- **解决办法**：连接互联网并运行 Windows Update 刷新系统信任证书库。

---

### 3. 原生停机超时与工作线程隔离 (`STUCK_WORKER`)

- **现象**：日志出现 `CRITICAL: Worker thread or callback pump did not terminate... STUCK_WORKER`。
- **原因**：底层 Tobii 驱动在阻塞调用中挂起，无法在设定时间内响应停机请求。
- **插件防护**：插件会自动实施安全隔离，跳过原生句柄析构，确保 OptiKey 宿主程序不会因为访问冲突而崩溃。此时建议重新拔插硬件或重启 Tobii Service 服务。
