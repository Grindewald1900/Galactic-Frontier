# 系统文档：生产链与品质

> 文档版本：v1.1
> 文档类型：**规则**
> **实现与验收状态**见 [PRODUCT-STATUS.md](PRODUCT-STATUS.md)；本文仅描述规则与设计标准。

> 上级约束：`02-core-product-design.md` §7.9 / §16.1 / §20 / §21 / §22  
> 关联：`14-deck-and-occupation.md`（`Produce` 占用）、`17-idle-and-offline.md`（周期/缺料暂停）、`19-durability-and-repair.md`（装备耐久上限受品质影响）、`20-market-and-card-trade.md`（品质影响挂单价与堆叠）、`22-resources-and-warehouse.md`（配方与流水线内容表）  
> 更新日期：2026-08-23  
> 变更：v1.1 — **手动工坊 → 自动化流水线** 设定；制造非瞬间完成；流水线专精/升级/启用规则（§4.10）。

---

## 1. 目标与非目标

### 1.1 目标

打通「采集 → 加工 → 成品」的本地可验证生产循环，并让**品质**成为可培养、可预期的结果，而非纯赌运气：

- MVP 至少 **3 条完整生产链**（原料→中间→成品）；
- **15–20 种资源**可堆叠入库；
- **10–15 件装备/模块**可制造并带品质；
- 玩家能通过技能、设施、材料与保底手段**抬高最低品质**；
- 品质影响属性、耐久上限、维修成本与市场价值。

### 1.2 非目标

- 离线时长与 `PausedBlock` 时钟语义 → `17-idle-and-offline.md`
- 耐久损耗与维修扣料公式 → `19-durability-and-repair.md`
- 市场撮合与卡牌上架 → `20-market-and-card-trade.md`
- 装备强化失败、永久损毁、自由星球建造（核心设计明确排除）
- 复杂跨区物流损耗（非 MVP）

---

## 2. 术语

| 术语 | 定义 |
| --- | --- |
| **资源（Resource）** | 可堆叠材料/中间件/消耗品，以 `itemDefId + quality` 为堆叠键（见 §4.4） |
| **配方（Recipe）** | 输入材料、产出、工时、设施要求、品质权重的静态配置 |
| **生产链（Chain）** | 至少两步配方串联：原料 → 中间 → 成品（或模块） |
| **加工（Process）** | 材料→材料的中间步骤；行动类型可用 `Process` |
| **制造（Manufacture）** | 产出装备/模块/高阶消耗品；行动类型可用 `Manufacture` |
| **品质（Quality）** | 离散档位；影响属性与价值，见 §4.3 |
| **设施（Facility）** | 舰船工坊等级；提供速度/品质加成与配方解锁 |
| **熟练度（Mastery）** | 每配方累计成功次数带来的品质/速度小幅加成 |
| **批次（Batch）** | 一次完整周期产出 1 件（或配置的 `outputQty`）成品 |
| **手动制造（Manual Craft）** | 玩家指派 `Produce` 卡组执行配方；占用人手；有完整周期 |
| **自动化流水线（Production Line）** | 港口/前哨安装的固定产线；**不占**卡组；玩家仅启用/停用与升级 |
| **流水线品类（Line Family）** | 产线可制造的**产品族**；同类近亲产物共用一条线（如冶炼线出铜锭/铁锭） |
| **流水线设计图（Line Blueprint）** | 建造或解锁某品类流水线的静态配置 |

MVP 实现可将 `Process` 与 `Manufacture` 合并为卡组用途 `Produce`，但配方表仍区分 `recipeKind`。

---

## 3. 硬约束（不可违背）

1. 生产行动必须由**独立卡组**执行，遵守占用与并行上限。  
2. **生产品质必须影响制造结果与市场价值**（§22）。  
3. 品质不得完全由极低概率决定；须提供抬高最低品质的手段。  
4. 停止生产：占用立即解除；**已扣材料默认不返还**，未完成批次无产出（与占用文档一致，本文确认）。  
5. 材料不足或仓库满 → `PausedBlock`，不惩罚已产出。  
6. 首发循环可单人完成，不依赖公会订单。

---

## 4. MVP 拍板决议

> 关闭核心设计 §21「生产品质」待定项；数值进配置表，语义按本表。

### 4.1 资源表规模与分类

| 项目 | MVP 默认 |
| --- | --- |
| 资源种数 | **18**（落在 15–20） |
| 装备/模块种数 | **12**（落在 10–15） |
| 物品大类 | `Material` / `Intermediate` / `Consumable` / `Equipment` / `ShipModule` |
| 堆叠 | 材料与中间件可堆叠；装备/模块默认 **不可堆叠**（每件独立实例，含耐久） |
| 权威 Id | `itemDefId`（静态定义）+ 实例 `itemInstanceId`（装备） |

#### 示例资源分布（可改名，保留结构）

| 链 | 原料 | 中间 | 成品 |
| --- | --- | --- | --- |
| 金属链 | 铁矿石、废合金 | 精炼锭、合金板 | 舰装护甲板、近战武器 |
| 能源链 | 结晶砂、冷却液 | 能芯单元 | 能量模块、护盾电容 |
| 生物/合成链 | 菌毯、溶剂 | 合成纤维 | 维修包、外勤夹克 |

采集节点产出原料；加工/制造消耗并转化。具体掉落量进节点表。

### 4.2 三条完整生产链（验收最小集）

每条链至少配置：

1. 采集或战斗可得的 **原料**；  
2. 一步 **加工** 配方（原料→中间）；  
3. 一步 **制造** 配方（中间→装备或模块）；  
4. 成品可装备或可消耗，并进入维修/市场循环。

| ChainId | 名称 | 最小路径 |
| --- | --- | --- |
| `chain_metal` | 金属工坊 | 铁矿石 → 精炼锭 → 合金板 → 护甲板 |
| `chain_energy` | 能源工坊 | 结晶砂 → 能芯单元 → 能量模块 |
| `chain_synth` | 合成工坊 | 菌毯+溶剂 → 合成纤维 → 维修包 / 外勤夹克 |

设施挂钩（与舰船文档设施表对齐，可扩展）：

| 设施 | 解锁/加成 |
| --- | --- |
| 装甲工坊 | 金属链速度/品质；降低战斗耐久损耗（耐久文档） |
| 能源车间 | 能源链 |
| 合成舱 | 合成链；维修包产出 |

### 4.3 品质档位

| 档位 Id | 显示名（中/英） | 排序 | MVP |
| --- | --- | --- | --- |
| `Q1` | 粗制 / Crude | 1 | 是 |
| `Q2` | 标准 / Standard | 2 | 是 |
| `Q3` | 精良 / Fine | 3 | 是 |
| `Q4` | 卓越 / Superior | 4 | 是 |
| `Q5` | 杰作 / Masterwork | 5 | 是（低概率+培养后可达） |

**不做**第六档神话级（避免 MVP 词条爆炸）。

#### 品质效果乘数（相对 Q2=1.0 的默认语义）

| 档位 | 主属性 | 耐久上限 | 工作效率* | 市场基准价 | 维修成本 |
| --- | --- | --- | --- | --- | --- |
| Q1 | 0.85 | 0.90 | 0.90 | 0.70 | 0.85 |
| Q2 | 1.00 | 1.00 | 1.00 | 1.00 | 1.00 |
| Q3 | 1.12 | 1.10 | 1.08 | 1.35 | 1.10 |
| Q4 | 1.25 | 1.20 | 1.15 | 1.80 | 1.25 |
| Q5 | 1.40 | 1.35 | 1.25 | 2.50 | 1.45 |

\*工作效率：工具/模块对采集或生产速度的加成；纯战斗装备可忽略该列。

附加效果（套装词条）MVP **可选**：Q4+ 才允许 1 条固定小词条；禁止每件随机稀有池导致市场 SKU 爆炸。

### 4.4 堆叠与市场展示键

| 物品类型 | 堆叠键 | 说明 |
| --- | --- | --- |
| 材料 / 中间件 / 消耗品 | `(itemDefId, quality)` | 同定义同品质合并数量 |
| 装备 / 模块 | 不堆叠 | 每件 `itemInstanceId`；市场按「定义+品质」聚合盘口，成交交割具体实例 |

低品质与高品质**不可**合并堆叠，避免「平均品质」歧义。

### 4.5 品质生成公式（可预期）

制造完成时：

```text
score = craftSkill
      + facilityBonus
      + deckWorkPower          // 卡组成员工程/生产属性平均或总和，配置选择
      + materialQualityBonus   // 输入材料品质贡献
      + masteryBonus
      + guaranteeBonus         // 保底/加料选项
      + noise                  // 小幅随机，默认 ±5 分，须可播种

quality = ScoreToQuality(score, recipe.thresholds)
quality = max(quality, recipe.minQualityFloor + playerFloorBonus)
quality = min(quality, recipe.maxQualityCap)
```

| 决议 | MVP |
| --- | --- |
| 随机性 | **有**，但权重低于培养项；同种子可复现（离线批量用 `hash(runSeed, batchIndex)`） |
| 最低品质地板 | 设施等级、熟练度、角色技能可提高 `minQualityFloor` |
| 保底制造 | 见 §4.6 |
| 材料品质 | 输入中**最低**材料品质提供地板；平均品质提供 `materialQualityBonus` |

`ScoreToQuality` 阈值示例（可配置）：

| 最低 score | 品质 |
| --- | --- |
| 0 | Q1 |
| 30 | Q2 |
| 55 | Q3 |
| 75 | Q4 |
| 95 | Q5 |

### 4.6 保底、加料与再加工

| 手段 | 规则 |
| --- | --- |
| **保底档** | 配方可选 `guaranteeQuality`：额外消耗 `guaranteeExtraMaterials`（默认 +50% 主材料），使 `quality = max(rolled, guaranteeQuality)` |
| **高级设施稳定** | 设施 ≥ 阈值时，`noise` 减半且 `minQualityFloor` +1 档（不超过 Q4） |
| **拆解** | Q1–Q3 成品可拆解：返还主材料的 `salvageRatio`（默认 **40%**，按数量向下取整）；不返还保证加料 |
| **再加工** | 消耗「再加工试剂」+ 同定义低品质装备，重新走一次制造 roll，**可能升一档或不变**，不降档；每件生命周期最多再加工 `maxReforge`（默认 **1**） |

### 4.7 生产行动与扣料时机

| 项目 | MVP 规则 |
| --- | --- |
| 启动 | `TryStart(Process|Manufacture, recipeId)`；校验设施等级、材料、并行与占用 |
| 扣料 | **启动时预扣**本批次全部材料（含保底加料） |
| 周期 | `cycleSeconds = recipe.baseSeconds / speedMultiplier` |
| 速度乘区 | 角色生产属性、设施、不满编效率（默认线性：`members/5`，下限 0.4） |
| 完成 | 产出写入仓库或 Pending；`mastery++`；若队列模式则尝试扣下一批材料 |
| 材料不足 | `PausedBlock`；已完成批次保留 |
| 仓库满 | `PausedBlock`；产出进 Pending（与离线文档一致） |
| **主动停止** | 占用立即解除；**已扣材料不返还**；当前批次产出 **0**；已完成批次不受影响 |

队列：MVP 允许「单卡组单配方重复批次」；不强制多配方队列 UI。

> **制造非瞬间完成**：无论手动或流水线，产出均须走完 `baseSeconds` 周期（受速度乘区影响）。不存在「点击即得」的制造（调试/任务特例除外）。

### 4.8 采集产量（本文件补齐节点侧）

离线文档已定周期语义；此处定产量：

```text
yield = floor(node.baseYieldPerCycle
        * deckGatherMultiplier
        * facilityGatherBonus
        * qualityToolBonus)   // 采集工具品质
```

| 项目 | 默认 |
| --- | --- |
| `deckGatherMultiplier` | `1 + 0.05 * sum(gatherStat)` 或配置表，须可调 |
| 节点品质 | 原料默认产出 **Q2**；稀有节点可加权出 Q3 |
| 高风险节点 | 额外耐久损耗（耐久文档）；产量更高 |

### 4.9 配方熟练度

| 项目 | MVP |
| --- | --- |
| 计数 | 每成功完成 1 批次 +1 |
| 效果 | 每 N 次（默认 10）+1 分 `masteryBonus`，软顶 +15 分 |
| 存档 | `recipeMastery[recipeId] = int` |

### 4.10 手动工坊与自动化流水线

#### 4.10.1 设计原则

| 原则 | 定稿 |
| --- | --- |
| **非瞬间** | 所有制造/加工均有周期；材料在启动时预扣（手动与流水线一致，见 §4.7） |
| **早期手动** | 开局仅 **手动工坊**：须指派 `Produce` 卡组，占用人手完成批次 |
| **后期自动化** | 消耗资源、矿产、**流水线设计图** 建造固定产线；玩家**不逐批手操** |
| **玩家管理面** | 对每条流水线：**启用 / 停用**、选择当前配方（须在品类允许范围内）、**升级产线等级** |
| **专精** | 一条流水线只服务**特定产品族**；近亲产物共用同线，高级产物须**升级**或**另建高级线** |

叙事见 `03-setting-and-lore.md` §4.4；内容表示例见 `09` §9。

#### 4.10.2 手动制造（Manual Craft）

| 项 | 规则 |
| --- | --- |
| 入口 | Crafting 屏 → 选配方 → `TryStart(Process\|Manufacture)` |
| 占用 | **占用**绑定 `Produce` 卡组全体成员（`01`） |
| 适用 | 新手教学、首配方解锁、小批量急单、尚未建线的配方 |
| 周期 | 与 §4.7 相同；角色工程属性、设施等级参与速度/品质 |
| 停止 | 占用立即解除；已扣料不退；当前批次无产出 |

MVP **仅实现**本形态；自动化为后 MVP 切片。

#### 4.10.3 自动化流水线（Automated Production Line）

| 项 | 定稿 |
| --- | --- |
| 建造 | `LineBlueprint` + 材料 + 矿产 + 港口工位（或 Cleared 星球工业湾） |
| 运行 | **不占用**卡组；后台按周期扣料产出 |
| 玩家操作 | ① 启用/停用 ② 在允许配方列表中选 **当前生产项** ③ 升级 `lineTier` |
| 暂停 | 缺料 / 仓库满 → `PausedBlock`（与 `04` 一致）；启用状态保留 |
| 离线 | 已启用且未 Paused 的流水线参与离线结算 |
| 品质 | 与手动共用 `QualityRules`；流水线 **无** 角色工程加成，靠产线等级 + 设施 + 材料 |

停用流水线：当前周期完成后停止下一批；**不**自动退还已预扣的本批材料（与手动停止语义对齐）。

#### 4.10.4 流水线品类与配方归属

每条流水线绑定一个 **`lineFamilyId`**。配方声明 `lineFamilyId` + `requiredLineTier`（或 `requiredLineDefId` 用于专用高级线）。

**同类近亲产物共用一条线** — 通过配方标签 `recipeTag` 归入同一品类：

| lineFamilyId | 中文 | 可共线产物示例 | 备注 |
| --- | --- | --- | --- |
| `line_smelting` | 冶炼流水线 | 铜锭、铁锭、精炼锭 | 输入矿石不同，同一冶炼周期模板 |
| `line_alloying` | 合金流水线 | 合金板、复合装甲坯 | 需中间锭 |
| `line_energy` | 能芯流水线 | 能芯单元、护盾电容芯 | 能源链 Process |
| `line_synth` | 合成流水线 | 合成纤维、溶剂精制 | 生物/化学中间件 |
| `line_weapon` | 武备流水线 | 脉冲步枪、穿甲刃 | Manufacture 武器族 |
| `line_module` | 模块装配线 | 装甲/反应堆/货仓模块物品 | 与 `mod_*` 设施成长并行 |

规则：

1. 启动配方时校验：`recipe.lineFamilyId == line.lineFamilyId` 且 `line.tier >= recipe.requiredLineTier`；  
2. **禁止**一条通用线生产所有物品；跨链成品须多条线并行；  
3. 玩家在同一冶炼线上切换「铜锭 / 铁锭」= 改 **当前配方**，非换线。

#### 4.10.5 升级产线 vs 建造高级专用线

| 路径 | 适用 | 说明 |
| --- | --- | --- |
| **升级现有线** | 同族 **中阶** 产物 | `line_smelting` T1→T2 解锁精炼锭；T2→T3 解锁合金 precursor |
| **新建高级专用线** | **高阶 / 机制** 产物 | 须独立 `LineBlueprint`，如 `line_phase_forge`（相位锻炉）、`line_entropy_purifier`（熵雾净化线） |
| 图纸来源 | — | 卡关稀有、秘境、NPC、阵营商店、Online 市场 |

示例（第七前沿）：

| 产物 | 最低要求 |
| --- | --- |
| 铁锭 / 铜锭 | `line_smelting` T1 |
| 精炼锭 | `line_smelting` T2 **或** 升级 T1→T2 |
| 复合装甲 | `line_alloying` T2 + `line_weapon` T1（多线协作） |
| 相位稳定模块 | **`line_phase_forge` T1**（专用蓝图；普通 `line_module` 不可代做） |

#### 4.10.6 与舰船设施模块的关系

| 层级 | 职责 |
| --- | --- |
| **舰船模块**（`mod_armor` 等） | 解锁手动配方、提供全局速度/品质加成；**不替代**流水线实体 |
| **流水线实例** | 独立槽位（港口 `dockWorkshopSlots`）；一条实例 = 一个品类 |
| **并行** | 多线可同时 Running；受工位数量与电力/维护（可选后 MVP）限制 |

手动制造仍受益于设施等级；自动化额外受益于 **`lineTier`**（每级 +速度、+品质地板，数值进配置表）。

#### 4.10.7 数据模型（流水线摘要）

```text
LineBlueprintDef
- lineDefId: string
- lineFamilyId: string
- displayNameKey: string
- tierCap: int
- buildCost: [{ itemDefId, qty }]
- buildSeconds: int
- allowedRecipeTags: string[]

ProductionLineInstance
- lineInstanceId: string
- lineDefId: string
- tier: int
- enabled: bool
- activeRecipeId: string | null
- state: Idle | Running | PausedBlock
- remainderSeconds: float
- locationId: string

RecipeDef（扩展字段，与 §5.2 合并）
- lineFamilyId?: string
- requiredLineTier: int
- requiredLineDefId?: string
```

#### 4.10.8 MVP 与迁移

| 阶段 | 范围 |
| --- | --- |
| **MVP（现状）** | 仅 **手动** `Produce` 卡组；周期制造；设施模块加成 |
| **MVP+** | 港口 1 条冶炼线 T1；启用/停用 + 选铜/铁锭 |
| **后 MVP** | 多线并行、星球工业湾、专用高级线、离线批量 |

实现时新增 `ProductionLineService`，与 `ProductionService` 共用扣料/品质/Pending 逻辑。

---

## 5. 数据模型（实现契约）

### 5.1 ItemDef / 实例

```text
ItemDef
- itemDefId: string
- displayNameKey: string
- category: Material | Intermediate | Consumable | Equipment | ShipModule
- maxStack: int                 // 装备=1
- baseValue: int                // 市场基准（Q2）
- equipSlot?: string
- baseStats?: {}
- baseMaxDurability?: int
- repairMaterialId?: string
- tags[]

ItemInstance                    // 仅非堆叠
- itemInstanceId: string
- itemDefId: string
- quality: QualityTier
- durability: int
- maxDurability: int            // 受品质乘数
- reforgeCount: int
- bound: bool                   // 市场文档
```

堆叠仓位：

```text
ItemStack
- itemDefId: string
- quality: QualityTier
- quantity: int
```

### 5.2 RecipeDef

```text
RecipeDef
- recipeId: string
- chainId: string
- recipeKind: Process | Manufacture
- inputs: [{ itemDefId, quantity, minQuality? }]
- output: { itemDefId, quantity }
- baseSeconds: int
- requiredFacilityId?: string
- requiredFacilityLevel: int
- scoreThresholds: { q2,q3,q4,q5 }
- minQualityFloor: QualityTier
- maxQualityCap: QualityTier
- guaranteeOptions?: [{ guaranteeQuality, extraInputMultiplier }]
- unlock?: { playerLevel?, chapterId?, regionCleared? }
```

### 5.3 生产进度载荷

```text
ProduceProgress
- recipeId: string
- useGuarantee: bool
- remainderSeconds: float
- batchesCompleted: int
- runSeed: long
- batchIndex: int
```

权威服务建议：`ProductionService` + 配置 `Resources/data/recipes.json`、`items.json`。

---

## 6. 规则细则

### 6.1 启动校验顺序

1. 卡组占用层 `TryStart` 通过；  
2. 配方已解锁、设施等级足够；  
3. 仓库可预扣材料；  
4. 输出目标空间：若完全无 Pending/仓库策略可用则拒绝或允许进 Pending（默认允许 Pending）。

### 6.2 与卡组用途

| `DeckPurpose` | 可启动 |
| --- | --- |
| `Produce` / `Flexible` | Process、Manufacture |
| `Gather` | 仅 Gather |
| `Combat` | 不可生产 |

### 6.3 验收用例

| 场景 | 期望 |
| --- | --- |
| 三条链各跑通原料→成品 | 仓库出现对应装备/模块 |
| 提高设施与技能 | 连续制造的最低品质上升（可统计） |
| 使用保底 Q3 | 产出 ≥ Q3，额外材料已扣 |
| 制造中停止 | 材料不退，无半成品，角色立即 Idle |
| 缺料 | `PausedBlock`，已完成批次仍在 |
| Q1 拆解 | 获得约 40% 主材料 |

---

## 7. UI / UX 要求

| 界面 | 要求 |
| --- | --- |
| Crafting Screen | 原生配方列表；显示链、输入、**工时（非瞬间）**、期望品质区间 |
| 流水线屏（后 MVP） | 已建产线列表；启用开关、当前配方、升级按钮、Paused 原因 |
| 保底选项 | 明确额外材料与最低品质 |
| 进行中 | 进度条、剩余批次时间、Paused 原因 |
| 仓库 | 按品质筛选/着色；装备显示品质徽章 |
| 拆解/再加工 | 二次确认；显示返还预览 |

---


## 9. 设计验收标准

- 至少 15 种资源可入库堆叠（按品质分堆）
- 3 条链均可：采集/获得原料 → 加工 → 制造成品
- 品质 5 档生效；属性/价值随品质变化（属性倍率可后续加深）
- 培养或保底可提高最低品质，而非纯玄学
- 停止生产不退已扣料、不发放半成品、占用立即解除
- 缺料/满仓 `PausedBlock` 无惩罚
- 10+ 装备/模块定义可被制造或配置给出
- EditMode：品质地板、堆叠隔离、耐久损耗/维修

---

## 10. 开放钩子

- 卡牌制造作为第四链（若做，须遵守交易绑定规则）  
- 公会订单（非 MVP）  
- Q5 随机附加词条池（须控制市场 SKU）  
- 批量制造 UI（一次预扣 N 批）  
- **自动化流水线 UI**（启用/停用/升级/选当前配方；`05` §4.10）  
- `ProductionLineService` + `LineBlueprintCatalog`（后 MVP）  

若取消「预扣材料」或改为「完成时扣料」，需升本文主版本并同步占用文档停止语义。

---

## 11. 参考

- 核心设计：`02-core-product-design.md` §7.9 / §16.1 / §21  
- 占用：`14-deck-and-occupation.md`  
- 离线：`17-idle-and-offline.md`  
- 耐久：`19-durability-and-repair.md`  
- 市场：`20-market-and-card-trade.md`  
- **内容表（物品/配方/设施等级）**：`22-resources-and-warehouse.md`  
- 开发计划：`13-mvp-development-plan.md` P3  
- 现有：`ItemEntity.cs`、`InventoryItemManagerBase.cs`、`AppShell` Crafting 导航
