# 系统文档：全自动回合制战斗

> 文档版本：v1.0  
> 状态：**MVP 规则已拍板，可供 P0/P2 实现**  
> 上级约束：`Documentation/01-core-product-design.md` §7.1 / §21 / §22  
> 关联：`01-deck-and-occupation.md`（出战卡组与占用）、开发计划 P0/P2  
> 更新日期：2026-08-09

---

## 1. 目标与非目标

### 1.1 目标

在**不要求玩家逐回合点选**的前提下，提供可预测、可加速、可跳过、可复现的自动回合制战斗，并同时服务：

- 主线 / 区域**首次挑战**（可观看演出）；
- 通关后**挂机刷取**（默认可跳过演出、可离线批量结算）；
- 完整**战报**（便于理解胜负与数值）。

战前决策是玩家的主要操作面；战中只提供观看、加速、跳过与结果确认。

### 1.2 非目标

- 多卡组占用与并行上限 → `01-deck-and-occupation.md`
- 区域解锁、舰船门、遭遇表配置权威 → `03-region-and-ship.md`
- 挂机刷取的时间产率、离线上限 → `04-idle-and-offline.md`
- 装备耐久损耗具体公式 → `06-durability-and-repair.md`
- 实时 PvP、手工点选技能、速度轴 QTE

---

## 2. 术语

| 术语 | 定义 |
| --- | --- |
| **遭遇（Encounter）** | 一次战斗的敌方配置（敌人列表、等级、奖励引用、Boss 标记等） |
| **结算器（Resolver）** | 纯规则层：输入阵容+策略+种子 → 输出 `BattleResult`（可无 Unity 演出） |
| **演出（Presentation）** | 将结算步骤以动画/特效播放；可加速或跳过 |
| **战前策略（CombatStrategy）** | 玩家配置的目标优先级与技能倾向等 |
| **站位槽（Slot）** | 卡组内 0–4 号位置，同时映射战场坐标与前后排 |
| **能量（Energy）** | 单位行动资源；满额时释放特殊攻击并清空 |

---

## 3. 硬约束

1. 战斗必须是**全自动回合制**；战中不得要求玩家点选角色、技能或目标。  
2. 出战卡组最多 **5** 人，开战时**全部入场**（不抽牌、不备牌进出）。  
3. 主线挑战与挂机刷取共用同一套结算规则，仅在遭遇配置、奖励与演出默认值上区分。  
4. 持久化卡牌成长数据不得被战斗中的临时生命/Buff 直接写脏（开战深拷贝或等价隔离）。  
5. 跳过演出与观看演出在**相同种子**下必须得到同一 `BattleResult`。

---

## 4. MVP 拍板决议

> 关闭核心设计 §21「战斗系统」待定项。优先贴合当前 `BattleController` 已跑通行为，再补齐策略、确定性与跳过。

### 4.1 战场站位（5 槽）

采用 **2 前排 + 3 后排**：

```text
玩家侧（面向敌方 →）          敌方侧（对称镜像）

后排  [2]  [3]  [4]            [4]  [3]  [2]  后排
前排     [0]  [1]                  [1]  [0]     前排
```

| 卡组槽 `slotIndex` | `Card.position`（现有） | 排 | UI 说明 |
| --- | --- | --- | --- |
| 0 | 1 | 前排 | 左前 / 主坦位倾向 |
| 1 | 2 | 前排 | 右前 |
| 2 | 3 | 后排 | 左后 |
| 3 | 4 | 后排 | 中后 |
| 4 | 5 | 后排 | 右后 |

规则：

- 编队槽位 = 战斗站位；不满编时对应槽位空置并隐藏。  
- `TargetSelector` 前后排语义与上表对齐（前排 = position 1–2，后排 = 3–5）。  
- MVP **不**做「前排全灭才可被后排选中」的强制索敌墙；前后排只影响技能默认目标倾向与策略优先级。  
- 站位对部分技能的范围标签生效：`Front` / `Back` / `All` / `RandomN`。

### 4.2 回合结构与行动顺序

| 项目 | MVP 规则 |
| --- | --- |
| 回合 | 双方存活单位按规则各行动 **至多 1 次** 构成一回合 |
| 每回合可行动人数 | **所有存活单位**（不是「每边只动 N 人」） |
| 行动顺序 | 按 **Speed 降序**；每回合开始时对当前存活单位重新排序 |
| Speed 相同 | 1）玩家单位优先于敌方；2）仍平则 `slotIndex` 更小者优先 |
| 回合上限 | 默认 **15**（`MaxRound`，可配置）；达上限未分胜负 → **防守方/敌方胜利**（玩家挑战失败） |
| Buff/Debuff | 在单位行动后或回合末统一 tick（与现有 Manager 对齐；实现时固定一种并写测试） |

回合内伪代码：

```text
for round in 1..MaxRound:
  living = allAlive sorted by (Speed desc, playerFirst, slotAsc)
  for unit in living:
    if unit dead: continue
    if no living enemies: end Victory
    act(unit)   // 选技能 → 选目标 → 结算
    if either side wiped: end
  tickStatuses(roundEnd)
if still running: player Defeat (max rounds)
```

### 4.3 能量与技能自动选择

保留并形式化现有能量门控：

| 项目 | MVP 默认 |
| --- | --- |
| 存在能量 | **是**（不用独立「行动点」双资源） |
| `maxEnergy` | 默认 **100**（可按卡覆盖） |
| 开战能量 | **0** |
| 普通攻击 | 执行 `NormalAttack`，然后 `energy += energyGainOnNormal`（默认 **30**） |
| 特殊攻击 | 当 `energy >= maxEnergy` 时执行 `SpecialAttack`，然后 **清空能量** |
| 溢出 | `energy` clamp 到 `maxEnergy` |
| 被动 | `PassiveSkill` 在开战时触发一次；持续型被动由 Buff 表达（MVP 可保持空实现） |

**技能选择（默认 Balanced）：**

```text
if energy >= maxEnergy and strategy.skillBias != ConserveSpecial:
    SpecialAttack
else:
    NormalAttack (+ energy)
```

角色仍通过 `Character` 策略类实现具体伤害与目标子集；控制器只决定「普攻 / 特攻」分支。

### 4.4 目标选择

分两层：

1. **技能固有选择器**（角色实现）：如前排、随机 N、全体等（现有 `TargetSelector`）。  
2. **战前策略修正**（玩家配置）：在技能给出的候选集合上再排序取目标。

若技能需要单体目标：在候选存活列表上按策略排序，取第 1 个。  
若技能为多目标 / 全体：策略只影响「优先打击谁」的排序，不改变命中人数上限。

### 4.5 角色阵亡

| 项目 | MVP 规则 |
| --- | --- |
| 判定 | 战斗内临时生命 `<= 0` → 死亡 |
| 表现 | 播放死亡/退场后隐藏；不再进入行动队列 |
| 复活 | 标准遭遇 **不允许** 通用复活；仅当技能显式实现时可复活 |
| 对持久卡牌 | 深拷贝作战；死亡**不**删除玩家卡池实例、**不**掉耐久以外的永久惩罚 |
| 耐久 | 按参战与行动类型在战后结算损耗（公式见耐久文档；战斗层只上报「参战/行动次数」事件） |

### 4.6 战前策略配置（CombatStrategy）

挂在卡组上（见占用文档 `combatStrategyId`），开战前可改。

#### 目标优先级 `TargetPriority`

| 值 | 行为 |
| --- | --- |
| `LowestHp` | 优先当前生命最低（斩杀） |
| `HighestHp` | 优先当前生命最高 |
| `HighestAttack` | 优先战斗攻击最高（威胁） |
| `FrontFirst` | 前排优先，同排内再按 `LowestHp` |
| `BackFirst` | 后排优先，同排内再按 `LowestHp` |
| `Random` | 在候选中伪随机（仍吃战斗种子） |

默认：`FrontFirst`。

#### 技能倾向 `SkillBias`

| 值 | 行为 |
| --- | --- |
| `Balanced` | 能量满则特攻（默认） |
| `ConserveSpecial` | 即使能量满也继续普攻（囤满不放；用于特殊测试/少数场景） |
| `PreferSpecial` | 与 Balanced 相同触发；预留「能量阈值降低」配置钩子（MVP 阈值仍为满额） |

MVP 不做更复杂的「技能树多选一」；角色仅普攻/特攻二元。

#### 消耗品 / 自动装备规则（占位）

- MVP 可只保留数据结构：`autoConsumableRules[]`（条件 → 使用物品 Id）。  
- 未实装前开战忽略；UI 可隐藏或显示「即将推出」。  
- 实装后仍必须全自动，禁止战中弹窗确认。

### 4.7 加速、跳过与战报

| 功能 | MVP 规则 |
| --- | --- |
| 加速 | 演出倍速：**1x / 2x / 3x** 循环（替代现网 `0.2x` 调试档作为正式三档；`0.2x` 仅 Debug 构建保留） |
| 跳过演出 | 立即用同一 Resolver + 同一种子算完剩余战斗，直接进入战报 |
| 开战即跳过 | 挂机刷取默认 `PresentationMode.Skip`；主线默认 `Play` |
| 战报 | 至少：胜负、回合数、我方每人造成伤害 / 承伤 / 治疗；确认后返回 Explore/Main |
| 返回 | **P0.3**：战报 Confirm → `BattleSceneExit.ReturnToExplore()`；Esc / Chrome → `ReturnToBridge()` |

倍速实现注意：优先调制演出等待，避免长期依赖全局 `Time.timeScale` 影响非战斗系统；过渡期可继续用 `Time.timeScale`，但跳过结算不得依赖 timeScale。

### 4.8 胜负与战斗类型

| `BattleMode` | 说明 | 占用状态（卡组文档） |
| --- | --- | --- |
| `MainCombat` | 主线/区域首次或手动挑战 | `MainCombat`，战斗结束解除 |
| `AutoCombat` | 通关后挂机刷取单场或批量 | `AutoCombat`，直到停止挂机 |

胜负：

- 敌方全灭 → 玩家胜利  
- 我方全灭 → 玩家失败  
- 达回合上限 → 玩家失败  

奖励、区域通关标记由区域/挂机文档定义；战斗层只产出 `BattleResult`。

### 4.9 确定性与随机

| 项目 | MVP 规则 |
| --- | --- |
| 战斗种子 | 每场 `battleSeed`（int/long）；挂机连续场次可 `hash(seed, fightIndex)` |
| 覆盖范围 | 命中、暴击、技能内随机目标、策略 `Random` |
| 禁止 | 结算路径调用未播种的 `UnityEngine.Random` |
| 用途 | 战报复现、跳过=观看一致、离线批量结算 |

---

## 5. 数据模型（实现契约）

### 5.1 CombatStrategy

```text
CombatStrategy
- strategyId: string
- displayName: string
- targetPriority: TargetPriority
- skillBias: SkillBias
- autoConsumableRules: []          // MVP 可空
```

### 5.2 BattleRequest

```text
BattleRequest
- battleId: string
- mode: MainCombat | AutoCombat
- deckId: string
- encounterId: string
- strategy: CombatStrategy
- battleSeed: long
- presentation: Play | Skip
- speedOption: x1 | x2 | x3       // 仅 Play 有意义
```

### 5.3 BattleResult

```text
BattleResult
- battleId: string
- outcome: Victory | Defeat
- roundsFought: int
- seed: long
- participantStats[]: { cardId, damage, injury, heal, survived }
- events[]: optional compact log   // 跳过模式可只留摘要
- rewardsHint: id?                 // 实际发放在外层系统
```

### 5.4 EncounterConfig（战斗侧最小集）

```text
EncounterConfig
- encounterId: string
- enemySlots[5]: { characterId or template, level, attrs override? }
- maxRoundOverride?: int
- isBoss: bool
```

敌人来源必须来自配置表，**禁止**正式流程 `FakeData()` 随机生成（P0 技术债）。

---

## 6. 架构分层

```text
UI / BattleScene (Presentation)
        ↓ BattleRequest
BattleFacade (选卡组、占用 MainCombat、加载遭遇)
        ↓
BattleResolver (纯 C# 结算，可 EditMode 测试)
        ↓ BattleResult
Reward / Region / AutoCombat loop
```

| 层 | 职责 |
| --- | --- |
| Resolver | 顺序、能量、策略、伤害、胜负；无 MonoBehaviour 也可跑 |
| Presentation | 按步骤播放；Skip 时不调用 |
| Facade | 与 `DeckService`、场景加载、战报 UI 粘合 |

演进建议：先把 `BattleController` 中的规则抽到 `BattleResolver`，控制器改为「演出驱动器」；不必一次重写所有角色协程，可先让 Resolver 调用无等待的伤害 API，角色动画逐步适配。

---

## 7. 伤害公式（沿用现网，定为契约）

```text
命中率 = clamp(攻击方命中 - 防守方闪避, 0, 1)
未命中 → 伤害 0

暴击倍率 = (rand < 暴击率) ? 暴伤 : 1

减伤率 = clamp(log₁.₄(max(防御, 1)) × 0.01 + 固定减伤, 0, 1)

伤害 = 战斗攻击 × 暴击倍率 × (1 - 减伤率) × 技能倍率
```

修改公式必须同步更新测试与本文版本号。

---

## 8. 与挂机刷取的接口

单场挂机：

1. 卡组 `TryStart(AutoCombat, regionId)` 占用成功；  
2. 循环：`BattleRequest(presentation=Skip)` → `BattleResult` → 发奖 / 扣耐久 / 检查停止条件；  
3. 玩家停止或离线达上限 → `Stop` 解除占用。

Resolver 必须支持**无场景**连续调用，供离线结算复用。

---

## 9. UI / UX 要求

| 界面 | 要求 |
| --- | --- |
| Explore 开战前 | 选择卡组、展示策略摘要、遭遇信息；确认后进入战斗 |
| 战斗中 | 不出现技能/目标点选；提供加速、跳过、回合数 |
| 跳过 | 一键；禁用后再点不重复结算 |
| 战报 | 胜负、回合、人均数据；主按钮返回舰桥/探索 |
| 失败 | 明确失败原因（团灭 / 回合上限）；不锁卡组 |

文案对齐 Nexus：`UiText` 已描述「全自动结算 / 加速 / 跳过」。

---

## 10. 与现有代码的差距

| 现有实现 | 差距 |
| --- | --- |
| `BattleController` 全自动按 Speed 行动 | 符合方向；需抽 Resolver + 种子 |
| 能量满特攻 / 否则普攻+30 | 已符合 §4.3；需接入 `SkillBias` |
| `TargetSelector` 前后排 | 已基本符合 §4.1；策略排序未做 |
| `MaxRound = 15` | 保留；明确超时算负 |
| 敌人 `FakeData()` | 必须改为遭遇表 |
| 编队来自全局 `GetInLineCardEntities` | 改为 `deckId` 成员 |
| `SpeedController` 含 0.2x | 正式三档改为 1/2/3 |
| 战报后返回主场景被注释 | P0 必修 |
| 跳过演出 | 未实现 |
| `CombatStrategy` | 未实现 |
| ~~无战斗种子~~ | **P0.4**：`BattleRng` / `CombatMath`；`BattleController.BattleSeed` |

**建议落地顺序：**

1. ~~P0：战报返回；战斗种子接入伤害/暴击~~ **完成**；遭遇表替换敌人仍属 P2.3  
2. P0/P2：`CombatStrategy` + 目标排序；加速档位调整；Skip 模式  
3. P2：`BattleResolver` 与挂机刷取无场景结算  
4. 持续：角色技能改为接受已排序目标列表，避免内部再 Random 未播种  

---

## 11. 验收清单

- [ ] 战中无任何「点选技能/目标」交互  
- [ ] 5 槽 2前3后站位与编队一致；空槽隐藏  
- [ ] 每回合所有存活单位按 Speed（及平局规则）各行动一次  
- [ ] 能量满放特攻并清空；否则普攻并获得默认 30 能量  
- [ ] 阵亡单位跳过行动；持久卡池不被战斗 HP 写脏  
- [ ] 战前可配置 `TargetPriority` / `SkillBias`，并影响自动索敌/技能分支  
- [ ] 1x/2x/3x 加速可用；跳过与观看同种子结果一致  
- [x] 战报展示后可返回 Main/Explore（P0.3：Confirm → Explore；Esc → Bridge）  
- [ ] 正式流程敌人来自遭遇配置，而非 FakeData()（P2.3）  
- [x] EditMode：同种子伤害序列一致；超时/歾灭判负胜（P0.4/P0.5；完整 BattleResult 回放随 Resolver 演进）

---

## 12. 开放钩子（不阻塞 MVP）

- Boss 战特殊机制阶段（仍须自动结算）  
- 消耗品自动规则实装  
- `PreferSpecial` 降低能量阈值的具体数值  
- 前排护卫（强制索敌墙）作为可选遭遇规则  
- 多特殊技能栏位  

若要将「每回合仅 N 人行动」或「手牌抽取式上场」纳入，需升本文主版本并修订核心设计。

---

## 13. 参考

- 核心设计：`Documentation/01-core-product-design.md` §7.1 / §21  
- 卡组占用：`01-deck-and-occupation.md`  
- 现有实现：`BattleController.cs`、`Character` 子类、`TargetSelector.cs`、`SpeedController.cs`、`BattleReportManager.cs`、`BattleChrome.cs`  
- 系统说明：`Documentation/zh-CN/04-core-systems.md` §6–7  
- 开发计划：`Documentation/zh-CN/11-mvp-development-plan.md`
