# 产品与文档状态（唯一权威）

> 更新日期：2026-08-23  
> 用途：**本文是唯一**记录「文档成熟度 / 代码实现 / 验收完成度」的地方。  
> 系统规则文档（`systems/*`）只写**规则与设计标准**，不在文首声称「已落地」。

---

## 1. 如何使用

| 角色 | 阅读顺序 |
| --- | --- |
| 新人 | [README](README.md) → 本文 §3 → 对应 `systems/*` 规则 |
| 实现功能 | 本文查实现状态 → 读规则文档 → [11-mvp-development-plan](11-mvp-development-plan.md) 查阶段任务 |
| 改规则语义 | 升规则文档**主版本** + 回写 [01-core-product-design](01-core-product-design.md)（若触及 §20/§22）+ 更新本文 |

**状态定义**

| 文档类型 | 含义 |
| --- | --- |
| **规则** | 玩法语义已拍板；数值可进配置表 |
| **内容契约** | 物品/配方/区域等内容表权威 |
| **设定** | 世界观与叙事用语 |
| **方向稿** | 长期方向；非 MVP 承诺 |

| 实现状态 | 含义 |
| --- | --- |
| **已实现** | 核心路径可在游戏中跑通 |
| **部分实现** | 有骨架或主路径，仍有文档 § 设计验收标准 未全覆盖 |
| **未开始** | 无有效领域逻辑 |
| **不适用** | Online / 后 MVP，单机不验收 |

---

## 2. MVP 总览

| 指标 | 当前值 |
| --- | --- |
| 相对 [01-core-product-design](01-core-product-design.md) §16.1 | **约 80%–85%** |
| 单机核心循环 | 战斗 → 区域 → 采集/制造 → NPC 经济 → 成长，已打通 |
| 主要缺口 | 角色继续扩至 50（可选）、抽卡分解、Characters/Cards 原生页、后 MVP 设定（舰队/流水线/星图） |

详细阶段任务与 Cursor 方法 → [11-mvp-development-plan.md](11-mvp-development-plan.md)。

---

## 3. 文档与实现对照表

| 文档 | 版本 | 类型 | 阶段 | 实现状态 | 验收 | 说明 |
| --- | --- | --- | --- | --- | --- | --- |
| [systems/00-setting-and-lore](systems/00-setting-and-lore.md) | v1.1 | 设定 | 全程 | 不适用 | — | 叙事权威；含手工工坊/流水线设定摘要 |
| [systems/01-deck-and-occupation](systems/01-deck-and-occupation.md) | v1.2 | 规则 | P1 | **已实现** | 完成 | 多卡组、占用、ActionScheduler |
| [systems/02-auto-battle](systems/02-auto-battle.md) | v1.0 | 规则 | P0/P2 | **部分实现** | 进行中 | 自动战斗通；站位/策略深度仍薄 |
| [systems/03-region-and-ship](systems/03-region-and-ship.md) | v1.2 | 规则 | P2 | **已实现** | 进行中 | world/ship、硬门、Boss；文档验收项待逐项勾选归档 |
| [systems/04-idle-and-offline](systems/04-idle-and-offline.md) | v1.1 | 规则 | P2/P3 | **部分实现** | 进行中 | 离线 cap/yield 已通；部分边界用例待补 |
| [systems/05-production-and-quality](systems/05-production-and-quality.md) | v1.1 | 规则 | P3 | **部分实现** | 进行中 | 手动制造链已通；**自动化流水线**仅规则、未实现 |
| [systems/06-durability-and-repair](systems/06-durability-and-repair.md) | v1.0 | 规则 | P3 | **已实现** | 完成 | 耐久/维修/不销毁 |
| [systems/07-market-and-card-trade](systems/07-market-and-card-trade.md) | v1.1 | 规则 | P4 | **部分实现** | 进行中 | Solo NPC 商店已通；玩家市场 **不适用** MVP |
| [systems/08-save-and-seed-data](systems/08-save-and-seed-data.md) | v1.0 | 规则 | P0 | **部分实现** | 进行中 | meta/迁移/Starter Seed；部分字段随 P5 扩展 |
| [systems/09-resources-and-warehouse](systems/09-resources-and-warehouse.md) | v1.3 | 内容契约 | P3 | **部分实现** | 进行中 | 资源/配方表已用；流水线内容表（§9）未实现 |
| [systems/10-onboarding-and-missions](systems/10-onboarding-and-missions.md) | v1.2 | 规则 | P5 | **部分实现** | 进行中 | 五步引导 + Missions 已通；订单/Bureau 可增强 |
| [systems/11-sector-and-region-content](systems/11-sector-and-region-content.md) | v1.1 | 内容契约 | P2/P5 | **部分实现** | 进行中 | RewardService/采集节点已通；Gate/阵营列待内容补全 |
| `systems/12-sector-special-modes.md` | — | 方向稿 | 后置 | **未开始** | — | 待写 |
| [systems/14-play-modes-and-persistence](systems/14-play-modes-and-persistence.md) | v0.2 | 方向稿 | Online | **不适用** | — | 存档互通方案 |
| [systems/15-gacha-and-progression](systems/15-gacha-and-progression.md) | v1.1 | 规则 | P5 | **部分实现** | 进行中 | 招募券抽卡已通；分解/软保底等待补 |
| [systems/16-debug-and-test-mode](systems/16-debug-and-test-mode.md) | v1.1 | 规则 | 全程 | **已实现** | 进行中 | Debug 页/礼品码；QA 清单随版本维护 |
| [systems/17-online-multiverse-cooperation](systems/17-online-multiverse-cooperation.md) | v0.1 | 方向稿 | Online | **不适用** | — | 位面/Hub/公会 |
| [systems/18-character-roster-and-lore](systems/18-character-roster-and-lore.md) | v1.0 | 内容契约 | P5 | **部分实现** | 进行中 | 36 人叙事/技能；向 50 扩可选 |
| [systems/19-card-energy-rank](systems/19-card-energy-rank.md) | v1.2 | 规则 | P5 | **未开始** | — | 规则已写；能级 UI/消耗未接 |
| [systems/20-stellar-map-and-navigation](systems/20-stellar-map-and-navigation.md) | v0.1 | 方向稿 | 后 MVP | **部分实现** | — | Explore 2D 地图已落地；探索度/舰队等规则未实现 |
| [01-core-product-design](01-core-product-design.md) | v0.6 | 规则 | 全程 | 不适用 | — | 产品约束与 MVP 范围真相源 |
| [11-mvp-development-plan](11-mvp-development-plan.md) | v2.1 | — | 全程 | 不适用 | — | 阶段路线图与 Cursor 工作法（非规则正文） |

**验收列说明**：「完成」= 该文档 §设计验收标准 主路径已在代码与测试中覆盖；「进行中」= 仍有未关项或后 MVP 规则未实现；「—」= 非实现类文档。

---

## 4. MVP §16.1 必含项（实现快照）

| 必含项 | 实现状态 | 主要证据 |
| --- | --- | --- |
| 全自动回合制战斗 | 已实现 | `BattleController` |
| 5 人卡组 + 多卡组 + 占用 | 已实现 | `DeckService`, `ActionScheduler` |
| 区域推进 + 舰船门 | 已实现 | `WorldService`, `ShipGate` |
| 挂机刷取 / 采集 / 制造并行 | 已实现 | `IdleEconomyTicker`, `ProductionService` |
| 离线收益 cap + yield 比例 | 已实现 | `IdleSettlementService` |
| 15–20 资源 + 3 生产链 + 品质 | 已实现 | `ItemCatalog`, `RecipeCatalog`, `QualityRules` |
| 耐久与维修 | 已实现 | `DurabilityService` |
| NPC 商店（Solo） | 已实现 | `NpcShopService`, `MarketScreen` |
| 6 区域 + 首领 | 已实现 | `RegionCatalog`, `world.json` |
| 新手五步 | 已实现 | `OnboardingService`, `MissionsScreen` |
| 36 可战斗角色 + 抽卡 | 部分实现 | `CardDataManager`, `GachaService` |
| 玩家市场 / 卡牌交易 | 不适用 | Online only |
| 舰队 / 流水线 / 探索度 | 未开始 | 仅设定文档 |

---

## 5. 后 MVP 设定（仅文档，未实现）

下列内容已在设定/规则文档中拍板，**不计入**当前 MVP 实现完成度：

- 舰队：一舰一编队、驻扎采集（`21-fleet-*` 若存在；或 `00`/`05` 摘要）
- 自动化流水线（`05` §4.10、`09` §9）
- 星域探索度、非线性卡关区（`20`、`00`）
- Online：玩家市场、多元宇宙位面（`07`、`14`、`17`）

---

## 6. 维护规则

1. **禁止**在 `systems/*` 文首写「P× 已落地 / 已完成」；只保留 `文档版本`、`文档类型`、约束与变更摘要。  
2. 实现进度变化时**只改本文** §3–§4，并必要时更新 [11-mvp-development-plan](11-mvp-development-plan.md) 阶段表。  
3. 规则语义变更 → 升对应系统文档主版本 + 更新本文版本号与说明。  
4. 各系统文档 §「设计验收标准」为**可测试条目**，不在此重复勾选；关项时更新本文 §3「验收」列。

---

## 7. 文档索引（无状态列）

| 分类 | 文档 |
| --- | --- |
| 产品与进度 | [01-core-product-design](01-core-product-design.md)、**本文**、[11-mvp-development-plan](11-mvp-development-plan.md) |
| 工程 | [01-quick-start](01-quick-start.md) … [10-core-classes](10-core-classes.md)、[08-known-issues](08-known-issues.md) |
| 系统规则 | [systems/00](systems/00-setting-and-lore.md)–[systems/20](systems/20-stellar-map-and-navigation.md)（见 §3 表） |

完整阅读路径 → [README.md](README.md)。
