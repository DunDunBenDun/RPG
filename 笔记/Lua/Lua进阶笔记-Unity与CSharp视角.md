# Lua 进阶笔记（Unity 与 C# 视角）

> 前置知识：已经读完 Lua 入门笔记，理解变量、函数、Table、`self` 和 `require`。
> 最后更新：2026-08-17。
> 项目现状：My RPG 尚未接入 Lua 运行时，涉及 Unity 的代码只讲设计思路。

这篇笔记介绍阅读游戏项目 Lua 代码时常见的进阶概念。学习重点是理解用途，不要求第一次就独立实现完整框架。

## 目录

1. [函数是一种值](#1-函数是一种值)
2. [闭包与私有状态](#2-闭包与私有状态)
3. [模块与依赖传入](#3-模块与依赖传入)
4. [错误保护](#4-错误保护)
5. [Metatable 与 __index](#5-metatable-与-__index)
6. [用 Table 表示类](#6-用-table-表示类)
7. [协程](#7-协程)
8. [资源与事件生命周期](#8-资源与事件生命周期)
9. [类型注解](#9-类型注解)
10. [Lua 与 C# 的边界](#10-lua-与-c-的边界)
11. [进阶阶段常见问题](#11-进阶阶段常见问题)

---

## 1. 函数是一种值

Lua 函数可以保存到变量中，也可以作为参数传给另一个函数。这和 C# 的委托、`Action`、`Func` 或 lambda 类似。

```lua
local function calculate(a, b, operation)
    return operation(a, b)
end

local result = calculate(10, 5, function(left, right)
    return left + right
end)

print(result)
```

解释：

- `calculate` 接收两个数字和一个函数 `operation`。
- 调用时传入了一个执行加法的匿名函数。
- `operation(a, b)` 得到 `15`，所以最终输出 `15`。
- 这种写法常用于回调、排序规则和事件处理。

---

## 2. 闭包与私有状态

内部函数可以继续使用外层函数的局部变量，这种组合称为闭包。

```lua
local function createCounter(start)
    local count = start

    return function()
        count = count + 1
        return count
    end
end

local counter = createCounter(10)

print(counter())
print(counter())
```

解释：

- `createCounter` 创建局部变量 `count`，然后返回一个函数。
- 返回的函数仍然能够读取和修改 `count`。
- 两次调用分别输出 `11` 和 `12`。
- `count` 没有暴露到外部，可以看作一份私有状态。

闭包很方便，但不要在每帧循环中无意义地创建大量新闭包。是否需要优化，应以实际性能分析结果为准。

---

## 3. 模块与依赖传入

模块不必直接寻找全局服务，可以让外部把需要的服务传进来。

```lua
local QuestLogic = {}

function QuestLogic.create(services)
    local instance = {}

    function instance.completeQuest(questId)
        services.quest:complete(questId)
    end

    return instance
end

return QuestLogic
```

解释：

- `QuestLogic.create` 接收一组外部服务。
- 模块不需要知道服务是从 Unity、测试代码还是其他 Lua 模块提供的。
- 测试时可以传入假的 `quest` 服务，减少对 Unity 环境的依赖。
- 这种思路接近 C# 的依赖注入。

---

## 4. 错误保护

### 4.1 使用 assert 检查调用条件

```lua
local function applyDamage(target, amount)
    assert(target ~= nil, "target is required")
    assert(type(amount) == "number", "amount must be a number")

    target.hp = target.hp - amount
end
```

解释：

- `assert` 的条件为假时会停止当前调用并报告错误。
- 第一处检查目标是否存在，第二处检查伤害值是否为数字。
- `assert` 适合发现程序写错，不适合表示“金币不足”等正常业务结果。

### 4.2 使用 pcall 保护一次调用

```lua
local ok, resultOrError = pcall(function()
    error("模拟错误")
end)

if not ok then
    print(resultOrError)
end
```

解释：

- `pcall` 会执行传入的函数，并捕获函数内部的 Lua 错误。
- 执行成功时 `ok` 是 `true`；失败时 `ok` 是 `false`。
- 失败原因保存在 `resultOrError` 中。
- 它类似在 C# 边界使用 `try/catch`，但写法和返回方式不同。

不要在底层把所有错误静默吞掉。游戏顶层可以保护调用，但仍应记录模块名、实体 ID 和完整错误信息。

---

## 5. Metatable 与 __index

Metatable 可以为 table 补充特殊行为。最常见的 `__index` 用于“当前 table 没有字段时，到哪里继续查找”。

```lua
local defaults = {
    speed = 5,
    damage = 10
}

local config = {
    damage = 20
}

setmetatable(config, {
    __index = defaults
})

print(config.damage)
print(config.speed)
```

解释：

- `config` 自己有 `damage`，所以输出 `20`。
- `config` 没有 `speed`，`__index` 会让它到 `defaults` 中查找。
- 因此 `config.speed` 输出 `5`。
- 这常用于默认配置、代理和 Lua 的类式写法。

Metatable 会改变普通读取和写入的行为。业务代码难以理解时，不要继续堆叠多层查找链。

---

## 6. 用 Table 表示类

Lua 没有 C# 的 `class` 关键字，常用 table、`__index` 和构造函数组合出类似类的写法。

```lua
local Player = {}
Player.__index = Player

function Player.new(name, maxHp)
    local instance = {
        name = name,
        hp = maxHp,
        maxHp = maxHp
    }

    return setmetatable(instance, Player)
end

function Player:takeDamage(amount)
    self.hp = math.max(0, self.hp - amount)
end

local hero = Player.new("Knight", 100)
hero:takeDamage(25)

print(hero.hp)
```

解释：

- `Player.new` 创建一张代表玩家实例的 table。
- `setmetatable(instance, Player)` 让实例可以从 `Player` 中找到方法。
- `hero:takeDamage(25)` 会自动把 `hero` 作为 `self`。
- 最终生命值输出 `75`。
- 这只是团队约定，不像 C# 类那样由编译器保证字段和访问权限。

游戏逻辑通常优先考虑组件组合，避免复制很深的 C# 继承结构。

---

## 7. 协程

Lua 协程可以暂停函数，并在之后继续执行。它不是线程，也不会自动并行。

```lua
local task = coroutine.create(function()
    print("第一步")
    coroutine.yield()
    print("第二步")
end)

coroutine.resume(task)
coroutine.resume(task)
```

解释：

- `coroutine.create` 创建协程，但不会立即执行。
- 第一次 `resume` 输出“第一步”，然后在 `yield` 处暂停。
- 第二次 `resume` 从暂停位置继续，输出“第二步”。
- 纯 Lua 协程不认识 Unity 的帧和 `WaitForSeconds`，需要 Lua 框架提供调度层。

---

## 8. 资源与事件生命周期

Lua 垃圾回收不会自动替你取消 C# 事件，也不等于销毁 Unity 对象。订阅和取消订阅应成对出现。

```lua
function View:onOpen()
    self.subscription = eventBus:subscribe(self.onMessage)
end

function View:onClose()
    if self.subscription ~= nil then
        self.subscription:dispose()
        self.subscription = nil
    end
end
```

解释：

- 页面打开时保存事件订阅对象。
- 页面关闭时主动调用 `dispose`，然后清空引用。
- 如果只订阅不取消，可能出现重复回调、对象无法释放或 Lua 环境销毁后仍被调用。
- 这里的 API 是设计示例，真实名称由最终 Lua 框架决定。

---

## 9. 类型注解

Lua 运行时仍是动态类型，但编辑器注解可以改善代码补全和错误提示。

```lua
---@class PlayerData
---@field name string
---@field hp number

---@param player PlayerData
---@param amount number
local function applyDamage(player, amount)
    player.hp = math.max(0, player.hp - amount)
end
```

解释：

- `---@class` 和 `---@field` 描述 table 应有哪些字段。
- `---@param` 描述函数参数。
- 这些注解主要供 Lua Language Server 等工具使用，不会自动进行运行时校验。

---

## 10. Lua 与 C# 的边界

如果 My RPG 以后接入 Lua，推荐让 Lua 发送业务意图，由 C# 操作 Unity 对象。

```lua
local command = {
    actorId = 1001,
    type = "CastSkill",
    skillId = "fire_slash",
    targetId = 2003
}

services.skill:execute(command)
```

解释：

- Lua 只描述“谁对谁释放哪个技能”。
- C# 服务负责检查角色、冷却、资源和当前状态。
- Animator、物理、GameObject 和伤害最终结算仍由 C# 控制。
- 这样可以避免 Lua 代码到处直接修改 Unity 组件。

跨 Lua/C# 边界时优先使用少量、批量、语义明确的调用，不要每帧逐个读取大量 Transform 属性。

---

## 11. 进阶阶段常见问题

1. `require` 通常会缓存模块，重复调用不等于重新执行文件。
2. Table 赋值仍然只复制引用。
3. `pairs` 不保证顺序。
4. `pcall` 捕获错误后仍应保留错误上下文。
5. Metatable 查找链不要设计得过深。
6. Lua 协程不是线程，也不会自动等待 Unity 帧。
7. C# 事件、Unity 对象和文件句柄需要明确释放。
8. 不执行玩家输入、AI 回复或网络文本作为 Lua 代码。
9. 接入 Unity 前先确定 Lua 框架、Lua 版本和目标平台支持。
10. 性能问题应在真实构建和目标设备上测量。

## 学习建议

理解本篇后，可以选择一个小模块练习，例如任务条件判断或技能配置处理。先让纯 Lua 逻辑独立运行和测试，再考虑接入 Unity。
