# 产品与文档状态（唯一权威）

> 更新日期：2026-08-29  
> **本文是唯一**记录实现状态 / 验收完成度的地方。规则文档不写「已落地」。  
> 叙事权威：[03-worldbuilding.md](03-worldbuilding.md)。舰队/探索度规则：[21-fleet-factions-and-exploration.md](21-fleet-factions-and-exploration.md)。

---

## 1. 如何使用

| 角色 | 阅读 |
| --- | --- |
| 新人 | [README](README.md) → 本文 → [03-worldbuilding](03-worldbuilding.md) |
| 实现 | 本文查状态 → 读 `11`–`21` 规则 → [10-mvp-development-plan](10-mvp-development-plan.md) |
| 改语义 | 升对应文档主版本 + 更新本文 +（若触 §20/§22）改 [02](02-core-product-design.md) |

**实现状态**：已实现 / 部分 / 未开始 / —（不适用）

---

## 2. MVP 总览

| 指标 | 值 |
| --- | --- |
| 相对 [02](02-core-product-design.md) §16.1 | **约 85%** |
| 核心循环 | 战斗→区域→采集/制造→NPC→成长，已打通 |
| 里程碑 | M0–M4 已达成；**M5 接近**（引导/星域/招募/36 人已通） |
| MVP 缺口 | 角色扩至 50（可选）、抽卡概率公示/十连折扣 |
| 后 MVP 缺口 | 舰队/探索度（21）、自动化流水线（15）、星图 M2+（20） |

---

## 3. 文档对照表

| # | 文档 | 类型 | 实现 | 验收 | 说明 |
| ---: | --- | --- | --- | --- | --- |
| 02 | [core-product-design](02-core-product-design.md) | 产品 | — | — | MVP 约束 |
| 03 | [worldbuilding](03-worldbuilding.md) | 设定 | — | — | **叙事权威 v1.0** |
| 04–09 | 工程文档 | 工程 | — | — | 描述现状代码 |
| 10 | [mvp-development-plan](10-mvp-development-plan.md) | 计划 | — | — | 阶段任务；近期顺序见 §10 |
| 11 | [deck-and-occupation](11-deck-and-occupation.md) | 规则 | 已实现 | 完成 | 多卡组占用 |
| 12 | [auto-battle](12-auto-battle.md) | 规则 | 部分 | 进行中 | 策略深度仍薄 |
| 13 | [region-and-ship](13-region-and-ship.md) | 规则 | 已实现 | 进行中 | 硬门/Boss；模块升级弹窗+仓库扣费已通 |
| 14 | [idle-and-offline](14-idle-and-offline.md) | 规则 | 部分 | 进行中 | yield/cap；采集仓 Gather Bank 已接 |
| 15 | [economy](15-economy.md) | 规则 | 部分 | 进行中 | 手动制造+物品说明/获取提示已通；**流水线未实现** |
| 16 | [market-and-card-trade](16-market-and-card-trade.md) | 规则 | 部分 | 进行中 | Solo NPC；玩家市场 Online |
| 17 | [sector-and-onboarding](17-sector-and-onboarding.md) | 内容 | 部分 | 进行中 | 五步+RewardService；星域叙事可再厚 |
| 18 | [online-and-play-modes](18-online-and-play-modes.md) | 方向 | — | — | 位面/Hub；MVP 不做 |
| 19 | [characters-and-progression](19-characters-and-progression.md) | 内容 | 部分 | 进行中 | 抽卡/约 36 人已通；Characters/Cards 原生页 + 分解已通；能级未接 UI |
| 20 | [stellar-map-and-navigation](20-stellar-map-and-navigation.md) | 方向 | 部分 | 进行中 | **M1** 单星域 2D 图+雷达+巡航+已探索列表已通；M2–M4 未开始 |
| 21 | [fleet-factions-and-exploration](21-fleet-factions-and-exploration.md) | 规则 | 未开始 | — | 舰队/卡关/探索度设定已拍板 |

---

## 4. MVP §16.1 快照

| 必含项 | 状态 |
| --- | --- |
| 全自动战斗 / 5 人多卡组占用 | 已实现 |
| 区域+舰船门 / 挂机采集制造 | 已实现 |
| 离线 cap+yield / 品质 / 耐久 | 已实现 |
| NPC 商店 / 6 区首领 / 新手五步 | 已实现 |
| 36 角色+抽卡 | 部分（约 36/50；原生卡册与分解已通） |
| 玩家市场 | Online only |
| 舰队 / 流水线 / 探索度 | 未开始（见 15 / 20 / **21**） |

---

## 5. 近期实现摘录（相对代码核对）

> 供审计用；不以本表代替 §3。证据以仓库代码为准。

| 主题 | 状态 | 主要证据 |
| --- | --- | --- |
| Explore M1 星域小地图 | 已实现（M1） | `ExploreScreen` + `NavigationService` + `SectorMapCatalog`；舰船坐标 `navX/navY`；`knownBodyIds`；紧凑雷达 + 已探索列表 |
| 物品双语说明 + 获取提示 | 已实现 | `ItemDef.descriptionEn/Zh`；`ItemAcquireCatalog`；`ItemTooltip` |
| 舰船模块升级 UI | 已实现 | `ModuleUpgradePopup`；`ShipService` 走仓库扣费 |
| 加载遮罩 / 奖励弹窗 / 采集仓 | 已实现 | `LoadingOverlay`；`RewardPopup`；Gather Bank |
| Characters / Cards 原生页 | 已实现 | `CharactersScreen` / `CardsScreen`；nav 不再走 Legacy |
| 卡牌分解 | 已实现 | `DismantleRules` + `CardDismantleService`；空闲未绑定实例可分解 |
| 功能逐步解锁 + 导航 grey out | 已实现 | `FeatureUnlockService` + `FeatureUnlockCatalog.json`；未解锁导航灰显 |
| 统一 Dialog / Snackbar / Notification | 已实现 | `NexusDialog` / `NexusSnackbar` / `NexusNotificationBar` |
| 玩家名称 / 头像自定义 | 已实现 | Settings 改 `playerName` / `avatar.png`；`playerID` 只读 |
| 舰桥舰队滑动组件 | 已实现 | 顶栏三统计；RunningOps → Fleet Carousel（旗舰卡 + 泊位卡） |
| 自动化流水线 | 未开始 | 规则在 [15](15-economy.md) §4.10；无 `ProductionLine` 领域代码 |
| 舰队 / 探索度 | 未开始 | 规则在 [21](21-fleet-factions-and-exploration.md) |
| 星图 M2+（海图/信标/多团块） | 未开始 | 方向稿 [20](20-stellar-map-and-navigation.md) §11 |

---

## 6. 维护规则

1. 实现进度**只改本文**。  
2. 世界观变更 → 升 [03-worldbuilding](03-worldbuilding.md) 主版本。  
3. 完整索引 → [README.md](README.md)。  
4. 路线图与 Cursor 任务切片 → [10-mvp-development-plan](10-mvp-development-plan.md)（§2 历史表不逐条同步）。
