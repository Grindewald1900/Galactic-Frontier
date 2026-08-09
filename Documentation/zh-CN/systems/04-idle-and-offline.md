# 系统文档：挂机刷取与离线收益

> 文档版本：v1.0  
> 状态：**MVP 规则已拍板，可供 P2/P3 实现**  
> 上级约束：`Documentation/01-core-product-design.md` §7.3 / §7.4 / §16.1 / §20 / §21 / §22  
> 关联：`01-deck-and-occupation.md`（占用 / `PausedCap`）、`02-auto-battle.md`（无场景结算）、`03-region-and-ship.md`（通关后刷取解锁）  
> 实现阶段：开发计划 P2.4 / P3.6（见 `../11-mvp-development-plan.md`）  
> 更新日期：2026-08-09

---

## 1. 目标与非目标

### 1.1 目标

在**不依赖玩家持续盯屏**的前提下，让已启动的挂机行动（自动战斗刷取、采集，以及后续生产）能够：

- 在线按周期持续产出；
- 离线按同一规则批量结算；
- 用**分阶段成长的离线时长上限**体现成长，而非强迫高频上线；
- 在达上限、仓库满、材料不足时**暂停而非惩罚**。

本系统是「开图之后」资源侧循环的时间与结算骨架；具体配方、品质与耐久公式分别见后续文档。

### 1.2 非目标

- 多卡组占用与并行上限本身 → `01-deck-and-occupation.md`
- 单场战斗回合规则与战前策略 → `02-auto-battle.md`
- 区域解锁与舰船硬门 → `03-region-and-ship.md`
- 生产链配方、品质档位 → `05-production-and-quality.md`
- 耐久损耗与维修公式 → `06-durability-and-repair.md`
- 全服市场撮合 → `07-market-and-card-trade.md`
- 强制推送、付费延长离线（可留钩子，非 MVP 必做）

---

## 2. 术语

| 术语 | 定义 |
| --- | --- |
| **挂机行动（Idle Action）** | 可长时间 `Running` 并按周期产出的卡组行动：`AutoCombat`、`Gather`，以及后续 `Process` / `Manufacture` 等 |
| **在线滴答（Online Tick）** | 客户端在前台时，按固定间隔推进进度并结算完整周期 |
| **离线结算（Offline Settle）** | 玩家重新上线（或会话恢复）时，用离线时长对所有挂机行动做批量结算 |
| **有效离线时长（Credited Offline）** | `min(真实离线秒数, 当前离线上限)`；仅这段时间产生收益 |
| **离线上限（Offline Cap）** | 玩家当前可累积的最大有效离线秒数；分阶段成长 |
| **待领收益（Pending Loot）** | 已结算但尚未写入仓库的奖励缓冲；上线可一键领取 |
| **暂停（PausedCap / PausedBlock）** | 行动因达上限或资源阻塞而停止推进；**不解除占用**，直到玩家继续或停止 |
| **周期（Cycle）** | 某行动完成一次完整产出的时间单位（刷取=一场战斗间隔；采集=节点周期） |

---

## 3. 硬约束（不可违背）

来自核心设计 §20 / §22，实现与测试必须满足：

1. 多项挂机行动通过**多支独立卡组**并行，不得用单卡组多任务绕过占用。  
2. 离线收益时间属于**成长内容**，须分阶段解锁/提升。  
3. 超过离线上限后：已获资源不丢失、行动暂停继续产出、角色与装备无额外惩罚、上线可立即恢复或调整。  
4. 挂机刷取仅在区域**首次通关**（首领区需 Boss 击杀）后允许，见区域文档。  
5. 离线批量战斗必须与在线跳过演出使用**同一 Resolver + 同一确定性种子规则**（见战斗文档）。  
6. 停止行动惩罚偏低：不扣已入包/已入待领的资源，不长时间锁角色。

---

## 4. MVP 拍板决议

> 下列决议用于关闭核心设计 §21「离线收益」待定项，并锁定挂机刷取 / 采集的时间语义。  
> 纯数值可进配置表调整；**语义默认按本表实现**。

### 4.1 哪些行动可离线继续

| 行动类型 | 在线挂机 | 离线继续 | MVP |
| --- | --- | --- | --- |
| `MainCombat` | 否（单次会话战斗） | 否 | 已有 |
| `AutoCombat` | 是 | 是 | P2 |
| `Gather` | 是 | 是 | P3（骨架可与 P2 并行） |
| `Process` / `Manufacture` | 是 | 是 | P3 |
| `Research` / `Transit` | 预留 | 预留 | 非 MVP 必做 |

规则：仅当卡组 `status == Running` 且行动类型属于上表「离线继续」时，才计入离线结算。  
`PausedCap` / `PausedBlock` 在离线期间**不推进**，也不消耗离线额度（见 §4.6）。

### 4.2 离线上限：初始值与成长

#### 基础上限（拍板）

| 项目 | MVP 默认 | 说明 |
| --- | --- | --- |
| 开局离线上限 | **2 小时**（7200s） | 核心设计要求「初期较短」 |
| MVP 软顶 | **12 小时**（43200s） | 首发不必做到 24h；后续扩展可再开 |
| 硬顶（防配置失误） | **24 小时** | 代码 clamp，即使表配错也不超过 |
| 计时单位 | 秒（存盘用 UTC Unix） | UI 显示为小时/分钟 |

#### 上限计算公式（语义）

```text
offlineCapSeconds = clamp(
    baseCap
    + sum(levelTierBonuses)      // 玩家等级档
    + sum(chapterBonuses)        // 主线章节档
    + sum(shipLevelBonuses)      // 舰船等级档
    + sum(facilityBonuses)       // 自动化设施等
    + sum(researchBonuses),      // 研究；可空
    0, hardCap)
```

**权重原则（拍板）：**

| 来源 | 权重意图 | MVP 建议贡献（达软顶前） |
| --- | --- | --- |
| 玩家等级 | 稳定成长主轴 | 约 **35%** |
| 主线章节 | 推进奖励，节点感强 | 约 **30%** |
| 舰船等级 | 与开图/基建绑定 | 约 **20%** |
| 设施 / 研究 | 可规划的额外投资 | 约 **15%** |

权重用于指导配表，不是运行时动态百分比；代码只读加成表求和。

#### 默认加成表（可配置 `OfflineCapTable`）

| 条件 | `+秒` | 累计示例（自 2h 起） |
| --- | --- | --- |
| 玩家等级 ≥ 5 | +1800（+0.5h） | 2.5h |
| 玩家等级 ≥ 10 | +1800 | 3.0h |
| 玩家等级 ≥ 15 | +3600（+1h） | 4.0h |
| 玩家等级 ≥ 20 | +3600 | 5.0h |
| 主线章节 1 通关 | +3600 | … |
| 主线章节 2 通关 | +3600 | … |
| 主线章节 3 通关 | +7200（+2h） | … |
| 舰船等级 ≥ 2 | +1800 | … |
| 舰船等级 ≥ 4 | +3600 | … |
| 舰船等级 ≥ 6 | +3600 | … |
| 设施「自动化核心」Lv.1 | +1800 | … |
| 设施「自动化核心」Lv.2 | +3600 | … |
| 设施「生命维持」Lv.1 | +1800 | LifeSupport 钩子 |
| 研究「深空待机协议」 | +3600 | 可选 |

配表须保证：完成 MVP 主线首圈 + 合理舰船/设施后，玩家可达 **约 8–12 小时**，体现成长且不强制半夜上线。

### 4.3 时钟与有效离线时长

| 项目 | MVP 规则 |
| --- | --- |
| 权威时间 | 以**上线结算时刻的服务器时间**为准；无服务器时用设备 UTC，并记录 `lastSeenAtUtc` |
| 离线秒数 | `rawOffline = max(0, nowUtc - lastSeenAtUtc)` |
| 有效离线 | `credited = min(rawOffline, offlineCapSeconds)` |
| 客户端改时 | 若 `nowUtc < lastSeenAtUtc`，视为无效，`credited = 0` 并打日志；不倒扣已有资源 |
| 会话切后台 | 超过 `backgroundGraceSeconds`（默认 **120s**）按离线处理；短切后台仍走在线滴答 |
| `lastSeenAtUtc` 更新 | 正常退出/进后台时写；结算完成后写为 `nowUtc` |

离线额度是**全局账户级**一条时间轴，不是每支队伍各算一条上限。  
多队伍并行时：在同一 `credited` 窗口内各自按自己的周期结算（见 §5）。

### 4.4 挂机自动战斗（AutoCombat）

前置：区域 `Cleared`（首领区需 `BossDefeated`），且卡组 `TryStart(AutoCombat, regionId)` 成功。

| 项目 | MVP 默认 |
| --- | --- |
| 单场间隔 | `fightIntervalSeconds`（默认 **90s**，可按区域配置） |
| 单场内容 | `farmEncounterId`（见区域文档）；调用 `BattleResolver`，`PresentationMode.Skip` |
| 胜负 | 胜→发奖；负→本场无产物，**不自动停止**挂机（避免离线偶发失败打断整晚） |
| 连败保护 | 连续失败 ≥ `maxConsecutiveLosses`（默认 **5**）→ `PausedBlock` 并提示「战力不足」 |
| 种子 | `battleSeed = hash(runSeed, fightIndex)`；`runSeed` 在启动挂机时生成并写入行动进度 |
| 奖励 | 经验/信用/掉落表引用；进入 **Pending Loot**（或配置为直接入仓，见 §4.7） |
| 耐久 | 每场按耐久文档扣减；若关键装备耐久归零导致无法作战 → `PausedBlock`（不销毁装备） |
| 人数修正 | 不满编允许；奖励/胜率由战力自然体现，**不另乘**「缺人惩罚系数」 |
| 停止 | 玩家停止 → 结算已完成场次；当前未打完的场次进度丢弃 |

在线滴答：每累计满 `fightIntervalSeconds` 结算 1 场。  
离线：`fights = floor(actionCreditedSeconds / fightIntervalSeconds)`，循环调用 Resolver（须有单次结算时间预算与场次上限保护，见 §6.3）。

### 4.5 挂机采集（Gather）骨架

细则产量曲线由生产/节点表给出；本文件锁定时间与阻塞语义。

| 项目 | MVP 默认 |
| --- | --- |
| 启动 | `TryStart(Gather, gatherNodeId)`；节点须已因区域通关解锁 |
| 周期 | `cycleSeconds`（节点配置，默认 **300s**） |
| 每周期产出 | `baseYield * deckGatherMultiplier`（乘区来自角色采集属性；公式在采集表） |
| 货舱 / 仓库满 | 见 §4.6 |
| 停止 | 已完成周期入 Pending/仓库；当前周期进度丢弃 |

采集与 `AutoCombat` 可并行（不同卡组），共享同一离线上限窗口。

### 4.6 阻塞与暂停（无惩罚）

| 原因 | 行动状态 | 占用 | 收益 | 离线额度 |
| --- | --- | --- | --- | --- |
| 有效离线用尽 | 全挂机行动 → `PausedCap` | 保持 | 已产生的保留 | 达 cap 后不再消耗、不再产出 |
| 仓库 / 货舱已满 | 该行动 → `PausedBlock` | 保持 | 已产生的保留；本周期不发 | 阻塞期间不推进该行动 |
| 制造材料不足 | 该行动 → `PausedBlock` | 保持 | 已完成批次保留 | 同上 |
| 刷取连败保护 | 该行动 → `PausedBlock` | 保持 | 已有场次保留 | 同上 |
| 耐久归零无法作战 | 该行动 → `PausedBlock` | 保持 | 已有场次保留 | 同上 |

**禁止的惩罚：**

- 不删除已入仓或 Pending 中的资源；  
- 不因达上限而扣耐久、扣角色、扣舰船；  
- 不强制解散卡组；  
- 不上线「惩罚性疲劳」。

上线后玩家可：领取 Pending → 清仓/补材料/修装备 → **继续**（`Paused* → Running`）或 **停止**（立即 `Idle`）。

### 4.7 待领收益与一键领取

| 项目 | MVP 规则 |
| --- | --- |
| 默认入账 | 离线结算产物进入 `PendingLoot`；在线滴答可配置 `onlineDirectDeposit`（默认 **true** 直接入仓） |
| 一键领取 | Bridge / 邮件式面板「领取全部」：尝试写入仓库；成功部分清空，失败部分留在 Pending 并提示空间不足 |
| 领取前可见 | 按行动分条展示预估/已结算汇总（资源数量、战斗胜场、失败场） |
| 溢出 | 领取时仓库满 → 剩余留 Pending，对应行动保持/进入 `PausedBlock` |
| 不上线自动丢弃 | Pending **无过期删除**（MVP）；防止「忘记领就没了」 |

### 4.8 在线滴答 vs 离线结算（同一公式）

```text
SettleAction(action, deltaSeconds):
  if action.status in {PausedCap, PausedBlock}: return empty
  usable = deltaSeconds
  while usable >= cycleSeconds and not blocked:
      result = CompleteOneCycle(action)  // 战斗或采集
      if result.blocked: set PausedBlock; break
      usable -= cycleSeconds
  action.progressRemainder = usable  // 不足一周期的进度保留（在线）
```

| 模式 | `deltaSeconds` 来源 | 余数进度 |
| --- | --- | --- |
| 在线 | 滴答间隔累加 | **保留**到下一滴答 |
| 离线 | 从全局 `credited` 分配到该行动的时间（见 §5.2） | **保留**写入 `progressPayload`，上线后继续 |

主动停止：丢弃余数进度；已完成周期不受影响。

### 4.9 与舰船 LifeSupport 的关系

区域文档钩子：生命维持可影响离线挂机。

MVP 拍板（二选一默认采用 **A**）：

| 方案 | 规则 | MVP |
| --- | --- | --- |
| **A. 加时长** | LifeSupport 只增加 `offlineCapSeconds`（见 §4.2 表） | **采用** |
| B. 加效率 | 离线产出 ×(1+lifeSupportBonus) | 不做；避免与上限成长叠乘难懂 |

深区「持续时间」类限制若存在，只约束**进入区域**或**在线连续刷取提示**，不另造第二套离线惩罚。

---

## 5. 数据模型（实现契约）

### 5.1 玩家离线状态

```text
PlayerIdleState
- lastSeenAtUtc: long
- offlineCapSeconds: int          // 可缓存；或以表重算为准
- pendingLoot: List<LootStack>    // itemId, qty, sourceActionId, sourceDeckId
- lastOfflineSettleAtUtc?: long
- lastCreditedOfflineSeconds?: int
```

建议存档：`saves/{playerId}/idle.json`（或并入 player 主档，但需进 `saveVersion` 迁移）。

### 5.2 离线时间如何分给多支队伍

全局只有一条 `credited` 秒数。每支 `Running` 挂机行动在该窗口内**各自独立**按自己的周期结算：

```text
对每支 Running 且可离线的行动:
  actionDelta = credited   // MVP：每支队伍都获得完整 credited 窗口
  SettleAction(action, actionDelta + savedRemainder)
```

说明：这与「并行队伍同时工作」的设计一致——2 小时离线、2 支队伍，各结算约 2 小时进度，而不是把 2 小时劈成两半。  
离线上限限制的是**现实时间覆盖长度**，不是「全账号产出工时池」。

### 5.3 行动进度载荷（示例）

`AutoCombat` 的 `progressPayload`：

```text
AutoCombatProgress
- regionId: string
- runSeed: long
- fightIndex: int
- consecutiveLosses: int
- remainderSeconds: float
- totalWins: int
- totalLosses: int
```

`Gather` 的 `progressPayload`：

```text
GatherProgress
- nodeId: string
- remainderSeconds: float
- cyclesCompleted: int
```

### 5.4 结算服务接口

```text
IIdleSettlementService
- OnAppPause(nowUtc)
- OnAppResume(nowUtc) -> OfflineSettleReport
- TickOnline(deltaSeconds)
- ClaimAllPending() -> ClaimResult
- TryResume(deckId) / Stop(deckId)
```

`OfflineSettleReport`：各行动场次/周期摘要、credited/raw 时长、是否触顶、阻塞原因列表。

---

## 6. 规则细则

### 6.1 上线结算顺序

1. 读取 `lastSeenAtUtc`，计算 `rawOffline` / `credited`。  
2. 重算 `offlineCapSeconds`（等级/章节/舰船可能已变——以上线时刻为准）。  
3. 对每支可离线行动 `SettleAction`。  
4. 若 `rawOffline >= offlineCap`：所有仍在推进的挂机行动标为 `PausedCap`。  
5. 生成 `OfflineSettleReport`；UI 弹出摘要 +「领取」入口。  
6. 更新 `lastSeenAtUtc = nowUtc`。

### 6.2 在线与离线切换

| 事件 | 行为 |
| --- | --- |
| 启动挂机 | `status=Running`，写 `startedAtUtc` / `lastSettledAtUtc`，初始化 progress |
| 前台滴答 | `TickOnline` |
| 进后台超过 grace | 写 `lastSeenAtUtc`，停滴答 |
| 回前台 / 杀进程重启 | `OnAppResume` 走离线结算 |
| 领取 | `ClaimAllPending` |
| 停止 | `SettlePartial`（仅完整周期）+ 清进度 + `Idle` |

### 6.3 性能与安全护栏

| 护栏 | MVP 默认 |
| --- | --- |
| 单次离线最大模拟场次 | `min(floor(credited/interval), maxFightsPerSettle)`，`maxFightsPerSettle` 默认 **500** |
| 超时 | 若解析过慢，可改为「期望值快速路径」——**仅当**与 Resolver 统计误差在配置允许带内；MVP 优先真结算，超场次则截断并提示「已达本次结算上限」 |
| 禁止 | 用纯随机期望替代战斗文档的确定性要求（除非开战前显式配置 `allowExpectedValueShortcut` 且不进战报复现） |

### 6.4 主线战斗不占用离线额度

`MainCombat` 不参与挂机结算，不写入 `PlayerIdleState` 进度。  
玩家在主线战斗期间若杀进程，仅恢复到战前 Explore；不产生刷取收益。

### 6.5 验收用例（逻辑）

| 场景 | 期望 |
| --- | --- |
| 离线 3h，上限 2h，1 支刷取 | 只结算 2h 场次；行动 `PausedCap`；已产生奖励可领 |
| 离线 2h，上限 2h，刷取+采集并行 | 两队各按 2h 结算 |
| 仓库满于第 N 周期 | 前 N-1 入 Pending/仓；该行动 `PausedBlock`；另一队伍不受影响 |
| 上线领一半（空间不够） | 能写入的领走；剩余留 Pending；不清空行动 |
| 停止 `PausedCap` 队伍 | 立即 `Idle`，占用解除；Pending 仍可领 |
| 改设备时间到过去 | `credited=0`，不扣资源 |
| 刷取连败 5 场 | `PausedBlock`，提示强化/换区 |

---

## 7. UI / UX 要求

| 界面 | 要求 |
| --- | --- |
| Bridge | 显示离线上限（当前/软顶）、各 Running 行动余量进度、Paused 原因 |
| 回上线弹窗 | 展示 raw/credited 时长、是否触顶、每队产出摘要；主按钮「领取全部」 |
| 挂机刷取面板 | 选已通关区域、预估每小时场次与掉落期望（只读提示）、开始/停止 |
| 采集面板 | 节点周期、产出/周期、满仓提示 |
| PausedCap | 文案明确「已达离线储存上限，收益已保留，不会损失」 |
| PausedBlock | 分原因文案：仓满 / 缺材料 / 战力不足 / 需维修 |

语言：扩展 Nexus `UiText` 中英键；禁止再使用 Bridge「采集占位」类空文案作为唯一说明。

---

## 8. 与现有代码的差距

| 现有实现 | 差距 |
| --- | --- |
| Bridge 采集/挂机文案占位 | 无 `ActionScheduler` / 无 Offline 结算 |
| `BattleController` 仅场景内战斗 | 需 `BattleResolver` 无场景批量调用 |
| 无 `lastSeenAtUtc` / cap 表 | 需 `PlayerIdleState` + `OfflineCapTable` |
| 背包 FakeData | 正式产出前须隔离 FakeData（P0） |
| Explore 区域全开 | 须先有通关态才能刷取（P2.1） |
| 无 Pending 领取流 | 需 Bridge 领取 UI |

**建议落地顺序：**

1. P0 完成后：`lastSeenAtUtc` + 空的 `IIdleSettlementService` 骨架与 EditMode 时钟测试  
2. P2.4：`AutoCombat` 在线循环 + 离线批量（依赖 Resolver / 区域 Cleared）  
3. P3.6：完善 cap 成长表、Pending 领取、仓满/缺材料 `PausedBlock`  
4. P3：`Gather` 周期结算接入同一服务  

---

## 9. 验收清单

- [ ] 开局离线上限为 2 小时；成长后可提升，硬顶 ≤ 24 小时  
- [ ] `rawOffline > cap` 时仅结算 cap 时长，行动 `PausedCap`，已产生收益不丢  
- [ ] 多支挂机队伍在同一 credited 窗口内各自完整结算（不互劈时间）  
- [ ] 通关前不能启动 `AutoCombat`；通关后可启动并在线/离线产出  
- [ ] 离线战斗与跳过演出同种子规则一致（或明确走护栏截断）  
- [ ] 仓库满 / 材料不足 / 连败保护 → `PausedBlock`，无额外惩罚  
- [ ] 上线可一键领取 Pending；空间不足时部分领取  
- [ ] 主动停止丢弃未完成周期进度，不扣已结算收益，占用立即解除  
- [ ] 客户端时间回拨不导致资源被扣  
- [ ] EditMode：cap 计算、credited 截断、并行双队结算、Paused 不推进  

---

## 10. 开放给数值/后续文档的钩子

本文件已拍板语义；下列仅调参或外置，不改语义：

- `OfflineCapTable` 各档具体秒数与解锁条件  
- 每区域 `fightIntervalSeconds` 与掉落表  
- 采集 `cycleSeconds` / `baseYield`（节点表）  
- `maxFightsPerSettle`、期望值快速路径开关  
- 付费延长离线、推送提醒（非 MVP）  
- LifeSupport 若改为效率加成，需升本文主版本并修订 §4.9  

生产文档负责：制造周期与扣料时机。  
耐久文档负责：每场/每周期耐久损耗数值。  
二者必须遵守本文的 `PausedBlock` 与「无惩罚暂停」语义。

---

## 11. 参考

- 核心设计：`Documentation/01-core-product-design.md` §7.3 / §7.4 / §21  
- 卡组占用：`01-deck-and-occupation.md`（`PausedCap`、停止低惩罚）  
- 战斗：`02-auto-battle.md` §8（挂机接口 / Resolver）  
- 区域：`03-region-and-ship.md` §4.6（通关后刷取）  
- 开发计划：`Documentation/zh-CN/11-mvp-development-plan.md` P2.4 / P3.6  
- 已知问题：`Documentation/zh-CN/08-known-issues.md`（FakeData、战斗返回）
