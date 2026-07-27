# My RPG — 策划与技术设计文档

> 版本：v0.1 (Demo)
> 引擎：Unity 2022.3.62f3 LTS + URP
> 类型：俯角单机 RPG Demo
> 最后更新：2026-07-27

---

## 1. 项目概述

一个俯视角（Top-down / 斜俯角）单机 RPG Demo。玩家操作一名角色，在一座小城镇中探索、与生活在城镇里的 NPC 交互、完成一个简单任务。核心亮点是 **NPC 接入 AI**：玩家可以用自然语言和 NPC 对话，NPC 的回复由大语言模型（LLM）生成，并且对话能够**推动任务进程**。

### 1.1 Demo 目标范围

| 模块 | 是否包含 | 说明 |
|------|---------|------|
| 玩家移动 / 镜头 | ✅ 已有 | 俯角移动、镜头相对方向、动画混合树、镜头跟随 |
| 城镇场景 | ✅ | 一个小城镇，几个功能建筑 + 可行走区域 |
| AI NPC 对话 | ✅ 核心 | 自然语言对话，LLM 生成回复，可推动任务 |
| 任务系统 | ✅ | 一条主线任务，2~3 个步骤 |
| 战斗系统 | ✅ | 基于已有敌人/攻击代码，做成完整战斗（血量、受击、死亡） |
| NPC 日程 / 作息 | ✅ | NPC 按时间在城镇中移动到不同地点 |
| 存档 | ⛔ Demo 外 | 后续迭代 |
| 多任务 / 任务树 | ⛔ Demo 外 | 后续迭代 |

### 1.2 可行性结论

**完全可行。** 除 AI 对话外，其余都是标准 RPG 模块，属于纯工程量。项目已具备的基础：

- URP 渲染管线、玩家控制器、镜头跟随、动画混合树已完成
- 敌人攻击、技能输入已有 stub，可扩展为完整战斗
- 依赖已含 `unitywebrequest`（调 AI 接口）、`textmeshpro`/`ugui`（对话 UI）、`modules.ai`（NavMesh 寻路，NPC 走动 / 日程用）
- 两套角色美术包（CombatGirls、Suriyun）可作为玩家与 NPC

**唯一需要重点设计的是 AI 接入**（延迟处理、人设上下文、结构化输出推动任务、失败兜底），详见第 7 节。

---

## 2. 核心玩法循环

```
进入城镇 → 探索 / 遇到 NPC → 自然语言对话
    → 从对话中得知任务 / 线索
        → 完成任务步骤（找人、找物、战斗）
            → 回报 NPC → 任务推进 / 完成 → 获得反馈
```

一条完整体验路径（示例任务见第 6 节）：

1. 玩家出生在城镇入口，自由移动探索
2. 走近铁匠 NPC，按交互键（E）打开对话框
3. 用自然语言聊天，AI 铁匠告诉玩家「我的工具被城外的野狼叼走了」
4. 玩家出城，进入战斗，击败野狼，拾取工具
5. 回到铁匠处对话，AI 识别到玩家带回了工具 → 任务完成 → 铁匠给予奖励并致谢

---

## 3. 系统架构总览

采用**分层 + 单一职责**结构。核心是若干 Manager（全局单例）+ 场景中的实体组件 + 数据资产（ScriptableObject）。

```
┌─────────────────────────────────────────────────────────┐
│                      GameManager                          │
│         (全局入口 / 游戏状态 / 各 Manager 引用)              │
└───────┬───────────┬────────────┬──────────┬───────────────┘
        │           │            │          │
   ┌────▼────┐ ┌────▼─────┐ ┌────▼────┐ ┌───▼──────┐
   │ Player  │ │ NPC 系统  │ │ 任务系统 │ │ 时间系统  │
   │ 控制/战斗│ │(数据/交互)│ │(Quest)  │ │(日程驱动) │
   └─────────┘ └────┬─────┘ └─────────┘ └──────────┘
                    │
              ┌─────▼───────────────┐
              │     AI 对话层         │
              │ ILLMClient (抽象)    │
              │  ├ NpcAgent (人设)   │
              │  ├ 结构化输出解析     │
              │  └ 对话 UI            │
              └─────────────────────┘
```

### 3.1 分层职责

- **表现层**：UI（对话框、任务日志、血条）、动画、特效
- **逻辑层**：Manager 们、各实体的行为组件（Controller / Combat / NPCBehaviour）
- **数据层**：ScriptableObject 资产（NPC 人设、任务定义、物品）+ 运行时状态
- **服务层**：AI 客户端（网络 IO，与游戏逻辑解耦，可切换后端）

### 3.2 通信方式

- Manager 之间：直接引用（通过 GameManager 拿）
- 逻辑 → UI：**事件（C# event / UnityEvent）**，避免逻辑层依赖 UI
- AI 结果 → 任务：AI 返回结构化数据，由 `ConversationManager` 解析后**发任务事件**，任务系统监听

---

## 4. 模块详细设计

### 4.1 GameManager（全局管理器）

- 场景加载时初始化，`DontDestroyOnLoad`
- 持有各子系统引用（QuestManager、TimeManager、DialogueManager 等）
- 管理全局游戏状态：`Exploring`（自由探索）/ `InDialogue`（对话中，玩家移动锁定）/ `InCombat`（战斗中）
- 状态切换时广播事件，供 UI、输入、NPC 行为响应

### 4.2 玩家系统（已有基础，需扩展）

| 组件 | 现状 | 需要做 |
|------|------|--------|
| `PlayerController` | ✅ 移动/镜头/动画完成 | 增加：对话中锁定移动；交互检测（检测附近可交互物） |
| `AnimCtrl` | 🟡 技能输入 stub | 接入战斗：普攻、技能触发动画 |
| `PlayerCombat`（新） | ❌ | 血量、攻击判定、受击、死亡 |
| `PlayerInteractor`（新） | ❌ | 检测最近的 `IInteractable`，E 键触发（对话 / 拾取） |

### 4.3 NPC 系统

一个 NPC = 若干组件的组合：

- `NpcIdentity`：引用一份 `NpcProfile`（ScriptableObject，见 5.1），持有运行时状态（当前位置、当前对话上下文）
- `NpcInteractable`：实现 `IInteractable`，玩家靠近按 E 触发对话
- `NpcScheduleAgent`：读取日程表，用 NavMesh 移动到目标点（见 4.6）
- `NpcAgent`（AI 侧）：负责为这个 NPC 构造 prompt、持有短期对话记忆

### 4.4 任务系统（QuestManager）

轻量事件驱动，Demo 只需支持一条线性任务：

- `QuestDefinition`（SO）：任务 id、标题、描述、步骤列表
- `QuestStep`：步骤 id、描述、完成条件类型（对话触发 / 击杀 / 拾取 / 到达）
- 运行时 `QuestState`：当前激活的 quest、当前 step 索引、各 step 完成标记
- 对外只暴露事件：`OnQuestStarted` / `OnStepCompleted` / `OnQuestCompleted`，UI 与 NPC 监听
- **关键接口** `TryAdvanceQuest(string triggerId)`：任何系统（战斗、拾取、AI 对话）都调用它上报一个「触发信号」，QuestManager 判断是否匹配当前步骤条件

### 4.5 战斗系统（基于已有代码扩展）

- `Health`（通用组件）：maxHP、currentHP、`TakeDamage()`、`OnDeath` 事件，玩家和敌人共用
- `SimpleEnemyAttack`（已有）：目前只 `Debug.Log`，改为真正调用目标 `Health.TakeDamage()`
- `EnemyController`（新）：巡逻 / 追击 / 攻击状态机（可复用 `StateMachine` 思路），用 NavMesh 追玩家
- `PlayerCombat`（新）：普攻做前方扇形/球形检测，命中敌人 `Health`
- 死亡：敌人死亡播放动画后销毁 / 掉落；玩家死亡 → 简单复活到城镇入口（Demo 简化）

### 4.6 时间与 NPC 日程系统

- `TimeManager`：维护游戏内时间（一天压缩为 N 分钟真实时长），广播 `OnHourChanged`
- `NpcScheduleAgent`：每个 NPC 一张日程表——`[(时间段, 目标地点)]`，如
  - 08:00 铁匠铺（工作）
  - 12:00 广场（吃饭 / 闲逛）
  - 20:00 家（休息）
- 时间到点 → 用 NavMesh 移动到目标点的锚点，播放对应待机动画
- 对话时把「当前时间 / 当前在做什么」注入 AI prompt，让回复更有代入感

---

## 5. 数据设计（ScriptableObject）

用 SO 定义所有静态内容，策划可在 Inspector 里直接改，不用碰代码。

### 5.1 NpcProfile（NPC 人设）

```
NpcProfile (SO)
├─ id: string                 // "blacksmith_gale"
├─ displayName: string        // "铁匠 盖尔"
├─ prefab: GameObject         // 使用的角色模型
├─ persona: string (多行)      // 性格/身份/说话风格，写进 system prompt
├─ knowledge: string (多行)    // 这个 NPC 知道什么、和任务的关系
├─ boundaries: string         // 不该聊什么 / 如何拒绝越界问题
├─ schedule: ScheduleEntry[]  // 日程表
└─ questHooks: string[]       // 关联的任务 id（用于注入任务上下文）
```

### 5.2 QuestDefinition（任务）

```
QuestDefinition (SO)
├─ id: string
├─ title: string
├─ summary: string
└─ steps: QuestStep[]
        ├─ id: string
        ├─ description: string        // 显示在任务日志
        ├─ conditionType: enum        // Dialogue / Kill / Collect / Reach
        └─ triggerId: string          // 与 TryAdvanceQuest 的信号匹配
```

### 5.3 ItemDefinition（物品，最小化）

```
ItemDefinition (SO)
├─ id, displayName, icon
└─ description
```
Demo 只需支持「任务物品」（如铁匠的工具），背包可做成一个简单 `List<ItemDefinition>`。

---

## 6. Demo 关卡与任务设计

### 6.1 城镇布局（示意）

```
        [城门/出口]  ← 通向城外野狼区（战斗区）
             │
        ┌────┴────┐
        │  广场    │  ← NPC 白天聚集点，喷泉
        └──┬───┬──┘
     [铁匠铺] [杂货店]        [几间民居]
        │       │
   (盖尔·任务)  (店主·闲聊)   (村民·闲聊/日程)
             │
        [玩家出生点]
```

需要 3~4 个 NPC 即可：
- **盖尔（铁匠）**：任务发布者
- **杂货店主**：纯闲聊，展示 AI 自由对话
- **1~2 名村民**：展示日程作息（白天广场、晚上回家）

### 6.2 主线任务：《铁匠的工具》

| 步骤 | 条件类型 | 触发信号 | 说明 |
|------|---------|---------|------|
| 1. 接受委托 | Dialogue | `quest_start_gale` | 和盖尔对话，AI 判断玩家答应帮忙 |
| 2. 击败野狼 | Kill | `wolf_defeated` | 出城，战斗击败野狼 |
| 3. 拾取工具 | Collect | `tool_collected` | 野狼死亡后掉落，拾取 |
| 4. 归还工具 | Dialogue | `quest_finish_gale` | 回到盖尔，AI 识别玩家带回工具 → 完成 |

**AI 如何触发步骤 1 和 4**：见 7.3。核心是让 AI 在回复里附带一个 `action` 字段，游戏解析后调 `QuestManager.TryAdvanceQuest(triggerId)`。

---

## 7. AI 对话系统（核心）

### 7.1 分层设计（可切换后端）

```
对话 UI (输入框 / 气泡 / "思考中")
    │  玩家文本
    ▼
ConversationManager  ← 全局，管理"当前正在和谁对话"
    │  组装请求
    ▼
NpcAgent (每 NPC 一个)  ← 拼 system prompt + 对话历史 + 当前情境
    │  ChatRequest
    ▼
ILLMClient (接口)   ← ★可切换后端的关键抽象
    ├─ AnthropicClient   (官方 API)
    ├─ OpenAICompatClient(中转 / OpenAI 兼容)
    └─ MockClient        (离线假数据，开发期用，不花钱)
    │  HTTP (UnityWebRequest, async)
    ▼
   LLM 服务
```

`ILLMClient` 只定义一个方法，其它全是实现细节：

```csharp
public interface ILLMClient
{
    // 传入完整消息列表，异步返回助手回复文本
    Task<LLMResponse> ChatAsync(LLMRequest request, CancellationToken ct);
}
```

切换后端 = 换一个实现类 + 换配置，游戏逻辑零改动。开发期先用 `MockClient` 跑通全流程，再接真实后端。

### 7.2 Prompt 结构

每次对话，`NpcAgent` 组装：

1. **System prompt**（来自 `NpcProfile`）：
   - 身份人设：`persona`
   - 知识与任务关系：`knowledge` + 当前任务状态注入
   - 边界约束：`boundaries`（如何拒绝越界 / 出戏问题）
   - **输出格式要求**：必须返回指定 JSON（见 7.3）
   - 当前情境：游戏内时间、NPC 此刻在做什么、玩家是否已接任务
2. **对话历史**：该 NPC 与玩家最近 N 轮（滑动窗口，控制 token）
3. **玩家最新输入**

### 7.3 结构化输出（让对话推动任务）★

关键：不让 AI 只输出纯文本，而是要求它返回 JSON，游戏解析后决定是否触发任务。

约定 NPC 返回：

```json
{
  "reply": "啊，你愿意帮我？太好了！我的工具在城外被野狼叼走了……",
  "action": "quest_start_gale",
  "emotion": "happy"
}
```

- `reply`：显示给玩家的对话气泡文本
- `action`：可空。非空时游戏调 `QuestManager.TryAdvanceQuest(action)`。**只有当前任务步骤的 triggerId 与之匹配才生效**，防止 AI 乱触发
- `emotion`：可空，驱动表情 / 动画 / 气泡样式

**为什么可靠**：AI 是否给 action 由 system prompt 严格约束（例：「仅当玩家明确表示愿意帮忙寻回工具时，action 才填 quest_start_gale，否则留空」）。即使 AI 偶尔乱给，QuestManager 的 triggerId 匹配和步骤顺序校验会挡住无效触发。归还工具（步骤 4）同理：prompt 里告诉 NPC「若玩家背包已带回工具（游戏会在情境里注明 has_tool=true），且玩家表示归还，则 action 填 quest_finish_gale」。

### 7.4 延迟与体验处理

- 发送后立即：锁定玩家输入、显示「…（思考中）」气泡
- `UnityWebRequest` 全程异步（`async/await` 或协程），**绝不阻塞主线程**
- 设超时（如 10s）；超时 / 网络失败 → 回退到 `NpcProfile` 里预设的一句兜底台词（如「抱歉，我走神了，你刚说什么？」），保证游戏不卡死
- 可选：流式输出（打字机效果），Demo 阶段可先不做

### 7.5 安全与成本

- **成本**：短对话、限制历史轮数、限制 max_tokens；开发期用 MockClient
- **越界 / 注入**：boundaries 约束 + 游戏侧只信任 JSON 里的 `action` 白名单，AI 文本本身不当作指令执行
- **API Key**：不硬编码进代码/场景。Demo 可放在本地配置文件（加入 .gitignore）；正式版应走服务端代理，客户端不持 key

---

## 8. 代码目录结构（建议）

在现有 `Assets/Script/` 下扩展：

```
Assets/Script/
├─ Core/
│   ├─ GameManager.cs
│   └─ GameState.cs            (枚举 + 状态事件)
├─ Player/
│   ├─ PlayerController.cs     (已有)
│   ├─ AnimCtrl.cs             (已有, 接战斗)
│   ├─ PlayerCombat.cs         (新)
│   └─ PlayerInteractor.cs     (新)
├─ Combat/
│   ├─ Health.cs               (玩家/敌人共用)
│   └─ EnemyController.cs      (新, 状态机)
├─ NPC/
│   ├─ NpcIdentity.cs
│   ├─ NpcInteractable.cs
│   └─ NpcScheduleAgent.cs
├─ Quest/
│   ├─ QuestManager.cs
│   └─ QuestState.cs
├─ Time/
│   └─ TimeManager.cs
├─ AI/
│   ├─ ILLMClient.cs
│   ├─ LLMRequest.cs / LLMResponse.cs
│   ├─ Clients/
│   │   ├─ MockClient.cs
│   │   ├─ AnthropicClient.cs
│   │   └─ OpenAICompatClient.cs
│   ├─ NpcAgent.cs
│   └─ ConversationManager.cs
├─ Interaction/
│   └─ IInteractable.cs
├─ Data/                       (ScriptableObject 定义)
│   ├─ NpcProfile.cs
│   ├─ QuestDefinition.cs
│   └─ ItemDefinition.cs
├─ UI/
│   ├─ DialogueUI.cs
│   ├─ QuestLogUI.cs
│   └─ HealthBarUI.cs
├─ Camera/FollowToPlayer.cs    (已有)
└─ Utilities/StateMachine.cs   (已有)
```

数据资产放 `Assets/Resources/Data/`（NPC 人设、任务、物品各一个文件夹）。

---

## 9. 开发里程碑（建议顺序）

按「先跑通闭环，再填内容」的顺序，每个里程碑都可运行、可验证。

### M0 — 场景与基建（地基）
- 搭建城镇场景（地面、建筑、NavMesh 烘焙、出生点、城门）
- `GameManager` + `GameState` 骨架
- 玩家放进场景，跑通移动 / 镜头（复用已有代码）

### M1 — 交互与对话 UI（不接 AI）
- `IInteractable` + `PlayerInteractor`（靠近 NPC 显示提示，E 触发）
- `DialogueUI`（输入框 + 气泡 + 思考中）
- `ConversationManager` + `ILLMClient` + **MockClient**（返回假回复）
- 验证：能走到 NPC 前打开对话、发消息、收到（假）回复、关闭

### M2 — 接真实 AI + 人设
- 实现 `AnthropicClient` / `OpenAICompatClient`（按选定后端）
- `NpcProfile` SO + `NpcAgent` 组装 prompt
- 验证：和不同 NPC 聊天，性格/知识不同，延迟与超时兜底正常

### M3 — 任务系统 + 结构化输出
- `QuestManager` + `QuestDefinition` SO
- AI 结构化输出解析 → `TryAdvanceQuest`
- `QuestLogUI`
- 验证：对话触发任务开始（步骤 1）

### M4 — 战斗
- `Health` + `PlayerCombat` + `EnemyController`
- 改造 `SimpleEnemyAttack` 造成真实伤害
- 野狼击杀 → 掉落 → 拾取 → 上报 `wolf_defeated` / `tool_collected`
- `HealthBarUI`
- 验证：完整战斗流程 + 任务步骤 2、3 推进

### M5 — NPC 日程 + 收尾
- `TimeManager` + `NpcScheduleAgent`
- NPC 按时间在城镇移动
- 归还工具对话触发任务完成（步骤 4）
- 验证：**从头到尾走通整条任务线** + NPC 有生活感

---

## 10. 风险与注意点

| 风险 | 影响 | 应对 |
|------|------|------|
| AI 延迟 / 网络失败 | 对话卡顿、体验差 | 异步 + 思考中反馈 + 超时兜底台词（7.4） |
| AI 不按 JSON 格式返回 | 解析失败 | 强约束 prompt + 容错解析（提取 JSON 块）+ 解析失败当纯闲聊处理 |
| AI 乱触发任务 | 任务逻辑错乱 | action 白名单 + 步骤顺序校验（7.3） |
| API 成本 | 频繁调用烧钱 | MockClient 开发、限历史轮数与 max_tokens |
| API Key 泄露 | 安全 | 不硬编码、不进 git；正式版走服务端代理 |
| NavMesh 未烘焙 | NPC/敌人不会走 | M0 就烘焙好 NavMesh |
| Unity 场景/资产协作 | 手改 .unity/.prefab 易坏 | 按 AGENTS.md：场景/预制体/SO 一律走 Unity MCP / 编辑器 API，不用文本工具改 |

---

## 11. 待确认 / 后续决策

- AI 后端具体选型（官方 Anthropic / OpenAI 兼容中转 / 本地）与 Key 获取方式
- 美术：玩家和各 NPC 分别用哪套模型（CombatGirls / Suriyun 里的哪个）
- 游戏内一天对应多少真实时长（影响日程节奏）
- 是否需要中文语音 / TTS（Demo 建议先纯文字）

---

*本文档为 Demo 阶段设计，随开发迭代更新。实现时严格遵循 `AGENTS.md` 的 Unity MCP 工作流。*





