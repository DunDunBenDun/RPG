# C# 进阶笔记（Unity 开发视角）

> 前置知识：掌握 C# 基础语法、类、接口和常用集合。
> 最后更新：2026-08-17。

说明：示例以解释概念为主，省略了部分 `using`、命名空间和组合代码。

## 目录

1. [值类型与引用类型](#1-值类型与引用类型)
2. [泛型](#2-泛型)
3. [委托与事件](#3-委托与事件)
4. [LINQ](#4-linq)
5. [异步与取消](#5-异步与取消)
6. [资源释放](#6-资源释放)
7. [接口与依赖传入](#7-接口与依赖传入)
8. [相等比较](#8-相等比较)
9. [Unity 中的性能与生命周期](#9-unity-中的性能与生命周期)
10. [工程设计原则](#10-工程设计原则)

---

## 1. 值类型与引用类型

```csharp
public struct Stats
{
    public int Hp;
}

Stats first = new Stats { Hp = 100 };
Stats second = first;
second.Hp = 50;

Console.WriteLine(first.Hp);
```

解释：

- `struct` 是值类型，赋值时会复制当前数据。
- 修改 `second` 不会影响 `first`，所以输出 `100`。

```csharp
public class Player
{
    public int Hp;
}

Player first = new Player { Hp = 100 };
Player second = first;
second.Hp = 50;

Console.WriteLine(first.Hp);
```

解释：

- `class` 是引用类型。
- 两个变量引用同一个对象，所以修改 `second.Hp` 后输出 `50`。
- 设计数据结构时要明确需要复制数据，还是共享同一个对象。

---

## 2. 泛型

```csharp
public static T FirstOrDefault<T>(IList<T> values)
{
    if (values.Count == 0)
    {
        return default;
    }

    return values[0];
}
```

解释：

- `T` 表示调用时才确定的类型。
- 同一个方法可以处理 `List<int>`、`List<string>` 等不同集合。
- `default` 返回该类型的默认值。

可以添加约束：

```csharp
public static void Initialize<T>(T component)
    where T : Behaviour
{
    component.enabled = true;
}
```

解释：

- `where T : Behaviour` 要求 `T` 必须是 Unity `Behaviour` 或其子类。
- 因此方法内部可以安全使用 `enabled` 等成员。

---

## 3. 委托与事件

```csharp
public class Health
{
    public event Action<int> HealthChanged;

    private int currentHp = 100;

    public void TakeDamage(int amount)
    {
        currentHp -= amount;
        HealthChanged?.Invoke(currentHp);
    }
}
```

解释：

- `Action<int>` 表示接收一个整数、没有返回值的回调。
- `event` 限制外部只能订阅和取消订阅，不能直接触发事件。
- `?.Invoke` 在存在订阅者时发送新的生命值。

订阅和取消应成对：

```csharp
private void OnEnable()
{
    health.HealthChanged += HandleHealthChanged;
}

private void OnDisable()
{
    health.HealthChanged -= HandleHealthChanged;
}
```

解释：

- 对象启用时订阅，禁用时取消。
- 忘记取消可能造成重复回调、对象无法释放或访问已销毁对象。

---

## 4. LINQ

```csharp
List<int> values = new List<int> { 5, 2, 8, 1 };

List<int> result = values
    .Where(value => value >= 5)
    .OrderBy(value => value)
    .ToList();
```

解释：

- `Where` 保留大于等于 `5` 的元素。
- `OrderBy` 对结果排序。
- `ToList` 创建新的列表，最终内容是 `5、8`。
- LINQ 很适合非高频业务代码，但在 Unity 每帧热点中可能产生额外分配。

---

## 5. 异步与取消

```csharp
public async Task<string> LoadTextAsync(
    string path,
    CancellationToken cancellationToken)
{
    string text = await File.ReadAllTextAsync(path, cancellationToken);
    return text;
}
```

解释：

- `async` 方法可以在等待操作完成时暂时返回控制权。
- `await` 等待文件读取，但不会写成阻塞式循环。
- `CancellationToken` 允许调用者请求取消。
- Unity API 通常只能在主线程访问，后台任务不能随意操作 GameObject。

调用并处理取消：

```csharp
using var source = new CancellationTokenSource();

try
{
    string text = await LoadTextAsync(path, source.Token);
}
catch (OperationCanceledException)
{
    Debug.Log("Load canceled");
}
```

解释：

- `CancellationTokenSource` 创建取消信号。
- 调用 `source.Cancel()` 后，支持取消的异步操作会结束并抛出取消异常。
- `using` 保证取消源最终被释放。

---

## 6. 资源释放

```csharp
public sealed class FileLogger : IDisposable
{
    private readonly StreamWriter writer;

    public FileLogger(string path)
    {
        writer = new StreamWriter(path);
    }

    public void Dispose()
    {
        writer.Dispose();
    }
}
```

解释：

- `IDisposable` 表示对象持有需要明确释放的资源。
- `Dispose` 关闭底层文件写入器。
- 文件、网络连接和原生句柄不应只等待垃圾回收。

---

## 7. 接口与依赖传入

```csharp
public interface IClock
{
    float Time { get; }
}

public sealed class CooldownService
{
    private readonly IClock clock;

    public CooldownService(IClock clock)
    {
        this.clock = clock;
    }
}
```

解释：

- `CooldownService` 依赖时间，但不直接依赖 Unity 的静态 `Time`。
- 外部把 `IClock` 实现传入构造函数。
- 测试时可以传入可控制时间的假实现。
- 依赖传入能减少全局单例和静态调用带来的耦合。

---

## 8. 相等比较

```csharp
public sealed class ItemId : IEquatable<ItemId>
{
    public int Value { get; }

    public ItemId(int value)
    {
        Value = value;
    }

    public bool Equals(ItemId other)
    {
        return other != null && Value == other.Value;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as ItemId);
    }

    public override int GetHashCode()
    {
        return Value;
    }
}
```

解释：

- 默认类比较通常关注是否为同一个对象。
- 实现 `IEquatable<T>` 后，可以按业务值比较。
- 重写 `Equals` 时也要提供一致的 `GetHashCode`，否则字典和集合可能表现异常。

---

## 9. Unity 中的性能与生命周期

缓存重复使用的组件引用：

```csharp
public sealed class PlayerView : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
}
```

解释：

- `Awake` 中查找并保存 `Animator`。
- 后续逻辑可以直接使用缓存引用。
- 不要在 `Update` 中反复执行相同的组件查找。

需要重点留意：

1. 每帧创建 List、lambda、字符串和 LINQ 结果。
2. 高频使用 `GetComponent`、`Find` 和字符串查找。
3. 事件只订阅不取消。
4. 协程、异步任务或回调在对象销毁后继续运行。
5. Unity 对象销毁后的特殊 `null` 行为。
6. 业务脚本直接操作 Animator 参数，造成参数名和规则分散。

---

## 10. 工程设计原则

1. 一个类只承担清晰职责。
2. 面向接口依赖，不直接依赖难以替换的具体实现。
3. Unity 对象引用通过 Inspector、组合根或明确工厂建立。
4. 逻辑层通过事件通知 UI，避免逻辑直接寻找界面对象。
5. 在系统边界验证文件、网络、玩家输入和外部数据。
6. 普通业务失败使用结果值，真正的程序错误尽早暴露。
7. 性能优化以前先使用 Profiler 找到真实热点。
8. 在 My RPG 中继续保持 `AnimCtrl → AnimatorManager ← StateMachine` 的动画边界。
