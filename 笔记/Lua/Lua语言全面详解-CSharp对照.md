# Lua 入门笔记（C# 对照）

> 面向读者：刚接触 Lua、已经了解一些 C# 基础的开发者。
> 最后更新：2026-08-17。
> 项目现状：My RPG 尚未接入 Lua 运行时，本文先学习通用基础语法。

这篇笔记只保留初学阶段常用的内容。建议按顺序阅读并亲手运行示例，不需要一次记住全部语法。

## 目录

1. [Lua 是什么](#1-lua-是什么)
2. [基本书写规则](#2-基本书写规则)
3. [变量和基础类型](#3-变量和基础类型)
4. [运算符和真假判断](#4-运算符和真假判断)
5. [条件判断](#5-条件判断)
6. [循环](#6-循环)
7. [函数](#7-函数)
8. [字符串](#8-字符串)
9. [Table：Lua 最重要的数据结构](#9-tablelua-最重要的数据结构)
10. [点号、冒号和 self](#10-点号冒号和-self)
11. [模块和 require](#11-模块和-require)
12. [常见错误](#12-常见错误)
13. [调试方法](#13-调试方法)
14. [Lua 与 C# 的主要区别](#14-lua-与-c-的主要区别)
15. [在 My RPG 中使用 Lua](#15-在-my-rpg-中使用-lua)
16. [入门练习](#16-入门练习)

---

## 1. Lua 是什么

Lua 是一门轻量、动态类型的脚本语言。它常被嵌入游戏中，用来编写任务、技能、UI 流程等容易变化的逻辑。

第一个 Lua 程序：

```lua
print("Hello, Lua")
```

解释：

- `print` 用来输出内容，类似 C# 的 `Console.WriteLine` 或 Unity 的 `Debug.Log`。
- `"Hello, Lua"` 是一段字符串。
- Lua 代码通常保存在 `.lua` 文件中。

Lua 的变量没有固定类型，同一个变量可以先保存数字，再保存字符串：

```lua
local value = 10
value = "ten"
```

解释：

- 第一行让 `value` 保存数字 `10`。
- 第二行让同一个变量改为保存字符串 `"ten"`。
- C# 的变量通常有固定类型，Lua 则在运行时根据当前值判断类型。

---

## 2. 基本书写规则

### 2.1 注释

```lua
-- 这是单行注释

--[[
这是多行注释
可以写很多行
]]
```

解释：

- `--` 后面的内容不会执行。
- `--[[ ... ]]` 用来包住多行注释。

### 2.2 通常不写分号

```lua
local hp = 100
hp = hp - 10
print(hp)
```

解释：

- 第一行创建生命值 `hp`。
- 第二行让生命值减少 `10`。
- 第三行输出 `90`。
- Lua 一般用换行分隔语句，不需要在行尾写分号。

### 2.3 代码块用 end 结束

```lua
local hp = 0

if hp <= 0 then
    print("角色死亡")
end
```

解释：

- `if` 开始条件判断。
- `then` 后面是条件成立时执行的代码。
- `end` 表示这段判断结束。
- Lua 不使用 C# 的大括号 `{ }` 包围代码块。

---

## 3. 变量和基础类型

### 3.1 优先使用 local

```lua
local playerName = "Knight"
local hp = 100
local isAlive = true
```

解释：

- `local` 表示局部变量，只在当前代码范围内使用。
- `playerName` 是字符串，`hp` 是数字，`isAlive` 是布尔值。
- 初学阶段创建变量时，默认都写 `local`。

不写 `local` 通常会创建全局变量，容易因为拼写错误产生问题：

```lua
local playerHealth = 100
playerHeath = 50
```

解释：

- 两个名字看起来很像，但第二行少了一个字母 `l`。
- Lua 不一定会立即提醒这个拼写错误，而可能创建一个新的全局变量。
- 使用 `local` 可以减少这类问题。

### 3.2 常用类型

| Lua 类型 | 说明 | 示例 |
| --- | --- | --- |
| `nil` | 没有值 | `local target = nil` |
| `boolean` | 真或假 | `true`、`false` |
| `number` | 数字 | `10`、`3.14` |
| `string` | 文本 | `"Knight"` |
| `function` | 函数 | `local function add() end` |
| `table` | 数组、字典或对象式数据 | `{ 10, 20 }` |

可以使用 `type` 查看值的类型：

```lua
print(type(100))
print(type("Knight"))
print(type(true))
print(type({}))
```

解释：

- 四行依次输出 `number`、`string`、`boolean` 和 `table`。
- `type` 常用于调试变量当前保存的值。

### 3.3 nil 表示没有值

```lua
local target = nil

if target == nil then
    print("没有找到目标")
end
```

解释：

- `nil` 类似 C# 的 `null`，表示变量当前没有有效值。
- `==` 用来判断两个值是否相等。

---

## 4. 运算符和真假判断

### 4.1 常用运算符

```lua
local a = 10
local b = 3

print(a + b)
print(a - b)
print(a * b)
print(a / b)
print(a % b)
```

解释：

- `+`、`-`、`*`、`/` 分别表示加、减、乘、除。
- `%` 表示取余数，`10 % 3` 的结果是 `1`。

常用比较和逻辑运算：

| 作用 | Lua 写法 | C# 写法 |
| --- | --- | --- |
| 相等 | `==` | `==` |
| 不相等 | `~=` | `!=` |
| 大于、小于 | `>`、`<` | `>`、`<` |
| 大于等于、小于等于 | `>=`、`<=` | `>=`、`<=` |
| 并且 | `and` | `&&` |
| 或者 | `or` | `||` |
| 取反 | `not` | `!` |

### 4.2 只有 false 和 nil 是假

```lua
if 0 then
    print("0 在 Lua 中是真")
end

if "" then
    print("空字符串在 Lua 中也是真")
end
```

解释：

- Lua 中只有 `false` 和 `nil` 会让条件不成立。
- 数字 `0` 和空字符串 `""` 都会让条件成立。
- 判断数量时应明确写 `count > 0`，不要只写 `if count then`。

### 4.3 字符串使用两个点连接

```lua
local name = "Knight"
local text = "Player: " .. name

print(text)
```

解释：

- `..` 用来连接字符串。
- 最终输出 `Player: Knight`。
- Lua 不使用 `+` 拼接字符串。

---

## 5. 条件判断

```lua
local hp = 20
local state

if hp <= 0 then
    state = "dead"
elseif hp < 30 then
    state = "danger"
else
    state = "normal"
end

print(state)
```

解释：

- 如果生命值小于等于 `0`，状态是 `dead`。
- 否则，如果生命值小于 `30`，状态是 `danger`。
- 其他情况状态是 `normal`。
- 当前 `hp` 是 `20`，所以输出 `danger`。
- Lua 写 `elseif`，不是 C# 的 `else if`。

---

## 6. 循环

### 6.1 for 循环

```lua
for i = 1, 3 do
    print(i)
end
```

解释：

- `i` 从 `1` 开始，每次增加 `1`。
- 循环会包含结束值 `3`，所以依次输出 `1`、`2`、`3`。

可以指定每次增加多少：

```lua
for i = 2, 10, 2 do
    print(i)
end
```

解释：

- 第三个数字 `2` 是步长。
- 这段代码依次输出 `2`、`4`、`6`、`8`、`10`。

### 6.2 while 循环

```lua
local count = 3

while count > 0 do
    print(count)
    count = count - 1
end
```

解释：

- 只要 `count > 0`，循环就会继续。
- 每次循环都让 `count` 减少 `1`。
- 最终依次输出 `3`、`2`、`1`。

---

## 7. 函数

### 7.1 定义和调用函数

```lua
local function add(a, b)
    return a + b
end

local result = add(10, 20)
print(result)
```

解释：

- `add` 是函数名，`a` 和 `b` 是参数。
- `return` 把计算结果交给调用者。
- `add(10, 20)` 返回 `30`，并保存到 `result`。

### 7.2 返回多个值

```lua
local function divide(a, b)
    if b == 0 then
        return nil, "不能除以 0"
    end

    return a / b, nil
end

local result, errorMessage = divide(10, 2)
print(result)
```

解释：

- Lua 函数可以一次返回多个值。
- 除数是 `0` 时，函数返回 `nil` 和错误信息。
- 计算成功时，函数返回计算结果和 `nil`。
- `divide(10, 2)` 的结果是 `5`。

调用失败时可以这样处理：

```lua
local result, errorMessage = divide(10, 0)

if result == nil then
    print(errorMessage)
end
```

解释：

- `divide(10, 0)` 无法计算，所以 `result` 是 `nil`。
- 条件成立后输出 `不能除以 0`。

---

## 8. 字符串

```lua
local name = "Knight"
local hp = 80

local message = string.format("%s 的生命值是 %d", name, hp)
print(message)
```

解释：

- `string.format` 按格式组合字符串。
- `%s` 会放入字符串 `name`。
- `%d` 会放入整数 `hp`。
- 最终输出 `Knight 的生命值是 80`。

常用字符串函数：

```lua
print(string.len("abc"))
print(string.upper("hello"))
print(string.lower("HELLO"))
print(string.sub("abcdef", 2, 4))
```

解释：

- `string.len("abc")` 返回长度 `3`。
- `string.upper` 把英文转成大写。
- `string.lower` 把英文转成小写。
- `string.sub("abcdef", 2, 4)` 取出第 2 到第 4 个位置，结果是 `bcd`。

注意：Lua 字符串通常按字节保存，`#"你好"` 不一定得到汉字数量 `2`。处理中文长度时，应使用项目运行时提供的 UTF-8 工具。

---

## 9. Table：Lua 最重要的数据结构

Table 可以当数组、字典，也可以用来表示一份对象数据。

### 9.1 保存玩家数据

```lua
local player = {
    name = "Knight",
    hp = 100,
    alive = true
}

print(player.name)
print(player["hp"])

player.level = 2
player["mana"] = 50
```

解释：

- `{ ... }` 创建一张 table，保存名字、生命值和存活状态。
- `player.name` 得到 `"Knight"`。
- `player["hp"]` 得到 `100`。
- 后两行直接添加 `level` 和 `mana` 字段，不需要提前声明。
- `player.name` 与 `player["name"]` 的作用相同。

### 9.2 特殊字符串键和数字键

```lua
local data = {
    ["display-name"] = "Knight",
    [1001] = "item"
}

print(data["display-name"])
print(data[1001])
```

解释：

- `data["display-name"]` 得到 `"Knight"`。
- 键名中包含 `-`，所以不能写成 `data.display-name`。
- `data[1001]` 得到 `"item"`。
- 字符串、数字或变量都可以放在方括号中作为键。

### 9.3 当作数组使用

```lua
local items = { "Sword", "Potion", "Key" }

print(items[1])
print(items[3])
```

解释：

- Lua 数组通常从 `1` 开始，不是 C# 的 `0`。
- `items[1]` 是 `"Sword"`。
- `items[3]` 是 `"Key"`。
- 初学阶段应让数组索引保持连续，不要在中间留下空位。

### 9.4 添加和删除数组元素

```lua
local items = { "Sword", "Potion" }

table.insert(items, "Key")
table.remove(items, 1)

print(items[1])
```

解释：

- `table.insert` 把 `"Key"` 添加到数组末尾。
- `table.remove(items, 1)` 删除第一个元素 `"Sword"`。
- 删除后 `"Potion"` 移到第一个位置，所以输出 `Potion`。

### 9.5 遍历数组

```lua
local items = { "Sword", "Potion", "Key" }

for index, item in ipairs(items) do
    print(index, item)
end
```

解释：

- `ipairs` 按 `1、2、3` 的顺序遍历连续数组。
- `index` 是当前位置，`item` 是当前位置保存的值。

### 9.6 遍历字典

```lua
local player = {
    name = "Knight",
    hp = 100
}

for key, value in pairs(player) do
    print(key, value)
end
```

解释：

- `pairs` 会遍历 table 中的所有键和值。
- `key` 可能是 `name` 或 `hp`，`value` 是对应的数据。
- `pairs` 不保证遍历顺序，不要依赖它先输出哪个字段。

### 9.7 nil 会删除字段

```lua
local player = {
    hp = 100
}

player.hp = nil
print(player.hp)
```

解释：

- 给 table 字段赋值 `nil`，等于删除这个字段。
- 删除后访问 `player.hp` 会得到 `nil`。

### 9.8 Table 是引用类型

```lua
local original = {
    hp = 100
}

local another = original
another.hp = 50

print(original.hp)
```

解释：

- `another = original` 没有复制一份新 table。
- 两个变量指向同一张 table。
- 修改 `another.hp` 也会影响 `original.hp`，所以输出 `50`。

### 9.9 与 C# 容器对照

| Lua 写法 | 常见用途 | C# 近似 |
| --- | --- | --- |
| `{ "A", "B" }` | 连续数组 | Array / `List<T>` |
| `{ hp = 100 }` | 一组对象数据 | class / Dictionary |
| `{ [id] = value }` | 按键查询数据 | `Dictionary<TKey, TValue>` |

---

## 10. 点号、冒号和 self

```lua
local player = {
    hp = 100
}

function player:takeDamage(amount)
    self.hp = self.hp - amount
end

player:takeDamage(20)
print(player.hp)
```

解释：

- `player:takeDamage(20)` 调用玩家自己的方法。
- 冒号会自动把 `player` 作为第一个参数传入。
- 函数内部使用 `self` 表示当前玩家。
- 生命值从 `100` 减少到 `80`。

下面两种调用方式作用相同：

```lua
player:takeDamage(10)
player.takeDamage(player, 10)
```

解释：

- 第一行使用冒号，Lua 自动传入 `player`。
- 第二行使用点号，需要手动把 `player` 作为第一个参数传入。
- 初学时，只要方法内部使用了 `self`，定义和调用都优先使用冒号。

---

## 11. 模块和 require

代码变多后，可以把功能拆到不同 Lua 文件中。

`PlayerConfig.lua`：

```lua
local PlayerConfig = {}

PlayerConfig.defaultSpeed = 5

function PlayerConfig.getSpeed()
    return PlayerConfig.defaultSpeed
end

return PlayerConfig
```

解释：

- `PlayerConfig` 是模块要公开的 table。
- table 中保存默认速度和读取速度的函数。
- `return PlayerConfig` 把这个模块交给其他文件使用。

在另一个文件中加载模块：

```lua
local PlayerConfig = require("PlayerConfig")
local speed = PlayerConfig.getSpeed()

print(speed)
```

解释：

- `require("PlayerConfig")` 加载 `PlayerConfig.lua`。
- 返回的 table 保存到局部变量 `PlayerConfig`。
- 调用 `getSpeed` 后输出 `5`。
- `require` 通常会缓存模块，重复加载时不会每次重新执行整个文件。

---

## 12. 常见错误

| 容易写错 | 正确理解 |
| --- | --- |
| 忘记写 `local` | 可能意外创建全局变量 |
| 把 `0` 当成假 | Lua 中 `0` 是真 |
| 把空字符串当成假 | Lua 中 `""` 是真 |
| 数组从 `0` 开始 | Lua 连续数组通常从 `1` 开始 |
| 使用 `!=` | Lua 的不等于是 `~=` |
| 使用 `+` 拼字符串 | Lua 使用 `..` |
| 认为 table 赋值会复制 | table 赋值只复制引用 |
| 依赖 `pairs` 的顺序 | `pairs` 不保证顺序 |
| 混用点号和冒号 | 冒号会自动传入 `self` |
| 给字段赋 `nil` 后还认为字段存在 | 赋 `nil` 等于删除字段 |

---

## 13. 调试方法

初学阶段最实用的方法是输出变量的值和类型：

```lua
local player = {
    name = "Knight",
    hp = 100
}

print("name:", player.name)
print("hp:", player.hp)
print("type:", type(player))
```

解释：

- 前两行输出字段名和字段值，方便确认数据是否正确。
- `type(player)` 输出 `table`。
- 如果结果不符合预期，优先检查变量名、`nil`、数组索引，以及点号和冒号是否写对。

---

## 14. Lua 与 C# 的主要区别

| 想做的事 | C# | Lua |
| --- | --- | --- |
| 创建局部变量 | `int hp = 100;` | `local hp = 100` |
| 没有值 | `null` | `nil` |
| 不相等 | `a != b` | `a ~= b` |
| 逻辑与 | `a && b` | `a and b` |
| 逻辑或 | `a || b` | `a or b` |
| 逻辑非 | `!a` | `not a` |
| 拼接字符串 | `a + b` | `a .. b` |
| 数组起始位置 | 通常从 `0` 开始 | 通常从 `1` 开始 |
| 列表 | `List<T>` | table |
| 字典 | `Dictionary<TKey, TValue>` | table |
| 实例自身 | `this` | `self` |
| 代码块 | `{ }` | `then/do ... end` |

最需要记住的五点：

1. 创建变量时优先写 `local`。
2. 只有 `false` 和 `nil` 是假。
3. Lua 数组通常从 `1` 开始。
4. Table 可以表示数组、字典和对象数据。
5. 冒号调用会自动传入 `self`。

---

## 15. 在 My RPG 中使用 Lua

截至 2026-08-17，My RPG 还没有接入 xLua、ToLua、MoonSharp 或其他 Lua 运行时。因此：

- 当前可以学习 Lua 语法，但项目里的 Lua 示例还不能直接在 Unity 中运行。
- 真正接入前，要先确定使用哪个 Lua 框架和 Lua 版本。
- Unity 生命周期、GameObject、物理、动画和资源加载应优先保留在 C#。
- Lua 更适合任务流程、技能规则、UI 流程等变化频繁的业务逻辑。
- 不要把玩家输入、AI 回复或网络文本直接当成 Lua 代码执行。

---

## 16. 入门练习

建议按顺序完成：

1. 创建 `name`、`hp` 和 `level` 三个局部变量并输出。
2. 使用 `if` 判断角色生命值属于正常、危险还是死亡。
3. 使用 `for` 输出 `1` 到 `10`。
4. 编写一个计算伤害的函数并返回结果。
5. 使用 table 保存玩家名字、生命值和背包。
6. 使用 `ipairs` 遍历背包中的物品。
7. 给玩家 table 添加一个使用 `self` 的受伤方法。
8. 把一组配置写成模块，再用 `require` 加载。

完成这些练习后，再学习更进阶的 Lua 内容，以及 Lua 与 Unity 的具体绑定方式。
