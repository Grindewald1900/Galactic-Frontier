# 系统文档：装备耐久与维修

> 文档版本：v1.0  
> 状态：**MVP 规则已拍板，可供 P3 实现**  
> 上级约束：`Documentation/01-core-product-design.md` §7.7 / §16.1 / §20 / §21 / §22  
> 关联：`02-auto-battle.md`（战后损耗事件）、`04-idle-and-offline.md`（归零暂停）、`05-production-and-quality.md`（品质影响耐久上限与维修成本）、`03-region-and-ship.md`（装甲工坊钩子）  
> 实现阶段：开发计划 P3.4（见 `../11-mvp-development-plan.md`）  
> 更新日期：2026-08-09

---

## 1. 目标与非目标

### 1.1 目标

用耐久建立**可预测的材料消耗环**，连接战斗、采集、制造与市场，同时避免成为繁琐障碍：

- 装备有耐久上限与当前值；
- 行动中损耗，材料维修；
- **归零不销毁**；
- 维修成本可预览、无随机失败；
- 支持自动维修，减少手动打扰；
- 高强度挂机长期需要维修材料（市场与生产需求）。

### 1.2 非目标

- 装备强化失败、永久损毁（设计排除）  
- 生产配方与品质 roll → `05-production-and-quality.md`  
- 市场挂单 → `07-market-and-card-trade.md`  
- 武器磨损影响外观的纯表现（可后置）

---

## 2. 术语

| 术语 | 定义 |
| --- | --- |
| **耐久（Durability）** | 装备实例上的整数；`0 … maxDurability` |
| **最大耐久** | 由物品定义 × 品质乘数决定；维修不可超过该值 |
| **损耗事件** | 战斗场次、采集周期等触发的耐久扣减 |
| **维修** | 消耗材料将当前耐久恢复至目标值 |
| **自动维修** | 满足阈值时从仓库扣料自动恢复 |
| **关键装备** | 归零会导致对应行动 `PausedBlock` 的槽位装备（见 §4.5） |
| **破损态** | `durability == 0` 时的属性与可用性规则 |

---

## 3. 硬约束（不可违背）

1. **不得**因耐久归零永久销毁装备。  
2. 维修必须**持续消耗生产材料**（§22）。  
3. 维修成本可预测；**禁止**随机维修失败。  
4. 不强迫玩家频繁手动点维修；须提供自动维修。  
5. 归零或关键破损时，挂机行动暂停（`PausedBlock`），不删装备、不扣已有收益。  
6. 战斗层只上报损耗事件；扣减与维修在本系统结算。

---

## 4. MVP 拍板决议

> 关闭核心设计 §21「装备耐久」待定项。

### 4.1 哪些物品有耐久

| 类别 | 有耐久 | 说明 |
| --- | --- | --- |
| `Equipment`（角色装备） | 是 | 武器、护甲、配件 |
| `ShipModule`（舰船模块） | 是（MVP 可选接入） | 若未接入战斗损耗，可先仅作仓库物品 |
| `Consumable` / 材料 | 否 | 按数量消耗 |
| 卡牌角色本体 | 否 | 卡牌不是装备耐久载体 |

开局与任务送的装备同样有耐久。

### 4.2 最大耐久

```text
maxDurability = floor(itemDef.baseMaxDurability * qualityDurabilityMultiplier)
```

品质乘数见生产文档 §4.3（Q1=0.90 … Q5=1.35）。  
制造出货时写入实例；再加工若升品质则按新品质**重算上限**，当前耐久按比例缩放：

```text
newCurrent = floor(oldCurrent / oldMax * newMax)
```

### 4.3 不同行动的损耗

损耗在**行动结算点**一次性结算（一场战斗结束、一个采集周期完成），不用逐回合扣。

#### 战斗（MainCombat / AutoCombat）

对每件**已装备且参战**的装备：

```text
loss = baseCombatLoss
     * slotWeight
     * encounterWearMult
     * (isAutoCombat ? autoCombatMult : 1)
     * (1 - shipFacilityWearReduction)   // 装甲工坊等，上限 0.5
loss = max(1, floor(loss))   // 参战且配置为应损耗时至少 1；未参战槽=0
current = max(0, current - loss)
```

| 参数 | MVP 默认 | 说明 |
| --- | --- | --- |
| `baseCombatLoss` | **3** | 单场基准 |
| `slotWeight` | 武器 1.0 / 护甲 1.2 / 配件 0.6 | 可配置 |
| `encounterWearMult` | 普通 1.0；Boss 1.5；高熵区 1.3 | 遭遇/区域表 |
| `autoCombatMult` | **1.0** | 与主线同耗，避免「挂机更亏」；若需加大消耗改配置而非隐藏惩罚 |
| 败北 | 仍扣耐久（已开战） | 与「负场无掉落」独立 |
| 跳过演出 / 离线 | 同一公式 | Resolver 后统一 `ApplyWear` |

未装备的背包装备不扣。

#### 采集（Gather）

| 节点风险 | 每周期损耗（已装备工具/护甲） |
| --- | --- |
| 低 | 工具 **1**；护甲 0 |
| 中 | 工具 **2**；护甲 **1** |
| 高（高风险节点） | 工具 **3**；护甲 **2** |

无「采集工具」槽时，可对任意已装备配件扣工具损耗，或跳过（节点表 `requiresToolWear`）。

#### 生产（Process / Manufacture）

| MVP | 规则 |
| --- | --- |
| 默认 | **不扣**角色装备耐久 |
| 特殊配方 | 配方可标 `wearOnCraft`；默认关闭 |

遗迹探索若未单列行动，并入高风险采集或特殊遭遇表。

### 4.4 耐久归零后的属性影响

| 状态 | 规则 |
| --- | --- |
| `durability > 0` | 属性全额（MVP **不做**百分比衰减曲线，降低心智负担） |
| `durability == 0` | 进入破损态：该装备**属性加成视为 0**；仍占用装备槽；**不销毁** |
| 破损武器 | 角色仍可出战，但失去该武器加成（战力自然下降） |
| 破损关键件 | 见 §4.5 |

> 决议：不用「耐久 50% 以下属性递减」，避免玩家焦虑与 UI 噪音；破损是清晰阈值。

### 4.5 关键装备与行动暂停

| 槽位标签 | 归零时 |
| --- | --- |
| `criticalForCombat`（可选） | `AutoCombat` → `PausedBlock`（需维修）；`MainCombat` 仍可强行出战但弹确认 |
| 普通槽 | 不自动暂停；属性为 0 |
| `criticalForGather` | 高风险采集节点拒绝开始或周期后 `PausedBlock` |

MVP 默认：每角色 **武器槽** 标为 `criticalForCombat`；其余非关键。  
全队所有关键装备均破损时，挂机刷取必 `PausedBlock`。

### 4.6 维修材料与费用公式

维修**不失败**、不暴击。

```text
missing = maxDurability - current
repairRatio = missing / maxDurability

materialQty = ceil(itemDef.repairBaseMaterials
                   * repairRatio
                   * qualityRepairMultiplier
                   * rarityMult)

creditFee = floor(itemDef.repairBaseCredits * repairRatio * qualityRepairMultiplier)
```

| 项目 | MVP 默认 |
| --- | --- |
| 材料种类 | `itemDef.repairMaterialId`（如合金板、合成纤维）；高级装备用更高阶材料 |
| `repairBaseMaterials` | 按装备定义，示例：普通武器满修 **10** 单位 |
| 品质维修乘数 | 见生产文档（Q1=0.85 … Q5=1.45） |
| 部分维修 | 允许指定恢复量或「一键修满」；费用按实际恢复比例 |
| 信用金 | 可收小额；可配置为 0（纯材料） |

预览：UI 必须在确认前显示材料与信用消耗。

### 4.7 自动维修

| 项目 | MVP 规则 |
| --- | --- |
| 开关 | 每玩家全局 + 可选每角色覆盖 |
| 触发时机 | 战斗结算后、采集周期后、上线离线结算后 |
| 触发阈值 | `current / maxDurability <= autoRepairThreshold`（默认 **0.3**） |
| 修到 | `autoRepairTarget`（默认 **1.0** 满） |
| 材料不足 | 跳过并提示；挂机若因此仍破损关键件 → `PausedBlock` |
| 是否占生产队列 | **否**；维修是瞬时仓库操作，不占 `Produce` 卡组 |
| 手动维修 | 随时可在仓库/角色面板进行，同样不占生产队列 |

### 4.8 维修与生产队列的关系（拍板）

| 问题 | 决议 |
| --- | --- |
| 维修是否占用生产队列？ | **不占用** |
| 维修包消耗品 | 可作快捷修：使用后按包恢复固定耐久，仍走同一费用边界校验 |
| 现场工匠行动 | 非 MVP（不新增 `Repair` 卡组行动） |

---

## 5. 数据模型（实现契约）

```text
ItemInstance  // 扩展
- durability: int
- maxDurability: int
- broken: bool                 // 可派生自 durability==0

EquipSlotDef
- slotId: string
- criticalForCombat: bool
- criticalForGather: bool
- wearWeight: float

RepairPolicy (player)
- autoRepairEnabled: bool
- autoRepairThreshold: float
- autoRepairTarget: float
- pauseAutoCombatWhenCriticalBroken: bool  // 默认 true

WearEvent
- source: MainCombat | AutoCombat | Gather | Craft
- ownerCardId / shipId
- itemInstanceId
- amount: int
- atUtc: long
```

服务：`DurabilityService.ApplyWear(events)`、`PreviewRepair`、`Repair`、`TryAutoRepair`。

---

## 6. 规则细则

### 6.1 结算顺序（战斗后）

1. `BattleResult` 产出；  
2. 生成参战装备 `WearEvent`；  
3. `ApplyWear`；  
4. `TryAutoRepair`；  
5. 若关键破损且自动修失败 → 挂机 `PausedBlock`；  
6. 发奖 / Pending。

### 6.2 上线离线批量

离线多场战斗：可逐场 ApplyWear + AutoRepair，或等价聚合：

```text
totalLoss = perFightLoss * fights
```

若中途关键破损且材料够自动修，则继续；材料在某场后耗尽则后续场次不再推进（与 `PausedBlock` 一致）。  
MVP 允许「聚合损耗 + 一次自动修」的近似，但不得修出比逐场更优的结果（禁止近似占便宜）。

### 6.3 验收用例

| 场景 | 期望 |
| --- | --- |
| 打 1 场 | 参战装备耐久下降且可预览公式结果 |
| 修满 | 材料按比例扣除，耐久=max，无失败 |
| 归零 | 装备仍在；属性加成 0；不消失 |
| 自动修开、仓内有料 | 低于 30% 自动修满 |
| 挂机至武器归零且无料 | `PausedBlock`，已刷奖励保留 |
| 装甲工坊加成 | 同遭遇损耗减少 |

---

## 7. UI / UX 要求

| 界面 | 要求 |
| --- | --- |
| 装备图标 | 耐久条或数值；破损红色徽章 |
| 维修弹窗 | 显示「当前→目标」、材料、信用；无失败字样 |
| 自动维修设置 | Bridge 或舰船/仓库设置页 |
| 挂机暂停 | 文案「关键装备破损，请维修」 |
| 战报 | 可选展示本场总损耗摘要（不强制逐件动画） |

---

## 8. 与现有代码的差距

| 现有实现 | 差距 |
| --- | --- |
| `ItemEntity` 无耐久字段 | 需实例模型 |
| 战斗结束无 Wear 事件 | `BattleResult` 后挂钩 |
| 无维修 API | 新建 `DurabilityService` |
| 无自动维修 | 策略存档 + 结算钩子 |

**建议落地顺序：**

1. 实例字段 + 品质最大耐久  
2. 战斗/采集 `ApplyWear`  
3. 手动维修预览与执行  
4. 自动维修 + 挂机破损暂停  
5. 舰船设施减耗  

---

## 9. 验收清单

- [ ] 装备具有当前/最大耐久；品质影响最大耐久  
- [ ] 主线与挂机战斗按表损耗；采集高风险额外损耗  
- [ ] 归零不销毁；属性加成视为 0  
- [ ] 维修消耗可预览、无随机失败、可部分修  
- [ ] 自动维修可开关，默认阈值 30%  
- [ ] 维修不占用生产卡组  
- [ ] 关键破损且无料时挂机 `PausedBlock`  
- [ ] EditMode：损耗公式、修满耗料、归零不删、自动修材料不足  

---

## 10. 开放钩子

- 耐久百分比衰减曲线（若产品坚持，升 v2）  
- 舰船模块参战损耗  
- 保险/免费修活动  
- 「场修」消耗品的特殊动画  

若改为「归零销毁」或「维修可失败」，必须修订核心设计 §7.7 / §16.2 并升本文主版本。

---

## 11. 参考

- 核心设计：`Documentation/01-core-product-design.md` §7.7 / §21  
- 战斗：`02-auto-battle.md`  
- 离线：`04-idle-and-offline.md`  
- 生产品质：`05-production-and-quality.md`  
- 舰船设施：`03-region-and-ship.md`  
- 开发计划：`Documentation/zh-CN/11-mvp-development-plan.md` P3.4
