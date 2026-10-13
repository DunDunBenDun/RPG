# Python 入门概览（C# 对照）

> 面向读者：已经了解 C#，希望快速看懂 Python。
> 最后更新：2026-08-17。
> 学习目标：以了解和编写简单工具脚本为主。

## 目录

1. [Python 适合做什么](#1-python-适合做什么)
2. [第一个程序与缩进](#2-第一个程序与缩进)
3. [变量和类型](#3-变量和类型)
4. [条件判断](#4-条件判断)
5. [循环](#5-循环)
6. [列表和字典](#6-列表和字典)
7. [函数](#7-函数)
8. [类](#8-类)
9. [模块](#9-模块)
10. [异常处理](#10-异常处理)
11. [文件读写](#11-文件读写)
12. [虚拟环境与依赖](#12-虚拟环境与依赖)
13. [C# 到 Python 对照](#13-c-到-python-对照)
14. [在 My RPG 中的用途](#14-在-my-rpg-中的用途)

---

## 1. Python 适合做什么

Python 常用于：

- 自动化脚本。
- 数据清洗和报表。
- AI、机器学习和数据分析。
- 构建工具、资源检查和批量处理。
- Web 服务与接口原型。

普通 Unity 运行时游戏逻辑仍以 C# 为主。Python 更适合作为编辑器外部工具或服务端辅助语言。

---

## 2. 第一个程序与缩进

```python
print("Hello, Python")
```

解释：

- `print` 输出文字。
- Python 通常直接解释执行 `.py` 文件，不需要像 C# 项目一样先生成程序集。

Python 使用缩进表示代码块：

```python
hp = 0

if hp <= 0:
    print("角色死亡")
```

解释：

- 条件末尾写冒号 `:`。
- 缩进后的代码属于 `if`。
- Python 不使用 C# 的大括号。
- 同一层级应保持一致缩进，通常使用四个空格。

---

## 3. 变量和类型

```python
player_name = "Knight"
hp = 100
speed = 5.5
is_alive = True
```

解释：

- Python 创建变量时不写类型。
- `True` 和 `False` 的首字母必须大写。
- Python 是动态类型语言，变量可以改为保存不同类型的值。

查看类型：

```python
print(type(hp))
print(type(player_name))
```

解释：

- 第一行输出整数类型 `int`。
- 第二行输出字符串类型 `str`。

没有值时使用 `None`：

```python
target = None

if target is None:
    print("没有找到目标")
```

解释：

- `None` 类似 C# 的 `null`。
- 判断是否为 `None` 时通常使用 `is None`。

---

## 4. 条件判断

```python
hp = 20

if hp <= 0:
    state = "dead"
elif hp < 30:
    state = "danger"
else:
    state = "normal"

print(state)
```

解释：

- Python 使用 `elif`，作用类似 C# 的 `else if`。
- 当前 `hp` 是 `20`，所以输出 `danger`。

逻辑运算使用英文单词：

| 作用 | Python | C# |
| --- | --- | --- |
| 并且 | `and` | `&&` |
| 或者 | `or` | `||` |
| 取反 | `not` | `!` |
| 相等 | `==` | `==` |
| 不相等 | `!=` | `!=` |

---

## 5. 循环

```python
for index in range(3):
    print(index)
```

解释：

- `range(3)` 产生 `0`、`1`、`2`。
- `for` 依次把这些值放入 `index`。

```python
count = 3

while count > 0:
    print(count)
    count -= 1
```

解释：

- 条件成立时持续循环。
- `count -= 1` 每次让数量减少 `1`。
- 最终输出 `3`、`2`、`1`。

---

## 6. 列表和字典

### 6.1 列表

```python
items = ["Sword", "Potion"]

items.append("Key")
items.remove("Sword")

print(items[0])
```

解释：

- Python `list` 类似 C# 的 `List<T>`，但可以混合保存不同类型。
- `append` 在末尾添加元素。
- `remove` 删除指定值。
- 索引从 `0` 开始，最终输出 `Potion`。

遍历列表：

```python
for item in items:
    print(item)
```

解释：

- `for` 会依次取得列表中的每个元素。
- 写法类似 C# 的 `foreach`。

### 6.2 字典

```python
player = {
    "name": "Knight",
    "hp": 100
}

print(player["name"])
player["level"] = 2
```

解释：

- `dict` 使用键和值保存数据，类似 C# 的 `Dictionary<TKey, TValue>`。
- `player["name"]` 得到 `Knight`。
- 最后一行添加新的 `level` 数据。

---

## 7. 函数

```python
def add(left, right):
    return left + right


result = add(10, 20)
print(result)
```

解释：

- `def` 定义函数。
- Python 不在参数前写固定类型。
- `return` 返回结果。
- 最终输出 `30`。

Python 支持可选的类型提示：

```python
def calculate_damage(attack: int, defense: int) -> int:
    return max(0, attack - defense)
```

解释：

- `attack: int` 表示希望参数是整数。
- `-> int` 表示希望返回整数。
- 类型提示主要帮助编辑器和检查工具，默认不会像 C# 编译器一样强制保证。

---

## 8. 类

```python
class Player:
    def __init__(self, name: str, hp: int):
        self.name = name
        self.hp = hp

    def take_damage(self, amount: int):
        self.hp -= amount


hero = Player("Knight", 100)
hero.take_damage(20)

print(hero.hp)
```

解释：

- `class` 定义类。
- `__init__` 在创建对象时初始化数据，作用接近 C# 构造函数。
- `self` 表示当前对象，作用接近 C# 的 `this`。
- 最终生命值输出 `80`。

---

## 9. 模块

假设 `math_tools.py` 中有：

```python
def add(left, right):
    return left + right
```

解释：

- 这个文件定义了一个可以被其他文件使用的函数。

另一个文件可以导入它：

```python
from math_tools import add

print(add(10, 20))
```

解释：

- `from ... import ...` 从模块中导入指定名称。
- 调用后输出 `30`。

---

## 10. 异常处理

```python
try:
    value = int("abc")
except ValueError as error:
    print(error)
```

解释：

- `int("abc")` 无法转换为整数，会产生 `ValueError`。
- `except` 捕获指定异常。
- 这和 C# 的 `try/catch` 思路相近。

---

## 11. 文件读写

```python
from pathlib import Path

path = Path("example.txt")
path.write_text("Hello", encoding="utf-8")
text = path.read_text(encoding="utf-8")

print(text)
```

解释：

- `Path` 表示文件路径。
- `write_text` 写入 UTF-8 文本。
- `read_text` 读取文本。
- 工具脚本处理项目文件时，应先确认路径范围，避免覆盖重要资源。

---

## 12. 虚拟环境与依赖

常见命令：

```text
python -m venv .venv
python -m pip install package_name
```

解释：

- 第一行创建项目独立的 Python 环境。
- 第二行在当前环境中安装依赖。
- 虚拟环境可以避免不同项目的依赖版本互相影响。
- 安装依赖会改变开发环境，应先确认项目是否确实需要。

---

## 13. C# 到 Python 对照

| C# | Python | 说明 |
| --- | --- | --- |
| `int hp = 100;` | `hp = 100` | Python 通常不写变量类型 |
| `null` | `None` | 判断常用 `is None` |
| `true / false` | `True / False` | Python 首字母大写 |
| `List<T>` | `list` | Python 默认不限制元素类型 |
| `Dictionary<K,V>` | `dict` | 都按键查找值 |
| `foreach` | `for item in items` | Python 写法更简短 |
| `this` | `self` | Python 方法要显式声明 `self` |
| `using namespace` | `import` | 都用于使用其他模块 |
| 编译期类型检查 | 运行时类型为主 | 类型提示不等于强制检查 |
| 大括号代码块 | 缩进代码块 | 缩进错误会影响程序结构 |

---

## 14. 在 My RPG 中的用途

Python 可以用于：

- 批量检查资源名称和目录结构。
- 处理 CSV、JSON 和策划数据。
- 生成报表或转换数据格式。
- 分析日志。
- 构建 AI 服务原型或离线工具。

不建议默认用 Python 直接控制 Unity 运行时 GameObject。My RPG 的游戏逻辑、动画、物理和 Unity 生命周期仍应以 C# 为主。

了解阶段建议练习：

1. 输出玩家名字和生命值。
2. 使用列表保存背包。
3. 使用字典保存物品 ID 和名称。
4. 编写读取 JSON 或文本文件的小脚本。
5. 编写一个检查重复文件名的只读工具。
