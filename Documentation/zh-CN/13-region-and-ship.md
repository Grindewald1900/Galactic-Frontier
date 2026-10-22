# 系统文档：区域推进与舰船门槛

> 文档版本：v1.3
> 文档类型：**规则**
> **实现与验收状态**见 [PRODUCT-STATUS.md](PRODUCT-STATUS.md)；本文仅描述规则与设计标准。

> 上级约束：`02-core-product-design.md` §7.8 / §7.10 / §16.1 / §20 / §22（v0.4）  
> 关联：`11-deck-and-occupation.md`、`12-auto-battle.md`；离线/采集细则见 `14-idle-and-offline.md`；节点内部远征见 `20-stellar-map-and-navigation.md` §17  
> 更新日期：2026-09-07  
> 变更：v1.3 区域内部从「一场主挑战即通关」改为短篇远征；`Cleared` 映射为航标恢复。v1.2 对齐实现：`WorldService` / `ShipService` / `IdleCombatTicker`。

---

## 1. 目标与非目标

### 1.1 目标

用**双重门槛**驱动主线推进，并承接世界观中「断航后须自建航道与抗熵雾能力」：

1. **战斗通关** — 证明能走完该节点远征并击败 Boss（航标恢复）；  
2. **舰船条件** — 证明具备进入并持续开发该区域的基础设施（航程、能源、**熵雾抗性**等）。

由此把「开图」与「舰船成长 / 资源投入」绑在同一循环里，并避免仅靠等待时间卡进度。  
设定权威：`03-worldbuilding.md`（群星航网 / 断航灾变 / 开拓局）。

### 1.2 非目标

- 战斗内回合规则与策略 → `12-auto-battle.md`
- 挂机刷取产率、离线时长上限 → `14-idle-and-offline.md`
- 采集节点产量、生产链 → `15-economy.md` / `15-economy.md`
- **通关掉落、挂机奖励表、区域叙事填充** → `17-sector-and-onboarding.md`
- 多星域大地图、跨服航线、公会占星（非 MVP）→ 空间探索方向见 `20-stellar-map-and-navigation.md`
- 虫洞 / 宇宙暗面 / 多元宇宙的完整数值与 UI（§10 仅定方向，非 MVP 必做）

---

## 2. 术语

| 术语 | 定义 |
| --- | --- |
| **星域（StarSector）** | MVP 内容容器；首发 **1** 个完整星域 |
| **区域（Region）** | 星域内可探索单元；对应星图上一个可攻略节点；内部是短篇远征（`20` §17），不是单场战斗 |
| **前置战斗** | 进入本区远征或解锁本区前，需前置节点已 **航标恢复** |
| **舰船门槛（ShipGate）** | 进入区域所需的舰船等级与/或分项能力最小值 |
| **首次通关（FirstClear）** | 该节点 Boss 首次胜利，状态写入 `BeaconRestored`（旧「一场主挑战胜利」不再等于通关） |
| **挂机刷取解锁** | **分阶段**：阶段 2 开放基础采集挂机；阶段 3 开放战斗挂机；航标恢复后合并为驻留挂机（`20` §17.7） |
| **区域首领（RegionBoss）** | 星域内指定 Boss 遭遇（MVP **1** 名） |
| **舰船（Ship）** | 玩家账号唯一主舰实体；等级 + 分项能力 + **可升级模块** |
| **舰船模块（ShipModule）** | 独立升级的子系统；提升对应 ShipStat 并解锁功能钩子（并行、离线比例、卡组槽等） |

---

## 3. 硬约束

1. 进入高级区域必须同时满足：**完成前置战斗 + 达到舰船要求**（§7.8 / §22.6）。  
2. 舰船升级是主线推进组成部分，但**不得**仅以强制等待时间阻挡玩家（可用资源/战斗/设施驱动）。  
3. 首发 **1** 个完整星域、**5–6** 个可探索区域、**1** 名区域首领。  
4. 首次通关与挂机刷取均走全自动战斗；不依赖多人合作。  
5. 区域进度与舰船状态必须进存档，且可迁移。

---

## 4. MVP 拍板决议

### 4.1 内容结构：1 星域 × 6 区域

沿用 Explore UI 现有 5 个命名，并增加第 6 区作为首领区（达成 MVP「5–6 区域 + 1 首领」）。

| `regionId` | 显示名（中） | 显示名（英） | 角色 |
| --- | --- | --- | --- |
| `sec01_outer_belt` | 外缘带 | Outer Belt | 新手起始区；无前置区域 |
| `sec01_mining_spur` | 矿脉支线 | Mining Spur | 早期资源向 |
| `sec01_quantum_rift` | 量子裂隙 | Quantum Rift | 中期战斗向 |
| `sec01_abyssal_edge` | 深渊边界 | Abyssal Edge | 高压区 |
| `sec01_convoy_lane` | 护航航道 | Convoy Lane | 过渡 / 后勤叙事 |
| `sec01_frontier_boss` | 边境锚点 | Frontier Anchor | **区域首领**所在 |

星域 Id：`sector_frontier_vii`（群星边境 · 第七前沿）——开拓局在断航带边缘的试验星域。  
阵营标签预留：`FactionA` / `FactionB`（内容填充阶段挂到遭遇与卡牌，不阻塞门逻辑）。

### 4.2 双重解锁状态机

每个区域对玩家有进度态。节点内部权威状态是 `20` §17 的 `NodeCampaignState`；下表为兼容映射，供旧 `WorldService` 读取：

| `RegionProgressState` | 对应远征态 | Explore 表现 |
| --- | --- | --- |
| `Locked` | 前置或舰船未满足 | 可见但不可开战；显示缺口 |
| `Challengeable` | `Advancing` / `Staging` / `BossReady` | 「继续攻略 n/m」；阶段 ≥2 可「部署挂机」 |
| `Cleared` | `BeaconRestored` / `Developed` | 「驻留挂机」；不再逐关点击 |
| `BossAvailable` | 星域首领节点且 `BossReady` | 挑战星域首领（边境锚点） |
| `BossDefeated` | 星域首领节点 `BeaconRestored` | 星域首圈完成标记 |

**进入校验（硬门）：**

```text
CanEnter(region) =
  prerequisitesCleared(region)
  AND shipMeets(region.shipGate)
  AND deckReady(selectedCombatDeck)   // ≥1 人且可启动，见卡组文档
```

- 仅战力不足：**不硬锁**（可挑战，允许失败）；UI 可显示「推荐战力」。  
- 舰船或前置不足：**硬锁**，不可加载 `BattleScene`。

### 4.3 前置战斗链（默认）

```text
外缘带
  → 矿脉支线
  → 量子裂隙
  → 深渊边界
  → 护航航道
  → 边境锚点（首领）
```

| 区域 | 前置通关 |
| --- | --- |
| 外缘带 | 无 |
| 矿脉支线 | 外缘带 FirstClear |
| 量子裂隙 | 矿脉支线 FirstClear |
| 深渊边界 | 量子裂隙 FirstClear |
| 护航航道 | 深渊边界 FirstClear |
| 边境锚点 | 护航航道 FirstClear |

配置表可改图，代码只读 `prereqRegionIds[]`。

### 4.4 舰船能力与门槛

#### 舰船分项（ShipStat）

与设计 §7.8 对齐，MVP 全部建模，门槛按区启用子集：

| 枚举 | 中文 | 用途摘要 |
| --- | --- | --- |
| `Level` | 舰船等级 | 总成长门槛；升级提升基础值与设施槽 |
| `Range` | 航行距离 | 远离母港的区域 |
| `Energy` | 能源容量 | 高耗能裂隙 / 持续航行 |
| `Hull` | 舰体强度 | 高压 / 首领区 |
| `EntropyResist` | 熵雾抗性 | 裂隙、深渊类 |
| `Cargo` | 货物承载 | 采集收益搬运相关（与采集文档挂钩） |
| `Scan` | 扫描能力 | 揭示节点 / 部分遭遇信息 |
| `LifeSupport` | 生命维持 | 深区持续时间、离线挂机钩子 |

#### 区域门槛表示例（可配置 `ShipGateTable`）

数值可调；语义为「随推进抬高，但不形成纯时间墙」。

| 区域 | Level | Range | Energy | Hull | Entropy | Cargo | Scan | Life |
| --- | --- | --- | --- | ---: | ---: | ---: | ---: | ---: |
| 外缘带 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 矿脉支线 | 1 | 1 | 0 | 0 | 0 | 1 | 0 | 0 |
| 量子裂隙 | 2 | 2 | 2 | 1 | 2 | 1 | 1 | 1 |
| 深渊边界 | 3 | 3 | 2 | 3 | 3 | 2 | 2 | 2 |
| 护航航道 | 3 | 4 | 3 | 3 | 2 | 3 | 2 | 2 |
| 边境锚点 | 4 | 4 | 4 | 4 | 4 | 3 | 3 | 3 |

`shipMeets`：对表中每个 **>0** 的分项，要求 `ship.stat >= required`；`Level` 始终检查。

### 4.5 舰船升级与模块化（反时间墙）

| 项目 | MVP 规则 |
| --- | --- |
| 主舰数量 | 账号 **1** 艘主舰 |
| 成长双轨 | **舰船等级**（总许可/基础值）+ **分模块等级**（功能与门槛） |
| 升级消耗 | **资源 + 信用点**（主）；可选短 construction 计时 |
| 计时原则 | 若存在建造时间，必须提供 **材料加速 / 立即完成（信用或加速券）**；禁止「只能挂机等升级才能开下一区」作为唯一路径 |
| 分项成长 | 舰船等级提升各 Stat 基础值；**模块**提供主要额外分项与系统解锁 |
| 与仓库 | 升级与改装消耗进本地仓库；失败不扣（确认后扣） |
| 与离线 | 「生命维持 / 自动化」模块同时提高 Offline Cap 与 **Offline Yield Ratio**（见离线文档） |

#### MVP 模块表（拍板语义）

| `moduleId` | 名称 | 主要 Stat / 解锁 |
| --- | --- | --- |
| `mod_propulsion` | 推进 | +Range；后续虫洞最低门槛钩子 |
| `mod_reactor` | 能源核心 | +Energy |
| `mod_armor` | 装甲工坊 | +Hull；降低战斗耐久损耗钩子 |
| `mod_entropy` | 熵雾屏障 | +EntropyResist |
| `mod_cargo` | 货舱扩展 | +Cargo |
| `mod_scanner` | 扫描阵列 | +Scan |
| `mod_life_support` | 生命维持 | +LifeSupport；**离线比例 / 时长** |
| `mod_automation` | 自动化核心 | 并行行动上限；**离线比例 / 时长** |
| `mod_command` | 调度中枢 | 额外卡组槽（见占用文档） |

规则：

- 模块等级与具体数值进 `ShipModuleTable`；  
- 区域 `ShipGate` 检查的是 **合计后的 ShipStat**（等级基础 + 模块），不是要求某模块必须装上（除非特殊玩法另写）；  
- MVP 至少实装上表模块的数据驱动升级；UI 按模块列表展示。

#### 与旧「设施」表述

原文「设施」一律视为 **ShipModule**；卡组解锁表中的设施名与上表 `moduleId` 对齐。

### 4.6 首次通关 vs 挂机刷取

节点内部按 `20` §17 推进。一场普通战斗胜利只推进 `currentStage`，**不**把区域标为 `Cleared`。

| 行为 | 条件 | `BattleMode` | 占用 |
| --- | --- | --- | --- |
| 远征阶段战 | `CanEnter` 且当前阶段可挑战 | `MainCombat` | 战斗期间 `MainCombat`，结束解除 |
| 节点 Boss | `BossReady`；必须单独确认 | `MainCombat` | 同上 |
| 基础采集挂机 | 阶段 ≥2 | `Gather` | 直至停止 |
| 战斗挂机 | 阶段 ≥3；掉落按已完成最高阶段 | `AutoCombat` | 直至停止 |
| 驻留挂机 | `BeaconRestored` 或 `Developed` | `AutoCombat` / `Gather` | 直至停止 |
| Boss 高难悬赏 | 航标已恢复；不阻碍航线 | `MainCombat` | 同上 |

流程：

```text
Explore 选节点
  → 校验 CanEnter + 选战斗卡组
  → BattleRequest(encounterId = current BattleStage.enemyGroupId)
  → 普通/精英胜利 → currentStage++，按 idleUnlock 开放挂机
  → Boss 胜利 → BeaconRestored / FirstClear，解锁相邻航线与驻留挂机
  → 返回 Explore，刷新进度环与 CTA
```

`mainEncounterId` 在样板落地前仍可作为当前阶段遭遇的兼容读取；新内容以 `battleStages[]` 为准。星域首领区（边境锚点）仍是独立深层节点，不是外缘港的第 6 场。

### 4.7 遭遇绑定

每个区域配置：

```text
RegionConfig
- regionId
- sectorId
- sortOrder
- prereqRegionIds[]
- shipGate: { Level, Range, ... }
- mainEncounterId          // 兼容：当前阶段遭遇；新内容见 battleStages[]
- farmEncounterId?         // 驻留挂机遭遇；可与阶段遭遇不同
- battleStages[]           // 远征关卡；权威见 20 §17.9
- gatherNodeIds[]          // 阶段 2 即可启动，不必等航标恢复
- recommendedPower: int    // 仅提示；UI 应显示当前阶段推荐战力
- bossRegion: bool
```

遭遇内容本身见 `12-auto-battle.md` 的 `EncounterConfig`；本系统负责 **选哪场遭遇、是否允许开打**。

### 4.8 失败、回退与可见性

| 情况 | 处理 |
| --- | --- |
| 阶段战失败 | 不回退已完成阶段；圆点下方记失败标记；不降低舰船；可立即再战 |
| 前置未完成 | 区域可见，显示「需通关：xxx」 |
| 舰船不足 | 区域可见，逐条列出缺口（如 熵雾抗性 2/3） |
| 跳过未解锁区 | 不允许；不得用 GM 外的后门进战斗 |

### 4.9 与舰长等级 / 探索进度

| 字段 | 规则 |
| --- | --- |
| `PlayerEntity.level`（舰长等级） | 卡组槽、并行上限、离线 cap 等规模解锁；**不替代**舰船门，也 **不** 直接加全体角色攻击 |
| `explorationProgress` | 由已通关区域数 / 星域权重汇总刷新（Bridge 展示） |
| 星域完成 | 首领 `BossDefeated` → 标记星域首圈完成（新手两小时终点之一） |

---

## 5. 数据模型（实现契约）

### 5.1 ShipEntity

```text
ShipEntity
- shipId: string
- displayName: string
- level: int
- stats: { range, energy, hull, entropyResist, cargo, scan, lifeSupport }  // 缓存合计值
- modules: List<{ moduleId, level }>
- pendingUpgrade?: { target: shipLevel|moduleId, finishesAtUtc, paidMaterials }
```

权威存档：`saves/{playerId}/ship.json`。

### 5.2 RegionRuntimeState（每玩家）

```text
RegionRuntimeState
- regionId: string
- cleared: bool
- clearCount: int
- bossDefeated: bool
- firstClearedAtUtc?: long
- lastFarmAtUtc?: long
```

汇总：

```text
PlayerWorldState
- currentSectorId: string
- regions: List<RegionRuntimeState>
- explorationProgress: int    // 0–100 缓存
```

建议文件：`saves/{playerId}/world.json`。

### 5.3 静态配置

| 资源 | 内容 |
| --- | --- |
| `Regions.json` / SO | `RegionConfig` 列表 |
| `ShipGates` 并入 Region 或独立表 | 门槛数值 |
| `ShipLevelTable` | 升级费用、基础属性成长 |
| `ShipFacilities.json` | 设施效果 |
| `Encounters.json` | 由战斗系统共享 |

---

## 6. 服务接口（逻辑层）

```text
IWorldProgressService
- GetRegionView(regionId) -> UI 模型（锁态、缺口、推荐战力、是否可刷取）
- CanEnter(regionId, ship, world) -> (ok, reasons[])
- RegisterBattleVictory(regionId, encounterId, result) -> 更新通关/首领
- IsFarmUnlocked(regionId) -> bool

IShipService
- GetShip()
- MeetsGate(ship, gate) -> (ok, missing[])
- TryBeginUpgrade(levelOrFacility) -> result
- ApplyUpgradeComplete()
- GetEffectiveStats()  // 基础 + 设施
```

Explore 开战前必须调用 `CanEnter`；失败不得 `LoadScene("BattleScene")`。

---

## 7. UI / UX 要求

| 界面 | 要求 |
| --- | --- |
| Explore 列表 | 每区显示：名称、锁态、舰船门槛摘要、通关/可刷取标记 |
| 锁定区 | 按钮禁用或点按弹出缺口列表（前置 + 舰船分项） |
| 攻略中 | 主按钮「继续攻略 n/m」；阶段 ≥2 次按钮「部署挂机」；工具「调整舰队」 |
| 已通关（航标恢复） | 主按钮「驻留挂机」；收起逐关操作；可选「高难悬赏」 |
| 舰船面板 | 等级、分项、设施、下一解锁预览（指向未开区域） |
| Bridge | 邻近区域摘要不再写死 “Ship gate TBD”，改为真实门槛状态 |
| 首领区 | 独立视觉强调；击杀后星域完成庆祝（可简） |

语言：扩展 `UiText`，去掉占位 “Ship gate TBD”。

---


## 9. 设计验收标准

- 星域内存在 6 个配置区域（含 1 首领区），非全部开局可进入  
- 外缘带仅要求默认舰船即可挑战  
- 未通关前置时后区硬锁，并显示前置名  
- 舰船分项不足时硬锁，并逐条显示缺口  
- 战力不足不硬锁，仅提示推荐战力  
- 阶段 2 开放基础采集挂机，阶段 3 开放战斗挂机；航标恢复后合并为驻留挂机  
- 相邻区域硬锁看前置节点是否 **航标恢复**，不看第一场普通战斗  
- 首领击杀后标记星域首圈完成  
- 舰船升级主路径为资源/信用，而非纯等待  
- 进度与舰船进存档；读档后门锁一致  
- Explore 开战带 `regionId`/`encounterId`，不再只靠 `PlayerPrefs` 索引  

---

## 10. 开放钩子

- 第二星域与跨域航行（非 MVP）  
- 扫描揭示隐藏遭遇  
- 货舱不足时采集收益削减（采集文档）  
- 生命维持影响离线挂机效率与 **Yield Ratio**（离线文档）  
- 多艘舰船 / 舰队（非 MVP，主舰唯一）  

### 10.1 星域特殊玩法（非 MVP，方向锁定）

对应核心设计 §7.10；**不阻塞**首圈 6 区 + 首领。

| 模式 | 要点 |
| --- | --- |
| **虫洞** | 航网特殊边（稳定/单向/潮汐/漂移/位面/秘境，见 `20` §9）；发现后改变星图连接；**失败可回到跃迁原点**；推进模块门槛防过度跳关 |
| **宇宙暗面** | 已通关区的地狱难度变体；更高掉落；可限次 |
| **多元宇宙 Boss 房** | 一命肉鸽连续战；失败结束本轮；奖励宜绑定/限兑，降低对线上经济冲击 |

完整规则见 `20-stellar-map-and-navigation.md`（v0.2 航网主形态）。

若取消「舰船硬门」或改为纯等级门，需修订核心设计 §7.8 并升本文主版本。

---

## 11. 参考

- 核心设计：`02-core-product-design.md` §7.8 / §16.1  
- 设定：`03-worldbuilding.md`  
- 战斗：`12-auto-battle.md`（`BattleRequest.encounterId`、`BattleMode`）  
- 卡组：`11-deck-and-occupation.md`（舰船设施解锁钩子）  
- **节点远征**：`20-stellar-map-and-navigation.md` §17  
- **星域内容与掉落表**：`17-sector-and-onboarding.md`  
- 现有 UI：`ExploreScreen.cs`、`UiText.SectorName`、`PlanetEntity`、`PlayerEntity`  
- 开发计划：`10-mvp-development-plan.md` P2
