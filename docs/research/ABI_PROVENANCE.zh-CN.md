[English](ABI_PROVENANCE.md) | [简体中文](ABI_PROVENANCE.zh-CN.md)

# Tobii Stream Engine 原生 ABI 源流考据 (ABI Provenance)

## 概述与规范声明
本代码库绝不包含任何 Tobii 专有头文件或二进制 SDK。下表客观记录了托管 C# 互操作声明及其技术对应关系。

| 原生函数符号 | 原生 C 签名 | 托管 C# 签名 | 调用约定 | 用途分类 |
| :--- | :--- | :--- | :--- | :--- |
| `tobii_api_create` | `tobii_error_t tobii_api_create(tobii_api_t**, ...)` | `tobii_error_t(out IntPtr, IntPtr, IntPtr)` | Cdecl | API 上下文全局初始化 |
| `tobii_api_destroy` | `tobii_error_t tobii_api_destroy(tobii_api_t*)` | `tobii_error_t(IntPtr)` | Cdecl | API 上下文析构 |
| `tobii_enumerate_local_device_urls` | `tobii_error_t tobii_enumerate_local_device_urls(...)` | `tobii_error_t(IntPtr, receiver, IntPtr)` | Cdecl | 本机设备硬件枚举 |
| `tobii_device_create` | `tobii_error_t tobii_device_create(...)` | `tobii_error_t(IntPtr, string, enum, out IntPtr)` | Cdecl | 建立硬件设备会话 |
| `tobii_device_destroy` | `tobii_error_t tobii_device_destroy(tobii_device_t*)` | `tobii_error_t(IntPtr)` | Cdecl | 销毁硬件设备会话 |
| `tobii_device_reconnect` | `tobii_error_t tobii_device_reconnect(tobii_device_t*)` | `tobii_error_t(IntPtr)` | Cdecl | 硬件底层重连 |
| `tobii_wait_for_callbacks` | `tobii_error_t tobii_wait_for_callbacks(size_t, ...)` | `tobii_error_t(IntPtr, IntPtr[])` | Cdecl | 阻塞等待原生事件回调 |
| `tobii_device_process_callbacks` | `tobii_error_t tobii_device_process_callbacks(...)` | `tobii_error_t(IntPtr)` | Cdecl | 处理并派发排队的回调事件 |
| `tobii_gaze_point_subscribe` | `tobii_error_t tobii_gaze_point_subscribe(...)` | `tobii_error_t(IntPtr, callback, IntPtr)` | Cdecl | 订阅高频注视点数据流 |
| `tobii_gaze_point_unsubscribe` | `tobii_error_t tobii_gaze_point_unsubscribe(...)` | `tobii_error_t(IntPtr)` | Cdecl | 取消注视点流订阅 |
| `tobii_error_message` | `char const* tobii_error_message(tobii_error_t)` | `IntPtr(tobii_error_t)` | Cdecl | 获取底层错误文本描述 |

---

## 结构体对齐与数据类型定义

- **调用约定**：所有托管委托显式声明为 `CallingConvention.Cdecl`。
- **枚举整型宽度**：采用 C# 默认 32 位有符号整型。
- **注视点结构体 (`tobii_gaze_point_t`)**：
  采用 `LayoutKind.Sequential` 顺序布局，字段包含 `timestamp_us` (Int64)、`validity` (enum 32位)、`position_x` (Single 32位单精度浮点) 与 `position_y` (Single 32位单精度浮点)。
- **洁净室开发保证**：代码完全为自主编写，不依赖也不侵犯任何第三方闭源专有代码版权。
