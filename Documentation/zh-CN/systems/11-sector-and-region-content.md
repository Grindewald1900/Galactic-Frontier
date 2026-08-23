# 系统文档：MVP 星域 / 区域内容与掉落

> 文档版本：v1.2  
> 状态：**P5.3 已落地（RewardCatalog / RewardService / 采集节点 / Explore 叙事）**  
> 上级约束：`Documentation/01-core-product-design.md` §7.8 / §16.1 / §20 / §22  
> 关联：`03-region-and-ship.md`（门与进度规则）、`04-idle-and-offline.md`（挂机周期/Pending）、`02-auto-battle.md`（遭遇开战）、`09-resources-and-warehouse.md`（物品 Id）、`06-durability-and-repair.md`（刷取耐久）、`00-setting-and-lore.md`（世界观与区域威胁—解法）  
> 实现权威：`RegionCatalog` / `EncounterCatalog` / `RewardCatalog` / `RewardService` / `GatherNodeCatalog` / `WorldService` / `IdleCombatTicker`  
> 更新日期：2026-08-23  
> 变更：v1.2 — 区域表增加 Gate / 主导敌方；对齐 `21` 舰队与探索度设定。

---

## 1. 目标与非目标

### 1.1 目标

锁定首发 **1 星域 × 6 区域** 的可玩内容契约：叙事定位、遭遇、舰船门、采集节点、**通关掉落**与 **挂机刷取奖励**，使战斗开图 → 舰船成长 → 资源三链形成闭环。

| 主题 | 本文件职责 |
| --- | --- |
| **星域（StarSector）** | 首发唯一星域元数据与完成条件 |
| **区域（Region）** | 6 区完整配置表（门、遭遇、采集、推荐战力） |
| **遭遇（Encounter）** | 主挑战 / 刷取 / Boss 敌编与难度阶梯 |
| **通关掉落** | 首次通关、重复主挑战、Boss 击杀奖励表 |
| **挂机奖励** | 每周期 `AutoCombat` 掉落与信用点 |
| **采集挂钩** | 区域解锁后的节点引用（产量见 `09`） |

### 1.2 非目标

- 双重门槛状态机与存档字段 → `03`
- 离线时长 / Yield Ratio / Pending 语义 → `04`
- 战斗回合规则与伤害公式 → `02`
- 物品定义与生产配方 → `09`
- 虫洞 / 宇宙暗面 / 多元宇宙 → `12`（后置）
- 第二星域、公会占星、阵营战争（非 MVP）

---

## 2. 术语

| 术语 | 定义 |
| --- | --- |
| **星域** | 内容容器；MVP 仅 `sector_frontier_vii` |
| **区域** | 星域内可探索单元；进度权威在 `world.json` |
| **主挑战遭遇** | `mainEncounterId`；用于首次/重复通关 |
| **刷取遭遇** | `farmEncounterId`；通关后 `AutoCombat` 使用 |
| **首次通关奖励（FirstClear）** | 该区 `cleared` 从 false→true 时发放一次 |
| **重复挑战奖励（RepeatClear）** | 已通关后再打主挑战胜利时的较弱奖励 |
| **挂机周期奖励（FarmCycle）** | 每次刷取结算胜利时的常规掉落 |
| **掉落条目（LootEntry）** | `{ itemDefId, qtyMin, qtyMax, quality, chance }`；`chance=1` 为保底 |

---

## 3. 硬约束

1. 首发 **1** 星域、**6** 区域、**1** 首领；区域 Id 与 `03` / `RegionCatalog` 对齐。  
2. 进入高级区必须满足前置通关 + 舰船门；战力不足不硬锁。  
3. 通关奖励与挂机奖励必须能支撑：**舰船升级消耗**、**三条生产链原料**、**维修包循环**。  
4. 首次通关奖励显著高于单次挂机；禁止挂机效率完全碾压开图动力。  
5. 掉落只用 `09` 已定义的 `itemDefId`（及信用点）；不发明未入库物品。  
6. 满仓时挂机/采集走 `PausedBlock` + Pending，不丢已结算产物（`04`）。

---

## 4. 星域（StarSector）

| 字段 | MVP 值 |
| --- | --- |
| `sectorId` | `sector_frontier_vii` |
| 显示名（中） | 群星边境 · 第七前沿 |
| 显示名（英） | Frontier Sector VII |
| 区域数 | 6 |
| 首领区 | `sec01_frontier_boss` |
| 首圈完成条件 | 首领 `BossDefeated` |
| 探索进度 | `floor(clearedRegions / 6 * 100)`；首领击杀计为通关 |
| 阵营标签 | 预留 `FactionA` / `FactionB`（遭遇可挂叙事标签，不阻塞逻辑） |
| 第二星域 | **不做** |
| 叙事归属 | 群星开拓局试验星域（见 `00-setting-and-lore.md`） |

叙事一句话：第七前沿是开拓局在断航带边缘钉下的试验星域；**每一区都有明确威胁，需要对的装备与编制才能钉稳航路钉子**（见 `00-setting-and-lore.md` §6）。

---

## 5. 区域总表（Region）

与 `RegionCatalog` / `03` §4.1–4.4 对齐；本表补齐**主题、威胁、制造解法、采集、掉落引用**。

| sort | regionId | 中文 | 英 | 主题 | Gate | 主导敌方 | 玩家可观察威胁 | 推荐制造/装备解法 | 前置 | 推荐战力 | Boss |
| ---: | --- | --- | --- | --- | :---: | --- | --- | --- | --- | ---: | --- |
| 0 | `sec01_outer_belt` | 外缘带 | Outer Belt | 新手 / 废料与铁矿 | | 佣兵/叛逃 | 巡逻穿甲；前排易崩 | 护甲板、维修包 | — | 80 | 否 |
| 1 | `sec01_mining_spur` | 矿脉支线 | Mining Spur | 金属+能源双采 | | 商盟武装 | 矿脉高热；持续灼损 | 冷却/耐热外勤装 | 外缘带 | 120 | 否 |
| 2 | `sec01_quantum_rift` | 量子裂隙 | Quantum Rift | 能源链；熵雾 | **是** | **熵雾教团** | 相位干扰；技能失手 | 扫描/稳定模块；相位核心 | 矿脉支线 | 200 | 否 |
| 3 | `sec01_abyssal_edge` | 深渊边界 | Abyssal Edge | 高压战斗 | **是** | **断航残舰** | 重甲封锁；维修压力 | 穿甲武器；航网残片 | 量子裂隙 | 280 | 否 |
| 4 | `sec01_convoy_lane` | 护航航道 | Convoy Lane | 后勤/信用 | | **异种群落** | 孢子污染；长期中毒 | 净化耗材、生命维持 | 深渊边界 | 320 | 否 |
| 5 | `sec01_frontier_boss` | 边境锚点 | Frontier Anchor | 星域首领 | Boss | 混合联军 | 复合威胁 | 多链综合构筑 | 护航航道 | 450 | **是** |

叙事与敌方 5 槽编制：`21-fleet-factions-and-exploration.md` §5；掉落与配方：`09`。

### 5.1 舰船门槛（复述，权威同 `03`）

| 区域 | Lv | Range | Energy | Hull | Entropy | Cargo | Scan | Life |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 外缘带 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 矿脉支线 | 1 | 1 | 0 | 0 | 0 | 1 | 0 | 0 |
| 量子裂隙 | 2 | 2 | 2 | 1 | 2 | 1 | 1 | 1 |
| 深渊边界 | 3 | 3 | 2 | 3 | 3 | 2 | 2 | 2 |
| 护航航道 | 3 | 4 | 3 | 3 | 2 | 3 | 2 | 2 |
| 边境锚点 | 4 | 4 | 4 | 4 | 4 | 3 | 3 | 3 |

### 5.2 遭遇与采集绑定

| regionId | mainEncounterId | farmEncounterId | gatherNodeIds |
| --- | --- | --- | --- |
| `sec01_outer_belt` | `enc_outer_main` | `enc_outer_farm` | `node_outer_iron` |
| `sec01_mining_spur` | `enc_mining_main` | `enc_mining_farm` | `node_spur_crystal`, `node_spur_fungal` |
| `sec01_quantum_rift` | `enc_rift_main` | `enc_rift_farm` | `node_rift_energy`（新增） |
| `sec01_abyssal_edge` | `enc_abyss_main` | `enc_abyss_farm` | `node_abyss_scrap`（新增） |
| `sec01_convoy_lane` | `enc_convoy_main` | `enc_convoy_farm` | `node_convoy_bio`（新增） |
| `sec01_frontier_boss` | `enc_frontier_boss` | `enc_frontier_farm` | —（Boss 区无采集；刷取仅战后） |

> 已实现节点见 `GatherNodeCatalog` / `09`；标注「新增」的节点为本内容契约要求，实现时应补入 Catalog。

### 5.3 新增采集节点（内容默认）

| nodeId | regionId | 产出 | qty | 品质 | 风险 | 说明 |
| --- | --- | --- | ---: | ---: | ---: | --- |
| `node_rift_energy` | `sec01_quantum_rift` | `mat_energy_cell` | 1 | Q2 | 2 | 能源链补给 |
| `node_abyss_scrap` | `sec01_abyssal_edge` | `mat_scrap` | 3 | Q2 | 3 | 高压废料场；耐久损耗偏高 |
| `node_convoy_bio` | `sec01_convoy_lane` | `mat_biofiber` | 2 | Q2 | 2 | 合成链后勤 |

周期秒数沿用 `EconomyConstants.GatherCycleSeconds`。

---

## 6. 遭遇（Encounter）内容

与 `EncounterCatalog` 对齐；扩展掉落引用（当前实现仅有 `lootScrap`，见 §11 差距）。

### 6.1 主挑战 / Boss

| encounterId | 显示名（英） | 建议中文 | Boss | 敌编（角色键×等级） | 建议敌方强度阶 |
| --- | --- | --- | :---: | --- | ---: |
| `enc_outer_main` | Outer Belt Patrol | 外缘巡逻队 | 否 | Asra1, Magki1 | 1 |
| `enc_mining_main` | Mining Spur Raiders | 矿脉劫掠者 | 否 | Magki2, Sernia2 | 2 |
| `enc_rift_main` | Quantum Rift Wardens | 裂隙看守 | 否 | Asra3, Magki3, Sernia3 | 3 |
| `enc_abyss_main` | Abyssal Edge Host | 深渊宿主 | 否 | Sernia4, Asra4, Magki4 | 4 |
| `enc_convoy_main` | Convoy Ambush | 护航伏击 | 否 | Magki5, Sernia5, Asra4 | 5 |
| `enc_frontier_boss` | Frontier Anchor Boss | 边境锚点首领 | **是** | Sernia6, Asra6, Magki6, Sernia5 | 6 |

### 6.2 挂机刷取遭遇（较弱）

| encounterId | 显示名（英） | 建议中文 | 敌编 | 相对主挑战 |
| --- | --- | --- | --- | --- |
| `enc_outer_farm` | Outer Belt Scraps | 外缘残骸清扫 | Asra1 | 明显更弱 |
| `enc_mining_farm` | Mining Spur Sweep | 矿脉清扫 | Magki2 | 较弱 |
| `enc_rift_farm` | Rift Echoes | 裂隙残响 | Asra3, Magki2 | 较弱 |
| `enc_abyss_farm` | Abyssal Drift | 深渊漂流体 | Sernia4, Asra3 | 较弱 |
| `enc_convoy_farm` | Convoy Escort Sweep | 护航清扫 | Magki4, Asra4 | 较弱 |
| `enc_frontier_farm` | Anchor Debris Field | 锚点残骸带 | Asra5, Magki5 | Boss 击杀后解锁 |

刷取解锁：普通区 `Cleared`；首领区需 `BossDefeated`（`03` §4.6）。

---

## 7. 掉落模型（实现契约）

### 7.1 条目与表

```text
LootEntry
- itemDefId: string          // 或特殊键 "credit"
- qtyMin: int
- qtyMax: int                // 含上下界；单值时 min=max
- quality: int               // 1–5；信用点忽略
- chance: float              // 0–1；1=必掉

RewardTable
- tableId: string
- guaranteed: LootEntry[]    // 先全部发放
- weighted: LootEntry[]      // 各独立 roll chance
```

```text
RegionRewardBinding
- regionId
- firstClearTableId
- repeatClearTableId
- farmCycleTableId           // AutoCombat 胜利每周期
```

遭遇可覆盖：`EncounterConfig.rewardTableId?`；默认回退到区域绑定表。

### 7.2 发放时机

| 事件 | 使用表 | 次数 |
| --- | --- | --- |
| 主挑战胜利且首次通关 | `firstClear` | 每区 **1** 次 |
| 主挑战胜利且已通关 | `repeatClear` | 每次胜利 |
| Boss 击杀（首次） | `firstClear`（首领表） | **1** 次；并标记星域完成 |
| `AutoCombat` 周期胜利 | `farmCycle` | 每成功周期 |
| `AutoCombat` 周期失败 | 无掉落 | 不停止挂机（`04`） |
| 采集周期 | 节点产量（非本表） | 见 `09` |

在线默认直接入仓；离线进 Pending（`04` §4.7）。

### 7.3 平衡原则（MVP）

| 原则 | 默认 |
| --- | --- |
| 首次通关价值 | ≈ **8–15** 次同区挂机周期期望 |
| 挂机主产出 | 废料 + 该区主题材料；信用点为辅 |
| 装备掉落 | 仅首次通关/Boss 高概率或保底；挂机 **低概率**（≤5%）防通胀 |
| 品质 | 材料默认 Q2；装备首次通关 Q2–Q3；Boss 成品可 Q3 |
| 与舰船升级 | 每通一区，首次奖励应覆盖升到**下一区门槛**所需废料/信用的大部分（约 60–100%） |

---

## 8. 通关掉落表（FirstClear / RepeatClear）

数量为可调默认；语义锁定「每区主题 + 推进舰船」。

### 8.1 外缘带 `sec01_outer_belt`

**首次通关 `reward_outer_first`**

| 类型 | itemDefId / credit | qty | quality | chance |
| --- | --- | ---: | ---: | ---: |
| 保底 | `credit` | 40 | — | 1 |
| 保底 | `mat_scrap` | 12 | Q2 | 1 |
| 保底 | `mat_iron_ore` | 8 | Q2 | 1 |
| 保底 | `mat_repair_parts` | 2 | Q2 | 1 |
| 概率 | `mat_alloy_plate` | 1 | Q2 | 0.35 |

**重复主挑战 `reward_outer_repeat`**：`credit` 8 + `mat_scrap` 3（Q2）。

### 8.2 矿脉支线 `sec01_mining_spur`

**首次 `reward_mining_first`**

| 类型 | Id | qty | Q | chance |
| --- | --- | ---: | --- | ---: |
| 保底 | `credit` | 60 | — | 1 |
| 保底 | `mat_scrap` | 10 | Q2 | 1 |
| 保底 | `mat_crystal_sand` | 8 | Q2 | 1 |
| 保底 | `mat_fungal` | 6 | Q2 | 1 |
| 保底 | `mat_alloy_plate` | 2 | Q2 | 1 |
| 概率 | `mat_energy_cell` | 1 | Q2 | 0.40 |

**重复**：`credit` 12 + `mat_crystal_sand` 2 + `mat_fungal` 2。

### 8.3 量子裂隙 `sec01_quantum_rift`

**首次 `reward_rift_first`**

| 类型 | Id | qty | Q | chance |
| --- | --- | ---: | --- | ---: |
| 保底 | `credit` | 90 | — | 1 |
| 保底 | `mat_energy_cell` | 6 | Q2 | 1 |
| 保底 | `mat_crystal_sand` | 10 | Q2 | 1 |
| 保底 | `mat_scrap` | 14 | Q2 | 1 |
| 保底 | `int_charged_core` | 1 | Q2 | 1 |
| 概率 | `eq_energy_pack` | 1 | Q2 | 0.25 |

**重复**：`credit` 16 + `mat_energy_cell` 2 + `mat_scrap` 4。

### 8.4 深渊边界 `sec01_abyssal_edge`

**首次 `reward_abyss_first`**

| 类型 | Id | qty | Q | chance |
| --- | --- | ---: | --- | ---: |
| 保底 | `credit` | 120 | — | 1 |
| 保底 | `mat_scrap` | 18 | Q2 | 1 |
| 保底 | `mat_repair_parts` | 5 | Q2 | 1 |
| 保底 | `con_repair_kit` | 2 | Q2 | 1 |
| 保底 | `int_armor_frame` | 1 | Q2 | 1 |
| 概率 | `eq_shield_vest` | 1 | Q3 | 0.30 |

**重复**：`credit` 20 + `mat_repair_parts` 2 + `mat_scrap` 5。

### 8.5 护航航道 `sec01_convoy_lane`

**首次 `reward_convoy_first`**

| 类型 | Id | qty | Q | chance |
| --- | --- | ---: | --- | ---: |
| 保底 | `credit` | 150 | — | 1 |
| 保底 | `mat_biofiber` | 8 | Q2 | 1 |
| 保底 | `mat_alloy_plate` | 4 | Q2 | 1 |
| 保底 | `int_synth_mesh` | 1 | Q2 | 1 |
| 保底 | `con_repair_kit` | 2 | Q2 | 1 |
| 概率 | `eq_gather_drill` | 1 | Q2 | 0.35 |

**重复**：`credit` 24 + `mat_biofiber` 3 + `mat_alloy_plate` 1。

### 8.6 边境锚点（首领）`sec01_frontier_boss`

**首次击杀 `reward_boss_first`（星域首圈大奖）**

| 类型 | Id | qty | Q | chance |
| --- | --- | ---: | --- | ---: |
| 保底 | `credit` | 300 | — | 1 |
| 保底 | `mat_scrap` | 40 | Q2 | 1 |
| 保底 | `mat_alloy_plate` | 8 | Q2 | 1 |
| 保底 | `mat_energy_cell` | 8 | Q2 | 1 |
| 保底 | `mat_biofiber` | 8 | Q2 | 1 |
| 保底 | `con_repair_kit` | 5 | Q2 | 1 |
| 保底 | `eq_pulse_rifle` **或** `eq_composite_armor` | 1 | Q3 | 1（二选一：战力低给枪，否则甲；或固定步枪） |
| 保底 | `mod_cargo`（模块物品） | 1 | Q3 | 1 |
| 概率 | `eq_synth_cloak` | 1 | Q3 | 0.40 |

**Boss 无「重复首次」**；重复挑战首领用较弱表 `reward_boss_repeat`：`credit` 40 + `mat_scrap` 10 + 各主题材料×2。

---

## 9. 挂机刷取奖励（FarmCycle）

### 9.1 时间与强度

| 项目 | 内容默认 | 说明 |
| --- | --- | --- |
| 周期 | 设计目标 **90s**（`04`）；原型可 30s | `WorldConstants.FarmCycleSeconds` |
| 胜→发奖 / 负→无奖 | 是 | 不自动停挂机 |
| 离线 | 同表 × Yield Ratio | `04` |
| 耐久 | 每胜场按 `06` 扣装备；废料不足以支撑无限无修刷取 | |

### 9.2 分区周期表

每行 = 单次周期胜利期望骨架（保底 + 独立概率）。

#### 外缘 `reward_outer_farm`

| Id | qty | Q | chance |
| --- | ---: | --- | ---: |
| `mat_scrap` | 2 | Q2 | 1 |
| `credit` | 3 | — | 1 |
| `mat_iron_ore` | 1 | Q2 | 0.45 |

#### 矿脉 `reward_mining_farm`

| Id | qty | Q | chance |
| --- | ---: | --- | ---: |
| `mat_scrap` | 2 | Q2 | 1 |
| `credit` | 4 | — | 1 |
| `mat_crystal_sand` | 1 | Q2 | 0.50 |
| `mat_fungal` | 1 | Q2 | 0.35 |
| `mat_alloy_plate` | 1 | Q2 | 0.12 |

#### 裂隙 `reward_rift_farm`

| Id | qty | Q | chance |
| --- | ---: | --- | ---: |
| `mat_scrap` | 3 | Q2 | 1 |
| `credit` | 5 | — | 1 |
| `mat_energy_cell` | 1 | Q2 | 0.40 |
| `mat_crystal_sand` | 1 | Q2 | 0.35 |
| `int_charged_core` | 1 | Q2 | 0.08 |

#### 深渊 `reward_abyss_farm`

| Id | qty | Q | chance |
| --- | ---: | --- | ---: |
| `mat_scrap` | 3 | Q2 | 1 |
| `credit` | 6 | — | 1 |
| `mat_repair_parts` | 1 | Q2 | 0.40 |
| `con_repair_kit` | 1 | Q2 | 0.10 |
| `mat_alloy_plate` | 1 | Q2 | 0.18 |

#### 护航 `reward_convoy_farm`

| Id | qty | Q | chance |
| --- | ---: | --- | ---: |
| `mat_scrap` | 3 | Q2 | 1 |
| `credit` | 8 | — | 1 |
| `mat_biofiber` | 1 | Q2 | 0.45 |
| `mat_alloy_plate` | 1 | Q2 | 0.20 |
| `con_field_ration` | 1 | Q2 | 0.15 |

#### 锚点刷取 `reward_frontier_farm`（Boss 死后）

| Id | qty | Q | chance |
| --- | ---: | --- | ---: |
| `mat_scrap` | 4 | Q2 | 1 |
| `credit` | 10 | — | 1 |
| `mat_energy_cell` | 1 | Q2 | 0.30 |
| `mat_biofiber` | 1 | Q2 | 0.30 |
| `mat_alloy_plate` | 1 | Q2 | 0.25 |
| `eq_scope` | 1 | Q2 | 0.03 |

### 9.3 与现网 `lootScrap` 的兼容

当前 `EncounterConfig.lootScrap` 仅表示废料保底数量。迁移映射：

```text
farmCycle.guaranteed.mat_scrap.qty  ≈  encounter.lootScrap
（外缘 2、矿脉 2、裂隙 3… 见 EncounterCatalog 现有值，可上调至本表）
```

完整多物品表落地后，`lootScrap` 降为调试回退字段。

---

## 10. 区域内容卡片（设计摘要）

### 10.1 外缘带 — 新手圈

- **玩法**：无舰船门压力；教会主挑战 → 通关 → 挂机 / 采集铁矿。  
- **经济**：废料喂舰船 Lv1→门槛；铁矿启动 `chain_metal`。  
- **一句话**：许可证后的第一站——清理航道残骸，证明你能活下来。

### 10.2 矿脉支线 — 双原料

- **门槛**：Range1 + Cargo1（推货舱/推进）。  
- **经济**：晶砂 + 菌毯，同时喂能源链与合成链入口。  
- **挂机**：材料多样性最高的前中期区。  
- **一句话**：失联矿带，回收航材与双链原料。

### 10.3 量子裂隙 — 能源压力

- **门槛**：Energy / Entropy 抬升，强迫升 `mod_reactor` / `mod_entropy`。  
- **经济**：能量芯与充能核心；耐久开始有感。  
- **叙事**：熵雾渗漏严重的裂隙，需能源与抗性才能深入。

### 10.4 深渊边界 — 维修坑

- **门槛**：Hull / Entropy 高压。  
- **经济**：维修零件与维修包；逼出 `rcp_make_repair_kit` 与装甲工坊。  
- **挂机**：高产出伴随高耐久消耗（`06`）。  
- **叙事**：灾变残影与高压战场，维修成为日常。

### 10.5 护航航道 — 后勤冲刺

- **门槛**：Range / Cargo 再抬；信用点奖励偏高。  
- **经济**：生物纤维与合金板，冲刺合成/金属成品。  
- **叙事**：尝试恢复后勤线，为总攻边境锚点集结补给。

### 10.6 边境锚点 — 首领

- **门槛**：全面 Lv4 档。  
- **首次击杀**：星域完成 + 成品装备/模块大奖。  
- **刷取**：残骸带，综合材料与极低概率饰品。  
- **叙事**：本星域威胁核心；清除后第七前沿首圈宣告收复。

---

## 11. 推进节奏与期望产出（设计用）

假设挂机周期按 **90s**、在线满效率、通关后立即刷取：

| 阶段 | 目标玩家动作 | 资源侧期望 |
| --- | --- | --- |
| 开局→外缘通关 | 主挑战 1–3 次 | 首次奖励够升 Range/Cargo 摸矿脉 |
| 矿脉通关 | 采集双节点 + 短挂机 | 攒齐精炼锭 / 充能核心配方材料 |
| 裂隙–深渊 | 升反应堆/装甲/熵抗 | 维修包成为刚需 |
| 护航→首领 | 三链出一件成品 | Boss 奖巩固战力与货舱 |

探索进度 UI：每通一区约 +16–17%；Boss 击杀 → 100% 并触发「第七前沿首圈完成」。

---

## 12. 数据模型扩展

```text
RegionConfig  (现有字段 +)
- gatherNodeIds: string[]
- firstClearRewardId: string
- repeatClearRewardId: string
- farmRewardId: string

EncounterConfig  (现有 +)
- displayNameZh?: string
- rewardTableId?: string     // 可选覆盖
- lootScrap                  // 兼容回退

RewardTable / LootEntry      // 新建 Catalog：RewardCatalog
```

发放服务建议：`RewardService.Grant(tableId, dest: Warehouse|Pending)`；由 `RegisterBattleVictory`（首次/重复）与 `IdleEconomyTicker.TickFarm`（挂机）调用。

---

## 13. 与实现差距

| 现状（v1.1） | 备注 |
| --- | --- |
| `RewardService` 按 FirstClear/Repeat/Farm 表发放 | 满仓进 Pending |
| 挂机使用 `farmRewardId` 多物品 + 信用点 | `lootScrap` 仅作表缺失回退 |
| `RegionConfig` 含 gather / reward / blurb / faction | 已对齐 |
| 采集节点含裂隙/深渊/护航 | `GatherNodeCatalog` |
| 装备/模块可出现在 FirstClear/Boss 表 | 走 `ItemFactory` |

## 14. 验收清单

- [x] 6 区配置齐全；外缘可打，后区前置+舰船硬锁  
- [x] 每区主挑战与刷取遭遇可开战；Boss 区 `isBoss`  
- [x] 每区首次通关发放对应 FirstClear 表，且只发一次  
- [x] 通关后挂机使用 farm 表；含废料以外的主题材料  
- [x] Boss 首次击杀发大奖并标记星域完成  
- [x] 挂机失败不停队列；满仓 Pending/PausedBlock  
- [x] 掉落 Id 均存在于 `ItemCatalog`；信用点入账 `CurrencyService`  
- [x] 新采集节点在对应区通关后可启动 

---

## 15. 开放钩子

- 阵营标签影响掉落权重（`FactionA/B`）  
- 稀有挂机日（限时双倍）  
- 扫描模块揭示隐藏遭遇  
- 第二星域与跨域航行  
- 暗面/虫洞掉落倍率（`12`）  

---

## 16. 参考

- 规则：`03-region-and-ship.md`、`04-idle-and-offline.md`、`02-auto-battle.md`  
- 物品：`09-resources-and-warehouse.md`  
- 代码：`RegionCatalog.cs`、`EncounterCatalog.cs`、`GatherNodeCatalog.cs`、`WorldService.cs`、`IdleEconomyTicker.cs`  
- 开发计划：`../11-mvp-development-plan.md` P2 / P5
