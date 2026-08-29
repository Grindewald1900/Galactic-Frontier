# MVP 开发进度与 Cursor 后续开发计划

> 文档版本：v2.4  
> 对照设计：[02-core-product-design.md](02-core-product-design.md)  
> 实现快照：**见 [PRODUCT-STATUS.md](PRODUCT-STATUS.md)**  
> 更新日期：2026-08-23  
> 变更摘要：对照合并后 `01`–`21` 文档与主干代码刷新进度；修正 §10 文档引用与近期任务。

本文档回答三件事：

1. 以核心设计 / MVP 为尺子，**当前做到哪一步**；
2. 差距在哪里、按什么顺序补齐；
3. 如何用 **Cursor** 高效推进后续开发（任务切片、上下文入口、验收方式）。

---

## 1. 一句话结论

**当前进度与验收完成度以 [PRODUCT-STATUS.md](PRODUCT-STATUS.md) 为准**（约 **85%** MVP）。

项目处于可演示的单人核心循环阶段：战斗、多卡组、采集/制造/耐久/离线、星港 NPC、新手五步、Frontier VII 奖励、约 36 名角色与招募抽卡已打通；Explore 已切到 **M1 单星域 2D 地图**（雷达/巡航/已探索列表），物品说明与模块升级 UI 已补齐。  
相对 MVP，剩余主要是角色继续扩容（可选至 50）、抽卡分解、Characters/Cards 原生页。后 MVP 设定（舰队/探索度、流水线、星图 M2+）规则已写、实现未开始。

---

## 2. 当前开发进度总览

> **本节 §2.1–§2.2 为历史快照。** 维护时请只更新 [PRODUCT-STATUS.md](PRODUCT-STATUS.md) §2–§4；下列表格保留供阶段审计参考，不再逐条同步。

### 2.1 完成度快照

| 维度 | 评估 | 说明 |
| --- | --- | --- |
| 战斗原型 | ★★★★☆ | 全自动可跑通；种子伤害+战报返回已通；敌人/策略仍弱 |
| 卡牌与编队 | ★★★★☆ | **P1 完成**：多卡组 UI、占用、调度器、备用预设 |
| UI 壳层 | ★★★★☆ | Nexus 含 Recruit / Ship / Explore 地图；Inventory / Crafting / Market / Missions 已原生；Characters/Cards 仍可接 Legacy；locale JSON 已接入 |
| 存档与数据 | ★★★★☆ | **P0–P3**：FakeData 隔离、meta/版本、双背包、world/ship/idle、Starter Seed；`gacha.json`；`navX/navY`/`knownBodyIds` |
| 经济循环 | ★★★★☆ | **P3+P4**：采集/制造/耐久/品质/离线 + 星港 NPC 商店；制造首周期即时入仓；物品说明/获取提示；流水线未做 |
| 成长与主线 | ★★★★☆ | **P2+P5.4**：6 区进度 + 舰船门 + 首领 + 新手五步；Explore M1 空间导航 |
| 内容量 | ★★★☆☆ | 可战斗角色约 **36** 名（`CharacterName` 枚举，不含 Default）；可继续向 50 扩 |
| 规则文档 | ★★★★☆ | 合并为 `01`–`21`；状态唯一源 `PRODUCT-STATUS`；`12`/`18`/`20`/`21` 仍有开放项 |

整体相对 MVP §16.1：**约 85%**（引导/星域/招募/Explore M1 已通；分解与卡册原生页仍薄）。

### 2.2 MVP 必含项对照表

状态定义：

- **已实现**：核心规则可在游戏中跑通（可为原型深度）
- **部分实现**：有骨架或相关能力，但未满足设计约束
- **未开始**：无有效领域逻辑（仅文案/导航不算）

| MVP 必含项（设计 §16.1） | 状态 | 当前证据 / 缺口 |
| --- | --- | --- |
| 全自动回合制卡牌战斗 | 已实现 | `BattleController.BattleRoutine` 按速度自动结算 |
| 每队 5 张出战角色卡 | 已实现 | 每 `DeckEntity` 5 槽；Formation 可切换卡组 |
| 多卡组管理 | 已实现 | 6 槽/开局 2 解锁；Formation 页签 + Bridge 舰队组件（待改：删顶栏并行卡、水平滑动） |
| 角色单队伍占用规则 | 已实现 | Running/PausedCap/PausedBlock 占用；`ActionScheduler` 停止立即解锁 |
| 主线/区域战斗自动结算 | 部分实现 | 区域遭遇可打；章节叙事仍薄 |
| 通关后挂机自动战斗刷取 | 已实现 | `AutoCombat` + `IdleEconomyTicker` 周期产废料 |
| 基础挂机采集 | 已实现 | Explore 停靠后 Gather CTA + `Gather` 行动周期产出 |
| 多行动并行 + 行动队列 | 已实现 | P1 并行 + P2 AFK + P3 采集/制造 |
| 分阶段离线收益上限 | 已实现 | `IdleSettlementService` + yield 0.50 起 / cap 2h 起 |
| 舰船等级限制区域推进 | 已实现 | `ShipGate` + `ShipService` 硬门 |
| 15–20 种资源 | 已实现 | `ItemCatalog` 18 资源类 + 堆叠键 `(itemDefId, quality)` |
| 3 条完整生产链 | 已实现 | metal / energy / synth + `CraftingScreen` |
| 10–15 装备/模块 | 已实现 | 8 装备 + 4 舰船模块定义 |
| 装备耐久与材料维修 | 已实现 | `DurabilityRules` / `DurabilityService`；归零不销毁 |
| 基础制造 + 品质差异 | 已实现 | `QualityRules` + 配方预览 Q 区间 |
| 全服玩家市场 + 价格历史 | 未开始（**非 MVP**） | v0.4：仅 Online；MVP 不做 |
| 普通卡牌交易 | 未开始（**非 MVP**） | 仅 Online；Solo 走 NPC/分解 |
| NPC 商店 + 基础货币 | 已实现 | `MarketScreen` + `NpcShopService`；`creditPoints` / `creditsBound` |
| 5–6 可探索区域 + 1 区域首领 | 已实现 | 6 区含 Frontier Anchor；`world.json` 进度 |
| 1 个完整星域 / 2 基础阵营 | 已实现 | Frontier VII 奖励/叙事；`FactionA`/`FactionB` 标签可见 |
| 30–50 角色及相关卡牌 | 部分实现 | **36** 名可战斗（P5.1a+b）；可继续向 50 |
| 约两小时新手流程 | 已实现 | `OnboardingService` + Missions + 软 CTA；曲线由星域奖励支撑 |
| 基础仓库和舰船升级 | 已实现 | 原生 `InventoryScreen`（类型/品质页签）+ 双背包存档；`ShipService` 模块/等级升级 |
| 离线收益比例（开局 50%） | 已实现 | `OfflineRules.YieldRatio`；Bridge Claim；打开仓库时领取 pending loot |
| 单机模式完整可玩 | 部分实现 | 核心循环可演示；Explore M1 已通；缺卡牌分解、Characters/Cards 原生页；角色量 36/50 |

### 2.3 已有可复用资产（后续不要推倒重来）

| 资产 | 路径 / 类 | 后续用法 |
| --- | --- | --- |
| 自动战斗引擎 | `Battle/BattleController.cs` | 扩展战前策略、挂机刷取、确定性战报 |
| 编队 UI | `LineupManager`、`FormationScreen` | 抽象为「卡组槽」，再扩多卡组 |
| 卡牌领域模型 | `CardEntity`、`CardDataManager` | 加占用态、绑定/交易字段 |
| Nexus 壳 | `AppShell`、`Bridge/Explore/Formation/Inventory/Crafting/Market` | 新系统优先做原生 Screen，少挂 Legacy |
| 存档基础设施 | `DataUtil` | 版本迁移、分文件已落地；新内容字段走 Migrator |
| 背包骨架 | `InventoryItemManagerBase` + `InventoryScreen` | 已接 `ItemCatalog`；继续服务制造/商店/Debug |
| 文档入口 | `*` | Cursor 任务前必读 |

### 2.4 高优先级技术债（历史项；P0–P3 已清）

下列阻塞项已在 P0–P3 关闭，保留作审计痕迹：

1. ~~**FakeData 污染存档**~~ → **P0.1 完成**；~~分文件 / 版本 / Starter Seed~~ → **P0.2 完成**  
2. ~~**本地/远程背包共用 `itemData.json`**~~ → **P0.2 完成**（分文件 + 迁移）  
3. ~~**无存档版本与迁移**~~ → **P0.2 完成**（`meta.json` + Migrator 0→1…→4）  
4. ~~**战斗结束返回主场景被注释**~~ → **P0.3 完成**（战报 Confirm → Explore）  
5. ~~**缺少 Domain 层测试**~~ → **P0.5** `BattleDomain`；**P1** `DeckRulesTests`；**P3/P4** Inventory/Quality/Durability/NpcShop 测试已追加  

P5 阶段新债优先记入 [08-known-issues.md](08-known-issues.md)（内容管线、任务存档字段等）。

详见 [08-known-issues.md](08-known-issues.md)。

---

## 3. 与核心设计的对齐约束（后续不可违背）

来自设计文档 §20 / §22，实现与 Cursor 提示词都应遵守：

1. 战斗为**全自动回合制**，不要求玩家逐回合点选  
2. 每个战斗/工作卡组 **5 名角色**  
3. 角色不能同时出现在多个**正在运行**的队伍中  
4. 多项挂机行动 = 多支独立队伍并行  
5. 离线收益时间是成长内容（分阶段解锁）；**离线收益比例开局 50%**，随舰船提升  
6. 舰船是区域推进必要条件（战斗通关 + 舰船条件）；成长以**模块化**为主  
7. 耐久维修持续消耗材料；装备不因耐久归零永久销毁  
8. 生产品质影响制造价值；线上才影响玩家市场价值  
9. **单机禁用玩家市场**；MVP 经济走 **NPC 商店**；普通卡玩家市场仅 Online  
10. 首发以**单机**完成主要内容；**不可**依赖玩家市场或多人  
11. Online 市场按全服规模设计并提供历史价格（非 MVP）  
12. （保留）玩法细则以 `11`–`21` 规则文档为准  

设计 §21 中「待确定」细则（站位、品质档位、离线秒数等）应在对应系统文档中拍板后再写死数值；实现阶段可先用可配置默认值 + ScriptableObject/JSON。

---

## 4. 推荐总体路线图

```mermaid
flowchart LR
    P0["P0 稳基线\n存档/FakeData/战斗闭环"] --> P1["P1 卡组与占用\n多队伍调度"]
    P1 --> P2["P2 区域推进\n模块化舰船 + 挂机刷怪"]
    P2 --> P3["P3 经济循环\n采集/生产/耐久/品质"]
    P3 --> P4["P4 NPC 商店\n单机经济"]
    P4 --> P5["P5 内容填充\n角色/区域/新手两小时"]
```

原则：

- **先规则引擎，后内容量**：没有占用与行动调度，加 50 张卡也撑不起循环。  
- **先本地可验证，后联网**：市场可先做「本地模拟全服盘口 + 可替换的后端接口」，避免阻塞单人循环。  
- **每个阶段结束必须可玩**：玩家能感知新决策（分角色、开区域、修装备、挂单），而不是只有后台类。

---

## 5. 分阶段开发计划（含验收标准）

### P0 — 稳基线（约 1–2 周）

**目标**：让现有原型可安全迭代，战斗主链路可回归。

| 任务 | 产出 | 验收 |
| --- | --- | --- |
| P0.1 FakeData 隔离 | **已完成** `IDevDataProvider` / `DevDataSettings`；正式流程默认关闭 | 新档启动不再随机覆写背包/材料 |
| P0.2 存档 v1 | **已完成** `meta.json`、原子写、`inventory_*`、Migrator 0→1、Starter Seed | 旧档能读或明确迁移；双背包数据不串 |
| P0.3 战斗闭环 | **已完成** 胜负战报 + Confirm → Explore；Esc → Bridge | Explore 进出战斗完整一轮无卡死 |
| P0.4 战斗确定性种子 | **已完成** `BattleRng` + `CombatMath`；命中/暴击走种子 | 同种子伤害序列一致（EditMode） |
| P0.5 Domain 测试脚手架 | **已完成** `Assets/Tests/EditMode` + `GalacticFrontier.BattleDomain` | ≥5 个稳定 EditMode 测试 |

**Cursor 任务切片示例：**

> 阅读 `06-data-and-save.md` 与 `DataUtil.cs`。为存档增加 `saveVersion`，并把 `ItemManager`/`RemoteItemManager` 拆成独立 JSON。不要改第三方包。完成后更新 `06-data-and-save.md`。

---

### P1 — 多卡组与角色占用（约 2–3 周）

**目标**：落实设计 §6 / §7.2 / §7.3 的「角色是有限资源」。

| 任务 | 产出 | 验收 |
| --- | --- | --- |
| P1.1 卡组数据模型 | **已完成** `DeckEntity` / `PlayerDeckState` / `decks.json`；Migrator 1→2 | 可序列化进存档；旧编队可迁移 |
| P1.2 多卡组 UI | **已完成** Formation 卡组页签 + Bridge 运行摘要/并行 | 至少 2 个卡组槽；未解锁槽显示条件；**待做**：Bridge 舰队滑动组件（[09](09-figma-ui.md)） |
| P1.3 占用状态机 | **已完成** `DeckRules` + `DeckOccupationMap`；Explore/Battle 进出占用 | 占用中角色不可加入其他运行中卡组（EditMode） |
| P1.4 行动调度器 | **已完成** `ActionScheduler` 开始/停止/完成/PausedCap | 停止后立即 Idle，可重分配（EditMode） |
| P1.5 备用卡组 | **已完成** 多解锁卡组可 idle 共享编制；UI 可切换编辑 | 未运行编队不占用角色 |

**拍板清单（实现前写入系统文档）：**

- 初始卡组槽数量与解锁条件  
- 停止行动后解除占用时机  
- 同名卡是否可同时进不同卡组  

---

### P2 — 区域推进、舰船门、挂机刷怪（约 2–3 周）

**目标**：落实设计 §7.1 / §7.8：打通「开战 → 通关 → 刷取 → 开下一区」。

| 任务 | 产出 | 验收 |
| --- | --- | --- |
| P2.1 区域进度图 | **已完成** `RegionCatalog` + `world.json` + Explore 锁态 | Explore 按进度解锁 |
| P2.2 舰船模型 | **已完成** `ShipEntity` / 模块 / Ship Bay | 区域入口校验通关+舰船 |
| P2.3 敌方配置表 | **已完成** `EncounterCatalog` + Battle 装载 | 每区遭遇；DevData 仅兜底 |
| P2.4 战后挂机战斗 | **已完成** AutoCombat + `IdleCombatTicker` | 周期废料 + farmWear 暂停 |
| P2.5 战前策略 v0 | **已完成** `CombatStrategyId` + 目标重排 | Formation 可切换策略 |
| P2.6 区域首领占位 | **已完成** Frontier Anchor Boss 遭遇 | 可挑战；击杀标记星域完成 |

---

### P3 — 经济循环：采集 / 生产 / 耐久 / 品质（约 3–4 周） — **已完成**

**目标**：形成设计核心循环中「战斗开图后的资源侧」。

| 任务 | 产出 | 验收 |
| --- | --- | --- |
| P3.1 资源表 | **已完成** `ItemCatalog` 18+12；`ItemEntity` itemDef/quality/durability | 仓库按 `(itemDefId, quality)` 堆叠 |
| P3.2 采集行动 | **已完成** `GatherNodeCatalog` + Explore Gather + `IdleEconomyTicker` | 与战斗卡组并行不冲突 |
| P3.3 生产链 ×3 | **已完成** `RecipeCatalog` + `ProductionService` + `CraftingScreen`；Start 首周期即时入仓 | metal / energy / synth 可跑通 |
| P3.4 装备与耐久 | **已完成** `DurabilityRules` / `DurabilityService`；`equippedToCardId` | 归零不销毁；维修包可修 |
| P3.5 品质系统 v0 | **已完成** `QualityRules` + 制造预览 | 输入/设施抬高最低品质 |
| P3.6 离线结算 v0 | **已完成** `idle.json` + `IdleSettlementService` + Bridge Claim；打开仓库领取 pending | 开局 50% 比例；上线领取 |

**内容目标（MVP 量）：** 10–15 件装备/模块；品质与耐久接入战斗/采集消耗。 **saveVersion = 4**。

---

### P4 — NPC 经济与商店（约 2 周；原全服市场后置） — **已完成（MVP）**

**目标**：落实设计 v0.4 §7.5 单机路径；`16-market-and-card-trade.md` MVP 部分。

| 任务 | 产出 | 验收 |
| --- | --- | --- |
| P4.1 PlayMode 门控 | **已完成** `PlayMode.Solo` + `PlayerMarketService` | Solo 无法打开玩家市场 |
| P4.2 货币字段 | **已完成** `creditPoints` + `creditsBound` + `CurrencyService`（含 `Changed` 刷新顶栏） | 购买扣款一致 |
| P4.3 NPC 货架配置 | **已完成** `NpcShops.json` + `NpcShopCatalog` | 按区域/舰船解锁商品 |
| P4.4 购买 / 回收 | **已完成** `NpcShopService` | 满仓/余额不足有提示 |
| P4.5 Nexus 入口 | **已完成** `MarketScreen` 星港商店 | 不再进抽卡 |
| P4.6（后置）玩家市场 | Online only 契约保留 | **非 MVP 验收项** |

> 全服玩家市场与卡牌上架挪到 **线上版本**；不要在 Solo 用 LocalMock 冒充全服盘口。

---

### P3/P4 后补丁（非独立阶段；已并入主干）

规则闭环后的体验修补，避免误判为未完成：

| 补丁 | 证据 | 说明 |
| --- | --- | --- |
| 原生仓库页 | `InventoryScreen` | Legacy World Space 面板被 Nexus 遮挡；现按类型/品质页签展示 |
| 制造即时产出 | `ProductionService.TryStartRecipe` | Start 成功后立刻结算首周期；失败退还材料 |
| 货币 UI 同步 | `CurrencyService.Changed` → AppShell credits | 买卖后顶栏信用点即时刷新 |
| Debug 全图鉴数量 | `DebugScreen` | 可改信用点与全部 `ItemCatalog` 堆叠 |

---

### P5 — 内容填充与新手两小时（约 3–5 周，可贯穿全程） — **主切片已完成；扩容进行中**

**目标**：把规则填成可体验的 MVP 内容量。  
**原则**：先做可演示的**引导脊骨**（文档 → Missions），再包装星域奖励，再分批扩角色与阵营，最后粗调数值。

```mermaid
flowchart LR
  doc10["P5.0 10"] --> missions["P5.4 Missions"]
  missions --> loopPolish["P5.3 sector polish"]
  loopPolish --> rosterBatch["P5.1 roster batches"]
  rosterBatch --> factions["P5.2 factions"]
  factions --> balance["P5.5 balance"]
```

| 任务 | 产出 | 验收 |
| --- | --- | --- |
| P5.0 新手系统文档 | **已完成** [17-sector-and-onboarding.md](17-sector-and-onboarding.md) | 步骤：编队→首战→采集→制造→NPC 商店 |
| P5.4a Missions UI | **已完成** `MissionsScreen` + `OnboardingService` + `onboarding.json` | Bridge/Missions 五步；Claim；可存档 |
| P5.4b 软引导 CTA | **已完成** Bridge 下一步条 + 目标屏 `OnboardingBanner` | 各目标屏可感知下一步 |
| P5.3 星域包装 | **已完成** `RewardCatalog` / `RewardService` + 采集节点 + Explore 短叙事 | FirstClear/Repeat/Farm 多物品；裂隙/深渊/护航节点 |
| P5.1a 角色批次 1 | **已完成** +8 可战斗卡（共享 Burn/Stun/Freeze 模板）→ 共 11 | 抽卡池/编队/战斗可用 |
| P5.1b 角色批次 2 | **已完成** +25 → 共约 **36** | 同模板；抽卡/编队/战斗可用 |
| P5.2 2 基础阵营 | **已完成** `FactionTags` + Explore/编队标签 | Guard / Syndicate 可辨；无完整阵营玩法 |
| P5.5 数值初平衡 | **已完成** 挂机 45s、维修/NPC 价差粗调 | AFK 有材料压力但不软锁 |
| 抽卡正式路径 | **已完成** `GachaService` + `RecruitScreen` + 招募券扣库存 | Dev OFF 可抽；Lv1 入池；软保底 |
| （后置）P4.6 / `12` | Online 玩家市场；特殊星域模式 | **非 MVP** |

> Characters / Cards 页仍可接 Legacy；P5 优先 Missions 原生页与内容表，不强求一次重写全部 Legacy 面板。

---

## 6. 使用 Cursor 的后续开发方法

### 6.1 推荐工作方式

| 做法 | 说明 |
| --- | --- |
| **一小步一对话** | 每个 Agent 会话只做一个 P 级子任务（如 P1.3），避免「做完整个经济系统」 |
| **先文档后代码** | 涉及 §21 未定规则时，先在 `Documentation/` 写系统短文再实现 |
| **固定上下文入口** | 每条任务开头点名：核心设计约束 + 对应 zh-CN 文档 + 入口类 |
| **Ask / Plan → Agent** | 架构与拍板用 Plan；落地改代码用 Agent |
| **Rules 固化约束** | 把 §22 十二条约束与「禁止改第三方包」写入 `.cursor/rules` |
| **验收清单进提示词** | 每任务附带「验收」表中的可勾选项 |
| **改完同步文档** | 行为变更必须更新 `05-core-systems` / `06-data-and-save` / 本文进度表 |

### 6.2 建议的 Cursor Rules（摘要）

可落盘为 `.cursor/rules/galactic-frontier-mvp.mdc`（需时再创建）：

- 第一方代码仅改 `Assets/Resources/Scripts`、`Prefabs`、`Scenes`、`data`、`Documentation`  
- 战斗保持全自动回合制  
- 卡组固定 5 人；占用规则必须可单测  
- 禁止新增 FakeData 污染正式存档路径  
- 重命名序列化字段用 `[FormerlySerializedAs]`；保留 `.meta`  
- 不提交 `Library/`、`Temp/`、`Logs/`  

### 6.3 标准任务提示词模板

```text
【背景】
阅读：
- 02-core-product-design.md（相关小节）
- 10-mvp-development-plan.md（当前阶段任务 ID）
- <相关专题>.md
- 入口类：<列出 2–4 个文件>

【任务】
实现 <P?-?>：<一句话目标>

【约束】
- 遵守核心设计 §22
- 最小改动；不改第三方包
- 存档兼容 / 说明迁移
- 完成后更新相关文档与本计划进度表状态

【验收】
- <可观察行为 1>
- <可观察行为 2>
- git diff --check 通过
```

### 6.4 各阶段优先打开的文件

| 阶段 | 优先阅读 |
| --- | --- |
| P0 | `DataUtil.cs`、`InventoryItemManagerBase.cs`、`BattleController.cs`、`06-data-and-save.md` |
| P1 | `LineupManager.cs`、`FormationScreen.cs`、`CardEntity.cs`、`CardListManager.cs` |
| P2 | `ExploreScreen.cs`、`PlanetListManager.cs`、`BattleController.cs`、`GameStatusManager.cs` |
| P3 | `ItemCatalog`、`ProductionService`、`CraftingScreen`、`InventoryScreen`、`15-economy.md` |
| P4 | `MarketScreen`、`NpcShopService`、`CurrencyService`、`16-market-and-card-trade.md` |
| P5 | `17-sector-and-onboarding.md`（先写）、`AppShell` Missions、`CardDataManager.cs`、`Character` 子类、`17-sector-and-onboarding.md` |

### 6.5 不建议交给 Cursor「一次做完」的事项

- 一次性实现全服市场后端 + 反作弊全套  
- 一次性写 50 个角色完整技能与数值  
- 大规模重写 MainScene 架构同时加玩法  
- 无规则文档的情况下硬编码品质/离线数值  

这些应拆成「接口 → 本地 Mock → 内容批 → 联网」多会话完成。

---

## 7. 系统文档与 PRODUCT-STATUS

核心设计 §21 细则落在 `Documentation/zh-CN/` 编号 **11–20** 规则文档。

- **版本 / 实现状态 / 验收完成度** → [PRODUCT-STATUS.md](PRODUCT-STATUS.md)  
- **文档索引** → [README.md](README.md)

### 7.1 仍待写 / 可扩展

| 文档 | 说明 |
| --- | --- |
| （并入 20）星域特殊模式细则 | 虫洞/暗面/Boss 房；方向见核心设计 §7.10 |
| （可选）research-and-transit | Research / Transit 完整循环 |

### 7.2 使用规则

1. 实现前读 **PRODUCT-STATUS**。  
2. 改规则语义 → 升对应文档 + 更新 PRODUCT-STATUS；必要时回写 `02` §20/§22。  
3. 数值进配置表。  
4. P0：先读 `06-data-and-save.md`。  
5. 调试：读 `07-development-guide.md`（含 Debug 章）。

---

## 8. 里程碑定义（便于排期）

| 里程碑 | 玩家可感知结果 | 依赖 | 状态 |
| --- | --- | --- | --- |
| **M0 可迭代原型** | 进出战斗稳定、存档不脏 | P0 | **已达成** |
| **M1 有限角色决策** | 两支队伍不能抢同一角色 | P1 | **已达成** |
| **M2 开图循环** | 通关区域 → 舰船门槛 → 挂机刷取 | P2 | **已达成** |
| **M3 经济自转** | 采集→制造→修装备形成材料消耗 | P3 | **已达成** |
| **M4 交易闭环** | NPC 可买可卖；经济可读 | P4 | **已达成** |
| **M5 MVP 可演示** | ~2 小时新手 + 内容量达标（纯单机） | P5 + 前序 | **接近**：引导/星域/招募/36 人/Explore M1 已通；可选扩至 50 + 分解/原生卡册 |

---

## 9. 进度维护约定

每完成一个 P 级子任务，更新：

1. **[PRODUCT-STATUS.md](PRODUCT-STATUS.md)** §3–§4（唯一状态源）  
2. 对应 [05-core-systems.md](05-core-systems.md) 实现说明  
3. 若引入新模块，在 [04-architecture.md](04-architecture.md) 增加目录说明  
4. 若发现新坑，记入 [08-known-issues.md](08-known-issues.md)  

建议在 Bridge 或内部 Debug 面板显示当前里程碑标签（如 `Build: M5 (near)`），方便试玩反馈对齐版本。

---

## 10. 近期建议执行顺序（立刻可开的 Cursor 任务）

P0–P4 与 M0–M4 已闭合；**P5 主切片与 Explore M1 / 物品 UX 补丁已完成**（快照 2026-08-23）。下一批：

1. ~~**P5.0–P5.5 / P5.1b / 抽卡正式路径**~~ **已完成**  
2. ~~**Explore M1**（单星域 2D + 雷达 + 巡航 + 已探索列表）~~ **已完成**（状态见 PRODUCT-STATUS §5）  
3. （可选 MVP 收尾）继续角色扩至 50（对照 [19](19-characters-and-progression.md)）  
4. ~~（可选 MVP 收尾）卡牌分解 + Characters/Cards 原生页（对照 [19](19-characters-and-progression.md)）~~ **已完成**
5. （可选）抽卡概率公示细节 / 十连折扣
6. **功能逐步解锁** + 导航 grey out + 解锁 Dialog（[09](09-figma-ui.md)）
7. **统一 Snackbar / Notification 栏**（Dialog 族与 `RewardPopup` 对齐）
8. **玩家资料**：Settings 改 `playerName` / `avatar.png`；`playerID` 只读（[06](06-data-and-save.md)）
9. **舰桥 UI**：删顶栏并行统计卡；RunningOps → 舰队水平滑动组件（[09](09-figma-ui.md)、[21](21-fleet-factions-and-exploration.md) §3.5）
10. （后 MVP · 规则已拍板）自动化流水线按 [15](15-economy.md) §4.10 立项  
11. （后 MVP · 规则已拍板）舰队 / 探索度按 [21](21-fleet-factions-and-exploration.md) 立项  
12. （后 MVP）星图 M2+ 按 [20](20-stellar-map-and-navigation.md) §11；Online 按 [18](18-online-and-play-modes.md)  

**不要**在 Solo 启动玩家市场或特殊星域模式（非 MVP）。

建议 Bridge / Debug 标签：`Build: M5 (near)`。

---

## 11. 参考索引

| 文档 | 用途 |
| --- | --- |
| [PRODUCT-STATUS.md](PRODUCT-STATUS.md) | 实现状态唯一权威 |
| [02-core-product-design.md](02-core-product-design.md) | 玩法约束 / MVP 范围 |
| [03-worldbuilding.md](03-worldbuilding.md) | 世界观与故事 |
| [05-core-systems.md](05-core-systems.md) | 代码实现与类职责 |
| [06-data-and-save.md](06-data-and-save.md) | 存档路径与契约 |
| [07-development-guide.md](07-development-guide.md) | 开发 / Agent / Debug |
| [08-known-issues.md](08-known-issues.md) | 技术债 |
| [19-characters-and-progression.md](19-characters-and-progression.md) | 抽卡 / 名册 / 能级 |
| [20-stellar-map-and-navigation.md](20-stellar-map-and-navigation.md) | 星图 / 航行（方向稿） |
| [21-fleet-factions-and-exploration.md](21-fleet-factions-and-exploration.md) | 舰队 / 阵营 / 探索度 |
