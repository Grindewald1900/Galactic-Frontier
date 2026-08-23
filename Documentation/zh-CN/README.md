# Galactic Frontier 中文开发文档

本目录帮助开发者理解项目结构、规则约束与实现现状。若代码与文档冲突，**以代码为准**，并在同一次变更中更新文档与 [PRODUCT-STATUS.md](PRODUCT-STATUS.md)。

## 必读入口

| 文档 | 用途 |
| --- | --- |
| **[PRODUCT-STATUS.md](PRODUCT-STATUS.md)** | **唯一权威**：各文档版本、实现状态、验收完成度 |
| [01-core-product-design.md](01-core-product-design.md) | 产品约束与 MVP 范围 |
| [11-mvp-development-plan.md](11-mvp-development-plan.md) | 阶段路线图与 Cursor 工作法 |

## 推荐阅读顺序

1. [快速开始](01-quick-start.md)
2. **[PRODUCT-STATUS.md](PRODUCT-STATUS.md)** — 当前做到哪、哪份文档看规则
3. [设定与故事背景](systems/00-setting-and-lore.md)
4. [项目结构](02-project-structure.md) → [架构总览](03-architecture.md)
5. [核心系统实现](04-core-systems.md) → [数据与存档](05-data-and-save.md)
6. [开发与验证](06-development-guide.md) → [Codex 工作指南](07-codex-guide.md)
7. 实现某玩法前：在 PRODUCT-STATUS §3 查对应 `systems/*` 文档

## 系统规则文档

规则语义写在 `systems/`；**不在各文件文首写实现状态**。索引与实现对照 → [PRODUCT-STATUS.md §3](PRODUCT-STATUS.md#3-文档与实现对照表)。

| 文档 | 类型 |
| --- | --- |
| [00 设定与故事背景](systems/00-setting-and-lore.md) | 设定 |
| [01 卡组与角色占用](systems/01-deck-and-occupation.md) | 规则 |
| [02 全自动战斗](systems/02-auto-battle.md) | 规则 |
| [03 区域推进与舰船](systems/03-region-and-ship.md) | 规则 |
| [04 挂机与离线](systems/04-idle-and-offline.md) | 规则 |
| [05 生产链与品质](systems/05-production-and-quality.md) | 规则 |
| [06 耐久与维修](systems/06-durability-and-repair.md) | 规则 |
| [07 市场与卡牌交易](systems/07-market-and-card-trade.md) | 规则 |
| [08 存档与种子](systems/08-save-and-seed-data.md) | 规则 |
| [09 资源表与仓库](systems/09-resources-and-warehouse.md) | 内容契约 |
| [10 新手引导与任务](systems/10-onboarding-and-missions.md) | 规则 |
| [11 星域内容与掉落](systems/11-sector-and-region-content.md) | 内容契约 |
| [14 运行模式](systems/14-play-modes-and-persistence.md) | 方向稿 |
| [15 抽卡与成长](systems/15-gacha-and-progression.md) | 规则 |
| [16 Debug 与测试](systems/16-debug-and-test-mode.md) | 规则 |
| [17 线上多元宇宙](systems/17-online-multiverse-cooperation.md) | 方向稿 |
| [18 角色设定](systems/18-character-roster-and-lore.md) | 内容契约 |
| [19 卡牌能级](systems/19-card-energy-rank.md) | 规则 |
| [20 星域地图与航行](systems/20-stellar-map-and-navigation.md) | 方向稿 |

待写：`systems/12-sector-special-modes.md`

## 项目一句话

`Galactic Frontier / 群星边境`：断航三百年后，作为群星开拓局舰长，在第七前沿推进区域、挂机采集制造、经星港 NPC 维持贸易的单人卡牌养成循环（Unity 2D 原型）。

## 环境基线

| 项目 | 值 |
| --- | --- |
| Unity | `6000.0.20f1` |
| 渲染 | URP `17.0.3` |
| 代码 | `Assets/Resources/Scripts` |
| 配置 | `Assets/Resources/data` |
| MVP 进度 | 见 [PRODUCT-STATUS §2](PRODUCT-STATUS.md#2-mvp-总览)（约 80%–85%） |

技术债 → [08-known-issues.md](08-known-issues.md)
