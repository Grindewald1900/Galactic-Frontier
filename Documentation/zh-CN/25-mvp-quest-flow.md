# MVP 任务流程总表

> 文档版本：v1.0  
> 文档类型：**内容契约（任务权威）**  
> 实现状态见 [PRODUCT-STATUS.md](PRODUCT-STATUS.md)。  
> 上级约束：[02-core-product-design.md](02-core-product-design.md) §16、[03-worldbuilding.md](03-worldbuilding.md)  
> 分系统细则：[17-sector-and-onboarding.md](17-sector-and-onboarding.md)（`onboarding_v1`）、[24-early-chapter-experience.md](24-early-chapter-experience.md)（`chapter_v1` 分镜）、[13-region-and-ship.md](13-region-and-ship.md) / [20-stellar-map-and-navigation.md](20-stellar-map-and-navigation.md)（远征阶段）  
> 更新日期：2026-09-08

本文是 MVP **全部任务**的唯一总表：编号、类型、描述、奖励、触发、完成、依赖。  
叙事口吻以 `03` 为准；完成判定与奖励数值以本文为准。改步骤语义须升本文主版本，并同步 `17` / `24` 中对应行。

---

## 1. 目标长度与任务量

| 指标 | MVP 默认 |
| --- | --- |
| 首圈体验 | **约 3 小时**有向目标（与 `24` 完整首章对齐）；`17` 的两小时演示是前半截 |
| 内容容器 | 1 星域 `sector_frontier_vii` × 6 战斗节点（阶段合计 **43** 场） |
| 有向任务数 | **51** 条定义（不含循环的每次重复实例） |
| 并行上限 | 1 条剧情 Active + 1 条远征环 Active + 最多 **2** 条循环任务 Active |
| 终点 | 边境锚点航标恢复（`sec01_frontier_boss` Boss 通关）+ 新手航线 Claim 完毕 |

任务不是「一场战斗一条剧情」。战斗阶段由 `NodeCampaignCatalog` 承载；任务层只在**教学、叙事拐点、开挂机、修航标、舰船门、循环资源**上给可追踪目标。

| 阶段 | 玩家时间 | 主要任务 | 玩家应学会 |
| --- | --- | --- | --- |
| 序章 + 教学 | 0–15 min | `CH-00`…`CH-02`，`OB-01` | 编队、招募、首胜 |
| 外缘 + 矿脉 | 15–50 min | `CH-03`…`CH-05`，`OB-02`，外缘/矿脉 R1 | 站位、探测、破甲 |
| 生产闭环 | 50–90 min | `CH-06`…`CH-07`，`OB-03`…`OB-05`，外缘/矿脉 R2–R3 | 采集、挂机、制造、商店 |
| 开图后半 | 90–150 min | `CH-08`…`CH-09`，裂隙/深渊/护航三环，舰船门 | 路线、模块门槛、多卡组占用 |
| 星域首领 | 150–180 min | `CH-10`，锚点三环 | 综合补给与 Boss |
| 循环填充 | 全程可并行 | `LP-*` | 同一难度重复刷取，服务升级而非推主线 |

---

## 2. 类型与编号

### 2.1 类型

| 类型码 | 名称 | 重复 | 难度 | 说明 |
| --- | --- | --- | --- | --- |
| `Onboarding` | 系统教学 | 一次 | 固定 | `onboarding_v1`；Claim 推进功能解锁 |
| `Story` | 剧情主链 | 一次 | 随章递增 | `chapter_v1`；Claim 可镜像教学步骤 |
| `Expedition` | 多环远征 | 每环一次 | **层层递增** | 每节点 3 环：采集解锁 → 战斗挂机 → 航标恢复 |
| `Loop` | 循环任务 | 可重复 | **同区一致** | 模板相同；换区只换掉落，不换规则 |
| `Ship` | 舰船门 | 一次 | 层层递增 | 为下一区硬门准备模块等级 |

### 2.2 编号规则

```text
OB-nn          教学          ob_*
CH-nn          剧情          ch1_*
EX-{node}-Rn   远征第 n 环   ex_{node}_r{n}
LP-{kind}-{node} 循环        lp_{kind}_{node}
SH-nn          舰船          sh_*
```

`node`：`outer` / `mining` / `rift` / `abyss` / `convoy` / `anchor`。

### 2.3 状态机（所有一次型任务共用）

`Locked` → `Active`（触发满足）→ `Ready`（完成条件满足）→ `Claimed`。

- 完成条件必须可观测（存档字段或服务事件），禁止「只点领取」。
- Claim 发奖；满仓进 Pending Loot（与 `14` 一致）。
- 循环任务 Claim 后立刻可再次 Active（无冷却）；同模板同时只允许 1 条 Active。

### 2.4 镜像与防双份奖励

| 规则 | 行为 |
| --- | --- |
| 剧情 Claim 带 `linkedOnboardingStepId` | 置对应 `OnboardingFlags`，教学步骤可领，**不**把教学奖励并进剧情包 |
| 剧情与远征完成条件重叠 | 以先 Ready 的为准；后到者自动 Ready，Claim 仍各发**本表自己的奖励**（教学/剧情偏信用与券，远征偏材料） |
| 循环 vs 远征 | 循环统计挂机/采集次数，**不**替代航标恢复 |

---

## 3. 总依赖（主脊骨）

```text
CH-00 序章
  → CH-01 招募科尔
    → CH-02 编队（镜像 OB-01）
      → CH-03 外缘清理（镜像 OB-02）
        → CH-04 扫描矿脉
          → CH-05 无人矿场
            → CH-06 挂机采集（镜像 OB-03）
              → CH-07 制造维修（镜像 OB-04）
                → OB-05 星港（可与 CH-07 并行，仅依赖 OB-04）
                → CH-08 两条航路
                  → CH-09 远征补给
                    → CH-10 边境锚点

外缘 EX-R1..R3 在 CH-03 之后可推进（不阻塞矿脉剧情）
矿脉 EX-R1..R3 在 CH-05 之后可推进
其后节点：可见且舰船门通过 → 该节点 EX-R1 Active
SH-01..04 插在对应区域门之前（见 §7）
LP-* 在对应节点 R2（挂机解锁）之后可接
```

硬门（区域进入）仍以 `RegionCatalog` 为准：外缘 → 矿脉 → 裂隙 → 深渊 → 护航 → 锚点。  
`CH-08` 是**叙事分叉**（先强调护航或裂隙），不能绕过舰船门。

---

## 4. 系统教学 `onboarding_v1`（5）

链 id：`onboarding_v1`。同时最多 1 个 Active。实现：`OnboardingCatalog` / `OnboardingService`。  
**状态：已落地。**

| 编号 | stepId | 类型 | 中文 / 英文 | 描述 | 目标屏 | 触发 | 完成条件 | 依赖 | 奖励 | 状态 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| OB-01 | `ob_formation` | Onboarding | 编成首支队伍 / Form a Deck | 向任意已解锁卡组放入至少 1 名角色 | Formation | 新档；链启动 | 任一解锁卡组 `MemberCount >= 1` | — | 信用点 +50 | 已落地 |
| OB-02 | `ob_first_battle` | Onboarding | 完成首场区域战 / First Battle | 从探索发起**挑战**并获胜 | Explore | Claim OB-01 | 任意区域挑战胜利 1 次；**挂机 AutoCombat 不算** | OB-01 | `mat_scrap` ×10 Q2 | 已落地 |
| OB-03 | `ob_gather` | Onboarding | 开始采集 / Start Gathering | 派出卡组采集，或拿到采集产物 | Explore | Claim OB-02 | 曾启动 `Gather` **或**仓库含任意采集产出 ≥1 | OB-02 | `mat_iron_ore` ×6 Q2 | 已落地 |
| OB-04 | `ob_craft` | Onboarding | 完成一次制造 / Craft Once | 工坊成功开始一次配方 | Crafting | Claim OB-03 | `craftedOnce`（Debug 改数量不算） | OB-03 | `con_repair_kit` ×1 Q2 | 已落地 |
| OB-05 | `ob_npc_shop` | Onboarding | 星港购或售 / Visit Starport | NPC 商店完成一次买或卖 | Market | Claim OB-04 | `shopTradedOnce`；纯打开商店不算 | OB-04 | 信用点 +100 | 已落地 |

链完成后：Missions 显示「新手航线完成」。其后**必须**继续给具体目标（远征环 / 循环 / 剧情），禁止只留「继续探索」。

---

## 5. 剧情主链 `chapter_v1`（11）

链 id：`chapter_v1`。实现：`ChapterQuestCatalog` / `ChapterQuestService`（Wave A 六步已落地；CH-06…CH-10 为契约）。

### 5.1 Wave A（已落地）

| 编号 | stepId | 类型 | 中文 / 英文 | 描述 | 目标屏 | 触发 | 完成条件 | 依赖 | 镜像教学 | 奖励 | 对话 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| CH-00 | `ch1_prologue` | Story | 失控的返航 / Runaway Return | 击退清扫无人机，外缘港临时停靠 | Battle | 新档首次进 Nexus | `flags.prologueWon`（`enc_prologue_sweep`） | — | — | 信用点 +120；`con_recruit_ticket` ×1 | `ch1_wake` / `ch1_prologue_clear` |
| CH-01 | `ch1_recruit_cole` | Story | 失效的人员信标 / Failed Personnel Beacon | 紧急招募；首次必得科尔 | Recruit | Claim CH-00 | 拥有 Cole **或** `emergencyRecruitDone` | CH-00 | — | 信用点 +80 | `ch1_mita_recruit` |
| CH-02 | `ch1_formation` | Story | 拼凑出的舰队 / Patchwork Fleet | 阿斯拉前排、科尔后排、战术 ≠ Balanced | Formation | Claim CH-01 | `formationReady` 或 `MeetsFormationLayout` | CH-01 | `ob_formation` | 信用点 +100 | `ch1_asra_formation` |
| CH-03 | `ch1_outer_cleanup` | Story | 外缘港清理令 / Outer Haven Cleanup | 赢得外缘带**挑战**（非挂机） | Explore | Claim CH-02 | `outerCleanupWon` 或 `sec01_outer_belt` 至少 1 阶段已清 | CH-02 | `ob_first_battle` | 信用点 +150；`mat_scrap` ×15 Q2 | 战后 `StrategyComparePopup` |
| CH-04 | `ch1_scan_signal` | Story | 熵雾中的周期信号 / Periodic Signal | 探测矿脉支线至至少 Fogged/Located | Explore | Claim CH-03 | `miningSignalScanned` 或 `body_mining_spur` ≥ Fogged | CH-03 | — | 信用点 +80 | `ch1_sernia_signal` |
| CH-05 | `ch1_mining_spur` | Story | 无人矿场 / Abandoned Mine | 清剿矿脉支线至少第 1 阶段 | Explore | Claim CH-04 | `miningSpurWon` 或矿脉 `highestClearedStage >= 1` | CH-04 | — | 信用点 +200；`mat_iron` ×20 Q2 | `ch1_magki_armor` |

序章遭遇 `enc_prologue_sweep` 不写入常规区域 clear。科尔保底：`gacha.json` → `starterColeGranted`。

### 5.2 Wave B / C（契约，代码未接）

| 编号 | stepId | 类型 | 中文 / 英文 | 描述 | 目标屏 | 触发 | 完成条件 | 依赖 | 镜像教学 | 奖励 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| CH-06 | `ch1_idle_gather` | Story | 让机器替我们工作 / Let Machines Work | 在已解锁采集的节点挂机至少一个完整周期 | Explore | Claim CH-05 且外缘或矿脉阶段 ≥2 | 采集结算成功 ≥1；建议引导 15s / 2min 节奏 | CH-05 | `ob_gather` | 信用点 +80；对应节点产出 ×8 Q2 |
| CH-07 | `ch1_hull_leak` | Story | 舰体仍在漏气 / Hull Still Leaking | 制造 1 个维修包并理解材料缺口 | Crafting | Claim CH-06 | `craftedOnce` 且仓库曾持有 `con_repair_kit` | CH-06 | `ob_craft` | 信用点 +100；`con_repair_kit` ×1 Q2 |
| CH-08 | `ch1_two_routes` | Story | 两条航路 / Two Routes | 在星图查看护航航道与量子裂隙；选择「先强调」的一条 | Explore | Claim CH-07 | 两个目标体均 ≥ Fogged，并确认一次路线选择（存 `flags.routeChoice`：`convoy` / `rift`） | CH-07 | — | 信用点 +120 |
| CH-09 | `ch1_expedition_prep` | Story | 为远征准备补给 / Prep the Push | 第二卡组解锁或占用检查；离线/挂机规划 | Formation / Ship | Claim CH-08 | 至少 2 个解锁卡组 **或** 舰船任意模块升到满足下一区门；并领取一次离线/挂机 Pending | CH-08 | — | 信用点 +150；`mat_scrap` ×20 Q2 |
| CH-10 | `ch1_open_the_lock` | Story | 你打开了一道锁 / You Opened a Lock | 击败边境锚点 Boss，恢复航标 | Explore | 锚点挑战已开放（护航航标已恢复） | `sec01_frontier_boss` 末阶段胜利 / 航标恢复 | CH-09 + EX-convoy-R3 | — | 信用点 +400；`con_recruit_ticket` ×2；航标核心叙事道具（展示，无第二星域传送） |

`CH-08` 不改变 `RegionCatalog` 硬门顺序。选择只影响追踪器优先显示哪条 `EX-*-R1`。

---

## 6. 多环远征 `EX-*`（18）

每战斗节点 **3 环**，难度随 `NodeCampaignCatalog` 推荐战力递增。  
环与阶段绑定（1-based `highestClearedStage`）：

| 环 | 完成条件 | 通常对应阶段 | 解锁玩法 |
| --- | --- | --- | --- |
| R1 | `highestClearedStage >= GatherIdleMinStage`（多阶段节点 = 2） | 第 2 阶段 | 采集挂机 |
| R2 | `highestClearedStage >= CombatIdleMinStage`（= 3） | 第 3 阶段 | 战斗挂机 |
| R3 | 航标恢复 / 末阶段 Boss 胜利 | 末阶段（6–8） | 满效率驻留；相邻节点可 Located |

**触发：** 该 `regionId` 可挑战（可见且舰船门通过，或已在推进中）且上一环已 Claim（R1 无环前置）。  
**依赖（节点间）：** 矿脉 R1 依赖外缘至少可挑战；其后按区域前置。舰船门见 §7。

奖励为任务 Claim 包；阶段首次掉落仍走 `RewardCatalog` 战斗结算，**不要**把整区 FirstClear 提前发进 R1。

| 编号 | questId | 节点 | 环 | 中文 | 描述 | 完成条件 | 依赖 | 奖励（Claim） | 推荐战力带 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| EX-outer-R1 | `ex_outer_r1` | 外缘港 | 1 | 残骸中的采集权 | 打到可采集 | 外缘阶段 ≥2 | CH-03 或外缘可挑战 | 信用点 +40；`mat_iron_ore` ×8 Q2 | 80–90 |
| EX-outer-R2 | `ex_outer_r2` | 外缘港 | 2 | 航道驻留 | 解锁战斗挂机 | 外缘阶段 ≥3 | R1 | 信用点 +50；`mat_scrap` ×12 Q2 | 100 |
| EX-outer-R3 | `ex_outer_r3` | 外缘港 | 3 | 修复外缘航标 | 击败劫掠旗舰 | 外缘末阶段 / 航标恢复 | R2 | 信用点 +120；`con_recruit_ticket` ×1 | 160 |
| EX-mining-R1 | `ex_mining_r1` | 矿脉支线 | 1 | 矿口采集权 | 打到可采集 | 矿脉阶段 ≥2 | CH-05 或矿脉可挑战 | 信用点 +50；`mat_iron` ×10 Q2 | 120–135 |
| EX-mining-R2 | `ex_mining_r2` | 矿脉支线 | 2 | 矿道驻留 | 解锁战斗挂机 | 矿脉阶段 ≥3 | R1 | 信用点 +60；`mat_scrap` ×16 Q2 | 150 |
| EX-mining-R3 | `ex_mining_r3` | 矿脉支线 | 3 | 修复矿脉航标 | 击败矿核巨兽 | 矿脉航标恢复 | R2 | 信用点 +160；能源/矿类材料 ×12 Q2 | 230 |
| EX-rift-R1 | `ex_rift_r1` | 量子裂隙 | 1 | 雾中采集权 | 打到可采集 | 裂隙阶段 ≥2 | 矿脉航标恢复 + 舰船门（裂隙） | 信用点 +70；`mat_energy_cell` ×6 Q2 | 200–220 |
| EX-rift-R2 | `ex_rift_r2` | 量子裂隙 | 2 | 裂隙驻留 | 解锁战斗挂机 | 裂隙阶段 ≥3 | R1 | 信用点 +80；`mat_energy_cell` ×8 Q2 | 245 |
| EX-rift-R3 | `ex_rift_r3` | 量子裂隙 | 3 | 修复裂隙航标 | 击败裂隙之心 | 裂隙航标恢复 | R2 | 信用点 +200；扫描/相位材料 ×8 Q2 | 390 |
| EX-abyss-R1 | `ex_abyss_r1` | 深渊边界 | 1 | 边界废料权 | 打到可采集 | 深渊阶段 ≥2 | 裂隙航标恢复 + 舰船门 | 信用点 +80；`mat_scrap` ×20 Q2 | 260–285 |
| EX-abyss-R2 | `ex_abyss_r2` | 深渊边界 | 2 | 深渊驻留 | 解锁战斗挂机 | 深渊阶段 ≥3 | R1 | 信用点 +90；`con_repair_kit` ×2 Q2 | 310 |
| EX-abyss-R3 | `ex_abyss_r3` | 深渊边界 | 3 | 修复深渊航标 | 击败深渊冠冕 | 深渊航标恢复 | R2 | 信用点 +240；穿甲/抗熵材料 ×8 Q2 | 480 |
| EX-convoy-R1 | `ex_convoy_r1` | 护航航道 | 1 | 航道采集权 | 打到可采集 | 护航阶段 ≥2 | 深渊航标恢复 + 舰船门 | 信用点 +90；`mat_biofiber` ×8 Q2 | 300–320 |
| EX-convoy-R2 | `ex_convoy_r2` | 护航航道 | 2 | 护航驻留 | 解锁战斗挂机 | 护航阶段 ≥3 | R1 | 信用点 +100；信用点额外 +40 | 345 |
| EX-convoy-R3 | `ex_convoy_r3` | 护航航道 | 3 | 修复护航航标 | 击败私掠旗舰 | 护航航标恢复 | R2 | 信用点 +280；`con_recruit_ticket` ×1 | 470 |
| EX-anchor-R1 | `ex_anchor_r1` | 边境锚点 | 1 | 锚点外围 | 打开复合开场 | 锚点阶段 ≥2 | 护航航标恢复 + 舰船门 | 信用点 +100；混合材料包 | 400–425 |
| EX-anchor-R2 | `ex_anchor_r2` | 边境锚点 | 2 | 锚点驻留 | 解锁战斗挂机 | 锚点阶段 ≥3 | R1 | 信用点 +120；`con_repair_kit` ×3 Q2 | 455 |
| EX-anchor-R3 | `ex_anchor_r3` | 边境锚点 | 3 | 重启边境锚点 | 击败边境主权舰 | 锚点航标恢复 | R2 | 与 CH-10 镜像：若 CH-10 未 Claim，本环 Ready 时剧情亦可 Ready；**只发远征包**（信用点 +200 + 废料 ×30 Q2），首领大奖走 CH-10 | 650 |

阶段中文名以 `NodeCampaignCatalog` 为准（外缘 6 / 矿脉 6 / 护航 7 / 裂隙 8 / 深渊 8 / 锚点 8）。R1–R2 不要求打完中间所有阶段，但推荐战力按当前 Next Stage。

---

## 7. 舰船门 `SH-*`（4）

层层递增。触发：下一区域已在星图可见（邻接已恢复航标），但 `ShipGaps` 非空。完成：对应模块等级满足 `RegionCatalog` 门（`13` §5.1）。

| 编号 | questId | 中文 | 描述 | 触发 | 完成条件 | 依赖 | 奖励 |
| --- | --- | --- | --- | --- | --- | --- | --- |
| SH-01 | `sh_range_cargo` | 把航程钉住 | 升级航程与货舱，进入矿脉 | 矿脉可见且门失败 | Range≥1 且 Cargo≥1 | CH-04 或外缘航标 | 信用点 +40 |
| SH-02 | `sh_rift_gate` | 抗住相位干扰 | 为量子裂隙补齐能源/熵/扫描等门 | 裂隙可见且门失败 | 满足裂隙 Gate 全项 | 矿脉航标 | 信用点 +80；`mat_energy_cell` ×4 Q2 |
| SH-03 | `sh_abyss_gate` | 加固船体 | 为深渊补齐船体/熵/货舱 | 深渊可见且门失败 | 满足深渊 Gate | 裂隙航标 | 信用点 +100；`con_repair_kit` ×2 Q2 |
| SH-04 | `sh_anchor_gate` | 全谱适航 | 锚点全部门槛 | 锚点可见且门失败 | 满足锚点 Gate（Lv/Range/Energy/Hull/Entropy/Cargo/Scan/Life） | 护航航标 | 信用点 +150 |

舰船升级材料来自循环任务与远征，不在 SH 包里发够升满的量。门失败时 Explore 弹窗跳转舰船舱（现有 `NexusDialog`）与 SH Active 并存。

---

## 8. 循环任务 `LP-*`（13 条模板，可重复）

**规则：** 同一模板、同一节点内难度一致（次数、推荐战力、失败不升级）。换节点只换 `regionId` / 产出物。  
**触发：** 该节点 R2 已 Claim（战斗挂机已开）；采集类循环在 R1 Claim 后即可。  
**完成：** 计数达到 N 后 Ready；Claim 发奖并**清零计数**，模板可再 Active。  
**并发：** 全局最多 2 条 Loop Active；同 `kind`+`node` 不能叠两份。

N 默认：**战斗挂机 3 周期**、**采集 3 周期**、**商店 1 次买卖**、**维修包 1 次制造**。不随重复次数增加 N（难度一致）。

| 编号 | questId | kind | 节点 | 中文 | 完成条件 | 奖励（每次 Claim） |
| --- | --- | --- | --- | --- | --- | --- |
| LP-farm-outer | `lp_farm_outer` | farm | 外缘 | 清扫残骸 | 该节点 AutoCombat 胜利 ×3 | 信用点 +30；`mat_scrap` ×8 Q2 |
| LP-gather-outer | `lp_gather_outer` | gather | 外缘 | 外缘铁矿 | `node_outer_iron` 结算 ×3 | `mat_iron_ore` ×9 Q2 |
| LP-farm-mining | `lp_farm_mining` | farm | 矿脉 | 矿脉清扫 | AutoCombat 胜利 ×3 | 信用点 +40；`mat_scrap` ×10 Q2 |
| LP-gather-mining | `lp_gather_mining` | gather | 矿脉 | 双采值班 | 矿脉任一采集节点结算 ×3 | 该节点产出 ×9 Q2 |
| LP-farm-rift | `lp_farm_rift` | farm | 裂隙 | 裂隙残响 | AutoCombat 胜利 ×3 | 信用点 +55；`mat_energy_cell` ×4 Q2 |
| LP-gather-rift | `lp_gather_rift` | gather | 裂隙 | 雾中取能 | `node_rift_energy` 结算 ×3 | `mat_energy_cell` ×6 Q2 |
| LP-farm-abyss | `lp_farm_abyss` | farm | 深渊 | 深渊漂流 | AutoCombat 胜利 ×3 | 信用点 +70；`mat_scrap` ×14 Q2 |
| LP-gather-abyss | `lp_gather_abyss` | gather | 深渊 | 高压废料 | `node_abyss_scrap` 结算 ×3 | `mat_scrap` ×15 Q2 |
| LP-farm-convoy | `lp_farm_convoy` | farm | 护航 | 护航清扫 | AutoCombat 胜利 ×3 | 信用点 +80；信用点额外 +20 |
| LP-gather-convoy | `lp_gather_convoy` | gather | 护航 | 生物纤维 | `node_convoy_bio` 结算 ×3 | `mat_biofiber` ×8 Q2 |
| LP-farm-anchor | `lp_farm_anchor` | farm | 锚点 | 锚点残骸带 | Boss 战后 AutoCombat 胜利 ×3 | 信用点 +90；`mat_scrap` ×16 Q2 |
| LP-shop | `lp_shop_restock` | shop | — | 星港补给 | NPC `TryBuy` 或 `TrySell` 成功 ×1 | 信用点 +25 |
| LP-craft-repair | `lp_craft_repair` | craft | — | 补齐维修包 | 成功制造 `con_repair_kit` ×1 | `mat_scrap` ×6 Q2（材料循环，不发成品） |

`LP-shop` 触发：OB-05 已 Claim。`LP-craft-repair` 触发：OB-04 已 Claim。锚点无采集循环（与 `17` Boss 区无采集一致）。

循环任务**不是**日常重置日历；MVP 无日界。玩家可一直刷到材料够升舰船/进阶。

---

## 9. 数量核对

| 类型 | 条数 | 落地 |
| --- | ---: | --- |
| Onboarding | 5 | 5 |
| Story | 11 | 6 |
| Expedition | 18 | 0（阶段战斗已有，任务层未接） |
| Ship | 4 | 0（门检已有，任务层未接） |
| Loop | 13 | 0 |
| **合计** | **51** | **11** |

与玩法内容对齐：43 个战斗阶段由远征环 + 剧情拐点覆盖，不需要 43 条独立剧情。循环任务把挂机时间变成可追踪目标，撑满 3 小时首圈与链后自由刷取。

---

## 10. 追踪器与 Claim 表现

| 层 | 数据源 | 显示 |
| --- | --- | --- |
| 上层 | `chapter_v1` Active | 标题、进度、Go、Claim |
| 中层 | 当前 `EX-*-Rn` 或 `SH-*` | 「下一航路钉子」 |
| 下层 | `onboarding_v1`（链未完成时） | 系统教学 |
| 侧栏 | 最多 2 条 `LP-*` | 计数 `k/N` |

已 Claim 的一次型任务不再显示「前往」。循环任务 Claim 后若仍符合触发，刷新为下一轮 `0/N`。

Go 目标屏：与各表「目标屏」列一致；舰船门失败走现有 Explore 对话框。

---

## 11. 实现备注

| 项 | 说明 |
| --- | --- |
| Wave A | `CH-00`…`CH-05`、`OB-01`…`OB-05` 已接 `chapter_quests.json` / `onboarding.json` |
| 远征环 | 完成条件可读 `RegionRuntimeState.highestClearedStage` 与航标状态；需新链或扩展 Quest 服务，勿把 43 阶段逐条写成 Story |
| 循环 | 计数应写在独立 `quest_loops.json`（或 `onboarding` 扩展字段），Claim 清零；挂机结算处 +1 |
| 旧档 | 已完成 onboarding 且无章节档：沿用 `SkipChapterForLegacySave`；远征/循环对旧档从当前星图进度 Evaluate，可直接 Ready 已满足的环 |
| 非目标 | 日周常日历、阵营声望、多结局、Online 市场任务、第二星域 |

---

## 12. 修订规则

1. 增删 stepId / 改完成条件 / 改链顺序 → 升本文 **主版本**，并改 `17` 或 `24` 对应段落。  
2. 只改默认数量或文案 → 次版本。  
3. 区域阶段名、推荐战力以 `NodeCampaignCatalog` 与 `17` 为准；任务层只引用阶段索引。  
4. 实现状态只写 [PRODUCT-STATUS.md](PRODUCT-STATUS.md)，本文保持契约。
