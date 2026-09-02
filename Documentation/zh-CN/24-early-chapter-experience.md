# 首章前期体验（Chapter 1 · Wave A）

> 文档版本：v1.0  
> 文档类型：**内容 / 体验契约（首章权威）**  
> **实现与验收状态**见 [PRODUCT-STATUS.md](PRODUCT-STATUS.md) §3 文档 24。  
> 上级约束：[02-core-product-design.md](02-core-product-design.md)、[03-worldbuilding.md](03-worldbuilding.md) §10  
> 关联：[17-sector-and-onboarding.md](17-sector-and-onboarding.md)（`onboarding_v1` 系统骨架）、[22-ux-loop-and-ia.md](22-ux-loop-and-ia.md)（任务追踪器双层结构）  
> 更新日期：2026-09-01  
> **Wave A 范围**：序章 + 任务 1–5（`chapter_v1` 六步）；任务 6–10 与第 2–3 天节奏见本文 §4 标注，**不在 Wave A 代码验收内**。

---

## 1. 体验目标与三次反馈闭环

首章设计目标不是把所有系统依次教一遍，而是让玩家**连续完成三次强反馈闭环**。前期结束时，玩家应理解：

> 战斗打开空间，挂机提供资源，生产解决航行问题，探索决定下一步发展方向。

### 1.1 阶段节奏（完整首章 ~3 小时；Wave A 覆盖前 48 分钟）

| 阶段 | 时长 | 主要爽点 | 玩家形成的认知 | Wave A |
| --- | --- | --- | --- | --- |
| 0–15 分钟 | 序章 + 任务 1 | 抽卡、快速组队、首次胜利 | 新角色能改变队伍 | **是** |
| 15–45 分钟 | 任务 2–3 | 升级、克制敌人、战力跃升 | 失败不是只靠堆数值 | **是** |
| 45–90 分钟 | 任务 4–5 + 采集预备 | 采集、挂机、制造模块 | 等待会转化为永久成长 | **部分**（任务 4–5；制造闭环 Wave B） |
| 90–180 分钟 | 任务 6–10 | 路线选择、未知信号、Boss | 探索能发现计划外内容 | 否（Wave B） |
| 第 2–3 天 | 自动化、第二舰队 | 离线积累、专业化资源 | 离线积累服务于更远探索 | 否（Wave C） |

首日不强迫连续等待：第一次制造 15–30 秒、第一次正式挂机约 10 分钟（Wave B 落地）；Wave A 仅铺垫矿脉战斗与扫描节点。

### 1.2 三次闭环在 Wave A 的落点

| 闭环 | 完整首章意图 | Wave A 实现 |
| --- | --- | --- |
| **战斗** | 获得新角色 → 调整阵容 → 反败为胜 | 任务 1–3：科尔保底招募 → 阿斯拉前排/科尔后排/战术变更 → 外缘带再战 + `StrategyComparePopup` |
| **生产** | 发现航路障碍 → 挂机采集 → 制造模块 → 打开新节点 | **未闭环**；任务 5 胜利后区域可采集，完整制造/模块教学在 Wave B（任务 6–7） |
| **探索** | 选择路线 → 异常坐标 → 击败航标守卫 → 「断航可能是主动封锁」 | **部分**：任务 4 扫描定位 `body_mining_spur`；断航真相与 Boss 在 Wave B/C |

---

## 2. 前期主要 NPC

### 2.1 舰载导航 AI「弥塔」（Mita）

| 项 | 内容 |
| --- | --- |
| 定位 | 新手引导者，非全知系统助手 |
| 设定 | 前任舰长留下的旧式导航核心；资料库损坏，只能读取部分航网记录 |
| 功能 | 教操作、汇总目标与缺口、标记异常信号；关键剧情记忆错乱（Wave B+） |
| 性格 | 冷静、简短；偶尔将玩家误识别为前任舰长 |
| 伏笔 | 「身份校验失败……欢迎回来，舰长。更正：您不是那个人。」 |
| Wave A 实现 | `Dialogues.json` 中 `ch1_wake`、`ch1_prologue_clear`、`ch1_mita_recruit`；立绘暂用 `portraitId: "Asra"` 占位 |

弥塔每次只解释**当前决策**（1–3 句），不用长墙文本。

### 2.2 阿斯拉（Asra）

边境卫队机械师；前排护卫、装甲强化。Wave A：序章编队、`ch1_asra_formation` 编队教学、`ch1_prologue_clear` 战后台词。

### 2.3 玛吉（Magki）

裂隙商盟回收者；破甲、处决。Wave A：`ch1_magki_armor`（矿脉战前）；`enc_mining_main` 破甲教学遭遇。

### 2.4 瑟妮娅（Sernia）

相位信号监听者；控制、增益。Wave A：`ch1_sernia_signal`（扫描任务）；Explore 探测矿脉信号。

### 2.5 科尔（Cole）

外缘港飞行员；**后排突击**、优先攻击敌方后排。Wave A 第四名正式舰员：

- 角色：`Characters/Roster/Cole.cs`（`Archetype.Assassin`，`GetBackRowCards` 索敌）
- 招募：`GachaService` 首次 pull 必出 Cole（`gacha.json` → `starterColeGranted`）
- 编队验收：任务 2 要求 Cole 在后排（slot index ≥ 3）

### 2.6 米娅（Mia）— Wave B 占位

休眠舱医师；治疗、净化。完整五人队与隐藏遗迹线在 **任务 8–10（Wave B）** 实现；Wave A 文档与 `chapter_v1` _catalog 均不引用。

---

## 3. 前期敌人教学矩阵

前期敌人各负责一个战斗概念；Wave A 已落地遭遇以 **粗体** 标出。

| 敌人 | 机制 | 教学目标 | Wave A 遭遇 |
| --- | --- | --- | --- |
| **清扫无人机** | 低血量、数量感 | 认识范围攻击、首战低风险 | **`enc_prologue_sweep`**（`isTutorial: true`） |
| 残骸切割机 | 高护甲 | 使用玛吉破甲 | `enc_mining_main`（任务 5） |
| 失控护卫机 / 外缘巡逻 | 保护后排 | 学习索敌和站位 | **`enc_outer_main`**（Asra + Magki，任务 3） |
| 熵雾孢子体 | 持续伤害 | 净化或速攻 | Wave B |
| 相位掠食者 | 间歇隐身 | 扫描或标记 | Wave B |
| 商盟劫掠者 | 抢夺战利品后撤退 | 优先目标选择 | Wave B |
| 战争残响 | 复制玩家技能 | 调整阵容而非堆战力 | Wave B |
| 航标守卫 | 多阶段 Boss | 综合检验战斗、舰船和补给 | Wave C（任务 10） |

战后失败分析全文（「第 4 回合前排失守…」）为 **Wave B+** 目标；Wave A 用 `StrategyComparePopup` 固定文案代替。

---

## 4. 序章 + 任务 1–10 分镜

### 序章：失控的返航（0–5 分钟）— **Wave A**

玩家在一艘失控舰船上醒来；弥塔误认身份；外缘港拒绝靠港；清扫无人机接近。可用档案：阿斯拉、玛吉、瑟妮娅（`ChapterQuestService.EnsureStarterCards`）。  
流程：`AppShell` 首次进 Nexus → `ch1_wake` → `enc_prologue_sweep` → 自动 Claim `ch1_prologue` → `ch1_prologue_clear` + 奖励（120 信用点 + 招募券 ×1）。

### 任务 1：失效的人员信标（5–12 分钟）— **Wave A**

`ch1_recruit_cole`：紧急招募；**首次任意招募必得科尔**；`RecruitScreen` 显示章节提示与「前往编队」CTA。

### 任务 2：拼凑出的舰队（12–18 分钟）— **Wave A**

`ch1_formation`：阿斯拉前排 + 科尔后排 + `combatStrategyId != "Balanced"`；Claim 时镜像 `ob_formation`。

### 任务 3：外缘港清理令（18–28 分钟）— **Wave A**

`ch1_outer_cleanup`：击败 `sec01_outer_belt` 主遭遇；Claim 镜像 `ob_first_battle`；战后 `StrategyComparePopup`（52% → 86% 文案）。  
`ch1_mita_strategy` 对话脚本已写入 `Dialogues.json`，当前以弹窗 UI 文案为主（尚未经 `DialogueService` 自动播放）。

### 任务 4：熵雾中的周期信号（28–38 分钟）— **Wave A**

`ch1_scan_signal`：探测 `body_mining_spur` 至 `GridNodeState.Located`；`ch1_sernia_signal`。

### 任务 5：无人矿场（38–48 分钟）— **Wave A**

`ch1_mining_spur`：击败 `sec01_mining_spur`；`ch1_magki_armor`；胜利后区域可采集（衔接 `ob_gather`）。

### 任务 6：让机器替我们工作 — **Wave B**

挂机采集三段节奏（15s / 2min / 10min）；`ob_gather` 完整教学。

### 任务 7：舰体仍在漏气 — **Wave B**

首次制造维修包（20s）；扫描组件材料缺口与市场解法；`ob_craft` 叙事包装。

### 任务 8：两条航路 — **Wave B**

护航航道 vs 量子裂隙；决策对话与路线分叉。

### 任务 9：为远征准备补给 — **Wave B**

远征准备度 UI；离线 20–30 分钟资源规划。

### 任务 10：你打开了一道锁 — **Wave C**

边境锚点三阶段 Boss；航标修复与前任舰长警告。

---

## 5. Wave A 实现映射表

| stepId | 中文名 | 完成条件（可观测） | linkedOnboarding | dialogueId（开始 / 领取） | 奖励（Claim） | 钩子 |
| --- | --- | --- | --- | --- | --- | --- |
| `ch1_prologue` | 失控的返航 | `flags.prologueWon`（赢得 `enc_prologue_sweep`） | — | `ch1_wake` / `ch1_prologue_clear` | 120 信用点 + 招募券 ×1 | `AppShell.TryBeginEntryFlow`；`BattleController.NotifyPrologueWon`；序章胜利自动 TryClaim |
| `ch1_recruit_cole` | 失效的人员信标 | 拥有 Cole **或** `flags.emergencyRecruitDone` | — | `ch1_mita_recruit` / — | 80 信用点 | `GachaService.TryPull` → `NotifyEmergencyRecruit`；`starterColeGranted` |
| `ch1_formation` | 拼凑出的舰队 | `flags.formationReady` 或 `MeetsFormationLayout` | `ob_formation` | `ch1_asra_formation` / — | 100 信用点 | `DeckService` 改槽/战术 → `ChapterQuestService.Evaluate` |
| `ch1_outer_cleanup` | 外缘港清理令 | `flags.outerCleanupWon` 或 `sec01_outer_belt` 已清 | `ob_first_battle` | — / — | 150 信用点 + 废料 ×15 | `BattleController` 外缘胜利；`StrategyComparePopup` |
| `ch1_scan_signal` | 熵雾中的周期信号 | `flags.miningSignalScanned` 或 `body_mining_spur` Located | — | `ch1_sernia_signal` / — | 80 信用点 | `GridService` 探测 → `NotifyMiningSignalScanned` |
| `ch1_mining_spur` | 无人矿场 | `flags.miningSpurWon` 或 `sec01_mining_spur` 已清 | — | `ch1_magki_armor` / — | 200 信用点 + 铁矿 ×20 | `BattleController` 矿脉胜利 |

**目录常量**：`ChapterQuestCatalog.ChapterId = "chapter_v1"`  
**序章专用**：`PrologueEncounterId = "enc_prologue_sweep"`，`PrologueRegionId = "ch1_prologue"`（不写常规 `world.json` region clear）

---

## 6. 系统集成契约

### 6.1 与 `onboarding_v1` 并行

| 系统 | 职责 |
| --- | --- |
| `chapter_v1`（`ChapterQuestService`） | 剧情线性链、弥塔对话、首章战斗与招募节奏 |
| `onboarding_v1`（`OnboardingService`） | 五步系统教学；`FeatureUnlockCatalog.json` 解锁依据 |
| 镜像规则 | `linkedOnboardingStepId` 在剧情步骤 **Claim** 时调用 `OnboardingService.NotifyFormationReady` / `NotifyFirstBattleWon` |

新档：两条链同时从各自第一步 Active。旧档：若 `onboarding.json` 已完成且缺 `chapter_quests.json`，`SkipChapterForLegacySave()` 标记 `chapterSkipped` + `chapterCompleted`，避免强迫老玩家重跑序章。

### 6.2 `DialogueService`

- 数据源：`Assets/Resources/Data/Dialogues.json`
- 触发：`ChapterQuestService` 在步骤 Active（`dialogueIdOnStart`）、Claim（`dialogueIdOnClaim`）、序章入口（`TryBeginEntryFlow`）调用 `DialogueService.Play`
- Wave A 脚本：`ch1_wake`、`ch1_prologue_clear`、`ch1_mita_recruit`、`ch1_asra_formation`、`ch1_mita_strategy`（数据就绪）、`ch1_sernia_signal`、`ch1_magki_armor`

### 6.3 `FeatureUnlockService`

功能解锁**仍按** `onboarding_v1` 步骤 Claim 推进，不由 `chapter_v1` 直接写 `unlocks.json`。剧情链通过 `linkedOnboardingStepId` 减少重复操作，不替代解锁表。

### 6.4 `chapter_quests.json` 存档

路径：`saves/{playerId}/chapter_quests.json`（`DefaultProperty.CHAPTER_QUEST_DATA`）

```json
{
  "chapterId": "chapter_v1",
  "activeStepId": "ch1_prologue",
  "completedStepIds": [],
  "claimedStepIds": [],
  "flags": {
    "prologueWon": false,
    "emergencyRecruitDone": false,
    "formationReady": false,
    "outerCleanupWon": false,
    "miningSignalScanned": false,
    "miningSpurWon": false,
    "strategyCompareShown": false
  },
  "chapterCompleted": false,
  "chapterSkipped": false,
  "count": 0
}
```

读写：`DataUtil.SaveChapterQuestState` / `LoadChapterQuestState`；`count` 由服务写入为 `completedStepIds.Count`。

### 6.5 UI 入口

| 组件 | 行为 |
| --- | --- |
| `AppShell` | `EnsureLoaded` + `TryBeginEntryFlow`（序章） |
| `QuestTrackerDrawer` | 上层「首章」步骤 + 下层 `onboarding_v1` |
| `CommanderGoalService` | 优先 Active 剧情步骤 |
| `MissionsScreen` | 首章分区 + onboarding 分区 |
| `RecruitScreen` | `ch1_recruit_cole` Active 时章节提示 |

---

## 7. Wave A PlayMode 验收清单

- [ ] **新档序章**：进入 Nexus → 播放 `ch1_wake` → 序章战斗 ≤2 分钟 → 自动推进任务 1，奖励入账（信用点 + 招募券）
- [ ] **科尔保底**：首次 `GachaService.TryPull` 必得 Cole；`gacha.json` 中 `starterColeGranted: true`
- [ ] **任务 2 编队**：阿斯拉 slot 0–2、科尔 slot 3–4、`combatStrategyId != Balanced` 后可 Claim；Claim 后 `ob_formation` 可领
- [ ] **任务 3 外缘战**：`sec01_outer_belt` 胜利后 `StrategyComparePopup` 出现一次；外缘清理可 Claim；`ob_first_battle` 可领
- [ ] **任务 4 扫描**：Explore 探测后 `body_mining_spur` ≥ Located；步骤可 Claim
- [ ] **任务 5 矿脉**：`sec01_mining_spur` 胜利可 Claim；矿脉区域可发起采集
- [ ] **任务追踪器**：剧情步骤与 onboarding 五步同时可见；已完成剧情不挡 onboarding
- [ ] **功能解锁**：仍随 `ob_*` Claim 解锁导航；不因剧情跳步提前解锁制造/市场
- [ ] **旧档兼容**：已完成 onboarding 的旧档不重跑序章（`chapterSkipped`）
- [ ] **双语**：任务标题/提示/对话 `textEn`/`textZh` 切换正常
- [ ] **持久化**：杀进程重进，`chapter_quests.json` 与 `gacha.json` 进度一致

---

## 8. 风险与降级

| 风险 | 降级方案（Wave A 已采用或备选） |
| --- | --- |
| Cole 立绘缺失 | `ImageUtil` 默认图；角色名 Debug 可见 |
| 序章满编 5 人复杂 | 3 人实战卡（Asra/Magki/Sernia）+ 战斗内教程 encounter；未实现无人机占位卡 |
| 战术对比难精确模拟 | `MeetsFormationLayout` 二元判定 + `StrategyComparePopup` 固定 52%→86% 文案 |
| `ch1_mita_strategy` 未接线 | 弹窗 UI 已覆盖教学意图；后续可 Claim/战后 `DialogueService.Play` |
| 旧存档污染 | `SkipChapterForLegacySave`：`onboarding` 完成则跳过整章 |
| 双轨任务困惑 | `QuestTrackerDrawer` 分区标题「首章」/「新手航线」；`CommanderGoalService` 剧情优先 |

---

## 附录 A. 代码索引

| 路径 | 说明 |
| --- | --- |
| `Assets/Resources/Scripts/ChapterQuest/` | 领域层 + `ChapterQuestService` |
| `Assets/Resources/Data/Dialogues.json` | `ch1_*` 对话脚本 |
| `Assets/Resources/Scripts/Gacha/GachaService.cs` | `starterColeGranted` |
| `Assets/Resources/Scripts/World/Domain/EncounterCatalog.cs` | `enc_prologue_sweep` 等 |
| `Assets/Resources/Scripts/ChapterQuest/StrategyComparePopup.cs` | 任务 3 战术反馈 |

完整首章（任务 6–10）、前 3 天节奏、NPC 闲聊与失败分析见本文 §4 标注及 [10-mvp-development-plan.md](10-mvp-development-plan.md) Wave B/C 里程碑。
