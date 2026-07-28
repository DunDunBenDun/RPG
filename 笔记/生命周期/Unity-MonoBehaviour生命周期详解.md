# Unity MonoBehaviour 生命周期详解

> 适用基准：Unity 2022.3 LTS。本项目当前编辑器版本为 2022.3.62f3c1。

## 1. 什么是 MonoBehaviour 生命周期

挂在 GameObject 上的脚本通常继承自 `MonoBehaviour`。Unity 会在特定时机自动调用脚本中名称和签名匹配的消息方法，例如 `Awake`、`Start`、`Update` 和 `OnDestroy`。这些方法不是由我们手动调用的普通业务方法，而是 Unity 驱动脚本运行的入口。

最常用的运行顺序可以先记成：

```text
对象加载或创建
    ↓
Awake                     每个实例通常一次
    ↓
OnEnable                  每次变为“启用且激活”时
    ↓
Start                     第一次 Update 前一次
    ↓
┌── 每个物理时间步：FixedUpdate（每帧可能 0 次、1 次或多次）
│
├── 每个渲染帧：Update（通常 1 次）
│
└── 每个渲染帧：LateUpdate（在 Update 之后，通常 1 次）
    ↓
OnDisable                 每次不再“启用且激活”时
    ↓
OnDestroy                 实例被销毁时
```

这里的“通常一次”是针对单个组件实例而言。销毁对象后重新创建的是新实例，会重新经历整个过程。

## 2. 初始化阶段

### 2.1 Reset：添加或重置组件时设置默认值

```csharp
private void Reset()
{
    moveSpeed = 5f;
}
```

`Reset` 主要在编辑器中执行：

- 第一次把组件添加到 GameObject 时调用。
- 在 Inspector 中对组件执行 **Reset** 时调用。
- 适合填写合理的编辑器默认值或自动寻找同一对象上的组件。
- 不应依赖它完成运行时初始化，因为构建后的正常运行流程不会把它当作启动方法。

### 2.2 OnValidate：Inspector 数据发生变化时校验

```csharp
private void OnValidate()
{
    moveSpeed = Mathf.Max(0f, moveSpeed);
}
```

`OnValidate` 是编辑器校验回调，脚本加载或 Inspector 中的序列化值变化时可能被调用，而且调用次数可能很多。

适合：

- 限制 Inspector 参数范围。
- 在编辑器中尽早报告错误配置。
- 更新轻量、可重复计算的派生数据。

不适合：

- 编写只应执行一次的初始化逻辑。
- 创建或销毁大量资源。
- 假定它一定在主线程或按固定次数执行。
- 把它当成运行时存档或业务事件。

### 2.3 Awake：实例自身的最早运行时初始化

```csharp
private Rigidbody rb;

private void Awake()
{
    rb = GetComponent<Rigidbody>();
}
```

`Awake` 的核心特点：

- 一个组件实例通常只调用一次。
- 场景加载或运行时实例化对象时，由 Unity 调用。
- 序列化字段已经反序列化，因此可以读取 Inspector 中配置的值。
- 适合建立组件自身不依赖外部执行顺序的内部状态，例如缓存同一对象上的组件。
- 活跃 GameObject 上的脚本即使组件未勾选启用，`Awake` 也可能先完成；处于非激活层级中的对象通常要等到首次激活时才调用。

不要依赖不同 GameObject 之间的 `Awake` 先后顺序。对象 A 的 `Awake` 直接读取对象 B 在 `Awake` 中才赋好的值，很容易产生时序问题。

### 2.4 OnEnable：每次进入可运行状态

```csharp
private void OnEnable()
{
    GameEvents.PlayerDied += HandlePlayerDied;
}

private void OnDisable()
{
    GameEvents.PlayerDied -= HandlePlayerDied;
}
```

只有同时满足以下条件，组件才是“启用且激活”的：

1. 组件的 `enabled` 为 `true`。
2. 组件所在 GameObject 的 `activeInHierarchy` 为 `true`。

进入这个状态时调用 `OnEnable`。所以它不止执行一次：组件重新勾选、GameObject 重新激活、对象池中的对象再次取出，都会再次触发。

适合：

- 订阅事件，并在 `OnDisable` 中成对取消订阅。
- 注册到管理器，并在禁用时注销。
- 启动每次启用都需要重新运行的行为。

`OnEnable` 不能写成协程；需要协程时，在普通 `OnEnable` 中调用 `StartCoroutine`。

### 2.5 Start：第一次参与逐帧运行前初始化

```csharp
private void Start()
{
    target = GameObject.FindWithTag("Player").transform;
}
```

`Start` 的核心特点：

- 对单个组件实例最多调用一次。
- 只会在组件处于启用状态时，于它第一次执行 `Update` 之前调用。
- 如果组件开始时被禁用，`Start` 会延迟到它首次启用后再执行。
- 组件禁用再启用不会重复执行 `Start`，但会重复执行 `OnEnable`。
- 可以写成 `IEnumerator Start()`，让它成为协程；此时 Unity 会启动该协程。

适合放置依赖“场景中其他初始对象已经完成 `Awake`”的初始化。但运行中动态创建对象时，仍不要假定所有对象之间存在固定的 `Start` 顺序。

## 3. Awake、OnEnable、Start 到底怎么选

| 回调 | 单实例次数 | 典型用途 | 关键限制 |
| --- | ---: | --- | --- |
| `Awake` | 1 | 缓存自身组件、建立内部状态 | 不依赖其他对象的 `Awake` 顺序 |
| `OnEnable` | 多次 | 订阅事件、注册服务、恢复每次启用的状态 | 必须考虑重复调用，并与 `OnDisable` 成对 |
| `Start` | 1 | 需要等初始场景对象完成 `Awake` 后的初始化 | 组件未启用时不会执行 |

常用判断：

```text
只属于这个实例、越早准备越好？        → Awake
每次启用都必须重新执行？              → OnEnable
只执行一次，且要晚于初始对象的 Awake？ → Start
```

不要把所有初始化机械地塞进 `Start`。明确数据归属和依赖关系，生命周期才不容易失控。

## 4. 游戏循环阶段

### 4.1 FixedUpdate：固定时间步的物理逻辑

```csharp
private void FixedUpdate()
{
    rb.AddForce(moveDirection * moveForce, ForceMode.Force);
}
```

`FixedUpdate` 按固定的模拟时间步运行，默认 `Time.fixedDeltaTime` 通常为 `0.02` 秒，即目标频率 50 次/秒。

它与画面帧率不是一一对应关系：

- 某个渲染帧很快时，可能一次 `FixedUpdate` 都没有。
- 某个渲染帧耗时较长时，Unity 可能连续执行多次 `FixedUpdate` 追赶物理时间。
- 因此不能假定“每次 `Update` 前恰好有一次 `FixedUpdate`”。

适合：

- 对非运动学 Rigidbody 施加力或扭矩。
- 与物理模拟时间步一致的控制逻辑。
- 需要使用 `Time.fixedDeltaTime` 的固定步长计算。

不适合：

- 只按渲染帧更新的 UI、镜头和普通视觉效果。
- 直接捕获只持续一帧的输入事件。更稳妥的做法是在 `Update` 或输入回调中记录输入，再在 `FixedUpdate` 中应用物理操作。

注意：`FixedUpdate` 是固定模拟步长，不代表真实调用间隔在 CPU 时间上绝对稳定。

### 4.2 Update：每个渲染帧的主要游戏逻辑

```csharp
private void Update()
{
    float horizontal = Input.GetAxisRaw("Horizontal");
    transform.Rotate(0f, horizontal * turnSpeed * Time.deltaTime, 0f);
}
```

`Update` 在组件启用且激活时，通常每个渲染帧调用一次。帧率越高，单位时间内调用次数越多。

适合：

- 读取输入。
- 非物理角色或对象的逐帧逻辑。
- 计时器、状态机、技能冷却。
- UI 状态和一般动画参数更新。

基于“每秒速度”的逻辑通常需要乘 `Time.deltaTime`：

```csharp
transform.position += direction * speed * Time.deltaTime;
```

否则对象会变成“每帧移动固定距离”，速度随帧率变化。

并非所有数值都要乘 `deltaTime`：已经表示本帧位移的值、鼠标本帧增量，或者使用内部已处理时间的 API，应根据语义判断，不能机械相乘。

### 4.3 LateUpdate：跟随 Update 之后收尾

```csharp
private void LateUpdate()
{
    transform.position = target.position + offset;
}
```

`LateUpdate` 在本帧所有常规 `Update` 之后执行，常用于：

- 摄像机跟随已经移动完毕的玩家。
- 根据角色最终姿态修正挂点或 UI。
- 在其他逐帧逻辑完成后做收尾计算。

虽然所有 `LateUpdate` 都晚于常规 `Update` 阶段，但不同对象的多个 `LateUpdate` 之间仍没有默认的可靠先后保证。

### 4.4 OnGUI：IMGUI 事件，不等同于每帧一次

`OnGUI` 属于 Unity 的即时模式 GUI 事件。一个渲染帧中可能因为 `Layout`、`Repaint` 和输入事件被调用多次，不应把普通游戏逻辑或“每帧只加一次”的计数放进去。

运行时正式 UI 通常使用 uGUI 或 UI Toolkit；`OnGUI` 更常见于调试界面和编辑器工具。

### 4.5 渲染相关回调

`OnPreCull`、`OnPreRender`、`OnPostRender`、`OnRenderObject` 等回调与相机和渲染过程有关，可能因多个相机而在一帧中调用多次。

经典内置渲染管线与 URP/HDRP 的可用回调并不完全相同。本项目使用 URP 时，定制渲染逻辑应优先考虑 URP 的 Renderer Feature、Render Pass 或 `RenderPipelineManager` 事件，不要默认所有内置管线回调都会按相同方式触发。

## 5. 暂停、焦点和应用退出

### 5.1 OnApplicationFocus

```csharp
private void OnApplicationFocus(bool hasFocus)
{
    // hasFocus 为 false：应用失去焦点
}
```

用于感知应用窗口是否获得焦点。编辑器、桌面端和移动端的具体触发行为可能不同，不要把它当成跨平台完全一致的暂停信号。

### 5.2 OnApplicationPause

```csharp
private void OnApplicationPause(bool pauseStatus)
{
    if (pauseStatus)
    {
        SaveGame();
    }
}
```

应用暂停状态变化时调用。在移动平台切到后台时尤其重要。Unity 通常在检测到暂停的那一帧末尾调用它，并可能再发出一帧用于显示暂停画面。

重要数据不要只依赖退出时保存；移动系统可能直接终止后台应用。进入暂停时保存通常更可靠。

### 5.3 OnApplicationQuit

应用正常退出前调用。它适合做轻量清理或最后保存，但不是所有平台和所有强制终止场景都保证触发。

在编辑器中停止 Play Mode 通常会触发相关退出流程，但编辑器行为不能代替目标平台测试。

## 6. 禁用与销毁阶段

### 6.1 OnDisable：离开可运行状态

以下情况通常会触发 `OnDisable`：

- 把组件的 `enabled` 设为 `false`。
- 对 GameObject 或其父对象调用 `SetActive(false)`。
- 对象即将销毁或场景卸载。
- 编辑器脚本重新编译或发生域重载。

适合：

- 取消在 `OnEnable` 中订阅的事件。
- 从管理器注销。
- 停止或释放只在启用期间拥有的资源。

`OnDisable` 和 `OnEnable` 都可能反复执行，所以两边逻辑应当可重复、成对且不泄漏订阅。

### 6.2 OnDestroy：实例最终销毁

```csharp
private void OnDestroy()
{
    ownedResource?.Dispose();
}
```

当组件或 GameObject 被销毁、场景卸载，或退出运行流程时，Unity 会对符合条件的实例调用 `OnDestroy`。Unity 文档特别说明：通常只有曾经处于激活状态的 GameObject 才能依赖收到 `OnDestroy`。

注意：

- `Destroy(obj)` 通常把实际销毁安排在当前 `Update` 循环结束后、渲染前完成，并非 C# 语句执行到这里就立刻彻底删除。
- 在调用 `Destroy` 后，不要继续把该对象用于后续业务。
- `OnDestroy` 不能写成协程。
- 需要在每次禁用时完成的清理应放在 `OnDisable`，不要只放在 `OnDestroy`。

`DestroyImmediate` 主要供谨慎的编辑器工具使用，运行时逻辑通常应使用 `Destroy`。

## 7. 启用状态对回调的影响

| 操作或状态 | 结果 |
| --- | --- |
| 激活对象，组件从禁用改为启用 | `OnEnable`；若从未执行过 `Start`，之后执行 `Start` |
| 激活对象，组件从启用改为禁用 | `OnDisable`，停止接收 `Update` 等常规更新 |
| GameObject 从激活改为非激活 | 其活跃组件收到 `OnDisable`，常规更新停止 |
| GameObject 从非激活改为激活 | 尚未初始化的组件先 `Awake`，启用组件再 `OnEnable`，需要时再 `Start` |
| 组件禁用后重新启用 | 再次 `OnEnable`，但不会再次 `Awake` 或 `Start` |
| 对象销毁后重新实例化 Prefab | 新实例重新经历 `Awake → OnEnable → Start` |

`activeSelf` 表示对象自己设置的激活值；`activeInHierarchy` 表示考虑父节点以后在层级中是否真正激活。判断组件是否能运行时，通常应关注后者以及组件的 `enabled`。

## 8. 协程与生命周期

协程不是独立线程。协程代码仍主要在 Unity 主线程上分段执行，`yield` 只是把后续执行推迟到指定时机。

```csharp
private IEnumerator Flash()
{
    spriteRenderer.enabled = true;
    yield return new WaitForSeconds(0.2f);
    spriteRenderer.enabled = false;
}
```

常见等待方式：

| 写法 | 继续执行的大致时机 |
| --- | --- |
| `yield return null` | 下一帧 |
| `yield return new WaitForFixedUpdate()` | 下一个固定更新阶段附近 |
| `yield return new WaitForEndOfFrame()` | 本帧相机和 GUI 渲染完成后、显示画面前 |
| `yield return new WaitForSeconds(t)` | 经过约 `t` 秒缩放时间后的第一个可执行帧 |
| `yield return new WaitForSecondsRealtime(t)` | 经过约 `t` 秒非缩放时间后的第一个可执行帧 |
| `yield return anotherCoroutine` | 等待另一个协程结束 |

生命周期相关规则：

- `SetActive(false)` 会停止挂在该 GameObject 上的协程；对象重新激活时，已停止的协程不会从原位置自动恢复。
- 仅把 `MonoBehaviour.enabled` 设为 `false`，不会自动停止已经启动的协程。
- 销毁组件或 GameObject 会停止相关协程。
- 也可以使用 `StopCoroutine` 或 `StopAllCoroutines` 主动停止。
- `WaitForSeconds` 受 `Time.timeScale` 影响，且等待时长不是精确线程定时；它会在满足时间后的某个主循环帧继续。

## 9. 常见物理消息

`OnCollisionEnter/Stay/Exit`、`OnTriggerEnter/Stay/Exit` 等由物理模拟结果驱动。它们不是 `Update` 的替代品，也不应假定与每个渲染帧一一对应。

2D 和 3D 物理系统使用不同的组件与回调：

- 3D：`Rigidbody`、`Collider`、`OnCollisionEnter`、`OnTriggerEnter`。
- 2D：`Rigidbody2D`、`Collider2D`、`OnCollisionEnter2D`、`OnTriggerEnter2D`。

不要混用 2D 与 3D 回调签名。

## 10. 多个脚本之间的执行顺序

默认情况下，不要依赖同一阶段中不同对象或不同脚本的调用顺序。例如，不能假定 `Player.Update` 一定早于 `Enemy.Update`。

当顺序确实是业务约束时，可选择：

1. **显式控制流程**：由一个管理器按明确顺序调用普通 C# 方法，最容易阅读和测试。
2. **Project Settings > Script Execution Order**：调整不同 `MonoBehaviour` 类型的执行次序。
3. **`[DefaultExecutionOrder(order)]`**：在脚本类型上声明默认次序，数值越小越早。

示例：

```csharp
[DefaultExecutionOrder(-100)]
public class InputCollector : MonoBehaviour
{
}
```

执行顺序设置只调整对应生命周期阶段的相对顺序，并不能把 `LateUpdate` 提前到 `Update` 之前。不要为了修补混乱依赖而大量设置顺序；优先让依赖显式。

## 11. 完整示例：观察实际调用过程

把下面脚本挂到测试对象上，在 Play Mode 中启用、禁用和销毁对象，就能观察主要回调。日志脚本只用于学习或调试，不建议长期留在正式对象上。

```csharp
using UnityEngine;

public class LifecycleLogger : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log($"{name}: Awake", this);
    }

    private void OnEnable()
    {
        Debug.Log($"{name}: OnEnable", this);
    }

    private void Start()
    {
        Debug.Log($"{name}: Start", this);
    }

    private void FixedUpdate()
    {
        Debug.Log($"{name}: FixedUpdate", this);
    }

    private void Update()
    {
        Debug.Log($"{name}: Update", this);
    }

    private void LateUpdate()
    {
        Debug.Log($"{name}: LateUpdate", this);
    }

    private void OnDisable()
    {
        Debug.Log($"{name}: OnDisable", this);
    }

    private void OnDestroy()
    {
        Debug.Log($"{name}: OnDestroy", this);
    }
}
```

`FixedUpdate`、`Update` 和 `LateUpdate` 每秒会产生大量日志，观察完应注释掉或移除，避免 Console 日志本身影响性能和时序。

## 12. 推荐的角色脚本职责分配

```csharp
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveForce = 20f;

    private Rigidbody rb;
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical"));
    }

    private void FixedUpdate()
    {
        Vector3 force = new Vector3(moveInput.x, 0f, moveInput.y) * moveForce;
        rb.AddForce(force, ForceMode.Force);
    }
}
```

这个例子展示了常见分工：

- `Awake`：缓存自身依赖。
- `Update`：采集逐帧输入。
- `FixedUpdate`：把缓存的输入应用到物理系统。

实际项目如果使用新版 Input System，可以用输入事件更新 `moveInput`，物理应用仍放在 `FixedUpdate`。

## 13. 常见错误与排查方式

### 错误 1：认为 Start 会在每次启用时执行

现象：对象池中的对象第一次正常，第二次启用时没有重置。

处理：每次启用都需要的重置放在 `OnEnable`；只需要一次的初始化留在 `Awake` 或 `Start`。

### 错误 2：依赖两个对象的 Awake 顺序

现象：偶尔出现空引用，调整层级后行为变化。

处理：通过 Inspector 引用、显式初始化入口或统一启动器表达依赖；不要靠层级排列猜测顺序。

### 错误 3：在 Update 中直接做非运动学 Rigidbody 的物理控制

现象：不同帧率下物理表现不同或产生抖动。

处理：在 `Update` 采集输入，在 `FixedUpdate` 施加力、扭矩或执行物理移动。

### 错误 4：忘记取消事件订阅

现象：禁用对象仍响应事件、重复响应、对象无法被回收，或者出现 MissingReferenceException。

处理：通常在 `OnEnable` 订阅，在 `OnDisable` 对称取消订阅。

### 错误 5：在 Update 中制造大量临时分配

现象：Profiler 中频繁出现 GC Alloc，游戏周期性卡顿。

处理：避免每帧无必要的 LINQ、字符串拼接和容器创建；用 Profiler 确认后再优化。

### 错误 6：把协程当后台线程

现象：协程中的重计算仍然卡住主线程。

处理：协程只负责分帧调度。CPU 密集工作需要合理拆分，或使用 Job System、Burst、任务线程；Unity 对象 API 通常仍只能在主线程访问。

### 错误 7：编辑器关闭域重载后静态状态残留

Unity 的 **Enter Play Mode Options** 可以关闭 Domain Reload 来加快进入 Play Mode。关闭后，静态字段和静态事件订阅可能不会在每次进入 Play Mode 时自动恢复初始状态。

处理：为静态状态提供明确重置逻辑，例如使用：

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void ResetStatics()
{
    instance = null;
}
```

## 14. 一页速查表

| 时机 | 方法 | 频率 | 最常见职责 |
| --- | --- | --- | --- |
| 编辑器添加/重置组件 | `Reset` | 按操作 | 默认值 |
| 编辑器序列化值变化 | `OnValidate` | 多次且不固定 | 参数校验 |
| 实例加载 | `Awake` | 每实例一次 | 自身初始化、缓存组件 |
| 进入启用且激活状态 | `OnEnable` | 可多次 | 订阅、注册、每次启用重置 |
| 首次更新前 | `Start` | 每实例一次 | 较晚的一次性初始化 |
| 固定物理时间步 | `FixedUpdate` | 每渲染帧 0～多次 | 物理控制 |
| 渲染帧主更新 | `Update` | 通常每帧一次 | 输入、状态、非物理逻辑 |
| Update 之后 | `LateUpdate` | 通常每帧一次 | 相机、跟随、收尾 |
| 离开启用且激活状态 | `OnDisable` | 可多次 | 取消订阅、注销、暂停行为 |
| 实例销毁 | `OnDestroy` | 每实例至多一次 | 最终清理 |
| 应用焦点变化 | `OnApplicationFocus` | 按事件 | 焦点处理 |
| 应用暂停变化 | `OnApplicationPause` | 按事件 | 暂停与保存 |
| 应用正常退出 | `OnApplicationQuit` | 通常一次 | 退出处理，不可视为绝对保证 |

## 15. 最重要的记忆原则

1. `Awake` 管“这个实例自己的一次性准备”。
2. `OnEnable` / `OnDisable` 管“每次启用期间拥有的东西”。
3. `Start` 管“第一次更新前、相对较晚的一次性准备”。
4. `Update` 管“逐渲染帧逻辑”，移动速度通常结合 `Time.deltaTime`。
5. `FixedUpdate` 管“固定时间步物理”，它和渲染帧不是一对一。
6. `LateUpdate` 管“等普通更新做完再跟随或收尾”。
7. 不依赖不同对象之间默认的回调先后顺序。
8. 禁用会重复发生，销毁才是实例终点；事件订阅通常在启用/禁用之间成对管理。

