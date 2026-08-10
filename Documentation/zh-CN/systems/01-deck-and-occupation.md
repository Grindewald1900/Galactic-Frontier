# 系统文档：卡组与角色占用

> 文档版本：v1.2  
> 状态：**P1 已完成**（数据、占用、Formation/Bridge UI、ActionScheduler、备用卡组）  
> 上级约束：`Documentation/01-core-product-design.md` §6.1 / §7.2 / §7.3 / §21 / §22  
> 实现阶段：开发计划 P1（见 `../11-mvp-development-plan.md`）  
> 更新日期：2026-08-09

---

## 1. 目标与非目标

### 1.1 目标

把角色变成**有限运营资源**：玩家必须在主线推进、挂机刷取、采集、制造等行动之间分配卡牌，而不是把最强阵容同时铺满所有系统。

系统需同时支持：

- 多套 **5 人卡组**（战斗 / 工作用途可区分）；
- **并行行动**（不同卡组同时执行不同任务）；
- 清晰的 **占用 / 解锁** 状态，停止行动时不施加高惩罚。

### 1.2 非目标（本文件不解决）

- 自动战斗回合内技能选择细则 → `02-auto-battle.md`
- 采集产量、生产配方、离线秒数 → `04` / `05` 系列
- 市场与卡牌交易绑定规则 → `07-market-and-card-trade.md`
- 舰船槽位设施的完整数值曲线 → `03-region-and-ship.md`（本文只引用解锁钩子）

---

## 2. 术语

| 术语 | 定义 |
| --- | --- |
| **卡牌实例** | 玩家拥有的一张具体角色卡，以存档内唯一 `cardId`（GUID）标识 |
| **角色模板** | `CharacterName`（如 Asra）；多张实例可共享同一模板 |
| **卡组（Deck）** | 最多 5 个出战/上岗槽位的编制单元，可保存、重命名、切换用途 |
| **卡组槽位（Deck Slot）** | 玩家账户上可拥有的卡组数量上限中的一格（含未解锁） |
| **编制（Membership）** | 某 `cardId` 是否写入某卡组的槽位列表 |
| **行动（Action）** | 卡组正在执行的任务，如主线战斗、挂机刷取、采集、加工、制造、研究、运输 |
| **占用（Occupation）** | 卡牌因所属卡组的**运行中行动**而被锁定的状态 |
| **并行上限** | 玩家可同时处于 `Running` 的卡组数量 |

---

## 3. 硬约束（不可违背）

来自核心设计 §22，实现与测试必须满足：

1. 每个战斗或工作卡组均由 **最多 5** 名角色组成（允许不满编启动的规则见 §6.3）。
2. 角色（卡牌实例）不能同时服务于多个**正在运行**的队伍。
3. 多项挂机行动通过**多支独立卡组**并行执行，不得用「同一卡组多任务」绕过占用。
4. 停止行动的惩罚必须偏低，不得用重罚阻止玩家调整阵容。

---

## 4. MVP 拍板决议

> 下列决议用于关闭核心设计 §21「卡组与角色占用」待定项。数值可配置，语义默认按本表实现。

### 4.1 编制 vs 占用（关键语义）

| 概念 | MVP 规则 |
| --- | --- |
| 编制 | **允许**同一 `cardId` 出现在多套**未运行**卡组的预设中（方便备用阵容） |
| 占用 | 仅当卡组行动状态为 `Running`（含离线继续结算）时，其成员被占用 |
| 冲突 | 启动行动时，若任一成员已被其他运行中卡组占用 → **拒绝启动**并提示冲突卡牌 |
| 编辑运行中卡组 | 必须先 **停止** 行动，才能改动该卡组成员；未运行卡组可随时改编制 |

说明：核心设计 §6.1「只能被一个正在执行任务的卡组占用」优先于 §7.3 字面「不能出现在多个卡组」。  
§7.3 在本系统中解释为：**不能同时出现在多个正在运行的卡组中**。

### 4.2 同名卡

| 决议 | 说明 |
| --- | --- |
| **允许** | 不同 `cardId`、相同 `CharacterName` 的卡可同时编入不同卡组，并可同时运行 |
| **禁止** | 同一 `cardId` 被两个 `Running` 卡组同时占用 |

重复卡培养因此具有运营价值，而非纯外观。

### 4.3 卡组槽位与并行上限

| 项目 | MVP 默认 |
| --- | --- |
| 卡组槽位总数上限 | **6**（含未解锁） |
| 开局已解锁卡组槽 | **2** |
| 开局并行运行上限 | **2** |
| MVP 并行运行软顶 | **4**（再高留给后续扩展，不进首发必做） |
| 每卡组槽位数 | **5**（与 `DefaultProperty.defaultLineupSize` 一致） |
| 空槽 | 允许；槽位可不填满 |

#### 解锁表示例（可配置表 `DeckUnlockTable`）

| 解锁内容 | 条件（满足任一或按表配置） | 开局后目标 |
| --- | --- | --- |
| 卡组槽 #3 | 玩家等级 ≥ 5 **或** 主线章节 1 通关 | 常用：战斗 + 采集 + 制造 |
| 卡组槽 #4 | 玩家等级 ≥ 10 **或** 舰船等级 ≥ 2 | |
| 卡组槽 #5 | 主线章节 2 通关 **或** 舰船设施「调度中枢」Lv.1 | |
| 卡组槽 #6 | 舰船等级 ≥ 4 **或** 研究「多编队协议」完成 | |
| 并行上限 3 | 玩家等级 ≥ 8 **或** 舰船设施「自动化核心」Lv.1 | |
| 并行上限 4 | 主线章节 3 通关 **或** 舰船等级 ≥ 5 | |

具体等级数字可在数值表中调整；代码侧只依赖解锁表，不写死魔法数散落各处。

### 4.4 卡组用途（DeckPurpose）

| 用途 | 可执行行动 | 备注 |
| --- | --- | --- |
| `Combat` | 主线/区域战斗、挂机刷取 | 战前策略配置挂在此用途 |
| `Gather` | 资源采集 | P3 接入 |
| `Produce` | 加工 / 装备制造 | P3 接入；MVP 可先合并为一个工作用途 |
| `Research` | 研究 | 可后置；枚举预留 |
| `Transit` | 运输 | 可后置；枚举预留 |
| `Flexible` | 未限定 | 开局默认；启动行动时再校验行动是否允许 |

MVP 实现建议：存盘保存 `DeckPurpose`，但早期可用 `Flexible` + 行动类型校验，避免 UI 过度复杂。

### 4.5 角色占用状态（CardOccupationState）

| 状态 | 含义 | 可否加入新的 Running 行动 |
| --- | --- | --- |
| `Idle` | 空闲 | 可 |
| `MainCombat` | 主线/区域战斗中（含战斗场景进行时） | 否 |
| `AutoCombat` | 挂机自动战斗刷取中 | 否 |
| `Gathering` | 资源采集中 | 否 |
| `Processing` | 加工生产中 | 否 |
| `Manufacturing` | 装备制造中 | 否 |
| `Researching` | 研究中 | 否 |
| `InTransit` | 运输中 | 否 |

派生规则：

- 卡牌的占用状态 = 其当前所属 **Running 卡组** 的行动类型映射；无 Running 归属则为 `Idle`。
- 若因数据异常出现多归属，以「最早启动的 Running 行动」为准并打错误日志，启动新行动一律失败直到修复。

### 4.6 停止与完成：解除占用时机

| 事件 | 占用解除时机 | 收益处理（原则） |
| --- | --- | --- |
| 行动 **正常完成** | 完成结算后立即 `Idle` | 发放完整本期收益 |
| 玩家 **主动停止** | 确认停止后 **立即** `Idle` | 结算**已进度部分**收益；不扣已得资源；不销毁装备；可收取轻微时间损耗（见下） |
| 行动 **失败**（如战斗败北） | 结算后立即 `Idle` | 按行动类型文档；占用必须解除 |
| 离线达到收益上限而 **暂停** | 成员仍视为占用（行动 `PausedCap`） | 上线后可继续或停止；停止则立即解除 |

**主动停止的低惩罚（MVP）：**

- 不扣除已入包资源；
- 不附加「疲劳」或长时间锁定；
- 允许损失最多 **当前周期未结算进度**（例如采集 10 分钟周期已过 3 分钟，停止则丢弃这 3 分钟进度）；
- 制造类若已消耗材料进入配方，停止时：材料不返还或按配方文档部分返还——默认 **已扣材料不返还，产出按进度 0 发放**（制造细则在生产文档最终确认；卡组层只保证占用立即解除）。

### 4.7 不满编与空卡组

| 场景 | MVP 规则 |
| --- | --- |
| 0 人卡组 | 不可启动任何行动 |
| 1–4 人卡组 | **允许**启动；战斗/采集效率由对应系统按人数或战力计算 |
| 主线首次挑战 | UI 强提示建议 5 人，但不强制锁死（可配置 `requireFullDeckForMainCombat`，默认 `false`） |

### 4.8 备用卡组

- 允许保存未启用的卡组预设（名称、5 槽、用途、战前策略引用）。
- 未运行卡组 **不占用** 角色。
- 并行上限只约束 `Running`（含 `PausedCap`）卡组数，不约束已解锁预设数。

---

## 5. 数据模型（实现契约）

以下为逻辑模型，字段名可按 C# 风格微调，但语义需稳定以便存档迁移。

### 5.1 DeckEntity

```text
DeckEntity
- deckId: string (GUID)
- displayName: string
- purpose: DeckPurpose
- slotCardIds: string[5]        // 空槽用 null / ""
- action: DeckActionState       // 见下
- combatStrategyId: string?     // 战前策略，战斗文档定义
- sortOrder: int
- unlocked: bool                // 该槽是否已解锁；未解锁不可编辑/启动
```

### 5.2 DeckActionState

```text
DeckActionState
- status: Idle | Running | PausedCap | Completing
- actionType: None | MainCombat | AutoCombat | Gather | Process | Manufacture | Research | Transit
- targetId: string?             // 区域 / 节点 / 配方等
- startedAtUtc: long
- lastSettledAtUtc: long
- progressPayload: json/bytes   // 行动子系统私有进度，卡组层不解释
```

### 5.3 卡牌侧（CardEntity 扩展）

现有 `CardEntity.position / LineupPosition` 表示**单全局编队槽**，无法表达多卡组。P1 迁移方向：

| 现状 | 目标 |
| --- | --- |
| `LineupPosition` 写在卡牌上 | 编制权威数据改到 `DeckEntity.slotCardIds` |
| `GetInLineCardEntities()` | 改为 `DeckService.GetMembers(deckId)` / `GetActiveCombatDeck()` |
| 战斗读全局编队 | 战斗入口显式传入 `deckId` |

过渡期兼容：

1. 读档时若无 `decks.json`，将所有 `position != None` 的卡导入「默认战斗卡组」槽位；
2. 清除卡牌上的旧 `position` 或保留但不再作为权威；
3. `saveVersion` 提升并写迁移（见存档文档）。

可选冗余字段（仅缓存，非权威）：

```text
CardEntity
- occupationState: CardOccupationState  // 可由 Deck 运行集推导，存盘可选
- occupyingDeckId: string?
```

**权威来源**：运行中卡组集合；卡牌上的占用字段若存在必须可被重建。

### 5.4 玩家侧汇总

```text
PlayerDeckState
- decks: List<DeckEntity>
- unlockedDeckSlots: int
- maxParallelActions: int
- activeCombatDeckId: string?   // Explore/主线开战默认选中
```

建议存档文件：`saves/{playerId}/decks.json`（与卡牌、物品分文件）。

---

## 6. 规则细则

### 6.1 编制规则

1. 每个卡组最多 5 个 `cardId`，同一卡组内 `cardId` 不可重复。
2. 将卡牌拖入卡组槽：若该槽已有成员则替换；被替换者仅离开该卡组编制，占用状态取决于其是否仍属于其他 Running 卡组（通常为否）。
3. 未解锁的卡组槽：不可写入成员、不可改名、不可启动。
4. 删除卡组：仅当 `status == Idle` 且非最后一张「可战斗卡组」策略（MVP：至少保留 1 个解锁卡组）。删除后编制解除。

### 6.2 启动行动

启动前校验（顺序建议）：

1. 卡组已解锁；
2. 卡组 `status == Idle`；
3. 成员数 ≥ 1；
4. 当前 Running（含 PausedCap）卡组数 `< maxParallelActions`；
5. 所有成员 `cardId` 均存在于玩家卡池；
6. 所有成员当前占用可接受（均未被其他 Running 卡组占用）；
7. 行动类型与目标合法（由行动子系统提供 `CanStart`）。

全部通过后：

- 卡组 `status → Running`；
- 写入 `actionType/targetId/startedAtUtc`；
- 成员占用映射为对应 `CardOccupationState`。

### 6.3 停止行动

1. 玩家确认停止 → 行动子系统 `SettlePartial()`；
2. 卡组 `status → Idle`，清空行动字段（或保留 lastTarget 仅供 UI）；
3. 成员占用立即变为 `Idle`（若无其他 Running 归属）；
4. UI 允许立刻把该卡编入其他卡组并启动（受并行上限约束）。

### 6.4 主线战斗与占用

| 阶段 | 占用 |
| --- | --- |
| 仅在 Explore 选中卡组、未开战 | 不占用 |
| 进入 `BattleScene` 至战报结束 | `MainCombat` 占用 |
| 战报关闭返回 | 解除占用（单次战斗）；若玩家将同一卡组转入挂机刷取则再进入 `AutoCombat` |

挂机刷取是**独立行动类型**，不是战斗场景的常驻状态。

### 6.5 并行示例（验收用例）

玩家拥有卡 A–J，解锁 3 卡组槽，并行上限 2：

| 卡组 | 编制 | 行动 | 结果 |
| --- | --- | --- | --- |
| 甲 | A B C D E | 挂机刷取 Running | 合法 |
| 乙 | F G H | 采集 Running | 合法（并行 2） |
| 丙 | A F I | 试图制造 | **拒绝**：A、F 已被占用 |
| 乙 停止后丙启动 | A 仍在甲中占用；编制含 A | **拒绝**：A 仍占用 |
| 将丙改为 I J 及空闲卡 | 制造 | 若并行位已释放则合法 |

备用预设：卡组丙预设里可以事先写上 A，只要不启动就不冲突。

---

## 7. UI / UX 要求

| 界面 | 要求 |
| --- | --- |
| 编队（Formation） | 支持卡组列表切换；显示每卡组用途、运行状态、5 槽 |
| 成员卡 | 占用中显示状态徽章（采集中 / 战斗中等）；禁用「加入其他 Running 编成」的误导操作 |
| 启动失败 | 列出冲突 `cardId` 与所在卡组名 |
| Bridge | 展示 Running 卡组摘要与并行 `当前/上限` |
| 停止 | 二次确认；文案标明「保留已结算收益，丢弃未完成周期进度」 |

语言：沿用 Nexus `UiText` 中英双语键。

---

## 8. 与现有代码的差距

| 现有实现 | 状态 |
| --- | --- |
| `DeckEntity` / `PlayerDeckState` + `decks.json` | **完成**（`Deck/Domain` + `DeckService`；Migrator 1→2） |
| 编制权威 | `DeckEntity.slotCardIds`；`LineupPosition` 为活跃战斗卡组镜像 |
| Formation / Bridge 多卡组 UI | **P1.2 完成**（页签切换、解锁条件、并行摘要、停止确认） |
| `ActionScheduler` | **P1.4 完成**（Start/Stop/Complete/PausedCap；低停止惩罚） |
| 备用卡组 | **P1.5 完成**（idle 共享编制 + UI 可编辑多套未运行卡组） |
| 采集/制造周期收益结算 | **后置 P3**（调度器只清进度标志，不发资源） |

**代码入口：**

- Domain：`Assets/Resources/Scripts/Deck/Domain/`（asmdef `GalacticFrontier.DeckDomain`）
- Runtime：`Assets/Resources/Scripts/Deck/DeckService.cs`
- UI：`FormationScreen.cs`、`BridgeScreen.cs`
- Tests：`DeckRulesTests.cs`、`ActionSchedulerTests.cs`
- Save：`SaveVersion.Current = 2`；`DefaultProperty.DECKS_DATA`

---

## 9. 验收清单

- [x] 开局 2 个卡组槽可用，并行上限为 2（`DeckStateFactory` + 单测）  
- [x] 每卡组最多 5 人；同卡组内不可重复 `cardId`（`DeckRules.TryAssignSlot`）  
- [x] 同一 `cardId` 不能同时处于两个 Running 卡组  
- [x] 不同 `cardId` 的同名角色可同时 Running  
- [x] 未运行卡组可共享同一 `cardId` 预设且不占用  
- [x] 启动冲突时有明确失败原因（`DeckCommandError` / `ConflictCardIds`）  
- [x] 停止后占用立即解除，可立刻改打其他行动  
- [x] 主动停止不扣除已入包资源、不加长时间锁定（当前仅清状态；资源惩罚属后续生产系统）  
- [x] 旧存档仅有 `LineupPosition` 时可迁移出默认战斗卡组（Migrator 1→2 / `EnsureLoaded`）  
- [x] 领域规则具备 EditMode 测试（冲突启动、并行上限、停止解锁、同名并行、Running 不可编辑）  
- [x] Formation / Bridge 多卡组 UI 与解锁提示（P1.2）  
- [x] 通用行动调度器（P1.4）  
- [x] 备用卡组可在 UI 中编辑且不占用（P1.5）

---

## 10. 开放给数值/后续文档的钩子

本文件已拍板语义；下列仅调参，不改语义：

- `DeckUnlockTable` 各档等级与章节阈值  
- `requireFullDeckForMainCombat`  
- 制造停止时材料返还比例（生产文档最终确认）  
- `Research` / `Transit` 行动的完整循环  

若产品希望改为「编制也不允许跨卡组重复」（更严），需修订 §4.1 并升本文主版本号至 v2.0。

---

## 11. 参考

- 核心设计：`Documentation/01-core-product-design.md`  
- 开发计划：`Documentation/zh-CN/11-mvp-development-plan.md`  
- 当前编队实现：`LineupManager.cs`、`FormationScreen.cs`、`CardEntity.LineupPosition`  
- 卡牌集合：`CardListManager.cs`  
- 存档：`Documentation/zh-CN/05-data-and-save.md`
