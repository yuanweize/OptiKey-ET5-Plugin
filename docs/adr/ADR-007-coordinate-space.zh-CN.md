[English](ADR-007-coordinate-space.md) | [简体中文](ADR-007-coordinate-space.zh-CN.md)

# ADR-007: 坐标空间语义与映射流水线 (Coordinate Space Semantics and Mapping Pipeline)

## 状态 (Status)
已采纳 (Accepted)

## 上下文 (Context)
在眼动追踪辅助软件中，在高 DPI 显示器（如 100%、125%、150%、200% Windows 缩放）和多显示器配置下，坐标空间不匹配是致命缺陷的常见根源：
- 在 100% DPI 下，注视点坐标准确；
- 在 125% 或 150% DPI 下，注视点产生偏移或完全无法命中按键；
- 在多显示器环境中，点位可能会被错误裁剪到主屏幕或错误映射到虚拟桌面范围。

为确保像素级精度，必须严格追踪并规定两端的坐标语义：
1. **输入端**：Tobii Stream Engine 坐标输出。
2. **接收端**：OptiKey 内部碰撞测试与注视点消费管线。

## 坐标语义的完整梳理

### 端点 A：Tobii Stream Engine 输出语义
Tobii Stream Engine 通过 `tobii_gaze_point_t` 传递注视数据：
```c
typedef struct tobii_gaze_point_t {
    int64_t timestamp_us;
    tobii_validity_t validity;
    float position[2]; // position[0] = x, position[1] = y
} tobii_gaze_point_t;
```
- **取值范围**：`position[0]` ($X$) 与 `position[1]` ($Y$) 是 `[0.0, 1.0]` 范围内的归一化 2D 浮点数值。
- **坐标原点**：`(0.0, 0.0)` 为校准显示平面的左上角；`(1.0, 1.0)` 为右下角。
- **参考系**：Tobii Eye Tracker 5 专门针对单个物理显示面板（安装硬件的主显示器）进行校准，不会自动跨越整个多显示器虚拟桌面。

### 端点 B：OptiKey 消费语义
OptiKey 如何消费 `IPointService.Point` 发出的 `System.Windows.Point`？
1. 在 `src/JuliusSweetland.OptiKey.Core/Observables/PointSources/PointServiceSource.cs`：
   ```csharp
   sequence = Observable.FromEventPattern<Timestamped<Point>>(
           eh => pointGeneratingService.Point += eh,
           eh => pointGeneratingService.Point -= eh)
       .Select(ep => kalmanFilter.Update(ep.EventArgs.Value))
       .Select(tp => tp.Value.ToPointAndKeyValue(PointToKeyValueMap))
   ```
2. 在 `src/JuliusSweetland.OptiKey.Core/UI/Controls/KeyboardHost.cs`：
   ```csharp
   var rect = new Rect
   {
       Location = key.PointToScreen(topLeftPoint),
       Size = (Size)key.GetTransformToDevice().Transform((Vector)key.RenderSize)
   };
   pointToKeyValueMap.Add(rect, key.Value);
   ```
   - 在 WPF 中，`Visual.PointToScreen(Point)` 将视觉坐标转换为**设备物理屏幕像素**。
   - `GetTransformToDevice().Transform(RenderSize)` 将逻辑 WPF 单位（96 DPI）乘以 DPI 缩放比例（例如 120 DPI 下为 1.25x），得到**物理像素尺寸**。
3. 在 `src/JuliusSweetland.OptiKey.Core/Observables/PointSources/MousePositionSource.cs`：
   ```csharp
   new Point(Cursor.Position.X, Cursor.Position.Y)
   ```
   `System.Windows.Forms.Cursor.Position` 返回 Win32 物理屏幕坐标。
4. 在上游历史 `TobiiPointService.cs` 中：
   ```csharp
   new Point(Graphics.PrimaryScreenWidthInPixels * gazePoint.position.x,
             Graphics.PrimaryScreenHeightInPixels * gazePoint.position.y)
   ```

**结论**：OptiKey 的 `IPointService.Point` 必须以眼动仪校准显示器（默认为主显示器）的**物理屏幕像素 (Physical Screen Pixels)** 发射。

## 转换流水线 (Transformation Pipeline)
```
+--------------------------------------------------------------------+
|                Tobii Stream Engine (硬件/驱动)                     |
| 归一化坐标: (normX, normY) 在 [0.0, 1.0] 范围内                   |
+--------------------------------------------------------------------+
                                  |
                                  | 本地回调 (tobii_gaze_point_t)
                                  v
+--------------------------------------------------------------------+
|               插件: ScreenCoordinateMapper                         |
| 1. 合法性检查: validity == TOBII_VALIDITY_VALID                   |
| 2. 截断限制: 将 normX, normY 截断在 [0.0, 1.0] 之间                |
| 3. 分辨率查询: Win32 GetSystemMetrics 或 DisplayMetrics            |
|    - PhysicalWidth = GetSystemMetrics(SM_CXSCREEN)                 |
|    - PhysicalHeight = GetSystemMetrics(SM_CYSCREEN)                |
|    - DisplayBounds = (Left, Top, Width, Height)                    |
| 4. 映射计算:                                                       |
|    pixelX = DisplayBounds.Left + (normX * DisplayBounds.Width)     |
|    pixelY = DisplayBounds.Top  + (normY * DisplayBounds.Height)    |
+--------------------------------------------------------------------+
                                  |
                                  | 以 Timestamped<Point> 形式发射
                                  v
+--------------------------------------------------------------------+
|                OptiKey: PointToKeyValueMap                         |
| 与 key.PointToScreen() (物理屏幕像素) 进行碰撞测试                 |
+--------------------------------------------------------------------+
```

## 高 DPI 与多显示器策略
1. **DPI 感知**：
   因为映射器直接使用目标显示器的物理像素尺寸，所以无论在 100%、125%、150% 还是 200% DPI 设置下，映射均保持不变，无需手动计算 DPI 缩放系数。
2. **目标显示器选择**：
   在单显示器环境中，目标为主显示器（`(0, 0, SM_CXSCREEN, SM_CYSCREEN)`）。
   在多显示器环境中，Tobii ET5 安装在特定屏幕（通常为主屏）上，插件默认为主屏。
3. **越界与无效数据处理**：
   `validity != TOBII_VALIDITY_VALID` 的样本（如闭眼或视线脱离）将被直接丢弃，不分发给 OptiKey，防止光标瞬移和异常抖动。

## 后果 (Consequences)
- 无论 Windows DPI 缩放比例如何设置，OptiKey 中的按键选择均能达到像素级准确度。
- 归一化传感器空间与宿主系统显示拓扑之间彻底解耦。
- 可通过覆盖多种显示分辨率和宽高比（1080p、1440p、4K、16:9、16:10、21:9）的测试矩阵进行确定性测试。
