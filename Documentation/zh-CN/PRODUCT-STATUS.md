# 产品与文档状态（唯一权威）

> 更新日期：2026-08-23  
> **本文是唯一**记录实现状态 / 验收完成度的地方。规则文档不写「已落地」。  
> 叙事权威：[03-worldbuilding.md](03-worldbuilding.md)。舰队/探索度规则：[21-fleet-factions-and-exploration.md](21-fleet-factions-and-exploration.md)。

---

## 1. 如何使用

| 角色 | 阅读 |
| --- | --- |
| 新人 | [README](README.md) → 本文 → [03-worldbuilding](03-worldbuilding.md) |
| 实现 | 本文查状态 → 读 `11`–`20` 规则 → [10-mvp-development-plan](10-mvp-development-plan.md) |
| 改语义 | 升对应文档主版本 + 更新本文 +（若触 §20/§22）改 [02](02-core-product-design.md) |

**实现状态**：已实现 / 部分 / 未开始 / —（不适用）

---

## 2. MVP 总览

| 指标 | 值 |
| --- | --- |
| 相对 [02](02-core-product-design.md) §16.1 | **约 80%–85%** |
| 核心循环 | 战斗→区域→采集/制造→NPC→成长，已打通 |
| 缺口 | 角色扩至 50、抽卡分解、Cards 原生页；后 MVP：舰队/流水线/探索度 |

---

## 3. 文档对照表

| # | 文档 | 类型 | 实现 | 验收 | 说明 |
| ---: | --- | --- | --- | --- | --- |
| 02 | [core-product-design](02-core-product-design.md) | 产品 | — | — | MVP 约束 |
| 03 | [worldbuilding](03-worldbuilding.md) | 设定 | — | — | **叙事权威 v1.0** |
| 04–09 | 工程文档 | 工程 | — | — | 描述现状代码 |
| 10 | [mvp-development-plan](10-mvp-development-plan.md) | 计划 | — | — | 阶段任务 |
| 11 | [deck-and-occupation](11-deck-and-occupation.md) | 规则 | 已实现 | 完成 | 多卡组占用 |
| 12 | [auto-battle](12-auto-battle.md) | 规则 | 部分 | 进行中 | 策略深度仍薄 |
| 13 | [region-and-ship](13-region-and-ship.md) | 规则 | 已实现 | 进行中 | 硬门/Boss |
| 14 | [idle-and-offline](14-idle-and-offline.md) | 规则 | 部分 | 进行中 | yield/cap |
| 15 | [economy](15-economy.md) | 规则 | 部分 | 进行中 | 手动制造通；流水线未实现 |
| 16 | [market-and-card-trade](16-market-and-card-trade.md) | 规则 | 部分 | 进行中 | Solo NPC；玩家市场 Online |
| 17 | [sector-and-onboarding](17-sector-and-onboarding.md) | 内容 | 部分 | 进行中 | 五步+RewardService |
| 18 | [online-and-play-modes](18-online-and-play-modes.md) | 方向 | — | — | 位面/Hub |
| 19 | [characters-and-progression](19-characters-and-progression.md) | 内容 | 部分 | 进行中 | 抽卡通；能级未接 UI |
| 20 | [stellar-map-and-navigation](20-stellar-map-and-navigation.md) | 方向 | 部分 | — | Explore 2D 已有 |
| 21 | [fleet-factions-and-exploration](21-fleet-factions-and-exploration.md) | 规则 | 未开始 | — | 舰队/卡关/探索度设定已拍板 |

---

## 4. MVP §16.1 快照

| 必含项 | 状态 |
| --- | --- |
| 全自动战斗 / 5 人多卡组占用 | 已实现 |
| 区域+舰船门 / 挂机采集制造 | 已实现 |
| 离线 cap+yield / 品质 / 耐久 | 已实现 |
| NPC 商店 / 6 区首领 / 新手五步 | 已实现 |
| 36 角色+抽卡 | 部分 |
| 玩家市场 | Online only |
| 舰队 / 流水线 / 探索度 | 未开始（见 03 / 15 / 20 / **21**） |

---

## 5. 维护规则

1. 实现进度**只改本文**。  
2. 世界观变更 → 升 [03-worldbuilding](03-worldbuilding.md) 主版本。  
3. 完整索引 → [README.md](README.md)。
