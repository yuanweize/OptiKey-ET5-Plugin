[English](INSTALLATION.md) | [简体中文](INSTALLATION.zh-CN.md)

# 安装与首次配置指南 (Installation and Setup)

本指南说明如何在 OptiKey 4.x 中安装、配置并启用适用于 Tobii Eye Tracker 5 (ET5) 的 **OptiKey-ET5-Plugin**。

---

## 1. 环境准备 (Prerequisites)

在安装插件之前，请确保满足以下条件：

1. **操作系统**：Windows 10 或 Windows 11（64 位版本）。
2. **硬件设备**：Tobii Eye Tracker 5 眼动仪通过 USB 接口连接，并牢固安装在已完成校准的主显示器下方。
3. **官方 Tobii 软件环境**：
   - 从 [Tobii Gaming 开始使用](https://gaming.tobii.com/getstarted/) 下载并安装官方 **Tobii Experience**。
   - 确保 Tobii 后台服务正常运行，并已在 Tobii 软件中完成显示屏配置与用户眼动校准，且注视气泡能正常跟随视线。
4. **OptiKey 4.x 宿主程序**：
   - 从 [OptiKey GitHub Releases](https://github.com/OptiKey/OptiKey/releases) 安装最新稳定版本的 OptiKey。

> [!IMPORTANT]
> 插件直接动态绑定用户本地合法安装的 Tobii 运行时环境（`tobii_stream_engine.dll`）。您**不需要**编译任何源代码、下载厂商 SDK，亦无需手动复制任何 DLL 文件到系统目录中。

---

## 2. 安装方式 (Installation Methods)

### 方式 A：在线插件安装（推荐）

OptiKey 4.x 支持通过 GitHub 话题标签直接在线搜索并安装插件：

1. 启动 OptiKey。
2. 打开**管理控制台**（按键盘 `Alt + M` 或点击界面菜单图标）。
3. 切换至**指向与选择 (Pointing & Selecting)** 选项卡。
4. 点击**联机查找更多眼动仪选项 (Find more eye tracker options online)**。
5. 在列表中找到 **Tobii Eye Tracker 5 (ET5)** 并点击**安装 (Install)**。
6. 若提示需要重启，请重新启动 OptiKey。

### 方式 B：手动 ZIP 安装包安装（离线备用）

如果在离线或特定受限环境下使用：

1. 从 [GitHub Releases](https://github.com/yuanweize/OptiKey-ET5-Plugin/releases) 下载最新的 `OptiKey-ET5-Plugin-vX.Y.Z.zip` 发布包。
2. 对照 `SHA256SUMS.txt` 核验压缩包的哈希值。
3. 如果 OptiKey 正在运行，请先完全退出。
4. 将 ZIP 压缩包解压至用户眼动仪插件目录下的独立子文件夹中：
   ```text
   %APPDATA%\OptiKey\OptiKey\EyeTrackerPlugins\OptiKey-ET5-Plugin\
   ```
   *（注：OptiKey 会递归扫描 `%APPDATA%\OptiKey\OptiKey\EyeTrackerPlugins\` 目录；将其置于独立子文件夹便于版本管理并防止文件冲突）*。
5. 启动 OptiKey。

---

## 3. 在 OptiKey 中启用眼动仪 (First-Time Setup)

1. 在 OptiKey 中打开**管理控制台**（`Alt + M`）。
2. 进入**指向与选择 (Pointing & Selecting)** 选项卡。
3. 在**指向设备 (Pointing device)** 下拉菜单中选择：
   ```text
   Tobii Eye Tracker 5 (ET5)
   ```
4. 点击**确定 (OK)** 保存配置。
5. OptiKey 将立刻连接到您的 Eye Tracker 5 并开始实时追踪眼动注视点。

---

## 4. 多设备高级配置（可选）

对于绝大多数普通用户（只连接了一台眼动仪），插件默认自动完成识别与直连，零配置开箱即用。

如果您的系统连接了多台 Tobii 眼动设备，为防止误操作控制错误的设备，插件默认会主动拒绝静默连接候选 0。此时可通过以下配置文件显式指定设备：

```text
%APPDATA%\OptiKey-ET5-Plugin\et5-plugin.config
```

配置示例：
```ini
# 单设备时自动连接（默认：true）
AutomaticDeviceSelection=true

# 多设备连接时指定的设备索引（0 或 1 等）
PreferredDeviceIndex=0

# 回调泵策略：Polling（低开销轮询，默认）或 WaitAndProcess（阻塞等待）
CallbackStrategy=Polling
```

---

## 5. 后续排错与参考

- 如遇无法定位驱动、校准失效或按键偏移问题，请参阅 [故障排除指南 (Troubleshooting)](TROUBLESHOOTING.md)。
- 了解支持的显示器分辨率、Windows DPI 缩放及系统版本，请参阅 [兼容性列表 (Compatibility)](COMPATIBILITY.md)。
