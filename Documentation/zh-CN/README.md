# Galactic Frontier 中文开发文档

本目录为**唯一**中文文档集。若与代码冲突，以代码为准，并同步更新 [PRODUCT-STATUS.md](PRODUCT-STATUS.md)。

## 必读入口

| 文档 | 用途 |
| --- | --- |
| **[PRODUCT-STATUS.md](PRODUCT-STATUS.md)** | 唯一权威：版本、实现状态、验收完成度 |
| [02-core-product-design.md](02-core-product-design.md) | 产品约束与 MVP 范围 |
| [13-mvp-development-plan.md](13-mvp-development-plan.md) | 阶段路线图与 Cursor 工作法 |

## 文档编号总表

| 编号 | 文档 | 类别 |
| ---: | --- | --- |
| 01 | [quick-start](01-quick-start.md) | 工程 |
| 02 | [core-product-design](02-core-product-design.md) | 产品 |
| 03 | [setting-and-lore](03-setting-and-lore.md) | 设定 |
| 04 | [project-structure](04-project-structure.md) | 工程 |
| 05 | [architecture](05-architecture.md) | 工程 |
| 06 | [core-systems](06-core-systems.md) | 工程 |
| 07 | [data-and-save](07-data-and-save.md) | 工程 |
| 08 | [development-guide](08-development-guide.md) | 工程 |
| 09 | [codex-guide](09-codex-guide.md) | 工程 |
| 10 | [known-issues](10-known-issues.md) | 工程 |
| 11 | [figma-ui](11-figma-ui.md) | 工程 |
| 12 | [core-classes](12-core-classes.md) | 工程 |
| 13 | [mvp-development-plan](13-mvp-development-plan.md) | 计划 |
| 14 | [deck-and-occupation](14-deck-and-occupation.md) | 规则 |
| 15 | [auto-battle](15-auto-battle.md) | 规则 |
| 16 | [region-and-ship](16-region-and-ship.md) | 规则 |
| 17 | [idle-and-offline](17-idle-and-offline.md) | 规则 |
| 18 | [production-and-quality](18-production-and-quality.md) | 规则 |
| 19 | [durability-and-repair](19-durability-and-repair.md) | 规则 |
| 20 | [market-and-card-trade](20-market-and-card-trade.md) | 规则 |
| 21 | [save-and-seed-data](21-save-and-seed-data.md) | 规则 |
| 22 | [resources-and-warehouse](22-resources-and-warehouse.md) | 内容契约 |
| 23 | [onboarding-and-missions](23-onboarding-and-missions.md) | 规则 |
| 24 | [sector-and-region-content](24-sector-and-region-content.md) | 内容契约 |
| 25 | `sector-special-modes` | 方向稿（待写） |
| 26 | [play-modes-and-persistence](26-play-modes-and-persistence.md) | 方向稿 |
| 27 | [gacha-and-progression](27-gacha-and-progression.md) | 规则 |
| 28 | [debug-and-test-mode](28-debug-and-test-mode.md) | 规则 |
| 29 | [online-multiverse-cooperation](29-online-multiverse-cooperation.md) | 方向稿 |
| 30 | [character-roster-and-lore](30-character-roster-and-lore.md) | 内容契约 |
| 31 | [card-energy-rank](31-card-energy-rank.md) | 规则 |
| 32 | [stellar-map-and-navigation](32-stellar-map-and-navigation.md) | 方向稿 |

## 推荐阅读顺序

1. [01-quick-start](01-quick-start.md)
2. [PRODUCT-STATUS](PRODUCT-STATUS.md)
3. [03-setting-and-lore](03-setting-and-lore.md)
4. [04-project-structure](04-project-structure.md) → [05-architecture](05-architecture.md)
5. [06-core-systems](06-core-systems.md) → [07-data-and-save](07-data-and-save.md)
6. [08-development-guide](08-development-guide.md) → [09-codex-guide](09-codex-guide.md)
7. 实现玩法：PRODUCT-STATUS §3 → 对应 `14`–`32` 规则文档

## 项目一句话

`Galactic Frontier / 群星边境`：断航三百年后，作为群星开拓局舰长，在第七前沿推进区域、挂机采集制造、经星港 NPC 维持贸易的单人卡牌养成循环。

## 环境基线

| 项目 | 值 |
| --- | --- |
| Unity | `6000.0.20f1` |
| 渲染 | URP `17.0.3` |
| 代码 | `Assets/Resources/Scripts` |
| 配置 | `Assets/Resources/data` |
| MVP 进度 | [PRODUCT-STATUS §2](PRODUCT-STATUS.md#2-mvp-总览)（约 80%–85%） |

技术债 → [10-known-issues.md](10-known-issues.md)
