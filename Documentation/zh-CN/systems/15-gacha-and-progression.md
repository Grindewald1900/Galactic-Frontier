# 系统文档：抽卡与卡牌成长入口

> 文档版本：v1.1  
> 状态：**MVP 规则已拍板；正式路径已落地（GachaService + RecruitScreen）**  
> 上级约束：`Documentation/01-core-product-design.md` §7.6 / §16 / §20 / §21（卡牌交易相关）  
> 关联：`07-market-and-card-trade.md`（绑定/交易）、`08-save-and-seed-data.md`（DevData / Starter）、`01-deck-and-occupation.md`（同名多开）、`09-resources-and-warehouse.md`（材料扣减）、`10-onboarding-and-missions.md`、`18-character-roster-and-lore.md`（角色叙事）、`19-card-energy-rank.md`（能级 ≠ 抽卡稀有度）  
> 更新日期：2026-08-12  
> 变更：v1.1 — Dev OFF 扣 `con_recruit_ticket`；Lv1 入池；软保底 `gacha.json`；Nexus 招募页。

---

## 1. 目标与非目标

### 1.1 目标

在**不把角色获取完全绑死在不可控随机**的前提下，提供可理解的招募（抽卡）循环：

1. 用材料 / 信用点支付，按权重生成角色卡实例；  
2. 结果进入玩家卡池，可立即编队 / 占用；  
3. 与掉落、任务奖励、Starter Seed 并列，作为获取渠道之一；  
4. 重复卡有明确用途（多开编队、分解兑换），避免纯废卡；  
5. 与 Market 导航解耦（Market = NPC 商店；抽卡为独立入口）。

### 1.2 非目标

- 实时 PvP 抽卡、跨服共享卡池  
- 复杂多卡池 UP 日历 / 限定联动运营后台（可留钩子）  
- 用抽卡替代全部掉落与任务给卡（违背核心设计）  
- 把抽卡页重新挂到「市场」导航  

---

## 2. 术语

| 术语 | 定义 |
| --- | --- |
| **招募 / 抽卡（Gacha）** | 消耗约定代价，按池权重生成 1..N 张 `CardEntity` 的流程 |
| **卡池（Pool）** | 可抽出的角色模板集合 + 稀有度权重 |
| **单抽 / 十连** | `drawCount = 1` 或 `10`（现 UI 已支持） |
| **预扣（Reserve）** | 点「+1/+10」时先把代价从「可用」挪到「本次消耗」区 |
| **确认抽取（Commit Draw）** | 扣真实库存并生成结果 |
| **入池（Grant）** | 结果写入 `CardListManager` 并存档 |
| **分解（Dismantle）** | 销毁卡实例换材料 / 信用（重复卡 sink） |
| **绑定（Bound）** | 不可上架玩家市场的卡；见市场文档 |
| **抽卡稀有度** | `CharacterTier`（E…SS）：抽出时的卡面品质；**不是**能级 |
| **卡牌能级** | `EnergyRank`（F…S）：实例成长轴（波动门槛 + 进阶条件）；见 `19-card-energy-rank.md` |
| **经验等级** | `Level`：吃经验升级 |

---

## 3. 硬约束

1. 角色获取**不得**仅依赖抽卡；掉落、任务、Starter 必须可获得可用战斗卡。  
2. 抽卡与 **NPC / 玩家市场导航分离**（已落地：Market → `MarketScreen`，禁止进 `SHOP_MENU` 抽卡）。  
3. 正式包默认**关闭** Dev 样例材料 / 样例结果；正式路径读库存 + `CardDataManager` 生成。  
4. 每张卡为独立 `cardId`（GUID）；同名模板可多开，服务多卡组（见占用文档）。  
5. Starter / 主线关键卡绑定规则与市场文档一致；**普通抽卡结果默认可交易（Online）**。  
6. 禁止在抽卡流程里无守卫地 `FakeData` 写档。

---

## 4. 当前实现快照（代码真相）

> 以下描述以仓库现码为准；与 §5 拍板目标的差距见 §9。

### 4.1 调用链

```text
CardDrawingManager (SHOP_MENU)
  选次数 AddDraw(±1 / ±10)  → 内存调整 provider/consumer 数量
  StartDraw()
    → MainScrollController.ShowPanel(DRAWCARDS_MENU)
    → CardResultManager.InitCards(drawCount)
         Dev ON  → DevData.CreateSampleGachaResults → CardDataManager.GetCardEntity
         Dev OFF → 空列表，直接 return（正式路径未接线）
    → FlipAllCards → ShowReport
         → 统计稀有度/角色
         → CardListManager.AddCardEntity(list)  // 翻牌结束即入池存档
    → Confirm → 回 SHOP_MENU（TODO 注释仍在，实际入池已在 ShowReport）
```

### 4.2 关键类

| 类 | 职责 |
| --- | --- |
| `Shop/CardDrawingManager` | 1/10/重置/确认 UI；材料槽 provider/consumer；次数预扣 |
| `Cards/CardResultManager` | 结果展示、翻牌演出、稀有度/角色统计、入池 |
| `Cards/CardDataManager` | 角色权重抽取、稀有度权重、生成 `CardEntity`、专长 |
| `Cards/CardListManager` | 卡池权威集合；`AddCardEntity` 去重 id 后存档 |
| `Utils/Save/IDevDataProvider` | `FillSampleGachaMaterials` / `CreateSampleGachaResults` |
| `GameStatusManager.IsDrawingCard` | 翻牌期间状态锁 |

### 4.3 生成规则（现码）

`CardDataManager`：

1. `GetCharacter()`：按各 `Character.weight` 加权（当前注册 Asra / Magki / Sernia）。  
2. `GetCardTier(character)`：按该角色 `possibleTiers` 权重表（整数权重，roll 1..10000）。  
3. `GetCardEntity`：赋名、职业、稀有度，并随机初值等级/攻防/速度（原型范围）。  
4. 构造时分配 `id = Guid`；专长走 `GetCharacterExpertises` + 随机专长管线。

Asra 示例稀有度权重（其余角色同结构）：

| Tier | 权重（示意） |
| --- | ---: |
| SS | 10 |
| S | 100 |
| A | 500 |
| B | 1000 |
| C | 2000 |
| D | 5000 |
| E | 10000 |

专长稀有度另有 `baseTierProbabilities`（E 50% … SS 0.2%），随等级微调，**不等于**卡面稀有度抽卡权重。

### 4.4 材料 UI 模型（现码）

- `providerItems`：玩家侧「可用材料」展示（Dev 下随机填充，**仅内存**）。  
- `consumerItems`：本次将消耗的镜像。  
- `itemQuantities[i]`：每抽消耗第 i 种材料的数量。  
- `AddDraw(count)`：`provider -= qty * count`，`consumer += qty * count`。  
- **未**调用 `ItemManager` / `InventoryStore`；正式模式列表为空，按钮全灭。

### 4.5 入池时机（现码注意）

| 步骤 | 是否入池 |
| --- | --- |
| `InitCards` | 否（仅生成列表） |
| `ShowReport`（翻牌结束） | **是** → `AddCardEntity` + `SaveCardData` |
| `ConfirmCards` | 否（仅切回商店；注释仍写 TODO Save） |

中途关闭结果面板会 `OnDisable → ClearCardResult`，但若已执行 `ShowReport`，卡已进存档。

---

## 5. MVP 拍板决议

### 5.1 产品定位

| 项目 | 决议 |
| --- | --- |
| 名称 | 招募站 / Recruitment（UI 可用双语；内部 Id `gacha`） |
| 入口 | Nexus **独立页或舰桥 CTA**（如 Hangar / Recruit）；**不得**占用 Market |
| 与掉落关系 | 抽卡补齐职业覆盖与重复卡；区域 FirstClear/Farm 仍给卡或碎片材料 |
| 单池 | MVP **一个标准池** `pool_standard`；后续再加 UP 池 |

### 5.2 代价与扣减

| 项目 | MVP 默认 |
| --- | --- |
| 单抽代价 | 配置表 `GachaCostTable`：例如 `recruit_ticket × 1` **或** 等价材料组合 |
| 十连 | `单抽代价 × 10`（无额外折扣，避免经济失衡；日后可配置 `tenPullDiscount`） |
| 库存来源 | **本地仓库** `ItemManager`（真实 `itemDefId`） |
| 预扣 | UI 可继续用 provider/consumer 表现，但权威数量以库存为准 |
| 提交时机 | 点「抽取」时：`TryConsume` 成功才生成；失败则不进结果页 |
| 失败回滚 | 生成中异常 → 退回已扣材料（事务顺序：扣 → 生成 → 入池；任一步失败全回滚） |

Starter Seed / 资源表应增加 `recruit_ticket`（或复用已有材料 Id）；未进表前可用临时 `itemDefId` 并在 `09` 登记。

### 5.3 生成与正式路径

正式模式（`DevData` 关闭）必须：

```text
for i in 1..drawCount:
  character = CardDataManager.GetCharacter()          // 或 Pool 过滤后的权重
  entity    = CardDataManager.GetCardEntity(character)
  applyGachaDefaults(entity)  // 见下
  results.Add(entity)
```

`applyGachaDefaults`（MVP）：

| 字段 | 规则 |
| --- | --- |
| `Level` | 固定 **1**（不要用现码 Random 1–10 作为正式规则） |
| 基础攻防速 | 按等级表 / 模板初始化，禁止开战前随机大范围飘移 |
| `boundReason` | `None`（默认可交易）；特定池可覆盖 |
| `source` | `Gacha`（枚举，便于统计与绑定时效） |
| `id` | 新建 GUID |

Dev 模式可继续用 `CreateSampleGachaResults`，但须打 `[DEV-DATA]` 日志，且**不得**在正式键路径默认开启。

### 5.4 卡池与权重

| 项目 | MVP |
| --- | --- |
| 角色集合 | `CardDataManager` 已注册角色；P5.1 扩容时同步进池 |
| 角色权重 | `Character.weight`（可配置化到 JSON 后替换硬编码） |
| 稀有度 | 每角色 `possibleTiers`；展示概率须与权重一致（UI 可折叠「详情」） |
| 保底 | **软保底**：连续 `pityThreshold`（默认 **40**）单抽未出 ≥A 时，下次强制从 ≥A 权重子表抽取；十连内按单次累计 pity |
| 硬保底 SS | MVP **不做**；避免运营债 |
| 种子 | 可选 `gachaSeed`；EditMode 需可复现单次抽取（接入后禁止未播种 `Random`） |

### 5.5 入池与确认 UX

统一为：

1. **扣材料成功** → 进入结果页并生成；  
2. **翻牌结束** → `Grant` 入池（与现 `ShowReport` 对齐，减少大改）；  
3. **确认** → 仅关闭/返回招募页；  
4. 若玩家在翻牌前强退：材料已扣则仍异步 Grant（或回滚材料——MVP 选 **强退也 Grant**，避免刷退币；UI 提示「结果已保存」）。

删除 `ConfirmCards` 上过时的「TODO Save」注释，改为说明「入池已在 ShowReport」。

### 5.6 绑定与来源

| 来源 | `boundReason` | Online 可交易 |
| --- | --- | --- |
| Starter Seed | `Starter` | 否 |
| 主线关键赠卡 | `MainStoryEssential` | 否 |
| 标准抽卡 | `None` | 是 |
| 区域掉落普通卡 | `None` | 是 |
| 活动/礼品码 | 按表配置 | 按表 |

与 `07-market-and-card-trade.md` §4.9 一致；本系统负责写入字段，市场负责校验。

### 5.7 重复卡与分解

| 项目 | MVP |
| --- | --- |
| 同名多实例 | **允许**（多卡组占用核心） |
| 强制合并 | **否** |
| 分解 | 允许对 `boundReason == None` 且 `occupation == Idle` 的卡分解 |
| 分解产出 | 按稀有度查 `DismantleTable` → 材料 / 少量 `recruit_ticket` / 信用 |
| 分解入口 | 角色详情或卡册；非抽卡页强制 |

### 5.8 成长衔接（轻量）

抽卡只负责「获得 Lv1 实例」。成长仍在卡册：

- 升级 / 进化 / 专长：现有 `CardEntity` 事件与经验逻辑；  
- 抽卡**不**直接给高等级成品（正式规则）；  
- 重复卡可用于未来「突破材料」钩子（MVP 可不做，分解即可）。

### 5.9 与新手引导

主链（`10-onboarding`）**不强制**抽卡步骤。可选支线：

- 「进行一次单抽」→ 软 CTA 指向招募页；  
- 奖励：额外 `recruit_ticket × 1`。

---

## 6. 数据模型（实现契约）

### 6.1 GachaCostTable / PoolConfig

```text
GachaPoolConfig
- poolId: string                 // pool_standard
- characterWeights: { characterName, weight }[]  // 可覆盖 Character.weight
- enabled: bool

GachaCostTable
- poolId: string
- costsPerPull: { itemDefId, quantity }[]
- tenPullMultiplier: int         // 默认 10
- pityThreshold: int             // 默认 40
- pityMinTier: CharacterTier     // 默认 TierA
```

### 6.2 玩家抽卡进度

```text
PlayerGachaState
- pityCounter: int               // 距上次 ≥ pityMinTier 的连续未命中单抽次数
- totalPulls: int
- lastPoolId: string
```

存档建议：`saves/{playerId}/gacha.json`（或并入 `meta` 扩展；新建文件需升 `saveVersion`）。

### 6.3 CardEntity 扩展字段

```text
source: None | Gacha | Drop | Starter | Mission | Gift | Craft
boundReason: None | Starter | MainStoryEssential | AccountBound | Admin
```

缺省兼容旧档：视为 `source=None`，`boundReason=None`（Starter 卡需迁移或种子重打标记）。

### 6.4 服务接口

```text
IGachaService
- GetPool(poolId)
- CanAfford(pullCount) -> (ok, missing[])
- TryPull(poolId, pullCount, presentation) -> GachaPullResult
    // 内含：扣库存、生成、更新 pity、Grant、返回实体列表

ICardDismantleService
- CanDismantle(cardId) -> (ok, reason)
- TryDismantle(cardId) -> rewards
```

UI（`CardDrawingManager` / 未来 Nexus `RecruitScreen`）只调服务，不直接 `Random`。

---

## 7. UI / UX 要求

| 界面 | 要求 |
| --- | --- |
| 招募主页 | 显示池名、单抽/十连代价、库存、pity 进度（简洁） |
| 材料不足 | 按钮禁用 + 缺口列表；可跳转 NPC 商店 |
| 结果页 | 翻牌演出可跳过；稀有度/角色统计保留 |
| 概率公示 | 至少「角色权重说明 + 稀有度档」；不必像素级动态页 |
| Market | 不得进入本流程 |
| Dev | Dev Data ON 时角标「样例材料/结果」 |

Nexus：建议新增 `AppScreen.Recruit` 或挂在舰桥「招募」；旧 `SHOP_MENU` / `DRAWCARDS_MENU` 可作过渡，最终迁原生 Screen。

---

## 8. 概率与展示（契约形状）

单次抽卡：

```text
character = WeightedRandom(pool.characterWeights)
tier       = WeightedRandom(character.possibleTiers)   // 或 pity 强制子表
entity     = BuildCard(character, tier, level=1, ...)
if tier >= pityMinTier: pityCounter = 0
else: pityCounter++
if pityCounter >= pityThreshold: next pull uses filtered tier table (≥ pityMinTier)
```

十连 = 连续 10 次单次逻辑（共享 pity），不是「十连内必出」。

---

## 9. 与现有代码的差距

| 现有 | 目标 |
| --- | --- |
| 正式模式 `InitCards` 空结果 | 走 `IGachaService.TryPull` / `CardDataManager` |
| 材料仅 Dev 内存列表 | 接本地仓库 + `GachaCostTable` |
| `GetCardEntity` 随机高等级 | 抽卡默认 Lv1 + 模板属性 |
| 无 pity | `PlayerGachaState` + 软保底 |
| 无 `source` / `boundReason` | 扩展字段 + 旧档默认 |
| Confirm TODO 误导 | 文档化入池时机并清注释 |
| 入口仍偏旧 `SHOP_MENU` | Nexus 独立招募页 |
| 无分解 | `ICardDismantleService` + 表 |
| `UnityEngine.Random` 未播种 | 抽卡种子可测 |

**建议落地顺序：**

1. `IGachaService` + 正式生成路径（修空结果）  
2. 库存扣减接 `GachaCostTable`；Dev 材料仅调试  
3. Lv1 正式生成与属性表对齐  
4. `source` / `boundReason` + 软保底存档  
5. Nexus Recruit Screen；概率与 pity UI  
6. 分解表与卡册入口  
7. EditMode：权重边界、pity 触发、扣费回滚、同种子复现  

---

## 10. 验收清单

- [x] Dev OFF 时单抽/十连仍能产出卡并入池（非空）  
- [x] 代价从本地仓库扣除；不足无法进入结果页  
- [x] 扣除失败或生成失败不丢材料、不产生半写入卡  
- [x] 翻牌结束后卡在 `CardListManager` 且存档可读回（Nexus Recruit 立即 Grant）  
- [x] 抽卡默认 Lv1；稀有度来自角色权重表  
- [x] 软保底按阈值触发且 UI 可感知进度  
- [x] Market 导航不进入抽卡  
- [x] 抽卡卡默认可交易标记（`boundReason=None`）  
- [x] 同名多卡可同时存在并编入不同卡组  
- [x] EditMode 覆盖 pity / 扣费数量  
- [ ] 分解表与卡册入口（后置）  

---

## 11. 开放钩子

- UP 池 / 限时池与独立 pity  
- 十连折扣、首抽半价  
- 碎片合成（非完整卡）  
- 抽卡动画 Addressables 皮肤  
- Online 服务端权威抽取（反作弊）  

若产品改为「抽卡为唯一获卡途径」或「禁止重复卡」，须修订核心设计并升本文主版本。

---

## 12. 参考

- 现码：`CardDrawingManager.cs`、`CardResultManager.cs`、`CardDataManager.cs`、`CardListManager.cs`、`DefaultDevDataProvider.cs`  
- 实现摘要：`Documentation/zh-CN/04-core-systems.md` §5  
- 市场绑定：`07-market-and-card-trade.md` §4.9  
- 存档 / Dev：`08-save-and-seed-data.md`  
- 核心设计：§7.6 卡牌交易、§21 待定项中与抽卡/重复卡相关部分  
- 开发计划：`11-mvp-development-plan.md`（可选文档 `15-gacha-and-progression`）
