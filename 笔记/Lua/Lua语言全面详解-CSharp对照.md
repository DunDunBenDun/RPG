# Lua 语言全面详解（C# 对照）

> 面向读者：已经掌握 C#，希望系统学习 Lua，并可能在 Unity 项目中使用 Lua。  
> 编写日期：2026-08-03。  
> 项目现状：当前 My RPG 项目尚未接入 Lua 运行时。  
> 版本策略：正文讲通用 Lua；涉及版本差异时明确标注 Lua 5.1、LuaJIT、5.2、5.3 或 5.4。

## 目录

1. [先建立正确的心智模型](#1-先建立正确的心智模型)
2. [Lua 版本与 Unity 运行时](#2-lua-版本与-unity-运行时)
3. [第一个 Lua 程序与代码块](#3-第一个-lua-程序与代码块)
4. [类型系统与真值规则](#4-类型系统与真值规则)
5. [变量、赋值与作用域](#5-变量赋值与作用域)
6. [运算符](#6-运算符)
7. [字符串](#7-字符串)
8. [Table：Lua 最核心的数据结构](#8-tablelua-最核心的数据结构)
9. [流程控制](#9-流程控制)
10. [函数](#10-函数)
11. [多返回值与可变参数](#11-多返回值与可变参数)
12. [冒号语法与 self](#12-冒号语法与-self)
13. [闭包与 Upvalue](#13-闭包与-upvalue)
14. [迭代器](#14-迭代器)
15. [模块与 require](#15-模块与-require)
16. [错误处理](#16-错误处理)
17. [Metatable 与元方法](#17-metatable-与元方法)
18. [用 Table 实现面向对象](#18-用-table-实现面向对象)
19. [协程](#19-协程)
20. [垃圾回收与资源生命周期](#20-垃圾回收与资源生命周期)
21. [Lua 与 C# 的核心差异总表](#21-lua-与-c-的核心差异总表)
22. [Unity/C# 与 Lua 交互](#22-unityc-与-lua-交互)
23. [My RPG 项目的推荐边界](#23-my-rpg-项目的推荐边界)
24. [工程规范与性能](#24-工程规范与性能)
25. [最常见的陷阱](#25-最常见的陷阱)
26. [调试、测试与工具](#26-调试测试与工具)
27. [学习路线与练习](#27-学习路线与练习)
28. [一页速查表](#28-一页速查表)

---

## 1. 先建立正确的心智模型

Lua 是一门小型、动态类型、可嵌入的脚本语言。它与 C# 最大的不同，不只是语法更短，而是运行模型不同。

### 1.1 C# 与 Lua 的基本定位

| 维度 | C# | Lua |
| --- | --- | --- |
| 类型检查 | 主要在编译期，静态类型 | 主要在运行期，动态类型 |
| 执行环境 | .NET / Mono / IL2CPP | Lua VM、LuaJIT 或宿主提供的解释器 |
| 核心数据结构 | class、struct、array、List、Dictionary 等 | table |
| 面向对象 | 语言原生支持 | 用 table、函数和 metatable 组合实现 |
| 异常 | `try/catch/finally` | `error`、`pcall`、`xpcall` |
| 异步/协作 | Task、线程、Unity Coroutine | Lua coroutine；具体调度由宿主决定 |
| 空值 | `null` | `nil` |
| 访问控制 | public/private/protected 等 | 没有语言级访问修饰符，主要靠局部作用域和模块约定 |

### 1.2 Lua 中“一切围绕值”

Lua 的变量没有固定类型，值才有类型：

```lua
local value = 10          -- value 当前引用 number
value = "ten"            -- 现在引用 string
value = function() end   -- 现在引用 function
```

C# 中：

```csharp
int value = 10;
// value = "ten"; // 编译错误
```

动态类型减少了样板代码，但很多错误要到运行路径真正执行时才暴露。因此 Lua 项目更依赖清晰命名、局部变量、测试、静态分析注解和边界校验。

### 1.3 Lua 不是“简化版 C#”

学习时不要逐字翻译 C# 写法。Lua 常见设计方式是：

- 用 table 同时承担数组、字典、对象、模块和命名空间。
- 用闭包保存私有状态。
- 用函数作为参数、返回值和事件回调。
- 用 metatable 定义继承、运算符和缺省查找。
- 用多返回值表达“结果 + 错误”等组合。

---

## 2. Lua 版本与 Unity 运行时

Lua 不同版本之间有真实语义差异。写 Lua 前必须先确定宿主到底运行哪一个版本。

### 2.1 常见版本

| 版本 | 常见特征 | 需要注意 |
| --- | --- | --- |
| Lua 5.1 | 老牌嵌入版本，很多游戏框架曾采用 | 没有 `_ENV`、`goto`、`//`、原生位运算；使用全局 `unpack` |
| LuaJIT 2.x | 高性能 JIT，语义主要接近 Lua 5.1 | 带 FFI 等扩展；移动端/IL2CPP 平台能力取决于集成方式，不能假定所有平台都能 JIT |
| Lua 5.2 | 引入 `_ENV`、`goto`、`table.unpack` 等 | 与 5.1 的环境和模块机制存在差异 |
| Lua 5.3 | number 增加整数/浮点子类型，加入 `//` 和位运算 | 数值与旧版、LuaJIT 的行为需确认 |
| Lua 5.4 | 加入 to-be-closed 变量、`<const>`，GC 与协程等继续演进 | 很多旧 Unity Lua 框架不一定以 5.4 为基准 |

### 2.2 本文怎么处理版本差异

- 通用语法尽量使用 5.1～5.4 都成立的写法。
- `//`、位运算、`_ENV`、`<close>` 等内容会标注最低版本。
- 不把 LuaJIT 当成“Lua 最新版”；它主要兼容 Lua 5.1，同时有自己的扩展。
- Unity 插件宣传的框架版本不等于内部 Lua VM 版本，接入时应查该插件实际使用的 VM。

### 2.3 当前项目结论

截至 2026-08-03，My RPG 的 `Packages/manifest.json` 和项目代码中没有发现 xLua、ToLua、MoonSharp 或其他 Lua 集成。因此现在可以学习和设计边界，但不能写出可运行的 Unity Lua 脚本并声称已经接通。

---

## 3. 第一个 Lua 程序与代码块

### 3.1 Hello World

```lua
print("Hello, Lua")
```

Lua 文件通常使用 `.lua` 扩展名。一个文件可以被当作一个 **chunk（代码块）** 加载并执行。Chunk 可以定义局部变量、函数，执行语句，并返回一个值。

### 3.2 注释

```lua
-- 单行注释

--[[
多行注释
]]
```

长注释可用带等号的分隔符，便于内部包含 `]]`：

```lua
--[=[
这里可以包含 ]]
]=]
```

### 3.3 语句结束

Lua 通常不需要分号：

```lua
local hp = 100
hp = hp - 10
print(hp)
```

分号可以写，但一般省略。换行通常也不是严格语句终止符，解析器根据语法判断表达式是否结束。

### 3.4 代码块以 end 结束

Lua 不使用 C# 的花括号：

```lua
if hp <= 0 then
    print("Dead")
end
```

C# 对照：

```csharp
if (hp <= 0)
{
    Console.WriteLine("Dead");
}
```

---

## 4. 类型系统与真值规则

### 4.1 八种基本类型

Lua 的基础类型：

| 类型 | 用途 | C# 近似概念 |
| --- | --- | --- |
| `nil` | 无值、不存在 | `null`，但还承担删除 table 键的语义 |
| `boolean` | `true` / `false` | `bool` |
| `number` | 数值 | `double`，或 Lua 5.3+ 中的 integer/float 子类型 |
| `string` | 不可变字节字符串 | `string`，但 Unicode 处理方式不同 |
| `function` | 一等函数值 | delegate / lambda |
| `table` | 关联数组，Lua 核心容器 | array + Dictionary + object 的组合 |
| `userdata` | 宿主/C 扩展暴露的外部对象 | C# 对象代理、native handle 的近似概念 |
| `thread` | Lua 协程对象 | 不是 OS Thread；近似协作式执行句柄 |

查看类型：

```lua
print(type(nil))          -- nil
print(type(true))         -- boolean
print(type(12.5))         -- number
print(type("hello"))     -- string
print(type({}))           -- table
print(type(function() end)) -- function
```

`type` 返回字符串，不返回像 C# `System.Type` 那样的反射对象。

### 4.2 真值规则：只有 false 和 nil 为假

Lua 中只有以下两个值是假：

```lua
false
nil
```

其他所有值都是真，包括：

```lua
0       -- 真
""      -- 真
{}      -- 真
```

这是 C# 开发者最容易误判的地方：

```lua
if 0 then
    print("会执行")
end

if "" then
    print("也会执行")
end
```

不要用 `if count then` 判断数字是否大于 0，应明确写：

```lua
if count > 0 then
end
```

### 4.3 nil 的两种常见语义

1. 变量或字段不存在。
2. 给 table 键赋 nil，表示删除这个键。

```lua
local player = { hp = 100 }
player.hp = nil
print(player.hp) -- nil，hp 键已不存在
```

因此 table 不能用普通字段保存“这个键存在，但值就是 nil”这种状态。需要区分时可使用一个专门的哨兵值：

```lua
local NIL_VALUE = {}
local data = { result = NIL_VALUE }
```

### 4.4 number 的版本差异

- Lua 5.1/5.2 常见实现中，number 通常是双精度浮点数。
- Lua 5.3+ 的 `number` 内部可分 integer 和 float，但 `type` 都返回 `"number"`。
- `math.type(value)` 在 Lua 5.3+ 可返回 `"integer"` 或 `"float"`。
- LuaJIT 的数值模型和扩展类型又有区别。

当 Lua 使用双精度浮点数表示整数时，大于 `2^53` 的整数不能保证每个值都精确表示。C# 的 `long`、数据库 ID、时间戳跨边界时必须确认绑定框架如何转换。

---

## 5. 变量、赋值与作用域

### 5.1 始终优先使用 local

```lua
local hp = 100
local name = "Knight"
```

如果不写 `local`，赋值通常会写入当前环境中的全局变量：

```lua
hp = 100 -- 通常是全局变量，不推荐
```

Lua 不会像 C# 那样因为变量未声明而编译失败。一个拼写错误可能悄悄创建新全局：

```lua
local playerHealth = 100
playerHeath = 50 -- 少写一个 l，创建了另一个全局变量
```

工程规范：除模块入口或宿主明确要求外，变量和函数默认都写 `local`。

### 5.2 局部作用域

`local` 的作用域是当前代码块：

```lua
local outer = 10

if outer > 0 then
    local inner = 20
    print(outer, inner)
end

print(outer)
-- print(inner) -- 得到 nil 或访问外部同名值，块内 local 已不可见
```

代码块包括函数、`if`、循环以及显式 `do ... end`：

```lua
do
    local temporary = "only here"
end
```

### 5.3 多重赋值

```lua
local x, y = 10, 20
x, y = y, x -- 直接交换，不需要临时变量
```

Lua 会先计算右侧全部表达式，再执行赋值，因此交换安全。

数量不匹配时：

```lua
local a, b, c = 1, 2 -- c = nil
local x, y = 1, 2, 3 -- 3 被丢弃
```

### 5.4 声明时的可见范围

新 local 通常在声明语句之后才进入作用域：

```lua
local x = 10

do
    local x = x -- 右侧 x 是外层的 10
    print(x)
end
```

递归局部函数推荐使用函数声明语法：

```lua
local function factorial(n)
    if n <= 1 then
        return 1
    end
    return n * factorial(n - 1)
end
```

### 5.5 全局环境与 _ENV

在 Lua 5.2+ 中，自由变量通常通过 `_ENV` 查找。Lua 5.1 使用不同的函数环境机制。初学阶段先记住：

- 未声明的名字通常会访问全局环境。
- 模块内使用 local 可以减少污染和查找成本。
- 沙箱不能只靠改一个表就认为绝对安全；宿主暴露的库和对象同样需要限制。

---

## 6. 运算符

### 6.1 算术运算

```lua
local a = 7 + 3   -- 10
local b = 7 - 3   -- 4
local c = 7 * 3   -- 21
local d = 7 / 3   -- 除法
local e = 7 % 3   -- 1
local f = 2 ^ 3   -- 8，乘方
local g = -a      -- 一元负号
```

Lua 5.3+ 支持向下取整除法：

```lua
local q = 7 // 3 -- 2
```

`//` 不是 Lua 5.1/LuaJIT 通用语法，跨版本代码可根据运行时使用 `math.floor(a / b)`，但负数语义也必须确认。

Lua 的 `%` 与向下取整除法相配，负数结果可能不同于 C# 余数：

```lua
print(-3 % 2) -- 通常为 1
```

C# 的 `-3 % 2` 为 `-1`。涉及网格循环、索引环绕时不要直接照搬。

### 6.2 比较运算

```lua
a == b
a ~= b -- 不等于，Lua 不是 !=
a < b
a > b
a <= b
a >= b
```

注意：

- Lua 不使用 `!=`。
- 不同基础类型通常不自动按 C# 的方式比较，例如 `1 == "1"` 为 false。
- table、function、thread、userdata 默认按身份比较，不按内容深度比较。

```lua
local a = { hp = 100 }
local b = { hp = 100 }
print(a == b) -- false，不是同一个 table

local c = a
print(a == c) -- true
```

### 6.3 逻辑运算

```lua
not value
left and right
left or right
```

`and` 和 `or` 返回操作数本身，不一定返回 boolean：

```lua
print(nil or "fallback") -- fallback
print("ready" and 100)   -- 100
```

常见默认值写法：

```lua
local speed = config.speed or 5
```

但如果 `false` 是合法配置，这种写法会错误地使用默认值：

```lua
local enabled = config.enabled
if enabled == nil then
    enabled = true
end
```

伪三元表达式：

```lua
local result = condition and valueA or valueB
```

当 `valueA` 为 `false` 或 `nil` 时会错误返回 `valueB`，因此它不等价于完整的 C# `condition ? valueA : valueB`。

### 6.4 字符串连接与长度

```lua
local fullName = firstName .. " " .. lastName
local length = #fullName
```

- Lua 使用 `..` 连接字符串，不使用 C# 的 `+`。
- `#string` 通常返回字节数，不等于 UTF-8 中文字符数量。
- `#table` 只对无洞的序列有可靠直观含义，详见 Table 章节。

### 6.5 位运算

Lua 5.3+ 提供 `&`、`|`、`~`、`<<`、`>>`。Lua 5.2 常通过 `bit32` 库，LuaJIT 常用 `bit` 库。三者不能不加判断地混写。

### 6.6 常用优先级

从低到高可大致记为：

```text
or
and
比较运算（< > <= >= ~= ==）
位运算（版本相关）
..（字符串连接）
+ -
* / // %
一元运算（not # - ~）
^（乘方）
```

`..` 和 `^` 通常是右结合。复杂表达式不要依赖记忆，直接加括号表达意图。

---

## 7. 字符串

### 7.1 字符串字面量

```lua
local a = "double quoted"
local b = 'single quoted'
local c = "line1\nline2"
```

长字符串：

```lua
local prompt = [[
第一行
第二行
不需要写 \n
]]
```

当内容包含 `]]` 时可使用：

```lua
local text = [=[
内容中允许出现 ]]
]=]
```

### 7.2 字符串不可变

和 C# string 类似，Lua 字符串一旦创建就不能原地修改。大量循环拼接可能生成许多临时字符串：

```lua
local parts = {}
for i = 1, 100 do
    parts[i] = tostring(i)
end

local result = table.concat(parts, ",")
```

对应 C# 中大量拼接时使用 `StringBuilder` 的思路。

### 7.3 常用 string API

```lua
string.len("abc")                 -- 3
string.upper("hello")             -- HELLO
string.lower("HELLO")             -- hello
string.sub("abcdef", 2, 4)        -- bcd
string.find("hello", "ell")       -- 返回起止位置
string.gsub("a-b-c", "-", "/")   -- a/b/c，并额外返回替换次数
string.format("HP: %d/%d", 80, 100)
```

方法式写法也常见：

```lua
local upper = ("hello"):upper()
```

这是因为字符串的 metatable 把方法查找连接到 `string` 库，并不是字符串本身真的像 C# class 一样声明了成员方法。

### 7.4 Lua Pattern 不是正则表达式

`string.find`、`match`、`gmatch`、`gsub` 支持 Lua Pattern。它比完整正则表达式轻量，语法也不同。

```lua
local id = string.match("player_1024", "player_(%d+)")
print(id) -- 1024
```

常见模式：

| 模式 | 含义 |
| --- | --- |
| `%d` | 数字 |
| `%a` | 字母 |
| `%w` | 字母或数字 |
| `%s` | 空白 |
| `.` | 任意字符 |
| `*` | 前一项 0 次或多次，贪婪 |
| `-` | 前一项 0 次或多次，非贪婪 |
| `+` | 前一项 1 次或多次 |
| `?` | 前一项 0 次或 1 次 |
| `^` / `$` | 开头 / 结尾 |
| `(...)` | 捕获 |

Lua Pattern 中 `%` 承担转义和字符类别作用，不是正则常见的反斜杠规则。

### 7.5 UTF-8 注意事项

Lua 字符串本质上是字节序列：

```lua
print(#"你好") -- UTF-8 环境中通常是 6，不是 2
```

Lua 5.3+ 标准库提供基础 `utf8` 模块，例如 `utf8.len`。旧版本或 LuaJIT 需要宿主/第三方库。处理中文截断、输入框长度和逐字动画时，不能按字节随意 `string.sub`，否则可能切坏 UTF-8 编码。

---

## 8. Table：Lua 最核心的数据结构

Table 是 Lua 唯一的内建复合数据结构。它既能当数组，也能当字典、对象、模块、集合或图结构节点。

### 8.1 字典用法

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

`player.name` 是 `player["name"]` 的语法糖。键不是合法标识符时只能用方括号：

```lua
local data = {
    ["display-name"] = "Knight",
    [1001] = "item"
}
```

### 8.2 数组用法：默认从 1 开始

```lua
local items = { "Sword", "Potion", "Key" }

print(items[1]) -- Sword
print(items[3]) -- Key
```

C# 数组从 0 开始，而 Lua 社区和标准库按 1 开始的序列设计。虽然 table 可以使用索引 0，但 `ipairs`、`table.insert`、长度运算等不会把它自然当作标准序列的一部分。

### 8.3 构造器混合写法

```lua
local config = {
    "first",
    "second",
    id = "quest_001",
    [10] = "tenth"
}
```

前两个无键项依次使用索引 1、2；其他是显式键。

### 8.4 nil 会删除键

```lua
local inventory = { gold = 100 }
inventory.gold = nil
print(inventory.gold) -- nil
```

Table 不能以 nil 为键。NaN 也不能作为稳定合法的 table 键。值可以是除 nil 删除语义外的任意 Lua 值。

### 8.5 长度运算符与“洞”

无洞序列：

```lua
local values = { 10, 20, 30 }
print(#values) -- 3
```

有洞序列：

```lua
local values = { [1] = 10, [3] = 30 }
print(#values) -- 不要依赖具体结果
```

Lua 对有多个“边界”的 table 不保证 `#` 符合人类理解的元素数量。需要稳定数量时：

- 保持数组连续。
- 维护显式 `count`。
- 字典数量用 `pairs` 自己计数。
- 需要保存 nil 的参数列表时使用 `table.pack` 的 `n` 字段（版本支持需确认）。

### 8.6 遍历

数组：

```lua
for index, value in ipairs(items) do
    print(index, value)
end
```

字典：

```lua
for key, value in pairs(player) do
    print(key, value)
end
```

区别：

- `ipairs` 从 1 开始，通常遇到第一个 nil 就停止。
- `pairs` 遍历所有键，但顺序不保证。
- 不要把 `pairs` 当前恰好呈现的顺序当作业务规则、存档格式或网络协议顺序。

### 8.7 常用 table API

```lua
local values = { 10, 20 }

table.insert(values, 30)       -- 末尾插入
table.insert(values, 2, 15)    -- 在位置 2 插入
local removed = table.remove(values, 1)
table.sort(values)
local text = table.concat(values, ",")
```

`table.insert/remove` 在中间位置操作需要搬移后续元素，和 C# `List<T>` 类似，不是常数时间。

Lua 5.2+ 使用 `table.unpack`；Lua 5.1 常用全局 `unpack`：

```lua
local a, b = table.unpack({ 10, 20 }) -- 5.2+
```

### 8.8 Table 是引用语义

```lua
local original = { hp = 100 }
local alias = original
alias.hp = 50

print(original.hp) -- 50
```

赋值不会复制 table，只复制引用。需要浅拷贝：

```lua
local function shallowCopy(source)
    local result = {}
    for key, value in pairs(source) do
        result[key] = value
    end
    return result
end
```

深拷贝必须考虑：

- 循环引用。
- 共享引用是否应保留。
- metatable 是否复制。
- function、userdata、thread 如何处理。

不要随手写递归深拷贝后就认为适用于所有数据。

### 8.9 Table 与 C# 容器对照

| Lua 写法 | 常见用途 | C# 近似 |
| --- | --- | --- |
| `{ 10, 20, 30 }` | 连续序列 | `List<int>` / array |
| `{ hp = 100 }` | 字段集合、对象 | class instance / Dictionary |
| `{ [id] = value }` | 映射 | `Dictionary<TKey,TValue>` |
| `{ [value] = true }` | 集合 | `HashSet<T>` |
| 返回一张含函数的 table | 模块 | static class / service object |

---

## 9. 流程控制

### 9.1 if / elseif / else

```lua
if hp <= 0 then
    state = "dead"
elseif hp < 30 then
    state = "danger"
else
    state = "normal"
end
```

Lua 写 `elseif`，不是 C# 的 `else if`。

### 9.2 while

```lua
local count = 3

while count > 0 do
    print(count)
    count = count - 1
end
```

### 9.3 repeat / until

类似 C# 的 `do/while`，但条件含义相反：直到条件为真才结束。

```lua
local value

repeat
    value = getNextValue()
until value ~= nil
```

循环体至少执行一次。`repeat` 块中的 local 在 `until` 条件处仍可见。

### 9.4 数值 for

```lua
for i = 1, 5 do
    print(i) -- 1,2,3,4,5；终点包含在内
end

for i = 10, 1, -2 do
    print(i) -- 10,8,6,4,2
end
```

与 C# 对照：

```csharp
for (int i = 1; i <= 5; i++)
{
}
```

不要在循环体内依赖修改控制变量来控制数值 for；使用 while 表达动态控制更清楚。

### 9.5 泛型 for

```lua
for key, value in pairs(data) do
end

for index, value in ipairs(array) do
end
```

其本质基于迭代器函数，后文单独解释。

### 9.6 break、continue 与 goto

```lua
for i = 1, 10 do
    if i > 5 then
        break
    end
end
```

Lua 没有通用的 `continue` 关键字。常见替代：

- 反转条件，减少需要跳过的分支。
- 把循环体提取成函数，用 `return` 提前结束本次处理。
- Lua 5.2+ 可谨慎使用 `goto` 和标签模拟 continue，但跨版本代码不适合依赖。

```lua
for i = 1, 10 do
    if i % 2 == 0 then
        goto continue
    end

    print(i)
    ::continue::
end
```

上述写法不适用于 Lua 5.1。

---

## 10. 函数

Lua 函数是一等值，可以保存、传递和返回。

### 10.1 定义函数

```lua
local function add(a, b)
    return a + b
end
```

等价形式：

```lua
local add
add = function(a, b)
    return a + b
end
```

### 10.2 函数没有重载签名检查

Lua 不会像 C# 那样根据参数类型和数量选择重载。传少的参数会是 nil，多余参数通常被忽略：

```lua
local function printName(name)
    print(name)
end

printName()                 -- name = nil
printName("A", "ignored")  -- 第二个参数未被使用
```

公共边界应主动校验：

```lua
local function dealDamage(target, amount)
    assert(target ~= nil, "target is required")
    assert(type(amount) == "number", "amount must be a number")
    target.hp = target.hp - amount
end
```

### 10.3 函数作为参数

```lua
local function apply(values, operation)
    local result = {}
    for i, value in ipairs(values) do
        result[i] = operation(value)
    end
    return result
end

local doubled = apply({ 1, 2, 3 }, function(value)
    return value * 2
end)
```

对应 C# 的 delegate、`Func<T, TResult>` 和 lambda。

### 10.4 函数作为返回值

```lua
local function makeMultiplier(factor)
    return function(value)
        return value * factor
    end
end

local double = makeMultiplier(2)
print(double(5)) -- 10
```

### 10.5 默认参数

Lua 没有与 C# 完全相同的默认参数声明语法，通常在函数体处理：

```lua
local function move(speed)
    if speed == nil then
        speed = 5
    end
end
```

如果 `false` 可能是有效值，不要用 `value = value or default`。

### 10.6 尾调用

当函数直接返回另一个函数调用的全部结果时，Lua 可以进行正确尾调用优化：

```lua
local function nextState(state)
    return runState(state.next)
end
```

是否适合用递归表达业务仍要考虑可读性、调试栈和不同宿主实现。

---

## 11. 多返回值与可变参数

### 11.1 多返回值

```lua
local function divide(a, b)
    if b == 0 then
        return nil, "division by zero"
    end

    return a / b, nil
end

local result, err = divide(10, 2)
if not result then
    print(err)
end
```

这类似 C# 的 tuple、`TryXxx(out value)` 或结果对象，但 Lua 是语言原生的返回值列表。

### 11.2 返回值展开规则

函数调用处于表达式列表最后时，通常展开全部返回值：

```lua
local function values()
    return 1, 2, 3
end

local a, b, c = values() -- 1,2,3
local x, y = 0, values() -- x=0, y=1，其余没有接收
```

不在列表最后时通常只保留第一个结果：

```lua
local a, b, c = values(), 99 -- a=1, b=99, c=nil
```

括号可以强制只取第一个返回值：

```lua
local onlyFirst = (values())
```

Table 构造器最后一个函数调用也可能展开多个结果：

```lua
local list = { 0, values() } -- 0,1,2,3
```

### 11.3 可变参数 ...

```lua
local function sum(...)
    local values = { ... }
    local total = 0

    for _, value in ipairs(values) do
        total = total + value
    end

    return total
end

print(sum(1, 2, 3)) -- 6
```

如果参数中可能含 nil，简单 `{ ... }` 配合 `#` 会丢失可靠数量。Lua 5.2+ 可用 `table.pack`：

```lua
local args = table.pack(...)
for i = 1, args.n do
    print(args[i])
end
```

Lua 5.1 需要自行保存 `select("#", ...)`：

```lua
local count = select("#", ...)
local third = select(3, ...)
```

---

## 12. 冒号语法与 self

Lua 中的冒号是最常见也最容易混淆的语法糖。

### 12.1 定义方法

```lua
local Player = {}

function Player:takeDamage(amount)
    self.hp = self.hp - amount
end
```

等价于：

```lua
function Player.takeDamage(self, amount)
    self.hp = self.hp - amount
end
```

### 12.2 调用方法

```lua
player:takeDamage(10)
```

等价于：

```lua
player.takeDamage(player, 10)
```

### 12.3 点号与冒号混错

```lua
player.takeDamage(10)
```

如果函数是按冒号方式定义的，上面会把 `10` 当作 self，`amount` 变成 nil，随后报错。

反过来，如果普通函数不需要 self，却用冒号调用，就会多传一个对象参数。

C# 实例方法会隐式拥有 `this`；Lua 冒号只是帮助传入第一个 `self` 参数，没有强制的类或实例检查。

---

## 13. 闭包与 Upvalue

### 13.1 什么是闭包

内部函数可以访问外层函数的局部变量。外层函数返回后，只要内部函数仍在使用这些变量，它们就会继续存活。

```lua
local function createCounter(start)
    local count = start or 0

    return function()
        count = count + 1
        return count
    end
end

local counterA = createCounter(0)
local counterB = createCounter(100)

print(counterA()) -- 1
print(counterA()) -- 2
print(counterB()) -- 101
```

这里的 `count` 是返回函数捕获的 **upvalue**。每次调用 `createCounter` 都会形成独立状态。

C# 对照：

```csharp
Func<int> CreateCounter(int start)
{
    int count = start;
    return () => ++count;
}
```

### 13.2 用闭包实现私有状态

Lua 没有 `private` 关键字，可以让局部变量只通过闭包访问：

```lua
local function createWallet(initialGold)
    local gold = initialGold

    return {
        add = function(amount)
            gold = gold + amount
        end,

        spend = function(amount)
            if amount > gold then
                return false
            end

            gold = gold - amount
            return true
        end,

        getGold = function()
            return gold
        end
    }
end
```

外部拿不到 `gold` 本身，只能通过返回的函数操作。

### 13.3 捕获的是变量，不一定是当时的值

```lua
local value = 10
local getValue = function()
    return value
end

value = 20
print(getValue()) -- 20
```

和 C# 闭包类似，闭包通常捕获变量所在的存储，而不是自动复制创建闭包那一刻的值。

循环中创建回调时，为了兼容不同 Lua 版本并明确意图，创建局部快照：

```lua
local callbacks = {}

for i = 1, 3 do
    local capturedIndex = i
    callbacks[i] = function()
        print(capturedIndex)
    end
end
```

### 13.4 闭包的成本

闭包非常有用，但每次创建新闭包都可能产生新的函数对象和 upvalue。以下场景要留意：

- 每帧循环中大量创建临时 lambda 式函数。
- 为成千上万个对象各自创建一组相同闭包。
- 闭包捕获大型 table 或 C# 对象，使其生命周期意外延长。

先用 Profiler 证明确实有问题，再决定缓存函数或改为显式对象；不要因为担心性能而放弃清晰代码。

---

## 14. 迭代器

### 14.1 pairs 和 ipairs 也是迭代器

```lua
for key, value in pairs(data) do
end
```

泛型 `for` 背后使用：

1. 迭代函数。
2. 不变状态。
3. 控制变量。

每次循环调用迭代函数，直到第一个返回值为 nil。

### 14.2 使用闭包创建简单迭代器

```lua
local function range(from, to, step)
    local current = from - step

    return function()
        current = current + step
        if current <= to then
            return current
        end
    end
end

for value in range(2, 10, 2) do
    print(value) -- 2,4,6,8,10
end
```

这是有状态闭包迭代器，易读但会创建闭包。

### 14.3 无状态迭代器概念

```lua
local function nextArray(array, index)
    index = index + 1
    local value = array[index]

    if value ~= nil then
        return index, value
    end
end

local function arrayIterator(array)
    return nextArray, array, 0
end

for index, value in arrayIterator({ "A", "B" }) do
    print(index, value)
end
```

这里迭代状态由泛型 for 传回，不需要每次构造捕获状态的闭包。

### 14.4 遍历期间修改 Table

遍历 table 时新增、删除键的行为不适合作为稳定业务规则。安全做法：

- 先收集待删除键，遍历结束后删除。
- 连续数组倒序删除，避免索引整体前移影响尚未访问的元素。
- 或构建新的结果 table。

```lua
for i = #enemies, 1, -1 do
    if enemies[i].dead then
        table.remove(enemies, i)
    end
end
```

---

## 15. 模块与 require

### 15.1 推荐的模块写法

`PlayerConfig.lua`：

```lua
local PlayerConfig = {}

local DEFAULT_SPEED = 5

function PlayerConfig.getMoveSpeed(level)
    return DEFAULT_SPEED + level * 0.2
end

return PlayerConfig
```

使用：

```lua
local PlayerConfig = require("PlayerConfig")
local speed = PlayerConfig.getMoveSpeed(10)
```

模块自身实现用 local 隐藏，只把需要公开的内容放进返回 table。

### 15.2 require 会缓存模块

第一次 `require("PlayerConfig")` 时通常会：

1. 按 `package.path` / `package.cpath` 或宿主自定义加载器寻找模块。
2. 加载并执行 chunk。
3. 把结果缓存到 `package.loaded`。
4. 返回模块值。

后续 require 通常直接返回缓存，不会重新执行整个文件。

这意味着：

- 模块顶层初始化通常只执行一次。
- 模块返回 table 的可变字段可能成为全局共享状态。
- 测试之间若复用同一 Lua VM，模块状态可能残留。
- “热重载”不是简单再次 require；清缓存后还要处理旧引用和运行时状态迁移。

### 15.3 模块名与路径

常见约定：

```lua
local QuestService = require("Game.Quest.QuestService")
```

加载器通常把点号转换成路径分隔，例如：

```text
Game/Quest/QuestService.lua
```

Unity Lua 框架经常替换或扩展加载器，从 TextAsset、AssetBundle、Addressables 或自定义文件系统读取，所以最终规则取决于选定运行时。

### 15.4 避免旧式 module(...)

Lua 5.1 的旧代码可能使用 `module(...)`。这种方式会改变环境并隐式操作全局，已不适合作为新项目默认模式。新代码优先“局部模块 table + return”。

### 15.5 循环依赖

模块 A require B，同时 B require A，可能拿到未完成初始化的模块或导致加载错误。处理方式：

- 抽取共同依赖到第三个模块。
- 用依赖注入把引用从组合根传入。
- 延迟到函数调用时再查找，但要明确这只是权衡，不要用它掩盖混乱结构。

### 15.6 load、loadfile、dofile

这些 API 可加载代码字符串或文件，版本签名不同：

- `load`：把字符串或 reader 提供的内容编译成函数。
- `loadfile`：加载文件为函数。
- `dofile`：加载并立即执行文件。

在 Unity 嵌入环境中，文件访问可能不可用或被框架重定向。对不可信内容调用动态加载还会产生严重安全风险。玩家输入、服务器普通数据和 AI 输出都不应直接当 Lua 代码执行。

---

## 16. 错误处理

Lua 没有 C# 的 `try/catch/finally` 语法，核心机制是 `error`、`assert`、`pcall` 和 `xpcall`。

### 16.1 抛出错误

```lua
local function setHealth(value)
    if type(value) ~= "number" then
        error("health must be a number")
    end
end
```

`error(message, level)` 的 level 可调整错误位置归属。公共工具函数可让错误指向调用者，但滥用层级会让栈信息更难理解。

### 16.2 assert

```lua
local player = assert(findPlayer(), "player not found")
```

当第一个参数是假值时，`assert` 抛错；成功时返回传入值，可用于边界前置条件。

不要用 assert 代替正常可恢复分支。例如“玩家金币不足”是业务结果，不应作为程序错误抛出。

### 16.3 pcall：保护调用

```lua
local ok, resultOrError = pcall(function()
    return riskyOperation()
end)

if ok then
    print("result", resultOrError)
else
    print("error", resultOrError)
end
```

如果目标函数返回多个值，`pcall` 会在第一个状态布尔之后返回它们：

```lua
local ok, a, b = pcall(function()
    return 10, 20
end)
```

### 16.4 xpcall：自定义错误处理

```lua
local ok, result = xpcall(
    function()
        return riskyOperation()
    end,
    function(err)
        return debug.traceback(tostring(err), 2)
    end
)

if not ok then
    print(result)
end
```

`xpcall` 适合补充堆栈。不同 Lua 版本对额外参数的支持不同，跨版本代码用闭包传参更稳妥。

### 16.5 没有 finally 怎么办

通用 Lua 中可以显式保护并在之后清理：

```lua
local resource = acquireResource()

local ok, result = xpcall(
    function()
        return useResource(resource)
    end,
    debug.traceback
)

releaseResource(resource)

if not ok then
    error(result)
end
```

Lua 5.4 的 to-be-closed 变量和 `__close` 能表达作用域清理，但不是 5.1/LuaJIT 通用能力。与 C# 或 Unity 资源交互时，仍应按所选绑定框架设计明确的 Dispose/释放协议。

### 16.6 错误边界原则

- Lua 模块内部：让真正的编程错误尽快失败，并保留堆栈。
- Lua → C# 边界：捕获并附带模块、函数、实体 ID 等上下文。
- C# → Lua 回调：确认桥接框架如何把 C# exception 转成 Lua error。
- 不要在最底层把所有错误吞掉并只返回 nil，否则根因会消失。
- 不要在每帧 Update 中反复触发同一错误，必要时停用故障逻辑或限频记录。

---

## 17. Metatable 与元方法

Metatable 让 table 或 userdata 在缺少字段、运算、调用等操作时拥有自定义行为。它是 Lua 面向对象和代理机制的基础。

### 17.1 设置和读取 metatable

```lua
local object = {}
local meta = {}

setmetatable(object, meta)
print(getmetatable(object) == meta) -- true
```

普通 table 默认没有 metatable。

### 17.2 __index：缺失字段查找

```lua
local defaults = {
    speed = 5,
    damage = 10
}

local config = {
    damage = 20
}

setmetatable(config, { __index = defaults })

print(config.damage) -- 20，自身字段优先
print(config.speed)  -- 5，自身缺失后从 defaults 查找
```

`__index` 也可以是函数：

```lua
setmetatable(config, {
    __index = function(target, key)
        error("unknown config key: " .. tostring(key))
    end
})
```

### 17.3 __newindex：缺失字段写入

```lua
local storage = {}
local proxy = {}

setmetatable(proxy, {
    __newindex = function(_, key, value)
        storage[key] = value
    end,

    __index = storage
})

proxy.hp = 100
print(storage.hp) -- 100
```

关键陷阱：`__newindex` 通常只在目标自身没有该键时触发。如果键已经真实存在，普通赋值会直接覆盖，绕过元方法。需要始终代理时，让实际数据保存在另一个 storage table，并保持 proxy 自身无业务字段。

### 17.4 常见元方法

| 元方法 | 触发场景 |
| --- | --- |
| `__index` | 读取缺失键 |
| `__newindex` | 写入缺失键 |
| `__tostring` | `tostring(value)` |
| `__call` | 把 table/userdata 当函数调用 |
| `__len` | 长度运算 `#value`（版本和类型支持有差异） |
| `__eq` | 相等比较，具体触发规则有版本差异 |
| `__lt` / `__le` | `<` / `<=` |
| `__add` / `__sub` | `+` / `-` |
| `__mul` / `__div` | `*` / `/` |
| `__concat` | `..` |
| `__pairs` | 自定义 `pairs`（Lua 5.2+ 等版本支持差异） |
| `__gc` | 垃圾回收终结行为，支持对象类型随版本不同 |
| `__close` | Lua 5.4 作用域关闭 |

### 17.5 运算符重载示例

```lua
local Vector2 = {}
Vector2.__index = Vector2

function Vector2.new(x, y)
    return setmetatable({ x = x, y = y }, Vector2)
end

function Vector2.__add(left, right)
    return Vector2.new(left.x + right.x, left.y + right.y)
end

function Vector2.__tostring(value)
    return string.format("(%s, %s)", value.x, value.y)
end

local result = Vector2.new(1, 2) + Vector2.new(3, 4)
print(result) -- (4, 6)
```

### 17.6 rawget、rawset 与 rawequal

这些函数绕过部分元方法：

```lua
local direct = rawget(object, "key")
rawset(object, "key", value)
local same = rawequal(a, b)
```

适合实现代理和元方法内部逻辑，避免递归触发自己。业务层不应为了绕过校验随意使用。

### 17.7 保护 metatable

如果 metatable 定义 `__metatable`，普通 `getmetatable` 会返回这个保护值，`setmetatable` 也会被阻止：

```lua
local meta = {
    __metatable = "locked"
}
```

这只是语言层的封装手段，不是面对恶意脚本的完整安全沙箱。

---

## 18. 用 Table 实现面向对象

Lua 没有原生 `class` 关键字，但可用 table + metatable 建立类式写法。

### 18.1 最小类模式

```lua
local Player = {}
Player.__index = Player

function Player.new(name, maxHp)
    local self = {
        name = name,
        hp = maxHp,
        maxHp = maxHp
    }

    return setmetatable(self, Player)
end

function Player:takeDamage(amount)
    self.hp = math.max(0, self.hp - amount)
end

function Player:isDead()
    return self.hp <= 0
end

return Player
```

使用：

```lua
local Player = require("Player")
local hero = Player.new("Knight", 100)

hero:takeDamage(25)
print(hero.hp) -- 75
```

查找 `hero.takeDamage` 时：

1. hero 自身没有该键。
2. hero 的 metatable 是 Player。
3. `Player.__index = Player`，于是从 Player table 找到方法。

### 18.2 继承示例

```lua
local Animal = {}
Animal.__index = Animal

function Animal.new(name)
    return setmetatable({ name = name }, Animal)
end

function Animal:speak()
    return "..."
end

local Dog = setmetatable({}, { __index = Animal })
Dog.__index = Dog

function Dog.new(name)
    local self = Animal.new(name)
    return setmetatable(self, Dog)
end

function Dog:speak()
    return "woof"
end
```

这里有两条查找链：

- 实例找 Dog 方法：实例 metatable 的 `__index = Dog`。
- Dog 找继承的 Animal 方法：Dog 自身 metatable 的 `__index = Animal`。

### 18.3 Lua 的“类”只是约定

和 C# 不同：

- 没有编译器保证字段一定存在。
- 没有 private/protected。
- 没有方法重载。
- 没有 interface 关键字。
- 任意代码都可能给实例新增字段。
- `is`、泛型约束、反射语义都取决于自建框架或绑定层。

需要接口时通常使用鸭子类型加边界校验：

```lua
local function useDamageable(target)
    assert(type(target.takeDamage) == "function", "target must support takeDamage")
    target:takeDamage(10)
end
```

### 18.4 优先考虑组合

Lua 的继承链越深，`__index` 查找和状态来源越难理解。游戏逻辑通常更适合组件式组合：

```lua
local actor = {
    health = Health.new(100),
    inventory = Inventory.new(),
    skills = SkillSet.new()
}
```

这和 Unity 的组件思维更一致，也比模拟庞大的 C# 类继承树更容易热重载和测试。

### 18.5 模块、类和单例不要混淆

```lua
local Service = {
    cache = {}
}

return Service
```

这个 require 返回的 table 通常是共享模块实例，接近单例。若要创建多个对象，应明确暴露 `new()`，不要让调用者误以为模块 table 是实例模板。

---

## 19. 协程

Lua coroutine 是协作式执行单元，不是操作系统线程，也不会自动并行。

### 19.1 create、resume、yield

```lua
local co = coroutine.create(function()
    print("step 1")
    local input = coroutine.yield("waiting")
    print("received", input)
    return "done"
end)

local ok, value = coroutine.resume(co)
print(ok, value) -- true, waiting

ok, value = coroutine.resume(co, 42)
print(ok, value) -- true, done
```

流程：

1. `create` 创建 suspended 协程，不立即运行函数体。
2. `resume` 开始或继续执行。
3. `yield` 暂停，并把值返回给 resume 调用方。
4. 下一次 resume 的额外参数成为 yield 表达式的返回值。
5. 函数正常 return 后协程变为 dead。

### 19.2 status

```lua
print(coroutine.status(co))
```

常见状态：

- `suspended`：尚未开始或已 yield。
- `running`：正在运行。
- `normal`：某些嵌套 resume 场景中处于活动但非当前运行。
- `dead`：结束或发生未处理错误。

### 19.3 wrap

```lua
local nextValue = coroutine.wrap(function()
    coroutine.yield(1)
    coroutine.yield(2)
    return 3
end)

print(nextValue()) -- 1
print(nextValue()) -- 2
print(nextValue()) -- 3
```

`wrap` 返回一个函数，使用更简洁，但错误传播形式和 `resume` 返回 `ok` 的方式不同。需要精确控制错误时，显式 `create/resume` 更清楚。

### 19.4 Lua coroutine 与 Unity Coroutine 的区别

| Lua coroutine | Unity Coroutine |
| --- | --- |
| Lua VM 提供的语言机制 | Unity MonoBehaviour 调度机制 |
| `coroutine.yield()` 本身不认识秒、帧、Unity YieldInstruction | `yield return null`、`WaitForSeconds` 等由 Unity 识别 |
| 谁 resume 由 Lua 代码或宿主调度器决定 | Unity 在玩家循环中继续 IEnumerator |
| 不自动访问 Unity API | IEnumerator 运行在 Unity 主线程并可使用 Unity API |

Unity Lua 框架通常会提供一层调度，把“等待一帧、等待秒数、等待异步请求”转换成 Lua coroutine 的 yield/resume。具体 API 完全依赖所选框架，不能把纯 Lua 示例直接当 Unity 调度 API。

### 19.5 协程错误与边界

- `coroutine.resume` 失败时返回 `false, error`，不会自动在外层抛出同样形式的错误。
- 某些 Lua 版本或 C API 调用边界不允许随意 yield。
- 协程被暂停时，它捕获的局部变量和引用仍然存活。
- Unity 对象销毁时，应取消或标记相关 Lua 协程，避免恢复后访问已销毁代理。
- 协程适合分步等待，不适合 CPU 密集并行计算。

---

## 20. 垃圾回收与资源生命周期

### 20.1 Lua GC 管什么

Lua 会自动回收不再可达的 table、function、thread、string 和部分 userdata。不同版本可能使用增量或分代等回收模式。

```lua
local data = { huge = createHugeTable() }
data = nil -- 只是移除一个引用，何时回收由 GC 决定
```

### 20.2 GC 不等于立即释放 Unity/C# 资源

Unity 纹理、GameObject、文件句柄、网络连接和 C# 事件订阅不应只依赖 Lua GC：

- Lua 代理被回收，不一定等于 UnityEngine.Object 被 Destroy。
- Unity 对象被 Destroy 后，Lua 侧代理可能仍存在，但已指向无效对象。
- C# event 持有 Lua delegate 时，Lua 回调可能一直被 C# 引用，无法回收。
- 文件、Socket 和 native handle 需要明确关闭。

推荐显式生命周期：

```lua
function View:onOpen()
    self.subscription = eventBus:subscribe(self.onEvent)
end

function View:onClose()
    if self.subscription then
        self.subscription:dispose()
        self.subscription = nil
    end
end
```

实际订阅 API 由框架决定，核心原则是订阅和取消必须成对。

### 20.3 循环引用

纯 Lua 的可达性 GC 通常能回收不可达循环：

```lua
local a = {}
local b = {}
a.other = b
b.other = a

a = nil
b = nil
```

但跨 Lua/C# 边界可能形成双方各自认为“对方还引用着”的生命周期环：

```text
C# Event → Lua Delegate → Lua Object → C# Owner
```

这类问题必须显式取消订阅和释放代理，不能只等任一侧 GC。

### 20.4 弱表

Metatable 的 `__mode` 可创建弱键/弱值 table：

```lua
local cache = setmetatable({}, {
    __mode = "v" -- 弱值
})
```

模式：

- `"k"`：弱键。
- `"v"`：弱值。
- `"kv"`：键和值都弱。

弱表适合缓存，但回收时机不可预测，业务正确性不能依赖值“暂时还没被回收”。

### 20.5 collectgarbage

```lua
collectgarbage("collect")
local kb = collectgarbage("count")
```

可用于诊断或调优，但不应在普通游戏流程里频繁强制完整 GC。完整回收可能造成明显卡顿。应先用目标设备 Profiler 观察分配速率、暂停和内存，再调整回收策略。

### 20.6 终结器不是可靠业务流程

`__gc` 支持的对象类型和行为随 Lua 版本不同，调用时机也不确定。存档、战斗结算、网络消息和 Unity 对象销毁不能依赖终结器。

Lua 5.4 的 `<close>` / `__close` 提供更确定的作用域退出清理，但选用旧 VM 或 LuaJIT 时不可用。

---

## 21. Lua 与 C# 的核心差异总表

| C# | Lua | 迁移时的思维调整 |
| --- | --- | --- |
| 静态类型变量 | 动态类型值 | 公共边界主动校验类型 |
| `null` | `nil` | table 字段赋 nil 会删除键 |
| `false` 才是假布尔 | 只有 false 和 nil 为假 | 0 和空字符串都是真 |
| 数组索引从 0 开始 | 标准序列通常从 1 开始 | 循环、桥接数组特别注意偏移 |
| class / struct | table + metatable | “类”是约定，不是编译器保证 |
| `this` | `self` 普通参数 | `:` 只是自动传第一个参数 |
| `!=` | `~=` | 语法不同 |
| 字符串 `+` / 插值 | `..` / `string.format` | 大量拼接用 `table.concat` |
| `&&`、`||`、`!` | `and`、`or`、`not` | and/or 返回操作数，不一定是 bool |
| `try/catch` | `pcall/xpcall` | 明确保护边界并保留 traceback |
| Tuple / out | 多返回值 | 记住表达式列表展开规则 |
| delegate/lambda | function | 函数是一等值 |
| IEnumerable | 迭代器 + 泛型 for | pairs 无顺序，ipairs 遇 nil 停止 |
| namespace/static class | 返回 table 的模块 | require 会缓存模块 |
| object equality 可重载 | table 默认身份相等，可用 `__eq` | 不会自动深比较 |
| `Count` 明确 | `#table` 仅适合序列 | 有洞 table 不能靠 `#` 计数 |
| enum | 常量 table / 字符串 / 数字约定 | 需要自行防止非法值 |
| interface | 鸭子类型或自建契约 | 在系统边界断言必要成员 |
| async/Task | coroutine + 宿主调度 | Lua coroutine 不会自动等待 Unity 帧 |
| IDisposable | 显式 close/dispose；5.4 有 close 机制 | 跨语言资源必须明确释放 |

---

## 22. Unity/C# 与 Lua 交互

本节讲架构与风险，不给出某个插件的可直接复制 API，因为当前项目尚未选定运行时。

### 22.1 常见 Unity Lua 方案

| 方案 | 大致特点 | 选型时重点确认 |
| --- | --- | --- |
| xLua | 常见的 Unity/C# 与 Lua 集成方案，支持代码生成与热修复相关能力 | 内部 Lua 版本、IL2CPP 生成配置、平台支持、维护状态 |
| ToLua 系方案 | 游戏项目中使用历史较长，通常围绕 LuaJIT/绑定生成 | Lua 版本、生成流程、团队现有工具链 |
| MoonSharp | C# 实现的 Lua 风格解释器，托管环境集成直接 | 与标准 Lua 的兼容范围、性能、Unity 平台表现 |
| 自定义 Native Lua 绑定 | 控制力强 | C API、跨平台编译、AOT、维护成本和安全责任都由团队承担 |

选型不能只看“能运行 Hello World”，还要验证：

- Windows、Android、iOS 等目标平台。
- Mono 与 IL2CPP。
- AOT 泛型、delegate、接口、ref/out、重载支持。
- Lua 版本和第三方库兼容性。
- 调试器、代码补全、性能分析和崩溃栈。
- 脚本打包、加密/签名、更新和版本回滚。
- 开源许可、维护状态和团队经验。

### 22.2 典型架构

```text
Unity / C# 稳定层
├─ Unity 生命周期与场景对象
├─ 输入、物理、动画、渲染、资源加载
├─ 网络、存档、安全校验
└─ 向 Lua 暴露受控服务接口
              │
              │ 低频、批量、语义化调用
              ▼
Lua 业务层
├─ 技能与任务规则
├─ UI 流程
├─ 活动与配置驱动逻辑
└─ 可快速迭代的玩法编排
```

不要把整个 Unity API 无限制暴露给 Lua，也不要让 C# 与 Lua 每帧进行成千上万次细粒度属性访问。

### 22.3 类型映射不是天然统一

常见概念映射：

| C# | Lua 侧可能表现 | 风险 |
| --- | --- | --- |
| `null` | nil 或特殊空代理 | UnityEngine.Object 的伪 null 语义可能不同 |
| `bool` | boolean | 通常直接 |
| `int/float/double` | number | 精度、整数子类型和装箱取决于 VM/绑定 |
| `long/ulong` | number、userdata 或专用封装 | 大整数精度特别重要 |
| `string` | string | UTF-8/UTF-16 转换和分配 |
| array / List | userdata 代理或 table | 索引从 0 还是 1 取决于绑定 API |
| Dictionary | userdata 代理或 table | 迭代器、键类型和分配 |
| class instance | userdata / table proxy | 生命周期、重载解析、属性访问成本 |
| delegate/event | function 代理 | 必须取消订阅，防止跨 VM 引用泄漏 |
| enum | userdata、number 或字符串 | 不能假定可直接与数字比较 |

接入后必须为团队写一份“实际绑定规则表”，不能只依赖通用 Lua 知识猜测。

### 22.4 C# 调 Lua 的概念流程

```text
C# 获取 Lua VM
  → 加载模块
  → 取得函数或生成接口代理
  → 转换参数
  → 保护调用
  → 转换返回值
  → 处理 Lua traceback
```

概念示例，不代表任何具体框架 API：

```csharp
// 伪代码：真实 API 由最终 Lua 框架决定
LuaResult result = luaRuntime.Call(
    module: "Game.Skill.SkillService",
    function: "TryCast",
    arguments: new object[] { actorId, skillId });
```

推荐把 Lua 模块映射为少量稳定接口，而不是在 C# 各处按字符串随意查全局函数。

### 22.5 Lua 调 C# 的概念流程

```text
Lua 业务代码
  → 调用绑定层暴露的服务
  → 绑定层校验和转换参数
  → C# 服务执行 Unity 操作
  → 返回纯结果数据
```

概念示例：

```lua
-- 伪代码：CS.Game.SkillService 的名字和用法取决于绑定框架
local accepted, reason = CS.Game.SkillService.TryCast(actorId, skillId)
```

更推荐注入窄接口：

```lua
local SkillLogic = {}

function SkillLogic.create(services)
    return {
        tryCast = function(actorId, skillId)
            return services.skill:tryCast(actorId, skillId)
        end
    }
end
```

这样纯 Lua 测试可以传入假服务，不必启动 Unity。

### 22.6 跨边界性能

跨 Lua/C# 调用通常包含参数检查、装箱、代理和 VM 边界成本。原则：

- 少量、语义化、批量调用。
- 避免 Lua 每帧逐个读取大量 Transform 属性。
- 避免 C# Update 每帧按字符串查找 Lua 全局函数。
- 缓存稳定的模块、接口代理、delegate 和 Hash。
- 大批实体模拟放在同一侧完成，再批量同步结果。
- 性能结论必须以真机和最终绑定框架的 Profiler 数据为准。

低效方向：

```text
Lua 循环 5000 个实体
  → 每个实体调用 C# GetPosition
  → 每个分量又跨一次边界
```

更好的方向：

```text
Lua 发出一次查询/命令
  → C# 批量处理实体
  → 返回紧凑结果或只发布变化事件
```

### 22.7 IL2CPP 与 AOT

IL2CPP 构建时不能像完整 JIT 环境那样随意生成新的 C# 泛型实例或动态代理。Lua 框架常通过代码生成、类型白名单或预生成 wrapper 解决。

必须验证：

- Lua 可能调用的 C# 类型、构造函数、泛型和 extension method 是否已生成。
- delegate 签名是否已注册。
- ref/out、多返回值和重载在 AOT 下的行为。
- Linker 是否裁剪了只从 Lua 访问的类型。
- Development Build 正常不代表 Release IL2CPP 一定正常。

### 22.8 UnityEngine.Object 的特殊 null

Unity 销毁对象后，C# 包装对象可能仍存在，但重载的 `== null` 返回 true。Lua 绑定层不一定完全复刻这个语义：

```text
Lua 变量持有代理 ≠ 底层 Unity 对象仍然有效
```

调用 Unity API 前应通过框架推荐方式验证对象有效性，并在场景卸载、对象池回收或 Destroy 时清理 Lua 引用。

### 22.9 线程规则

大多数 UnityEngine API 只能在 Unity 主线程访问。即使 Lua VM 被放在后台线程，也不能因此从 Lua 随意操作 GameObject、Transform 或 UI。

推荐：

- Unity 对象操作回到主线程服务。
- 后台只处理明确线程安全的纯数据。
- Lua VM 是否允许多线程访问取决于实现，默认不要跨线程同时调用同一个 VM。

### 22.10 安全与脚本更新

Lua 能执行代码，风险高于普通 JSON 配置：

- 不执行玩家输入、聊天文本、AI 回复或服务器普通字段作为 Lua 代码。
- 不可信脚本不暴露 `os`、`io`、`package`、`debug`、动态加载和任意 C# 反射入口。
- 下载脚本需要 HTTPS、签名/哈希验证、版本兼容和回滚。
- 限制可调用服务、执行时间、内存和指令量。
- 同进程脚本沙箱很难成为绝对安全边界；真正敌对代码应隔离执行。
- 热更新还要遵守目标平台政策，不能只从技术可行性判断。

---

## 23. My RPG 项目的推荐边界

当前项目的动画目标架构是：

```text
AnimCtrl（指令） → AnimatorManager ← StateMachine（状态数据）
```

如果未来接入 Lua，建议保留这三个部分在 C#：

- `AnimCtrl`：角色动画语义指令入口。
- `AnimatorManager`：唯一直接操作 Unity Animator 的业务入口。
- `StateMachine`：Animator StateMachineBehaviour 状态回报层。

Lua 不应直接散落调用：

```lua
animator:SetTrigger("Skill")
animator:SetFloat("Horizontal", value)
```

否则 Animator 参数名、优先级和状态约束会再次分散，破坏现有技术策划。

### 23.1 推荐的数据流

```text
Lua 技能/AI/任务逻辑
        │ 语义指令：actorId、skillId、目标等
        ▼
C# Gameplay/Skill Service
        │ 校验冷却、资源、角色状态
        ▼
AnimCtrl
        │ 动画指令
        ▼
AnimatorManager
        │ 操作 Animator
        ▼
StateMachine / Animation Event
        │ 粗粒度状态事件
        ▼
C# 战斗结算；必要时再通知 Lua
```

### 23.2 适合优先留在 C# 的内容

- Unity 生命周期、GameObject 与 Component 引用。
- 输入采集、CharacterController、Rigidbody 和碰撞。
- Animator 参数、StateMachineBehaviour、动画事件适配。
- 伤害最终结算、反作弊或必须保持权威的规则。
- 资源加载、存档 IO、网络底层和密钥处理。
- 高频逐帧循环和大量实体的性能敏感部分。

### 23.3 适合考虑放在 Lua 的内容

- 技能组合和数值规则。
- 任务步骤、对话流程和 NPC 行为编排。
- UI 页面流程与活动逻辑。
- 变化频繁、希望快速迭代的玩法规则。
- 数据驱动的条件判断，但普通静态数据也可以继续用 ScriptableObject/JSON，不必为了配置强行引入 Lua。

### 23.4 建议的跨层命令

Lua 向 C# 传递语义，而不是 Unity 细节：

```lua
local command = {
    actorId = 1001,
    type = "CastSkill",
    skillId = "fire_slash",
    targetId = 2003
}
```

C# 收到后负责：

1. 校验 actor 和 target。
2. 校验技能、冷却、消耗与当前状态。
3. 调用 AnimCtrl。
4. 由 AnimatorManager 选择真实 Animator 参数。
5. 在动画生效阶段完成伤害或发布事件。

Lua 不应该传 `"SkillTriggerParameterName"`、Animator State Hash 或 Unity instanceId 作为长期业务协议。

### 23.5 状态回传要粗粒度

StateMachine 的 `OnStateUpdate` 可能每帧发生。如果每帧把完整数据跨边界传给 Lua，会增加成本和耦合。

优先只回传业务事件：

```text
SkillStarted
HitWindowOpened
HitWindowClosed
SkillFinished
AnimationInterrupted
ActorDied
```

若纯 C# 战斗系统已经能消费，就不必为了“Lua 知道一切”再次传回 Lua。

### 23.6 接入前必须先做的最小验证

1. 选择 Lua VM 和 Unity 框架，固定版本。
2. 在 Editor、Windows IL2CPP 和一个目标移动平台运行同一测试。
3. 验证 C# 调 Lua、Lua 调 C#、异常栈和模块加载。
4. 验证 GameObject Destroy 后代理行为。
5. 验证 delegate/event 订阅与释放。
6. 验证整数 ID、中文字符串、table/List/Dictionary 转换。
7. 验证打包、脚本更新、哈希校验和版本回滚。
8. 用 Profiler 测试一帧内高频边界调用的真实成本。

---

## 24. 工程规范与性能

### 24.1 默认 local

```lua
local MathUtil = require("Common.MathUtil")
local max = math.max
```

局部变量可以避免全局污染，通常查找也更直接。不要为了微小性能把所有库函数都机械缓存；只在高频热点和可读性允许时使用。

### 24.2 模块返回明确 API

```lua
local QuestService = {}

function QuestService.start(questId)
end

return QuestService
```

不要依赖模块加载顺序向全局表偷偷挂大量字段。

### 24.3 命名约定示例

Lua 没有官方强制风格，团队必须统一。可采用：

| 对象 | 建议示例 |
| --- | --- |
| 局部变量/函数 | `current_hp` 或 `currentHp`，二选一统一 |
| 模块/类式 table | `QuestService`、`PlayerState` |
| 常量 | `MAX_RETRY_COUNT` |
| 私有实现 | 用 local 隐藏；下划线前缀只能表达约定，不能真正限制访问 |
| boolean | `is_dead`、`has_target`、`can_move` |

与现有 C# 项目协作时，可以在跨语言接口保持相同业务词汇，不必强行保持完全相同大小写风格。

### 24.4 类型注解

Lua 本身动态类型，但 Lua Language Server/EmmyLua 风格注解可改善补全和静态检查：

```lua
---@class PlayerData
---@field id integer
---@field name string
---@field hp number
---@field skills string[]

---@param player PlayerData
---@param amount number
---@return boolean died
local function applyDamage(player, amount)
    player.hp = math.max(0, player.hp - amount)
    return player.hp == 0
end
```

注解不是运行时校验，系统边界仍要验证不可信数据。

### 24.5 减少临时分配

热点中留意：

- 每帧创建大量 table。
- 每帧创建闭包。
- 大量字符串拼接和 `string.format`。
- C#/Lua 边界装箱和临时代理。
- 每帧重建配置或 require 路径。

不要过早复用所有 table，因为对象池会引入“旧字段未清理”“被外部继续引用”等新问题。先测量，再针对真正热点优化。

### 24.6 Table 形状保持稳定

大量对象尽量使用一致字段，减少随处动态添加临时键带来的理解成本。配置数据应有明确 schema，并在加载时一次性校验。

### 24.7 不依赖 pairs 顺序

任务步骤、战斗结算和存档序列化如果需要稳定顺序：

- 使用连续数组。
- 或单独维护排序后的 key 列表。
- 序列化前显式排序。

### 24.8 避免魔法字符串

```lua
local AnimationEvent = {
    SkillStarted = "SkillStarted",
    SkillFinished = "SkillFinished"
}
```

Lua 没有 enum，也无法阻止写错字符串。关键协议可集中常量，并在 C# 边界映射为真正的 enum/ID。

### 24.9 分层错误策略

- 编程错误：尽早 assert/error。
- 业务失败：返回 `false, reason` 或明确结果对象。
- 网络/外部数据：验证、限时、重试策略由服务层决定。
- 顶层事件循环：捕获并记录 traceback，防止整个调度器静默停止。

### 24.10 性能结论以数据为准

Lua VM、LuaJIT、C# 实现解释器和各绑定生成方式性能差异很大。任何“Lua 一定慢”或“LuaJIT 一定快”的结论都必须放到：

- 最终目标平台。
- 最终构建后端。
- 真实数据规模。
- 实际跨语言调用频率。

中验证。

---

## 25. 最常见的陷阱

### 25.1 忘记 local

```lua
function update() end -- 可能污染全局
```

推荐：

```lua
local function update()
end
```

### 25.2 把 0 当 false

```lua
if 0 then
    -- 会执行
end
```

### 25.3 把空字符串当 false

```lua
if "" then
    -- 也会执行
end
```

### 25.4 数组从 0 开始写

```lua
local values = { [0] = "A", [1] = "B" }
print(#values) -- 不能把 0 号元素当标准序列一部分
```

### 25.5 有洞数组使用 #

```lua
local values = { [1] = "A", [3] = "C" }
local count = #values -- 不可靠地代表元素数量
```

### 25.6 pairs 顺序当作固定顺序

```lua
for key, value in pairs(config) do
    -- 不保证顺序
end
```

### 25.7 table 赋值误以为复制

```lua
local copy = original -- 只是同一 table 的另一个引用
```

### 25.8 table 内容相同就认为 ==

```lua
print({ 1, 2 } == { 1, 2 }) -- false
```

### 25.9 用 !=

Lua 不等于写 `~=`。

### 25.10 用 + 拼字符串

Lua 使用 `..`：

```lua
local text = "HP: " .. tostring(hp)
```

### 25.11 混淆点号和冒号

```lua
object:method(value) -- 自动把 object 作为第一个参数
object.method(value) -- 不会自动传 object
```

### 25.12 or 默认值吞掉 false

```lua
local enabled = config.enabled or true -- config.enabled=false 时仍得到 true
```

### 25.13 伪三元的真分支是假值

```lua
local value = condition and false or true -- condition=true 仍得到 true
```

### 25.14 整数除法照搬 C#

`/`、`//` 和数值类型随 Lua 版本不同；负数 `%` 也与 C# 不同。

### 25.15 中文长度直接用 #

`#text` 通常是字节数，不是 Unicode 字符数。

### 25.16 修改 Sprite/Unity 对象后只等 Lua GC

Lua 引用消失不等于 Unity 对象立即 Destroy；Unity 对象 Destroy 后 Lua 代理也不一定自动变 nil。

### 25.17 C# event 只订阅不取消

这会让 Lua function 被 C# 长期引用，造成重复回调、对象无法回收或 VM 销毁后回调异常。

### 25.18 每帧高频跨语言调用

把每个 Transform 分量读写都跨一次边界，往往比纯 Lua 语句本身更值得优化。

### 25.19 require 以为会重新执行

require 通常返回缓存。清除 `package.loaded` 也不能自动替换其他模块已经保存的旧函数和旧 table 引用。

### 25.20 把 Lua table 当 JSON

Table 可以包含：

- 非字符串键。
- function、userdata、thread。
- 循环引用。
- metatable。
- 共享引用。

它不能天然无损序列化成 JSON。存档和网络必须定义纯数据 schema。

### 25.21 把 coroutine 当线程

协程不会并行计算，也不会自动等待 Unity 帧，必须有调度者 resume。

### 25.22 使用旧版本没有的语法

`//`、位运算、`goto`、`_ENV`、`<close>` 不是所有 Unity Lua VM 都支持。先锁定版本再写。

### 25.23 把 StateMachineBehaviour 当角色实例数据仓库

在本项目中，Lua 即使参与技能逻辑，也不应把每个角色的动画运行时状态塞进共享配置或 StateMachineBehaviour 资源。状态应由对应角色的 C# AnimatorManager 持有。

---

## 26. 调试、测试与工具

### 26.1 最小调试手段

```lua
print("player", playerId, "hp", hp)
print(type(value), tostring(value))
print(debug.traceback("checkpoint"))
```

正式项目应由统一日志服务增加：

- 时间。
- 日志级别。
- Lua 模块名。
- 实体/任务/技能 ID。
- C# 与 Lua 组合堆栈（框架支持时）。
- 限频和重复日志合并。

### 26.2 Table 调试输出

`tostring(table)` 默认通常只显示地址式标识，不会打印所有字段。可以写一个仅用于调试的安全 dump：

- 限制最大深度。
- 检测循环引用。
- 限制最大键数量和字符串长度。
- 不调用可能有副作用的任意 `__tostring`。
- 不把密钥、Token 和隐私数据写日志。

不要对巨大或循环 table 直接无限递归打印。

### 26.3 静态分析与格式化

常见工具方向：

- **Lua Language Server**：代码补全、诊断和注解支持。
- **EmmyLua 风格注解**：描述 class、字段、参数和返回值。
- **StyLua**：自动格式化。
- **Luacheck**：检查全局变量、未使用变量等问题。

是否采用及配置版本应在选定 Lua VM 后锁定到仓库，避免每位开发者格式不同。

### 26.4 单元测试

纯 Lua 业务逻辑应尽量脱离 Unity 对象，可直接测试：

```lua
local function assertEqual(actual, expected, message)
    if actual ~= expected then
        error(message or string.format(
            "expected %s, got %s",
            tostring(expected),
            tostring(actual)
        ))
    end
end

local function testDamage()
    local actor = { hp = 100 }
    applyDamage(actor, 30)
    assertEqual(actor.hp, 70)
end
```

团队也可以评估 Busted 等 Lua 测试框架，但运行环境和版本兼容要与项目一致。

### 26.5 跨语言集成测试

至少覆盖：

- C# → Lua 参数和多返回值。
- Lua error 是否保留 traceback。
- Lua → C# 重载选择和枚举。
- Unity 对象销毁后的调用。
- event 注册、取消和 VM 释放。
- 场景切换与 Domain Reload。
- IL2CPP Release 构建。
- 中文和 emoji 字符串。
- 超过 `2^53` 的 ID。
- 热更新前后旧模块引用与存档兼容。

### 26.6 排错顺序

```text
确认 Lua VM/版本
  → 确认模块实际加载路径
  → 检查 require 返回值
  → 检查参数 type 与 nil
  → 检查 . 和 :
  → 获取完整 traceback
  → 检查 C#/Lua 转换与对象生命周期
  → 在最小复现中验证
```

不要在不知道根因时反复清 `package.loaded`、强制 GC 或重启 Unity；这些操作可能暂时掩盖模块状态和引用泄漏。

---

## 27. 学习路线与练习

### 阶段 1：基础语法

目标：能独立阅读普通 Lua 模块。

练习：

1. 用 if/for 写一个经验值升级计算。
2. 用 table 表示背包，支持添加、删除、查询物品。
3. 写 `safeDivide(a, b)`，返回 `result, error`。
4. 验证 0、空字符串、nil、false 的条件判断。
5. 验证 `pairs`、`ipairs` 和有洞数组的行为。

### 阶段 2：函数与模块

目标：写出没有全局污染、可测试的模块。

练习：

1. 编写 `Health.lua` 模块。
2. 用闭包创建独立计时器。
3. 编写带 varargs 的日志函数。
4. 验证多返回值在表达式列表不同位置的展开。
5. 用 `require` 观察模块只初始化一次。

### 阶段 3：Table 与面向对象

目标：理解 `__index`，能读懂常见 Lua 类框架。

练习：

1. 实现 Player 类式 table。
2. 实现一个只读配置代理。
3. 实现 Vector2 的 `__add` 与 `__tostring`。
4. 比较继承与组件组合的代码复杂度。
5. 写一个能检测循环引用的浅层 table dump。

### 阶段 4：协程和错误边界

目标：理解 yield/resume，不把协程当线程。

练习：

1. 写一个分三步执行的 coroutine。
2. 从 resume 向 yield 传值。
3. 用 xpcall 记录 traceback。
4. 模拟“等待服务结果”的调度器。
5. 取消实体销毁后尚未完成的协程。

### 阶段 5：Unity 方案验证

在选定框架后再执行：

1. 建立最小 Lua VM 生命周期。
2. 加载一个返回 table 的模块。
3. C# 调 Lua 纯函数。
4. Lua 调一个窄 C# 服务接口。
5. 测试 Unity Object Destroy、事件解绑和 VM Dispose。
6. 生成 IL2CPP 构建并在目标设备运行。
7. 记录实际类型映射、性能和错误栈规则到项目文档。

### 阶段 6：My RPG 小闭环

推荐先做一个低风险闭环：

```text
Lua 读取技能配置并决定 skillId
  → C# SkillService 校验
  → AnimCtrl 发动画指令
  → AnimatorManager 播放
  → C# 收到动画生效事件并结算
  → 返回技能完成结果给 Lua
```

不要第一步就把玩家移动、物理、Animator 和所有 UI 全部迁到 Lua。

---

## 28. 一页速查表

### 28.1 基础语法

```lua
-- 局部变量
local hp = 100
local name = "Knight"

-- 条件
if hp <= 0 then
    print("dead")
elseif hp < 30 then
    print("danger")
else
    print("normal")
end

-- 循环
for i = 1, 10 do
    print(i)
end

while hp > 0 do
    hp = hp - 10
end

-- 函数
local function add(a, b)
    return a + b
end

-- Table
local player = {
    id = 1001,
    name = "Knight",
    items = { "Sword", "Potion" }
}

-- 遍历数组
for index, item in ipairs(player.items) do
    print(index, item)
end

-- 遍历字典（无顺序保证）
for key, value in pairs(player) do
    print(key, value)
end
```

### 28.2 C# 到 Lua 语法转换

| 想做的事 | C# | Lua |
| --- | --- | --- |
| 局部变量 | `int hp = 100;` | `local hp = 100` |
| 空值 | `null` | `nil` |
| 不等于 | `a != b` | `a ~= b` |
| 逻辑与 | `a && b` | `a and b` |
| 逻辑或 | `a \|\| b` | `a or b` |
| 逻辑非 | `!a` | `not a` |
| 字符串连接 | `a + b` | `a .. b` |
| 长度 | `array.Length` | `#sequence`，仅可靠用于序列 |
| 方法调用 | `obj.Method(x)` | `obj:method(x)`，前提是方法需要 self |
| 创建列表 | `new List<int>{1,2}` | `{ 1, 2 }` |
| 字典字段 | `dict["hp"]` | `data.hp` 或 `data["hp"]` |
| 异常保护 | `try/catch` | `pcall/xpcall` |
| 多结果 | tuple/out | 多返回值 |

### 28.3 必记规则

1. 默认写 `local`。
2. 只有 `false` 和 `nil` 为假；0 和空字符串都是真。
3. Lua 标准数组从 1 开始。
4. Table 赋值复制引用，不复制内容。
5. Table 字段赋 nil 等于删除键。
6. `#table` 只适合无洞序列。
7. `pairs` 没有固定顺序，`ipairs` 遇 nil 停止。
8. 字符串用 `..` 连接，不等于用 `~=`。
9. `:` 会自动把调用者作为第一个 self 参数。
10. `and/or` 返回操作数，不是固定返回 bool。
11. 函数调用在表达式列表中的位置会影响多返回值展开。
12. require 通常只执行模块一次并缓存结果。
13. Lua coroutine 不是线程，也不自动等 Unity 帧。
14. Lua GC 不负责替你可靠释放 Unity 对象和 C# 事件。
15. 使用任何版本特性前先确认实际 Lua VM。
16. 跨 C#/Lua 边界应低频、批量、语义化。
17. Unity 对象、动画、物理和最终权威校验优先留在 C#。
18. AI 输出、玩家输入和普通网络数据绝不能直接当 Lua 代码执行。

## 结语

对 C# 开发者来说，真正掌握 Lua 的关键不是背下更短的语法，而是接受三个核心事实：

1. **Table 是数据结构中心**：数组、字典、对象和模块都围绕它建立。
2. **Function 是一等值**：闭包、回调、迭代器和模块封装都依赖它。
3. **动态性需要工程约束**：local、清晰模块、类型注解、边界校验、测试和明确生命周期不可缺少。

在 My RPG 中，Lua 最适合作为变化频繁的玩法编排层，而不是绕过现有 C# 动画、物理和 Unity 生命周期架构。先锁定运行时并完成最小跨平台验证，再决定哪些系统迁入 Lua。
