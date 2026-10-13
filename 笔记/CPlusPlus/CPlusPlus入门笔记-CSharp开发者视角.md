# C++ 入门笔记（C# 开发者视角）

> 面向读者：已经掌握 C#，第一次系统了解现代 C++。
> 最后更新：2026-08-17。
> 建议标准：优先学习 C++17 及之后常见的现代写法。

说明：示例以单个知识点为主，部分片段假设 `Player` 等类型已经声明，并省略不影响当前概念的头文件。

## 目录

1. [C++ 与 C# 的定位差异](#1-c-与-c-的定位差异)
2. [编译和链接](#2-编译和链接)
3. [第一个程序](#3-第一个程序)
4. [变量和类型](#4-变量和类型)
5. [值、引用和指针](#5-值引用和指针)
6. [const](#6-const)
7. [对象生命周期与 RAII](#7-对象生命周期与-raii)
8. [智能指针](#8-智能指针)
9. [字符串和标准容器](#9-字符串和标准容器)
10. [类、构造和析构](#10-类构造和析构)
11. [头文件与源文件](#11-头文件与源文件)
12. [继承和虚函数](#12-继承和虚函数)
13. [模板](#13-模板)
14. [异常与错误处理](#14-异常与错误处理)
15. [C# 到 C++ 对照](#15-c-到-c-对照)
16. [Unity 项目中的用途](#16-unity-项目中的用途)

---

## 1. C++ 与 C# 的定位差异

| 维度 | C# | C++ |
| --- | --- | --- |
| 常见运行方式 | .NET、Mono、IL2CPP | 编译成本机机器码 |
| 内存管理 | 垃圾回收为主 | 值语义、RAII、智能指针和手动控制 |
| 空引用 | `null` | `nullptr` |
| 引用语义 | class 通常是引用类型 | 类型本身不决定所有使用方式 |
| 泛型能力 | Generics | Templates |
| 构建过程 | 编译程序集 | 预处理、编译、链接 |
| 主要风险 | 空引用、装箱、GC 分配 | 悬空指针、越界、未定义行为、资源泄漏 |

C++ 给开发者更多控制，也要求开发者更明确地管理对象所有权和生命周期。

---

## 2. 编译和链接

C++ 文件通常分为：

- `.h` 或 `.hpp`：声明类型和函数。
- `.cpp`：实现函数。
- 编译器先把源文件分别编译成目标文件。
- 链接器再把目标文件和库组合成最终程序。

如果函数已经声明但没有提供实现，代码可能通过编译，却在链接阶段报错。这是 C# 初学者接触 C++ 时常见的新概念。

---

## 3. 第一个程序

```cpp
#include <iostream>

int main()
{
    std::cout << "Hello, C++" << std::endl;
    return 0;
}
```

解释：

- `#include <iostream>` 引入标准输入输出功能。
- `main` 是普通 C++ 程序入口。
- `std::cout` 向控制台输出内容。
- `return 0` 表示程序正常结束。
- `std::` 表示名称来自 C++ 标准库命名空间。

---

## 4. 变量和类型

```cpp
#include <string>

std::string playerName = "Knight";
int hp = 100;
float speed = 5.5f;
bool isAlive = true;
```

解释：

- 这些基础类型与 C# 的概念接近。
- C++ 标准字符串使用 `std::string`。
- C++ 类型的实际大小可能受平台和编译器影响，需要精确位数时可使用 `std::int32_t` 等类型。

`auto` 可以让编译器推断类型：

```cpp
auto level = 2;
auto title = std::string("Warrior");
```

解释：

- `level` 被推断为 `int`。
- `title` 被推断为 `std::string`。
- 和 C# 的 `var` 类似，推断后类型仍然固定。

---

## 5. 值、引用和指针

### 5.1 值复制

```cpp
int first = 100;
int second = first;

second = 50;

std::cout << first << std::endl;
```

解释：

- `second = first` 复制整数值。
- 修改 `second` 不影响 `first`，所以输出 `100`。

### 5.2 引用

```cpp
int hp = 100;
int& hpReference = hp;

hpReference = 50;

std::cout << hp << std::endl;
```

解释：

- `int&` 表示整数引用。
- `hpReference` 是 `hp` 的另一个名字。
- 修改引用会直接修改原变量，所以输出 `50`。
- 引用创建时必须绑定有效对象，之后不能改为引用另一个对象。

### 5.3 指针

```cpp
int hp = 100;
int* hpPointer = &hp;

*hpPointer = 50;

std::cout << hp << std::endl;
```

解释：

- `&hp` 取得 `hp` 的内存地址。
- `int*` 表示指向整数的指针。
- `*hpPointer` 访问指针指向的值。
- 修改该值后，`hp` 变成 `50`。

空指针使用 `nullptr`：

```cpp
Player* player = nullptr;

if (player == nullptr)
{
    std::cout << "Player not found" << std::endl;
}
```

解释：

- `nullptr` 类似 C# 的 `null`。
- 解引用空指针会造成严重错误，所以使用前必须确认有效。

初学时可以这样理解：

- 普通值：拥有自己的一份数据。
- 引用：已有对象的别名。
- 指针：保存对象地址，可以为空，也可以改为指向其他对象。

---

## 6. const

```cpp
void PrintName(const std::string& name)
{
    std::cout << name << std::endl;
}
```

解释：

- `&` 避免复制整个字符串。
- `const` 保证函数不会通过这个引用修改字符串。
- `const T&` 是 C++ 中传递较大只读对象的常见方式。

成员函数末尾也可以写 `const`：

```cpp
class Player
{
public:
    int GetHp() const
    {
        return hp;
    }

private:
    int hp = 100;
};
```

解释：

- `GetHp() const` 表示该方法不会修改对象的普通成员状态。
- 这比 C# 更明确地表达只读意图。

---

## 7. 对象生命周期与 RAII

C++ 对象可以直接创建在作用域中。离开作用域时，析构函数会自动运行。

```cpp
void Run()
{
    std::string name = "Knight";
    std::cout << name << std::endl;
}
```

解释：

- `name` 在进入 `Run` 后创建。
- `Run` 结束时，`name` 自动销毁。
- 这种“对象生命周期跟随作用域”的思路是 RAII 的基础。

RAII 的核心是：资源由对象持有，对象销毁时自动释放资源。它常用于文件、锁、内存和网络句柄。

---

## 8. 智能指针

### 8.1 unique_ptr

```cpp
#include <memory>

auto player = std::make_unique<Player>();
player->TakeDamage(10);
```

解释：

- `std::unique_ptr<Player>` 表示这份对象只有一个明确所有者。
- `std::make_unique` 创建对象并交给智能指针管理。
- `->` 通过指针访问对象成员。
- 智能指针离开作用域时会自动释放对象。

### 8.2 shared_ptr

```cpp
auto firstOwner = std::make_shared<Player>();
auto secondOwner = firstOwner;
```

解释：

- `std::shared_ptr` 允许多个地方共同拥有对象。
- 每复制一次会增加引用计数。
- 最后一个所有者离开后，对象才会释放。
- 不应因为接近 C# 引用体验就到处使用 `shared_ptr`；优先明确单一所有权。

现代 C++ 中应优先使用值、引用和智能指针。除非接口确实要求，不要直接使用 `new` 和 `delete` 管理普通业务对象。

---

## 9. 字符串和标准容器

### 9.1 vector

```cpp
#include <vector>

std::vector<std::string> items;
items.push_back("Sword");
items.push_back("Potion");

std::cout << items[0] << std::endl;
```

解释：

- `std::vector<T>` 类似 C# 的 `List<T>`。
- `push_back` 在末尾添加元素。
- 索引从 `0` 开始。

遍历 `vector`：

```cpp
for (const std::string& item : items)
{
    std::cout << item << std::endl;
}
```

解释：

- 这是范围 `for` 循环，类似 C# 的 `foreach`。
- `const std::string&` 避免复制元素，同时禁止修改。

### 9.2 unordered_map

```cpp
#include <unordered_map>

std::unordered_map<int, std::string> itemNames;
itemNames[1001] = "Sword";
itemNames[1002] = "Potion";

std::cout << itemNames.at(1002) << std::endl;
```

解释：

- `std::unordered_map` 类似 C# 的 `Dictionary<TKey, TValue>`。
- `at(1002)` 读取对应值，结果是 `Potion`。
- 访问不存在的键时，`at` 会报告错误；`[]` 可能创建新元素。

---

## 10. 类、构造和析构

```cpp
class Player
{
public:
    Player(std::string name, int hp)
        : name(name), hp(hp)
    {
    }

    ~Player()
    {
        std::cout << "Player destroyed" << std::endl;
    }

    void TakeDamage(int amount)
    {
        hp -= amount;
    }

private:
    std::string name;
    int hp;
};
```

解释：

- `public` 成员可由外部访问，`private` 只允许类内部访问。
- 与类同名的函数是构造函数。
- 冒号后面是成员初始化列表。
- `~Player` 是析构函数，对象销毁时自动调用。
- C# 通常依赖垃圾回收，而 C++ 析构时间通常与对象作用域或所有权直接相关。

---

## 11. 头文件与源文件

`Player.h`：

```cpp
#pragma once

class Player
{
public:
    explicit Player(int hp);
    void TakeDamage(int amount);

private:
    int hp;
};
```

解释：

- 头文件声明类的结构和可调用成员。
- `#pragma once` 防止头文件被重复包含。
- `explicit` 防止单参数构造函数发生意外隐式转换。

`Player.cpp`：

```cpp
#include "Player.h"

Player::Player(int hp)
    : hp(hp)
{
}

void Player::TakeDamage(int amount)
{
    hp -= amount;
}
```

解释：

- 源文件包含头文件并提供具体实现。
- `Player::` 表示函数属于 `Player` 类。
- 声明和实现必须保持一致，否则会出现编译或链接错误。

---

## 12. 继承和虚函数

```cpp
class IDamageable
{
public:
    virtual ~IDamageable() = default;
    virtual void TakeDamage(int amount) = 0;
};

class Enemy : public IDamageable
{
public:
    void TakeDamage(int amount) override
    {
        hp -= amount;
    }

private:
    int hp = 100;
};
```

解释：

- 含纯虚函数的类可以承担类似 C# 接口的角色。
- `= 0` 表示派生类必须实现该函数。
- `override` 明确表示正在重写基类虚函数。
- 基类作为多态接口使用时，应提供虚析构函数。

---

## 13. 模板

```cpp
template <typename T>
T MaxValue(const T& left, const T& right)
{
    return left > right ? left : right;
}

int maxNumber = MaxValue(10, 20);
```

解释：

- `T` 是调用时确定的类型。
- 编译器会根据使用方式生成对应代码。
- 模板类似 C# 泛型，但编译模型、错误信息和能力范围不同。

---

## 14. 异常与错误处理

```cpp
try
{
    int value = std::stoi("abc");
}
catch (const std::exception& exception)
{
    std::cout << exception.what() << std::endl;
}
```

解释：

- `std::stoi` 转换失败时可能抛出异常。
- `catch` 使用常量引用接收异常，避免复制。
- `what()` 返回错误描述。
- 部分游戏或底层项目会关闭异常支持，因此错误策略要遵循具体工程约定。

---

## 15. C# 到 C++ 对照

| C# | C++ | 主要区别 |
| --- | --- | --- |
| `string` | `std::string` | C++ 需要包含对应标准库头文件 |
| `List<T>` | `std::vector<T>` | 两者都从 0 开始 |
| `Dictionary<K,V>` | `std::unordered_map<K,V>` | API 和迭代规则不同 |
| `null` | `nullptr` | 空指针不能解引用 |
| `var` | `auto` | 都是编译期类型推断 |
| class 引用 | 值、引用或指针 | C++ 使用方式由声明决定 |
| `IDisposable` | RAII / 析构函数 | C++ 更强调确定的作用域释放 |
| Generics | Templates | C++ 模板在编译期生成代码 |
| Interface | 纯虚基类 | C++ 还要关注虚析构 |
| 垃圾回收 | 值对象和智能指针 | 所有权必须更明确 |

---

## 16. Unity 项目中的用途

普通 Unity 游戏逻辑主要使用 C#。C++ 常见于：

- Unity 引擎和底层原生代码。
- 平台 SDK 与原生插件。
- 性能敏感的第三方库。
- 图形、音频、物理和网络底层。
- IL2CPP 生成的本机代码排查。

入门练习：

1. 编写 `Player` 类，包含生命值和受伤方法。
2. 使用 `std::vector` 保存背包物品。
3. 分别用值、引用和指针修改一个整数，观察结果。
4. 使用 `std::unique_ptr` 创建玩家对象。
5. 把 `Player` 的声明和实现拆到 `.h` 与 `.cpp`。
6. 实现一个带纯虚函数的 `IDamageable` 基类。
