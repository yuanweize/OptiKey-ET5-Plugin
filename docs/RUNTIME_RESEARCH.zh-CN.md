[English](RUNTIME_RESEARCH.md) | [简体中文](RUNTIME_RESEARCH.zh-CN.md)

# 运行时研究与 Tobii 交互规范 (Runtime Research)

> **文档状态**：工程研究与技术规范  
> **目标硬件**：Tobii Eye Tracker 5 (ET5, IS50 系列)  
> **运行环境**：已安装官方 Tobii Experience 的 Windows 10/11 x64 系统

---

## 1. Tobii Experience 实际安装的运行时组件

### A. 内核与底层硬件驱动
- **Tobii Eye Tracker 5 驱动 (`tobii_usb.sys` / `tobii_sensor.sys`)**：
  负责通过 USB 2.0/3.0 与 IS50 传感器进行底层数据传输与控制。
- **Tobii 虚拟设备驱动**：
  在 Windows 设备管理器中注册设备接口（位于 *Eye Tracker* 与 *通用串行总线设备* 类别下）。

### B. 后台守护服务
- **Tobii Service (`Tobii.Service.exe`)**：
  作为 Windows NT 服务在后台运行（通常设置为自动启动）。负责硬件电源管理、近红外补光频闪控制、眼动校准计算，并托管进程间通信通道。
- **Tobii Experience Helper Service**：
  负责协调 UWP / WinUI 商店前端应用与 Win32 后台服务之间的交互。

### C. 客户端原生运行时库
- **`tobii_stream_engine.dll`**：
  官方 C API 动态链接库，作为客户端通过本地 IPC / 共享内存与 `Tobii.Service.exe` 通信。
  - 常见安装目录：
    - `%ProgramFiles%\Tobii\Tobii Service\tobii_stream_engine.dll`
    - `%ProgramFiles%\Tobii\Tobii Eye Tracker 5\tobii_stream_engine.dll`
    - `%ProgramFiles(x86)%\Tobii\Tobii Eye Tracker 5\x64\tobii_stream_engine.dll`
  - 架构：**x64 (AMD64)**。

---

## 2. `tobii_stream_engine.dll` 确定性定位机制

为防止 `%PATH%` 污染或不安全的相对路径加载，插件采用确定性的四级解析机制：
1. **Windows 服务与注册表检索**：读取 `Tobii Service` 系统服务注册表项，直接定位安装目录。
2. **高完整性已知路径扫描**：优先探测 64 位 Program Files 目录。
3. **PE 二进制架构检查**：读取 DOS/PE 结构头，核对 `Machine == 0x8664`（AMD64）。
4. **Authenticode 代码签名验证**：通过 Win32 `WinVerifyTrust` 验证文件签名完整性与 Tobii 主题。

---

## 3. C# 互操作生命周期映射

1. **API 创建**：调用 `tobii_api_create` 初始化全局上下文。
2. **设备枚举**：调用 `tobii_enumerate_devices` 获取可用眼动仪硬件地址列表。
3. **设备创建与订阅**：连接目标设备并注册 `tobii_gaze_point_subscribe` 回调函数。
4. **事件驱动流转**：通过 `ICallbackPump` 循环分发原生事件并通知 OptiKey 派发光标位置。
5. **有界释放**：通过 `tobii_gaze_point_unsubscribe`、`tobii_device_destroy` 与 `tobii_api_destroy` 完成析构。若底层挂起则安全跳过释放，防止访问冲突崩溃。
