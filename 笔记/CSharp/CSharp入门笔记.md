# C# 入门笔记

> 面向读者：刚开始学习 C#，或希望系统补齐 Unity C# 基础。
> 最后更新：2026-08-17。

说明：大部分示例只展示一个知识点。创建对象和调用方法的语句，应放在 `Main`、Unity 生命周期函数或其他方法中执行。

## 目录

1. [第一个程序](#1-第一个程序)
2. [变量和类型](#2-变量和类型)
3. [字符串](#3-字符串)
4. [条件判断](#4-条件判断)
5. [循环](#5-循环)
6. [方法](#6-方法)
7. [数组和集合](#7-数组和集合)
8. [类和对象](#8-类和对象)
9. [属性](#9-属性)
10. [枚举和接口](#10-枚举和接口)
11. [空值和异常](#11-空值和异常)
12. [Unity 脚本基础](#12-unity-脚本基础)
13. [常见问题与练习](#13-常见问题与练习)

---

## 1. 第一个程序

```csharp
using System;

public static class Program
{
    public static void Main()
    {
        Console.WriteLine("Hello, C#");
    }
}
```

解释：

- `using System` 引入常用基础类型。
- `Program` 是一个类。
- `Main` 是普通控制台程序的入口。
- `Console.WriteLine` 输出一行文字。
- Unity 脚本通常不写 `Main`，入口由 Unity 生命周期提供。

---

## 2. 变量和类型

```csharp
string playerName = "Knight";
int hp = 100;
float speed = 5.5f;
bool isAlive = true;
```

解释：

- `string` 保存文本。
- `int` 保存整数。
- `float` 保存小数，数字末尾通常写 `f`。
- `bool` 只能是 `true` 或 `false`。
- C# 变量有明确类型，不能随意换成另一种类型。

可以让编译器推断局部变量类型：

```csharp
var level = 2;
var title = "Warrior";
```

解释：

- `var` 不是动态类型。
- 编译器会推断 `level` 是 `int`，`title` 是 `string`。
- 推断完成后，变量类型仍然固定。

---

## 3. 字符串

```csharp
string name = "Knight";
int hp = 80;

string message = $"{name} 的生命值是 {hp}";
Console.WriteLine(message);
```

解释：

- 字符串前面的 `$` 允许在 `{ }` 中插入变量。
- 最终输出 `Knight 的生命值是 80`。
- 这种写法称为字符串插值。

---

## 4. 条件判断

```csharp
int hp = 20;
string state;

if (hp <= 0)
{
    state = "Dead";
}
else if (hp < 30)
{
    state = "Danger";
}
else
{
    state = "Normal";
}

Console.WriteLine(state);
```

解释：

- 条件写在小括号中。
- 条件成立时执行对应大括号里的代码。
- 当前 `hp` 是 `20`，所以输出 `Danger`。

适合多个固定选项时，可以使用 `switch`：

```csharp
int itemType = 2;

switch (itemType)
{
    case 1:
        Console.WriteLine("Weapon");
        break;
    case 2:
        Console.WriteLine("Potion");
        break;
    default:
        Console.WriteLine("Unknown");
        break;
}
```

解释：

- `switch` 根据 `itemType` 选择一个分支。
- 当前值是 `2`，所以输出 `Potion`。
- `break` 表示结束当前分支。

---

## 5. 循环

```csharp
for (int i = 0; i < 3; i++)
{
    Console.WriteLine(i);
}
```

解释：

- `i` 从 `0` 开始。
- 只要 `i < 3` 就继续循环。
- 每次循环后执行 `i++`，所以输出 `0`、`1`、`2`。

```csharp
int count = 3;

while (count > 0)
{
    Console.WriteLine(count);
    count--;
}
```

解释：

- `while` 会在条件成立时重复执行。
- 每次循环让 `count` 减少 `1`。
- 最终输出 `3`、`2`、`1`。

---

## 6. 方法

```csharp
public static int Add(int left, int right)
{
    return left + right;
}

int result = Add(10, 20);
Console.WriteLine(result);
```

解释：

- `int` 表示方法会返回一个整数。
- `left` 和 `right` 是参数。
- `return` 把计算结果返回给调用者。
- `Add(10, 20)` 得到 `30`。

不返回结果的方法使用 `void`：

```csharp
public static void PrintName(string name)
{
    Console.WriteLine(name);
}

PrintName("Knight");
```

解释：

- `void` 表示方法没有返回值。
- 调用时把 `"Knight"` 传给参数 `name`。

---

## 7. 数组和集合

### 7.1 数组

```csharp
string[] items = { "Sword", "Potion", "Key" };

Console.WriteLine(items[0]);
Console.WriteLine(items.Length);
```

解释：

- C# 数组从 `0` 开始，所以 `items[0]` 是 `Sword`。
- `Length` 返回数组长度 `3`。
- 数组创建后长度固定。

### 7.2 List

```csharp
var items = new List<string>();

items.Add("Sword");
items.Add("Potion");
items.Remove("Sword");

Console.WriteLine(items[0]);
```

解释：

- `List<string>` 是可变长度的字符串列表。
- `Add` 添加元素，`Remove` 删除指定元素。
- 删除 `Sword` 后，第一个元素是 `Potion`。
- 使用 `List<T>` 需要引入 `System.Collections.Generic`。

### 7.3 Dictionary

```csharp
var itemNames = new Dictionary<int, string>
{
    [1001] = "Sword",
    [1002] = "Potion"
};

Console.WriteLine(itemNames[1002]);
```

解释：

- `Dictionary<int, string>` 使用整数作为键、字符串作为值。
- 通过键 `1002` 可以快速找到 `Potion`。

遍历集合：

```csharp
foreach (string item in items)
{
    Console.WriteLine(item);
}
```

解释：

- `foreach` 会依次取得集合中的每个元素。
- `item` 是当前元素。

---

## 8. 类和对象

```csharp
public class Player
{
    public string Name;
    public int Hp;

    public Player(string name, int hp)
    {
        Name = name;
        Hp = hp;
    }

    public void TakeDamage(int amount)
    {
        Hp -= amount;
    }
}

Player hero = new Player("Knight", 100);
hero.TakeDamage(20);

Console.WriteLine(hero.Hp);
```

解释：

- `Player` 类描述玩家有哪些数据和行为。
- 与类同名的 `Player(...)` 是构造函数。
- `new Player(...)` 创建一个玩家对象。
- `TakeDamage` 修改当前对象的生命值。
- 最终输出 `80`。

---

## 9. 属性

```csharp
public class Player
{
    public string Name { get; private set; }
    public int Hp { get; private set; }

    public Player(string name, int hp)
    {
        Name = name;
        Hp = hp;
    }
}
```

解释：

- 属性通过 `get` 提供读取，通过 `set` 控制写入。
- `private set` 表示外部可以读取，但只能由 `Player` 内部修改。
- 相比公开字段，属性更容易添加校验和维护规则。

---

## 10. 枚举和接口

### 10.1 枚举

```csharp
public enum PlayerState
{
    Idle,
    Moving,
    Attacking,
    Dead
}

PlayerState state = PlayerState.Idle;
```

解释：

- `enum` 定义一组固定选项。
- 使用 `PlayerState.Idle` 比直接写数字或字符串更安全。

### 10.2 接口

```csharp
public interface IDamageable
{
    void TakeDamage(int amount);
}

public class Enemy : IDamageable
{
    public void TakeDamage(int amount)
    {
        Console.WriteLine($"Enemy takes {amount} damage");
    }
}
```

解释：

- `IDamageable` 规定实现者必须提供 `TakeDamage` 方法。
- `Enemy : IDamageable` 表示敌人遵守这个约定。
- 调用者可以面向接口编程，而不必知道具体对象类型。

---

## 11. 空值和异常

```csharp
Player player = null;

if (player == null)
{
    Console.WriteLine("Player not found");
}
```

解释：

- `null` 表示变量没有引用有效对象。
- 使用对象前应先确认它不是 `null`。

```csharp
try
{
    int value = int.Parse("abc");
}
catch (FormatException exception)
{
    Console.WriteLine(exception.Message);
}
```

解释：

- `int.Parse("abc")` 无法把文字转换成整数，会抛出异常。
- `catch` 捕获指定异常，避免程序直接中断。
- 正常可预期的输入失败更适合使用 `int.TryParse`。

---

## 12. Unity 脚本基础

```csharp
using UnityEngine;

public class PlayerMover : MonoBehaviour
{
    [SerializeField] private float speed = 5f;

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        transform.Translate(Vector3.right * horizontal * speed * Time.deltaTime);
    }
}
```

解释：

- 继承 `MonoBehaviour` 后，脚本可以挂到 GameObject 上。
- `[SerializeField]` 让私有字段显示在 Inspector 中。
- `Update` 通常每帧执行一次。
- `Time.deltaTime` 让移动速度尽量不受帧率影响。
- 示例用于理解结构；My RPG 实际输入和移动逻辑应继续遵循现有项目架构。

---

## 13. 常见问题与练习

常见问题：

1. 忘记数组从 `0` 开始。
2. 把 `=` 和 `==` 混淆。
3. 字段全部设为 `public`，导致任何代码都能修改状态。
4. 忘记检查 `null`。
5. 在 `Update` 中重复创建大量临时对象。
6. 事件订阅后忘记取消订阅。
7. 在 Unity 中直接修改其他模块的内部状态，而不通过明确接口。

入门练习：

1. 创建玩家类，保存名字、生命值和等级。
2. 编写受伤、治疗和死亡判断方法。
3. 使用 `List<string>` 表示背包。
4. 使用 `Dictionary<int, string>` 保存物品 ID 和名称。
5. 使用接口统一处理玩家和敌人的受伤逻辑。
6. 编写一个 MonoBehaviour，在 `Update` 中读取输入并移动对象。
