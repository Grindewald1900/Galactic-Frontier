# 产品与文档状态（唯一权威）

> 更新日期：2026-08-23  
> 用途：**本文是唯一**记录「文档成熟度 / 代码实现 / 验收完成度」的地方。  
> 规则文档（`14`–`32`）只写**规则与设计标准**，不在文首声称「已落地」。

---

## 1. 如何使用

| 角色 | 阅读顺序 |
| --- | --- |
| 新人 | [README](README.md) → 本文 §3 → 对应编号规则文档 |
| 实现功能 | 本文查实现状态 → 读规则文档 → [13-mvp-development-plan](13-mvp-development-plan.md) |
| 改规则语义 | 升规则文档主版本 + 回写 [02-core-product-design](02-core-product-design.md)（若触及 §20/§22）+ 更新本文 |

**文档类型**：规则 / 内容契约 / 设定 / 方向稿 / 工程 / 计划 — 见 [README 总表](README.md#文档编号总表)。

**实现状态**：已实现 / 部分实现 / 未开始 / 不适用（Online 或后 MVP）。

---

## 2. MVP 总览

| 指标 | 当前值 |
| --- | --- |
| 相对 [02-core-product-design](02-core-product-design.md) §16.1 | **约 80%–85%** |
| 单机核心循环 | 战斗 → 区域 → 采集/制造 → NPC 经济 → 成长，已打通 |
| 主要缺口 | 角色扩至 50（可选）、抽卡分解、Cards 原生页、后 MVP 设定（舰队/流水线/星图） |

阶段任务 → [13-mvp-development-plan.md](13-mvp-development-plan.md)。

---

## 3. 文档与实现对照表

| 编号 | 文档 | 版本 | 类型 | 阶段 | 实现 | 验收 | 说明 |
| ---: | --- | --- | --- | --- | --- | --- | --- |
| 02 | [core-product-design](02-core-product-design.md) | v0.6 | 产品 | 全程 | — | — | MVP 范围与 §22 约束 |
| 03 | [setting-and-lore](03-setting-and-lore.md) | v1.1 | 设定 | 全程 | — | — | 叙事；手工工坊/流水线摘要 |
| 13 | [mvp-development-plan](13-mvp-development-plan.md) | v2.1 | 计划 | 全程 | — | — | 阶段路线图 |
| 14 | [deck-and-occupation](14-deck-and-occupation.md) | v1.2 | 规则 | P1 | 已实现 | 完成 | 多卡组、占用 |
| 15 | [auto-battle](15-auto-battle.md) | v1.0 | 规则 | P0/P2 | 部分 | 进行中 | 自动战斗通；策略仍薄 |
| 16 | [region-and-ship](16-region-and-ship.md) | v1.2 | 规则 | P2 | 已实现 | 进行中 | world/ship、硬门、Boss |
| 17 | [idle-and-offline](17-idle-and-offline.md) | v1.1 | 规则 | P2/P3 | 部分 | 进行中 | 离线 cap/yield |
| 18 | [production-and-quality](18-production-and-quality.md) | v1.1 | 规则 | P3 | 部分 | 进行中 | 手动制造通；流水线未实现 |
| 19 | [durability-and-repair](19-durability-and-repair.md) | v1.0 | 规则 | P3 | 已实现 | 完成 | 耐久/维修 |
| 20 | [market-and-card-trade](20-market-and-card-trade.md) | v1.1 | 规则 | P4 | 部分 | 进行中 | Solo NPC；玩家市场不适用 |
| 21 | [save-and-seed-data](21-save-and-seed-data.md) | v1.0 | 规则 | P0 | 部分 | 进行中 | meta/迁移/Starter Seed |
| 22 | [resources-and-warehouse](22-resources-and-warehouse.md) | v1.3 | 内容 | P3 | 部分 | 进行中 | 资源/配方表；流水线表未实现 |
| 23 | [onboarding-and-missions](23-onboarding-and-missions.md) | v1.2 | 规则 | P5 | 部分 | 进行中 | 五步引导 + Missions |
| 24 | [sector-and-region-content](24-sector-and-region-content.md) | v1.1 | 内容 | P2/P5 | 部分 | 进行中 | RewardService/采集节点 |
| 25 | `sector-special-modes` | — | 方向 | 后置 | 未开始 | — | 待写 |
| 26 | [play-modes-and-persistence](26-play-modes-and-persistence.md) | v0.2 | 方向 | Online | — | — | 存档互通 |
| 27 | [gacha-and-progression](27-gacha-and-progression.md) | v1.1 | 规则 | P5 | 部分 | 进行中 | 抽卡通；分解待补 |
| 28 | [debug-and-test-mode](28-debug-and-test-mode.md) | v1.1 | 规则 | 全程 | 已实现 | 进行中 | Debug/礼品码 |
| 29 | [online-multiverse-cooperation](29-online-multiverse-cooperation.md) | v0.1 | 方向 | Online | — | — | 位面/Hub |
| 30 | [character-roster-and-lore](30-character-roster-and-lore.md) | v1.0 | 内容 | P5 | 部分 | 进行中 | 36 人叙事/技能 |
| 31 | [card-energy-rank](31-card-energy-rank.md) | v1.2 | 规则 | P5 | 未开始 | — | 规则已写；UI 未接 |
| 32 | [stellar-map-and-navigation](32-stellar-map-and-navigation.md) | v0.1 | 方向 | 后 MVP | 部分 | — | Explore 2D 已落地 |

工程文档（`01`、`04`–`12`）描述代码与流程，实现状态见 §4 或各文档正文。

---

## 4. MVP §16.1 必含项（实现快照）

| 必含项 | 状态 | 主要证据 |
| --- | --- | --- |
| 全自动战斗 | 已实现 | `BattleController` |
| 5 人卡组 + 占用 | 已实现 | `DeckService`, `ActionScheduler` |
| 区域 + 舰船门 | 已实现 | `WorldService`, `ShipGate` |
| 采集/制造/挂机并行 | 已实现 | `IdleEconomyTicker`, `ProductionService` |
| 离线 cap + yield | 已实现 | `IdleSettlementService` |
| 资源链 + 品质 | 已实现 | `ItemCatalog`, `QualityRules` |
| 耐久维修 | 已实现 | `DurabilityService` |
| NPC 商店 | 已实现 | `NpcShopService` |
| 6 区 + 首领 | 已实现 | `RegionCatalog` |
| 新手五步 | 已实现 | `OnboardingService` |
| 36 角色 + 抽卡 | 部分 | `GachaService` |
| 玩家市场 | 不适用 | Online |
| 舰队/流水线/探索度 | 未开始 | 仅文档 |

---

## 5. 后 MVP（仅文档）

- 自动化流水线 → [18](18-production-and-quality.md) §4.10、[22](22-resources-and-warehouse.md) §9  
- 星域探索度 / 舰队 → [32](32-stellar-map-and-navigation.md)、[03](03-setting-and-lore.md)  
- Online → [20](20-market-and-card-trade.md)、[26](26-play-modes-and-persistence.md)、[29](29-online-multiverse-cooperation.md)

---

## 6. 维护规则

1. **禁止**在 `14`–`32` 文首写「已落地」；实现状态**只改本文** §3–§4。  
2. 规则语义变更 → 升对应文档主版本 + 更新本文。  
3. 完整编号索引 → [README.md](README.md#文档编号总表)。
