# MVP 开发进度与 Cursor 后续开发计划

> 文档版本：v1.3  
> 对照设计：`Documentation/01-core-product-design.md`（产品核心设计 **v0.4**）  
> 对照实现：`Assets/Resources/Scripts` 与现有 `zh-CN` 开发文档  
> 更新日期：2026-08-09  
> 变更摘要：对齐 v0.4——Solo/Online 分层、离线收益比例、舰船模块化、MVP 改 NPC 商店；§7 文档顺序更新。
本文档回答三件事：

1. 以核心设计 / MVP 为尺子，**当前做到哪一步**；
2. 差距在哪里、按什么顺序补齐；
3. 如何用 **Cursor** 高效推进后续开发（任务切片、上下文入口、验收方式）。

---

## 1. 一句话结论

项目处于**可玩的战斗与卡牌原型阶段**：全自动回合制战斗、单编队（5 人）、Nexus UI 壳、抽卡/背包/存档骨架已打通。  
核心设计要求的**多卡组并行挂机、生产链、耐久、品质、全服市场、舰船区域门、主线进度**等经济循环几乎尚未开工。  
后续应以「先打通单人核心循环，再扩内容量」为原则，用 Cursor 按阶段切片实现。

---

## 2. 当前开发进度总览

### 2.1 完成度快照

| 维度 | 评估 | 说明 |
| --- | --- | --- |
| 战斗原型 | ★★★★☆ | 全自动回合制可跑通，敌人/策略/战后闭环仍弱 |
| 卡牌与编队 | ★★★☆☆ | 单编队 5 槽可用；多卡组与占用状态未做 |
| UI 壳层 | ★★★☆☆ | Nexus 导航与关键页已有；多数玩法仍接旧面板或占位 |
| 存档与数据 | ★★☆☆☆ | 本地存档可用；**P0.1 FakeData 已隔离**；仍无版本迁移 / 双背包分文件 |
| 经济循环 | ★☆☆☆☆ | 采集 / 生产 / 耐久 / 品质 / 市场基本缺失 |
| 成长与主线 | ★☆☆☆☆ | 区域 UI 有入口，舰船门、章节、首领未落地 |
| 内容量 | ★☆☆☆☆ | 可战斗角色约 3 名，远低于 MVP 30–50 |

整体相对 MVP §16.1：**约 25%–35%**（战斗与壳层偏高，经济与成长偏低）。

### 2.2 MVP 必含项对照表

状态定义：

- **已实现**：核心规则可在游戏中跑通（可为原型深度）
- **部分实现**：有骨架或相关能力，但未满足设计约束
- **未开始**：无有效领域逻辑（仅文案/导航不算）

| MVP 必含项（设计 §16.1） | 状态 | 当前证据 / 缺口 |
| --- | --- | --- |
| 全自动回合制卡牌战斗 | 已实现 | `BattleController.BattleRoutine` 按速度自动结算 |
| 每队 5 张出战角色卡 | 部分实现 | `LineupManager` 单编队 5 槽；无多卡组 |
| 多卡组管理 | 未开始 | 仅一套出战阵容 |
| 角色单队伍占用规则 | 部分实现 | 编队内防重复；无采集/生产等占用态 |
| 主线/区域战斗自动结算 | 部分实现 | 可进 `BattleScene` 自动打；无章节进度与首次通关态 |
| 通关后挂机自动战斗刷取 | 未开始 | 无 AFK 战斗循环 |
| 基础挂机采集 | 未开始 | Bridge 文案占位 |
| 多行动并行 + 行动队列 | 未开始 | 无 Deck/Action 调度器 |
| 分阶段离线收益上限 | 未开始 | 无离线时钟与结算 |
| 15–20 种资源 | 未开始 | 背包 FakeData 玩具材料 |
| 3 条完整生产链 | 未开始 | Crafting 导航接旧 BUILDING 面板 |
| 10–15 装备/模块 | 未开始 | `ItemEntity` 无装备成长模型 |
| 装备耐久与材料维修 | 未开始 | 无耐久字段与维修 API |
| 基础制造 + 品质差异 | 未开始 | 无配方/品质公式 |
| 全服玩家市场 + 价格历史 | 未开始（**非 MVP**） | v0.4：仅 Online；MVP 不做 |
| 普通卡牌交易 | 未开始（**非 MVP**） | 仅 Online；Solo 走 NPC/分解 |
| NPC 商店 + 基础货币 | 未开始 | v0.4 MVP 必做；替换原「首发全服市场」 |
| 舰船等级限制区域推进 | 未开始 | Explore 文案 “Ship gate TBD”；须模块化 |
| 5–6 可探索区域 + 1 区域首领 | 部分实现 | Explore 有 5 区域入口；无解锁图与首领 |
| 1 个完整星域 / 2 基础阵营 | 未开始 | 无星域进度与阵营内容管线 |
| 30–50 角色及相关卡牌 | 部分实现 | 约 3 名可战斗角色（Asra / Magki / Sernia） |
| 约两小时新手流程 | 未开始 | Missions 为占位 |
| 基础仓库和舰船升级 | 部分实现 | 本地/远程背包原型；无舰船实体 |
| 离线收益比例（开局 50%） | 未开始 | 见 `04-idle-and-offline.md` §4.2b |
| 单机模式完整可玩 | 部分实现 | 本即单机原型；须去掉对玩家市场的依赖假设 |

### 2.3 已有可复用资产（后续不要推倒重来）

| 资产 | 路径 / 类 | 后续用法 |
| --- | --- | --- |
| 自动战斗引擎 | `Battle/BattleController.cs` | 扩展战前策略、挂机刷取、确定性战报 |
| 编队 UI | `LineupManager`、`FormationScreen` | 抽象为「卡组槽」，再扩多卡组 |
| 卡牌领域模型 | `CardEntity`、`CardDataManager` | 加占用态、绑定/交易字段 |
| Nexus 壳 | `AppShell`、`Bridge/Explore/Formation` | 新系统优先做原生 Screen，少挂 Legacy |
| 存档基础设施 | `DataUtil` | 加版本号、原子写、分文件（队伍/行动/市场缓存） |
| 背包骨架 | `InventoryItemManagerBase` | 去掉 FakeData 污染后接真实资源表 |
| 文档入口 | `Documentation/zh-CN/*` | Cursor 任务前必读 |

### 2.4 高优先级技术债（阻塞扩展）

必须在大规模做经济系统前处理，否则 Cursor 改一处易污染全局：

1. ~~**FakeData 污染存档**~~ → **P0.1 完成**（`Utils/Save` + 正式默认关闭）；Starter Seed / 分文件仍属 P0.2  
2. **本地/远程背包共用 `itemData.json`** → 存档模型拆分  
3. **无存档版本与迁移** → 加 `saveVersion` + migrator  
4. **战斗结束返回主场景被注释** → 打通 Explore → Battle → 结果 → 回主流程  
5. **缺少 Domain 层测试** → 战斗结算、占用规则、离线结算优先 EditMode 测试  

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
12. （保留）玩法细则以 `systems/*` 已拍板文档为准  

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
| P0.2 存档 v1 | `saveVersion`、原子写（tmp→replace）、本地/远程物品分文件 | 旧档能读或明确迁移；双背包数据不串 |
| P0.3 战斗闭环 | 胜利/失败 → 战报 → 返回 MainScene/Explore | Explore 进出战斗完整一轮无卡死 |
| P0.4 战斗确定性种子 | 同编队同敌人同种子战报一致 | EditMode 测试覆盖伤害与胜负判定 |
| P0.5 Domain 测试脚手架 | `Assets/Tests/EditMode` + asmdef（可选） | 至少 5 个稳定测试 |

**Cursor 任务切片示例：**

> 阅读 `Documentation/zh-CN/05-data-and-save.md` 与 `DataUtil.cs`。为存档增加 `saveVersion`，并把 `ItemManager`/`RemoteItemManager` 拆成独立 JSON。不要改第三方包。完成后更新 `05-data-and-save.md`。

---

### P1 — 多卡组与角色占用（约 2–3 周）

**目标**：落实设计 §6 / §7.2 / §7.3 的「角色是有限资源」。

| 任务 | 产出 | 验收 |
| --- | --- | --- |
| P1.1 卡组数据模型 | `DeckEntity`（id、用途、5 槽、状态） | 可序列化进存档 |
| P1.2 多卡组 UI | 在 Formation/Bridge 管理多套卡组 | 至少 2 个卡组槽；未解锁槽显示条件 |
| P1.3 占用状态机 | 角色状态：Idle / Combat / Gather / Craft / Research / Transit… | 占用中角色不可加入其他运行中卡组 |
| P1.4 行动调度器 | `ActionScheduler`：开始/停止/完成；停止惩罚低 | 停止行动后角色可重分配 |
| P1.5 备用卡组 | 允许保存未启用编队 | 未运行编队不占用角色 |

**拍板清单（实现前写入系统文档）：**

- 初始卡组槽数量与解锁条件  
- 停止行动后解除占用时机  
- 同名卡是否可同时进不同卡组  

---

### P2 — 区域推进、舰船门、挂机刷怪（约 2–3 周）

**目标**：落实设计 §7.1 / §7.8：打通「开战 → 通关 → 刷取 → 开下一区」。

| 任务 | 产出 | 验收 |
| --- | --- | --- |
| P2.1 区域进度图 | Region 配置：前置战斗、奖励、是否已通关 | Explore 按进度解锁，而非全开 |
| P2.2 舰船模型 | `ShipEntity`：等级 + **分模块** + ShipStat 合计 | 区域入口校验「通关 + 舰船」；模块升级可感知 |
| P2.3 敌方配置表 | 替换战斗 FakeData 敌人 | 每区域固定或加权敌人池 |
| P2.4 战后挂机战斗 | 通关区域可派卡组 AFK 刷取 | 消耗耐久/时间；产出进仓库 |
| P2.5 战前策略 v0 | 目标优先级、技能倾向（可配置枚举） | 不同策略影响自动技能选择 |
| P2.6 区域首领占位 | 1 个 Boss 战斗配置 | 可挑战、可失败、通关标记 |

---

### P3 — 经济循环：采集 / 生产 / 耐久 / 品质（约 3–4 周）

**目标**：形成设计核心循环中「战斗开图后的资源侧」。

| 任务 | 产出 | 验收 |
| --- | --- | --- |
| P3.1 资源表 | 15–20 种资源 ScriptableObject/JSON | 仓库可堆叠、可筛选 |
| P3.2 采集行动 | 卡组绑定矿点/节点，按时间产出 | 与战斗卡组并行不冲突 |
| P3.3 生产链 ×3 | 配方、设施等级、角色工程属性 | 至少 3 条「原料→中间→成品」可跑通 |
| P3.4 装备与耐久 | 装备字段：耐久、维修公式、自动维修规则 | 归零不销毁；维修消耗可预测 |
| P3.5 品质系统 v0 | 品质档位 + 受技能/设施/材料影响的公式 | 玩家可感知提高最低品质的手段 |
| P3.6 离线结算 v0 | 离线时长上限、**Yield Ratio**、仓库满/材料不足 | 开局 50% 比例；上线一键领取；超上限暂停不惩罚 |

**内容目标（MVP 量）：** 10–15 件装备/模块；品质与耐久接入战斗/采集消耗。

---

### P4 — NPC 经济与商店（约 2 周；原全服市场后置）

**目标**：落实设计 v0.4 §7.5 单机路径；`07-market-and-card-trade.md` MVP 部分。

| 任务 | 产出 | 验收 |
| --- | --- | --- |
| P4.1 PlayMode 门控 | `PlayMode.Solo`；市场 API 禁用 | Solo 无法打开玩家市场 |
| P4.2 货币字段 | `credits`（及可选绑定币钩子） | 购买扣款一致 |
| P4.3 NPC 货架配置 | `NpcShops.json` + 解锁条件 | 按区域/舰船解锁商品 |
| P4.4 购买 / 回收 | 与仓库原子增减 | 满仓/余额不足有提示 |
| P4.5 Nexus 入口 | Market → 星港商店 Screen | 不再进抽卡 |
| P4.6（后置）玩家市场 | Online only，见原订单簿契约 | **非 MVP 验收项** |

> 全服玩家市场与卡牌上架挪到 **线上版本**；不要在 Solo 用 LocalMock 冒充全服盘口。

---

### P5 — 内容填充与新手两小时（约 3–5 周，可贯穿全程）

**目标**：把规则填成可体验的 MVP 内容量。

| 任务 | 产出 | 验收 |
| --- | --- | --- |
| P5.1 角色扩充 | 向 30–50 张卡推进（可分批） | 抽卡/编队/战斗全链路可用 |
| P5.2 2 基础阵营 | 阵营标签、少量专属卡/对话 | Explore/卡牌可见区分 |
| P5.3 完整星域包装 | 5–6 区域叙事与奖励曲线 | 约 2 小时可完成首圈循环 |
| P5.4 新手引导 | Bridge/任务链：编队→首战→采集→制造→**NPC 商店** | 无强制多人、无玩家市场 |
| P5.5 数值初平衡 | 挂机产出、维修成本、NPC 价差粗调 | 高强度 AFK 有材料压力但不劝退 |

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
| **改完同步文档** | 行为变更必须更新 `04-core-systems` / `05-data-and-save` / 本文进度表 |

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
- Documentation/01-core-product-design.md（相关小节）
- Documentation/zh-CN/11-mvp-development-plan.md（当前阶段任务 ID）
- Documentation/zh-CN/<相关专题>.md
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
| P0 | `DataUtil.cs`、`InventoryItemManagerBase.cs`、`BattleController.cs`、`05-data-and-save.md` |
| P1 | `LineupManager.cs`、`FormationScreen.cs`、`CardEntity.cs`、`CardListManager.cs` |
| P2 | `ExploreScreen.cs`、`PlanetListManager.cs`、`BattleController.cs`、`GameStatusManager.cs` |
| P3 | `ItemEntity`、Inventory 系列、新建 `Production/` / `Actions/` |
| P4 | Nexus Market 适配层、新建 `Market/`、`IMarketService` |
| P5 | `CardDataManager.cs`、`Character` 子类、技能 JSON、Explore/任务 UI |

### 6.5 不建议交给 Cursor「一次做完」的事项

- 一次性实现全服市场后端 + 反作弊全套  
- 一次性写 50 个角色完整技能与数值  
- 大规模重写 MainScene 架构同时加玩法  
- 无规则文档的情况下硬编码品质/离线数值  

这些应拆成「接口 → 本地 Mock → 内容批 → 联网」多会话完成。

---

## 7. 建议的系统文档补齐顺序

核心设计 §21 要求细则落到系统文档。目录：`Documentation/zh-CN/systems/`。

### 7.1 第一波（§21 玩法细则）— 已完成

下列文档已关闭核心设计 §21 对应待定项，**可直接作为 P1–P4 实现依据**（语义默认值可写死为可配置常量；纯数值进配置表）。

| 顺序 | 文档 | 服务阶段 | 关闭的 §21 主题 | 状态 |
| --- | --- | --- | --- | --- |
| 1 | [systems/01-deck-and-occupation.md](systems/01-deck-and-occupation.md) | P1 | 卡组与角色占用 | **已拍板 v1.0** |
| 2 | [systems/02-auto-battle.md](systems/02-auto-battle.md) | P0/P2 | 战斗系统 | **已拍板 v1.0** |
| 3 | [systems/03-region-and-ship.md](systems/03-region-and-ship.md) | P2 | 区域推进 / **模块化舰船** / 特殊玩法方向 | **已拍板 v1.1** |
| 4 | [systems/04-idle-and-offline.md](systems/04-idle-and-offline.md) | P2/P3 | 离线时长 + **Yield Ratio 50%↑** | **已拍板 v1.1** |
| 5 | [systems/05-production-and-quality.md](systems/05-production-and-quality.md) | P3 | 生产品质 | **已拍板 v1.0** |
| 6 | [systems/06-durability-and-repair.md](systems/06-durability-and-repair.md) | P3 | 装备耐久 | **已拍板 v1.0** |
| 7 | [systems/07-market-and-card-trade.md](systems/07-market-and-card-trade.md) | P4 / Online | **MVP=NPC 商店**；全服市场仅 Online | **已拍板 v1.1** |

### 7.2 第二波（实现与内容管线）— 建议补齐顺序

| 顺序 | 文档 | 服务阶段 | 必须先于 | 状态 / 要拍板的内容 |
| --- | --- | --- | --- | --- |
| 8 | [systems/08-save-and-seed-data.md](systems/08-save-and-seed-data.md) | **P0** | 任何新存档字段 / 经济内容入库 | **已拍板 v1.0** |
| 9 | `systems/09-resources-and-warehouse.md` | **P3** | P3.1 资源表、货舱 | **待写**：15–20 资源；仓库/货舱；满仓与离线对齐 |
| 10 | `systems/10-onboarding-and-missions.md` | **P5** | 新手两小时 | **待写**：步骤含 NPC 商店，不含玩家市场 |
| 11 | `systems/11-faction-and-content-pipeline.md` | **P5** | 批量内容 | **待写** |
| 12 | `systems/12-sector-special-modes.md` | 后置 | 虫洞/暗面/多元宇宙实装 | **待写**（方向见核心设计 §7.10、区域文档 §10.1） |
| 14 | [systems/14-play-modes-and-persistence.md](systems/14-play-modes-and-persistence.md) | Online 立项 | 存档互通最终方案 | **方向稿 v0.1**（MVP 只读 Solo 边界） |

可选：

| 文档 | 何时需要 |
| --- | --- |
| `systems/13-research-and-transit.md` | `Research` / `Transit` 完整循环 |
| `systems/15-gacha-and-progression.md` | 抽卡与绑定/分解对齐 |

### 7.3 使用规则

1. **已拍板（7.1 与 7.2 中已完成项）**：Cursor 实现直接遵循；改语义须升对应系统文档主版本，并回写核心设计（若触及 §20/§22）。  
2. **待写（7.2 其余）**：未落盘前，实现只用**明显可配置的默认常量**，并在提交说明标注「待 `09`/`10`/… 确认」。  
3. **数值**：档位、秒数、掉落权重进配置表；系统文档只锁语义与公式形状。  
4. **写完一篇 7.2 文档后**：更新本表状态、[README.md](README.md) 系统规则表，并视需要修订核心设计 §21（将已关闭项移出或标注「见 systems/xx」）。  
5. **P0 编码入口**：先读 `08-save-and-seed-data.md`，再改 `DataUtil` / 库存 FakeData；完成后回写 [05-data-and-save.md](05-data-and-save.md) 路径树。

---

## 8. 里程碑定义（便于排期）

| 里程碑 | 玩家可感知结果 | 依赖 |
| --- | --- | --- |
| **M0 可迭代原型** | 进出战斗稳定、存档不脏 | P0 |
| **M1 有限角色决策** | 两支队伍不能抢同一角色 | P1 |
| **M2 开图循环** | 通关区域 → 舰船门槛 → 挂机刷取 | P2 |
| **M3 经济自转** | 采集→制造→修装备形成材料消耗 | P3 |
| **M4 交易闭环** | NPC 可买可卖；经济可读 | P4 |
| **M5 MVP 可演示** | ~2 小时新手 + 内容量达标（纯单机） | P5 + 前序 |

---

## 9. 进度维护约定

每完成一个 P 级子任务，更新：

1. 本文 **§2.2 对照表** 状态  
2. 对应 `04-core-systems.md` 实现说明  
3. 若引入新模块，在 `02-project-structure.md` 增加目录说明  
4. 若发现新坑，记入 `08-known-issues.md`  

建议在 Bridge 或内部 Debug 面板显示当前里程碑标签（如 `Build: M1`），方便试玩反馈对齐版本。

---

## 10. 近期建议执行顺序（立刻可开的 Cursor 任务）

按依赖排出的**下一批 5 个任务**（建议严格按序）：

1. ~~**P0.1** FakeData 隔离开关~~ **已完成**  
2. **P0.3** 战斗结束返回 Explore/Main 闭环  
3. **P0.2** 存档版本 + 双背包分文件  
4. **P1.1 + P1.3** `DeckEntity` + 角色占用状态（先数据后 UI）  
5. **P2.1** 区域通关标记与 Explore 解锁（为挂机刷怪铺路）  

完成以上五项后，项目将从「战斗 Demo」进入「可扩展的多队伍原型」，再开 P3 经济系统风险显著降低。

---

## 11. 参考索引

| 文档 | 用途 |
| --- | --- |
| [01-core-product-design.md](../01-core-product-design.md) | 产品真相源（玩法约束与 MVP 范围） |
| [04-core-systems.md](04-core-systems.md) | 当前代码真实行为 |
| [05-data-and-save.md](05-data-and-save.md) | 存档路径与序列化 |
| [06-development-guide.md](06-development-guide.md) | Unity 修改与验证清单 |
| [07-codex-guide.md](07-codex-guide.md) | 自动化代理工作边界（同样适用于 Cursor） |
| [08-known-issues.md](08-known-issues.md) | 技术债与演进顺序 |
| [09-figma-ui.md](09-figma-ui.md) | Nexus UI 扩展方式 |
| [10-core-classes.md](10-core-classes.md) | 核心类职责与调用链 |
| [systems/08-save-and-seed-data.md](systems/08-save-and-seed-data.md) | P0 存档版本、FakeData 边界、双背包与种子 |
