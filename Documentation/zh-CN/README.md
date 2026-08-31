# Galactic Frontier 中文开发文档

本目录为**唯一**中文文档集（共 **23** 篇编号文档 + 状态表）。若与代码冲突，以代码为准，并同步 [PRODUCT-STATUS.md](PRODUCT-STATUS.md)。

## 必读入口

| 文档 | 用途 |
| --- | --- |
| **[PRODUCT-STATUS.md](PRODUCT-STATUS.md)** | 实现与验收状态（唯一权威） |
| [02-core-product-design.md](02-core-product-design.md) | 玩法约束与 MVP 范围 |
| [03-worldbuilding.md](03-worldbuilding.md) | **世界观与故事（叙事权威）** |
| [10-mvp-development-plan.md](10-mvp-development-plan.md) | 阶段路线图 |

## 文档总表（01–23）

| # | 文档 | 类别 |
| ---: | --- | --- |
| 01 | [quick-start](01-quick-start.md) | 工程 |
| 02 | [core-product-design](02-core-product-design.md) | 产品 |
| 03 | [worldbuilding](03-worldbuilding.md) | **设定** |
| 04 | [architecture](04-architecture.md) | 工程（结构+架构） |
| 05 | [core-systems](05-core-systems.md) | 工程（实现+类图） |
| 06 | [data-and-save](06-data-and-save.md) | 工程/规则（存档+种子） |
| 07 | [development-guide](07-development-guide.md) | 工程（开发+Agent+Debug） |
| 08 | [known-issues](08-known-issues.md) | 工程 |
| 09 | [figma-ui](09-figma-ui.md) | 工程 |
| 10 | [mvp-development-plan](10-mvp-development-plan.md) | 计划 |
| 11 | [deck-and-occupation](11-deck-and-occupation.md) | 规则 |
| 12 | [auto-battle](12-auto-battle.md) | 规则 |
| 13 | [region-and-ship](13-region-and-ship.md) | 规则 |
| 14 | [idle-and-offline](14-idle-and-offline.md) | 规则 |
| 15 | [economy](15-economy.md) | 规则（生产+资源+耐久+流水线） |
| 16 | [market-and-card-trade](16-market-and-card-trade.md) | 规则 |
| 17 | [sector-and-onboarding](17-sector-and-onboarding.md) | 内容（新手+星域） |
| 18 | [online-and-play-modes](18-online-and-play-modes.md) | 方向稿 |
| 19 | [characters-and-progression](19-characters-and-progression.md) | 内容（抽卡+名册+能级） |
| 20 | [stellar-map-and-navigation](20-stellar-map-and-navigation.md) | 规则（航网/星图；M2 域内雏形已落地，宇宙层为 M3+） |
| 21 | [fleet-factions-and-exploration](21-fleet-factions-and-exploration.md) | 规则（舰队/阵营/探索度） |
| 22 | [ux-loop-and-ia](22-ux-loop-and-ia.md) | **产品（P6 指挥官循环 / IA / UX 契约）** |
| 23 | [procedural-universe-generation](23-procedural-universe-generation.md) | **规则（种子航网生成 + 验证器；M3+）** |

## 推荐阅读

1. [01-quick-start](01-quick-start.md) → [PRODUCT-STATUS](PRODUCT-STATUS.md)  
2. [03-worldbuilding](03-worldbuilding.md)（故事与主题）  
3. [02-core-product-design](02-core-product-design.md)（玩法硬约束）  
4. [04-architecture](04-architecture.md) → [05-core-systems](05-core-systems.md) → [06-data-and-save](06-data-and-save.md)  
5. 实现玩法：PRODUCT-STATUS §3 → 对应 `11`–`23`

## 合并说明（相对旧树）

| 新文档 | 合并自 |
| --- | --- |
| 03-worldbuilding | 旧设定稿 + **Worldbuilding v1.0** |
| 04-architecture | project-structure + architecture |
| 05-core-systems | core-systems + core-classes |
| 06-data-and-save | data-and-save + save-and-seed |
| 07-development-guide | development + codex + debug |
| 15-economy | production + resources + durability |
| 17-sector-and-onboarding | onboarding + sector-content |
| 18-online-and-play-modes | play-modes + online-multiverse |
| 19-characters-and-progression | gacha + roster + energy-rank |
| 21-fleet-factions-and-exploration | 自 `systems/21`（master）迁入根目录 |

## 环境基线

| 项目 | 值 |
| --- | --- |
| Unity | `6000.0.20f1` |
| MVP 进度 | 见 [PRODUCT-STATUS](PRODUCT-STATUS.md)（约 **85%**） |
