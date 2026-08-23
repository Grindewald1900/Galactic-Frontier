# Galactic Frontier 中文开发文档

本目录帮助开发者快速理解项目结构、当前实现、数据流和开发约束，也作为 Codex 后续修改项目时的上下文入口。

## 推荐阅读顺序

1. [快速开始](01-quick-start.md)：运行环境、启动场景和首次阅读路径。
2. [设定与故事背景](systems/00-setting-and-lore.md)：群星断航、开拓局与玩家身份（文案/叙事权威）。
3. [项目结构](02-project-structure.md)：第一方代码、资源和第三方目录边界。
4. [架构总览](03-architecture.md)：系统分层、生命周期和主要依赖。
5. [核心系统实现](04-core-systems.md)：卡牌、编队、抽卡、战斗、物品和 UI 的真实实现。
6. [数据与存档](05-data-and-save.md)：JSON、Resources、存档文件和路径规则。
7. [开发与验证](06-development-guide.md)：新增功能、Unity 序列化、测试和提交检查。
8. [Codex 工作指南](07-codex-guide.md)：自动化修改项目时应优先读取的上下文和安全边界。
9. [已知问题与技术债](08-known-issues.md)：原型数据、耦合点和后续重构方向。
10. [Figma UI 重构](09-figma-ui.md)：NEXUS 视觉系统、页面映射、运行时装配和扩展方式。
11. [核心类职责与关系](10-core-classes.md)：核心类的数据所有权、依赖方向、生命周期和主要调用链。
12. [MVP 进度与 Cursor 开发计划](11-mvp-development-plan.md)：对照核心设计的实现进度、分阶段路线图与 Cursor 任务方法。
13. [Debug 模式与测试工具](systems/16-debug-and-test-mode.md)：Dev Data、Debug Panel、礼品码与 EditMode/冒烟清单。

### 系统规则文档

在实现对应玩法前阅读；用于关闭核心设计 §21 的待定项。

| 文档 | 阶段 | 状态 |
| --- | --- | --- |
| [设定与故事背景](systems/00-setting-and-lore.md) | 全程 | 已拍板 v1.2（舰队 / 探索度 / 卡关区） |
| [舰队、阵营与星域探索度](systems/21-fleet-factions-and-exploration.md) | 后 MVP / 星图 | **设定已拍板 v1.0** |
| [卡组与角色占用](systems/01-deck-and-occupation.md) | P1 | MVP 规则已拍板 |
| [全自动回合制战斗](systems/02-auto-battle.md) | P0/P2 | MVP 规则已拍板 |
| [区域推进与舰船门槛](systems/03-region-and-ship.md) | P2 | 已拍板 v1.2（模块化 + 特殊玩法方向） |
| [挂机刷取与离线收益](systems/04-idle-and-offline.md) | P2/P3 | 已拍板 v1.1（含 Yield Ratio） |
| [生产链与品质](systems/05-production-and-quality.md) | P3 | MVP 规则已拍板 |
| [装备耐久与维修](systems/06-durability-and-repair.md) | P3 | MVP 规则已拍板 |
| [全服市场与卡牌交易 / NPC 商店](systems/07-market-and-card-trade.md) | P4 / Online | MVP=NPC；玩家市场仅 Online（v1.1） |
| [存档契约与种子数据](systems/08-save-and-seed-data.md) | P0 | MVP 规则已拍板 |
| [资源表、生产链与仓库](systems/09-resources-and-warehouse.md) | P3 | 已拍板 v1.2（悬停说明 / 获取渠道 / 模块升级 UI） |
| [新手引导与任务链](systems/10-onboarding-and-missions.md) | P5 | 已拍板 v1.1（P5.4a/b 已落地） |
| [星域 / 区域内容与掉落](systems/11-sector-and-region-content.md) | P2/P5 | 已拍板 v1.1（P5.3 RewardService 已落地） |
| `systems/12-sector-special-modes.md` | 后置 | 待写（虫洞/暗面/多元宇宙） |
| [运行模式与存档互通](systems/14-play-modes-and-persistence.md) | Online | 方向稿 v0.2（挂接位面/Hub） |
| [抽卡与卡牌成长入口](systems/15-gacha-and-progression.md) | P5 / 经济 | 已拍板 v1.1（正式路径已落地） |
| [Debug 模式与测试工具](systems/16-debug-and-test-mode.md) | 全程 | 已拍板 v1.1（单例开关 / 导航 Debug 页 / 设置礼品码） |
| [线上多元宇宙位面与合作](systems/17-online-multiverse-cooperation.md) | Online | 方向稿 v0.1（隔离探索 + Hub 市场 + 公会压力） |
| [卡牌角色设定与背景](systems/18-character-roster-and-lore.md) | P5 | 已拍板 v1.0（36 人叙事/技能/属性） |
| [卡牌能级](systems/19-card-energy-rank.md) | P5 / 成长 | 已拍板 v1.2（职业技 F–S 每档 6 个） |
| [星域地图与舰船航行](systems/20-stellar-map-and-navigation.md) | 后 MVP | 方向稿 v0.1（雷达/航行/跃迁/信标/边缘→中心） |

产品设计真相源（仓库根文档）：

- [核心产品设计](../01-core-product-design.md)（含 §5 故事背景摘要；细则见 `systems/00-setting-and-lore.md`）

## 项目一句话说明

`Galactic Frontier / 群星边境`：断航三百年后，你作为群星开拓局舰长，从宇宙边缘向中心推进——**扩建舰队**（一舰一队、图纸造舰）、突破**卡关航道**、**驻扎**已收复星球，并以**探索度**解锁主星、阵营商店与深空异象。

## 当前基线

| 项目 | 当前值 |
| --- | --- |
| Unity | `6000.0.20f1` |
| 渲染管线 | URP `17.0.3` |
| 输入 | 新 Input System 与旧 Input Manager 同时启用 |
| 公司/产品名 | `YeeStudio / Galactic Frontier` |
| 默认分辨率 | `1920 × 1080` |
| 主要代码目录 | `Assets/Resources/Scripts` |
| 主要场景目录 | `Assets/Resources/Scenes` |
| 运行时配置目录 | `Assets/Resources/data` |
| 相对 MVP 进度 | 约 25%–35%（战斗/壳层较强，经济循环未开工） |

> 注意：项目仍处于原型阶段。样例 / FakeData 已隔离到 Dev Data Mode（默认关闭，见 `systems/08-save-and-seed-data.md`）；正式流程不再启动覆写背包。详细进度见 [11-mvp-development-plan.md](11-mvp-development-plan.md)。
