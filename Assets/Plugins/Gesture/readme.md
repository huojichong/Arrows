# GestureManager 手势系统说明

## 一、概述

GestureManager 是一个**统一入口、可扩展、工程级**的手势识别模块，用于在 Unity 项目中同时支持：

* 单指手势

    * 点击（Click）
    * 拖动（Drag Begin / Drag / Drag End）
* 双指手势

    * 缩放（Pinch）
    * 旋转（Rotate）
    * 双指平移（Two-finger Pan）

该系统**不依赖 Collider**，基于数学计算与状态机实现，适合：

* Grid / Vector2Int 逻辑点击
* Arrow / Path / Spline 类玩法
* 地图 / 相机操作
* UI 与 3D 世界解耦的输入架构

---

## 二、设计目标

* **单一入口（Single Entry Point）**：外部系统只与 GestureManager 交互
* **手势互斥清晰**：双指优先，自动打断单指
* **职责分离**：输入采集、手势识别、业务逻辑完全解耦
* **高可扩展性**：可无侵入添加长按、三指等新手势

---

## 三、整体架构

```text
GestureManager
│
├── InputAdapter          // 跨平台输入采集（鼠标 / 触摸）
│
├── SingleFingerState     // 单指状态机（Click / Drag）
│       └── IGestureHandler（策略接口）
│
└── TwoFingerState        // 双指状态机（Pinch / Rotate / Pan）
        └── TwoFingerGestureContext（数据上下文）
```

---

## 四、核心模块说明

### 1. GestureManager（唯一入口）

职责：

* 每帧采集输入
* 判断当前是单指还是双指
* 调度对应状态机
* 保证手势互斥规则

关键规则：

* 当 `pointerCount >= 2`：

    * 双指生效
    * 单指状态被强制 Reset
* 当 `pointerCount == 1`：

    * 单指生效
    * 双指状态被 Reset

---

### 2. InputAdapter

职责：

* 屏蔽平台差异（Editor / PC / Mobile）
* 将输入统一抽象为 `PointerState`

特性：

* Editor / PC：使用 Mouse
* Mobile：使用 Touch
* 支持多指（fingerId）

---

### 3. SingleFingerState

职责：

* 识别单指点击与拖动
* 维护内部状态机：

```text
Idle → Pressed → Dragging → End
```

判定条件：

* Click：

    * 按下时间 < clickTimeThreshold
    * 移动距离 < dragDistanceThreshold
* Drag：

    * 屏幕位移超过 dragDistanceThreshold

识别结果通过 `IGestureHandler` 回调给业务层。

---

### 4. IGestureHandler（策略接口）

```csharp
public interface IGestureHandler
{
    void OnPointerDown(GestureContext ctx);
    void OnClick(GestureContext ctx);
    void OnDragBegin(GestureContext ctx);
    void OnDrag(GestureContext ctx);
    void OnDragEnd(GestureContext ctx);
}
```

说明：

* 每一种手势逻辑实现一个 Handler
* GestureManager 本身不包含任何业务逻辑
* 支持使用 CompositeGestureHandler 进行组合

---

### 5. CompositeGestureHandler

用途：

* 将多个 `IGestureHandler` 组合成一个
* 同一套输入驱动多种业务逻辑

示例：

* 点击选中 Grid
* 拖动绘制 Arrow

---

### 6. TwoFingerState

职责：

* 管理双指生命周期
* 计算几何关系变化

输出：`TwoFingerGestureContext`

包含信息：

* 起始 / 当前两指位置
* 起始 / 当前距离（用于缩放）
* 距离差值（deltaDistance）
* 角度变化（deltaAngle）
* 中心点（用于平移）

---

## 五、使用示例



### 1. 单指：Grid 点击 + 拖线

```csharp
gestureManager.SingleFingerHandler =
    new CompositeGestureHandler(
        new GridClickHandler(),
        new DragPathHandler()
    );

public class DragPathHandler : IGestureHandler
{
    public System.Action<Vector2Int, Vector2Int> OnDragPath;

    public void OnDrag(SingleFingerGestureContext ctx)
    {
        Vector2Int from = new Vector2Int(
            Mathf.RoundToInt(ctx.worldDown.x),
            Mathf.RoundToInt(ctx.worldDown.z)
        );

        Vector2Int to = new Vector2Int(
            Mathf.RoundToInt(ctx.worldCurrent.x),
            Mathf.RoundToInt(ctx.worldCurrent.z)
        );

        OnDragPath?.Invoke(from, to);
    }
    // override other functions.......
    
}


public class GridClickHandler : IGestureHandler
{
    public System.Action<Vector2Int> OnGridClicked;

    public void OnClick(SingleFingerGestureContext ctx)
    {
        Vector2Int grid = new Vector2Int(
            Mathf.RoundToInt(ctx.worldDown.x),
            Mathf.RoundToInt(ctx.worldDown.z)
        );

        OnGridClicked?.Invoke(grid);
    }

    // override other functions.......
}
```

---

### 2. 双指：相机控制

```csharp
gestureManager.OnTwoFingerUpdate += ctx =>
{
    camera.fieldOfView -= ctx.deltaDistance * 0.01f;
    camera.transform.Rotate(Vector3.up, ctx.deltaAngle);
};
```

---

## 六、推荐使用场景

* 基于 Vector2Int 的逻辑 Grid 点击
* Arrow / Snake / Path 拖动玩法
* 地图编辑器 / 关卡编辑器
* 相机缩放 / 旋转 / 平移
* 不希望给大量物体挂 Collider 的场景

---

## 七、扩展建议

该架构支持无侵入扩展：

* LongPressState（长按）
* ThreeFingerGesture（重置 / 特殊操作）
* 手势优先级与事件消费机制
* 编辑器下手势模拟

---

## 八、设计总结

> GestureManager 的核心价值不在于“识别了多少手势”，
> 而在于：
>
> **用一个统一调度器，把复杂的输入变化，转化为稳定、可组合的语义事件。**

这套实现可以直接用于正式项目，并且具备长期维护与扩展能力。
