# 产品与文档状态（唯一权威）

> 更新日期：2026-09-02  
> **本文是唯一**记录实现状态 / 验收完成度的地方。规则文档不写「已落地」。  
> 叙事权威：[03-worldbuilding.md](03-worldbuilding.md)（v1.3）。舰队/探索度：[21](21-fleet-factions-and-exploration.md)。航网探索：[20](20-stellar-map-and-navigation.md)。程序生成航网：[23](23-procedural-universe-generation.md)（M3 试点已落地）。首章前期 Wave A：[24](24-early-chapter-experience.md)。成长权威：[19](19-characters-and-progression.md) v1.1 三层自动成长。

---

## 1. 如何使用

| 角色 | 阅读 |
| --- | --- |
| 新人 | [README](README.md) → 本文 → [03-worldbuilding](03-worldbuilding.md) |
| 实现 | 本文查状态 → 读 `11`–`23` 规则 → [10-mvp-development-plan](10-mvp-development-plan.md) |
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
| 后 MVP 缺口 | 舰队/探索度（21）、自动化流水线（15）、星图 M3+（20：信标跃迁/完整宇宙交互）、程序生成航网深化（23：热力图/导出报告） |
| **P6 UX** | 指挥官循环 / 五级 IA / 缺口跳转 — 见 [22](22-ux-loop-and-ia.md)；**M6 未验收** |

---

## 3. 文档对照表

| # | 文档 | 类型 | 实现 | 验收 | 说明 |
| ---: | --- | --- | --- | --- | --- |
| 02 | [core-product-design](02-core-product-design.md) | 产品 | — | — | MVP 约束 |
| 03 | [worldbuilding](03-worldbuilding.md) | 设定 | — | — | **叙事权威 v1.3**（相位偏移 ↔ 种子航网，见 23；对齐自动成长） |
| 04–09 | 工程文档 | 工程 | — | — | 描述现状代码 |
| 10 | [mvp-development-plan](10-mvp-development-plan.md) | 计划 | — | — | 阶段任务；近期顺序见 §10 |
| 11 | [deck-and-occupation](11-deck-and-occupation.md) | 规则 | 已实现 | 完成 | 多卡组占用 |
| 12 | [auto-battle](12-auto-battle.md) | 规则 | 部分 | 进行中 | 策略深度仍薄 |
| 13 | [region-and-ship](13-region-and-ship.md) | 规则 | 已实现 | 进行中 | 硬门/Boss；模块升级弹窗+仓库扣费已通 |
| 14 | [idle-and-offline](14-idle-and-offline.md) | 规则 | 部分 | 进行中 | **v1.2** 废止 50% yield：**代码已改为有效时间内 100%**；cap 含舰长档；采集仓 Gather Bank 已接 |
| 15 | [economy](15-economy.md) | 规则 | 部分 | 进行中 | 手动制造+物品说明/获取提示已通；**流水线未实现**；领队技能门槛 + 团队能力已接 |
| 16 | [market-and-card-trade](16-market-and-card-trade.md) | 规则 | 部分 | 进行中 | Solo NPC；玩家市场 Online |
| 17 | [sector-and-onboarding](17-sector-and-onboarding.md) | 内容 | 部分 | 进行中 | 五步+RewardService；星域叙事可再厚 |
| 18 | [online-and-play-modes](18-online-and-play-modes.md) | 方向 | — | — | 位面/Hub；MVP 不做 |
| 19 | [characters-and-progression](19-characters-and-progression.md) | 内容 | 部分 | 进行中 | **v1.1 三层自动成长本波已落地**（舰长 XP / 战斗 XP / 五项专业技能 / F–S 能级上限与储存经验）；抽卡/约 36 人已通；Characters/Cards 原生页 + 分解已通；能级突破为废料+信用 |
| 20 | [stellar-map-and-navigation](20-stellar-map-and-navigation.md) | 规则/方向 | 部分 | 进行中 | **v0.3** 航网主形态；M2 域内已落地；宇宙层生成见 **23** |
| 21 | [fleet-factions-and-exploration](21-fleet-factions-and-exploration.md) | 规则 | 未开始 | — | 舰队/卡关/探索度设定已拍板 |
| 22 | [ux-loop-and-ia](22-ux-loop-and-ia.md) | 产品/UX | 部分 | 进行中 | P6 IA 已拍板；壳层 + 主循环页面改造已起步；M6 验收待 PlayMode |
| 23 | [procedural-universe-generation](23-procedural-universe-generation.md) | 规则/方向 | 部分 | 进行中 | **v0.1 试点**：`UniverseGenerator` + 验证器 + 种子持久化；Debug 屏可重掷/验证；M3 约 18 星域 |
| 24 | [early-chapter-experience](24-early-chapter-experience.md) | 内容/体验 | 部分 | 进行中 | **Wave A**：`chapter_v1` 六步、序章战斗、科尔保底、双层任务追踪；PlayMode 验收见文档 24 §7 |

---

## 4. MVP §16.1 快照

| 必含项 | 状态 |
| --- | --- |
| 全自动战斗 / 5 人多卡组占用 | 已实现 |
| 区域+舰船门 / 挂机采集制造 | 已实现 |
| 离线 cap+yield / 品质 / 耐久 | 部分（cap 已通；**有效时间内 yield=100%**） |
| NPC 商店 / 6 区首领 / 新手五步 | 已实现 |
| 36 角色+抽卡 | 部分（约 36/50；原生卡册与分解已通） |
| 玩家市场 | Online only |
| 舰队 / 流水线 / 探索度 | 部分（探索度随 M2 航网恢复；舰队 / 流水线未开始） |

---

## 5. 近期实现摘录（相对代码核对）

> 供审计用；不以本表代替 §3。证据以仓库代码为准。

| 主题 | 状态 | 主要证据 |
| --- | --- | --- |
| Explore 两层缩放地图 | 部分（M2） | `ExploreMapRig` 连续缩放+拖拽；缩小出星域进宇宙，回星域仅「进入星域」；仅已解锁+相邻节点 |
| 物品双语说明 + 获取提示 | 已实现 | `ItemDef.descriptionEn/Zh`；`ItemAcquireCatalog`；`ItemTooltip` |
| 舰船模块升级 UI | 已实现 | `ModuleUpgradePopup`；`ShipService` 走仓库扣费 |
| 加载遮罩 / 奖励弹窗 / 采集仓 | 已实现 | `LoadingOverlay`；`RewardPopup`；Gather Bank |
| Characters / Cards 原生页 | 已实现 | `CharactersScreen` / `CardsScreen`；详情含战斗/专业 XP 条；nav 不再走 Legacy |
| 卡牌分解 | 已实现 | `DismantleRules` + `CardDismantleService`；空闲未绑定实例可分解 |
| 功能逐步解锁 + 导航 grey out | 已实现 | `FeatureUnlockService` + `FeatureUnlockCatalog.json`；未解锁导航灰显 |
| 统一 Dialog / Snackbar / Notification | 已实现 | Dialog / Snackbar 在用；顶栏 Notification **暂时关闭**，摘要进战情日志 |
| 玩家名称 / 头像自定义 | 已实现 | Settings：已解锁角色立绘 + 本地 PNG/JPG 上传；`playerID` 只读 |
| 头像框 | 已实现 | `AvatarFrameCatalog` + Settings 装备；活动/礼品码/成就解锁 |
| 舰桥 / 探索 P0 UX | 部分 | Bridge：去重战力/信用、指挥任务卡、5 席 2+3 编队、附近节点；Explore：清剿后主 CTA 改挂机/采集、航程 stub、航线粗细/虚线、节点边框语汇 |
| 自动化流水线 | 未开始 | 规则在 [15](15-economy.md) §4.10；无 `ProductionLine` 领域代码 |
| 舰队 / 探索度 | 部分 | 探索度条已接航网恢复加权（[20](20-stellar-map-and-navigation.md) M2）；舰队规则仍在 [21](21-fleet-factions-and-exploration.md) |
| 星图 M3+（宇宙层 / 信标跃迁 / 虫洞边） | 部分 | 宇宙层 procedural 图已生成（`UniverseGenerator`）；Explore 宇宙缩放已接；信标跃迁/完整 M3 交互待做 |
| 程序生成航网（23） | 部分 | `UniverseGenerator` / `UniverseValidator` / `universeSeed`；Debug 重掷+验证；EditMode 测试 |
| 三层自动成长（19） | 部分 | `ProgressionService`：战斗/专业/舰长 XP、能级上限与储存经验、领队门槛、离线 100%；顶栏舰长 XP/战力条；Formation/Characters 卡牌 XP 条 |
| P6 指挥官循环（M6） | 部分 | Wave 0–2 已落地；M6 三十分钟验收待 PlayMode 回归 |
| 首章 Wave A（24） | 部分 | `ChapterQuestService`、`chapter_quests.json`、`enc_prologue_sweep`、`starterColeGranted`、七条 `ch1_*` 对话；任务 6–10 未做 |

---

## 6. 维护规则

1. 实现进度**只改本文**。  
2. 世界观变更 → 升 [03-worldbuilding](03-worldbuilding.md) 主版本。  
3. 完整索引 → [README.md](README.md)。  
4. 路线图与 Cursor 任务切片 → [10-mvp-development-plan](10-mvp-development-plan.md)（§2 历史表不逐条同步）。
