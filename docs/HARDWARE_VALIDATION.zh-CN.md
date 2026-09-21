[English](HARDWARE_VALIDATION.md) | [简体中文](HARDWARE_VALIDATION.zh-CN.md)

# 硬件验证协议与发布门禁规范 (Hardware Validation Protocol)

## 概述
鉴于辅助技术用户（如 ALS/MND 渐冻症患者）依赖眼动追踪实现至关重要的沟通交流，代码库制定了严格的硬件实测验证流程与发布门禁规范。

---

## 硬件前置环境诊断工具
在安装插件或启动 OptiKey 之前，推荐先运行无侵入式硬件诊断工具排查系统环境：

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\HardwareDiagnostics\inventory-tobii-runtime.ps1
```

该脚本执行：
- 扫描标准安装目录、卸载注册表及 Windows 服务配置。
- 校验 COFF PE 二进制文件结构（`IMAGE_FILE_MACHINE_AMD64` 64 位校验）。
- 校验 Authenticode 数字签名及证书主题发布者（`Tobii AB`）。
- 探查导出函数符号能力表（`tobii_api_*`、`tobii_device_*`、`tobii_gaze_*`）。
- 严格隐私保证：硬件 URL、设备序列号及个人用户名均自动脱敏。

---

## 硬件实测验证检查清单

### 1. 物理环境与基线配置
- [ ] Windows 10 x64 物理真机测试
- [ ] Windows 11 x64 物理真机测试
- [ ] 正品 Tobii Eye Tracker 5 硬件牢固固定于显示器底部
- [ ] 官方 Tobii Experience 软件已安装并启动
- [ ] 完成 Tobii 屏幕尺寸设定与个人眼动校准档案

### 2. 运行时发现与安全校验
- [ ] 运行诊断脚本定位合法路径下的 `tobii_stream_engine.dll`
- [ ] 确认数字签名有效且发布者为 Tobii AB
- [ ] 确认 x64 PE 二进制架构无误
- [ ] 确认成功枚举出 ET5 设备硬件

### 3. 多 DPI 缩放与注视点坐标精度实测
- [ ] 分辨率 1920x1080 @ 100% 缩放：OptiKey 键盘四角及中心按键点击准确无偏漂
- [ ] 分辨率 1920x1080 @ 125% 缩放：视觉注视焦点与高亮按键完全吻合
- [ ] 分辨率 2560x1440 @ 150% 缩放：注视选择判定保持像素级精确
- [ ] 分辨率 3840x2160 (4K) @ 200% 缩放：高分屏下坐标空间无截断、无缩放漂移

### 4. 物理异常扰动与自愈恢复（弹性容错测试）
- [ ] **USB 物理热拔插测试**：
  - 在 OptiKey 正在运行打字时拔出 ET5 的 USB 连接线。
  - 验证 OptiKey 界面保持响应（无卡死、无闪退），平滑转入重连状态（`Reconnecting`）。
  - 重新插回 USB 线缆。
  - 验证注视点在 3 秒内自动恢复流转，无需重启 OptiKey。
- [ ] **电脑睡眠与休眠唤醒测试**：
  - 在插件运行状态下将 Windows 置于睡眠状态。
  - 唤醒电脑，验证插件能自动重新绑定并恢复注视追踪。
