[English](THIRD_PARTY_NOTICES.md) | [简体中文](THIRD_PARTY_NOTICES.zh-CN.md)

# 第三方软件声明与致谢 (Third-Party Notices)

本项目引用并基于以下开源项目与社区技术规范：

## 1. OptiKey
- **版权声明**：Copyright (c) 2014-2026 Julius Sweetland, OPTIKEY LTD
- **许可证**：GNU General Public License v3.0-only (GPL-3.0-only)
- **官方网站**：https://github.com/OptiKey/OptiKey
- **用途**：提供插件契约接口规范（`JuliusSweetland.OptiKey.Contracts.dll`）。

## 2. .NET Reactive Extensions (System.Reactive)
- **版权声明**：.NET Foundation and Contributors
- **许可证**：MIT License / Apache License 2.0
- **官方网站**：https://github.com/dotnet/reactive
- **用途**：编译期契约类型引用（`Timestamped<T>`）；由于 OptiKey 宿主自带 Rx 运行时，发布包中无需重复打包分发 Rx 程序集。

## 3. Tobii Stream Engine 原生规范
- **版权声明**：Tobii AB
- **官方网站**：https://developer.tobii.com/
- **说明**：基于公开文档的 C API 签名定义，用于运行时动态绑定。本项目源码与分发包中绝对不包含任何 Tobii 专有 DLL 二进制文件或受版权保护的原生头文件。
