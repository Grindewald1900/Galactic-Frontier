# 系统文档：新手引导与任务链

> 文档版本：v1.2
> 文档类型：**规则**
> **实现与验收状态**见 [PRODUCT-STATUS.md](../PRODUCT-STATUS.md)；本文仅描述规则与设计标准。

> 上级约束：`../01-core-product-design.md` §16.1 / §20 / §22（v0.4）；开发计划 **P5.0 / P5.4**  
> 关联：`01-deck-and-occupation.md`、`02-auto-battle.md`、`03-region-and-ship.md`、`05-production-and-quality.md`、`07-market-and-card-trade.md`、`08-save-and-seed-data.md`、`11-sector-and-region-content.md`、`00-setting-and-lore.md`  
> 更新日期：2026-08-14  
> 变更：v1.2 — 对齐开拓舰长 / 群星开拓局叙事口吻（见 `00-setting-and-lore.md`）。

---

## 1. 目标与非目标

### 1.1 目标

把已打通的单人核心循环，收成一条**约两小时可演示**的新手脊骨，并以**开拓舰长**身份开场：

1. 用 **线性主任务链**（5 步）教会：编队 → 首战 → 采集 → 制造 → **NPC 商店（市场终端）**；  
2. 进度可**持久化**，重进游戏不丢；  
3. Bridge / Missions 与相关页有**软引导**（高亮下一步入口），不强弹强制教程；  
4. **Solo 不出现玩家市场**步骤或文案；  
5. 为后续区域包装（P5.3）与角色扩容（P5.1）留挂钩，但不依赖它们完成主链。

玩家设定摘要：群星开拓局新晋舰长，持开拓许可证，开局仅有小型开拓舰、基础战队、简易采集与市场终端（详见 `00-setting-and-lore.md`）。

完成本契约后，P5.4a/b 有唯一实现依据；M5「可演示」的引导侧条件满足。

### 1.2 非目标

- 分支剧情、多结局、阵营声望任务（P5.2 仅标签，不进本主链）  
- 强制锁定 UI / 模态「点这里」遮罩（MVP 只用软 CTA）  
- 玩家市场、Online 交易、礼品码（礼品码属 Debug/设置，见 `16`）  
- 虫洞 / 暗面等特殊星域模式（见 `12`，后置）  
- 一次性塞满 30–50 角色或重写 Characters/Cards Legacy 页  

---

## 2. 术语

| 术语 | 定义 |
| --- | --- |
| **主任务链（Onboarding Chain）** | 固定 5 步线性任务；id 前缀 `ob_` |
| **步骤（Step）** | 链上的一个节点；状态：`Locked` / `Active` / `Completed` |
| **完成条件（CompleteWhen）** | 可检测的玩家行为或存档事实（非纯点「领取」） |
| **领取（Claim）** | 步骤已满足完成条件后，玩家在 Missions 点领取奖励并推进下一步 |
| **软 CTA** | 文案/高亮按钮指向目标 `AppScreen`；可忽略，不挡玩法 |
| **演示两小时** | 目标体验时长；数值与掉落曲线由 P5.3/P5.5 调，**不**用本文件锁秒表 |

---

## 3. 硬约束（不可违背）

1. **Solo 主链终点是 NPC 商店**，不得引导玩家市场或 Online。  
2. **不强制多人、不依赖未实现系统**（研究 / 运输完整循环、特殊星域等）。  
3. 步骤必须 **可观测完成**：用已有服务事件或存档字段判定，禁止「只点领取即完成」冒充通关。  
4. **线性解锁**：同时最多一个 `Active` 主链步骤（可浏览已完成步骤）。  
5. 奖励不得破坏经济骨架：以信用点、材料、维修包为主；装备奖励至多 1 件且走 `ItemFactory`。  
6. 新增持久字段须登记存档契约（见 §7）；缺文件则默认初始化，不污染旧档语义。  
7. UI 优先 **原生 Nexus Screen**（替换 `AppShell.BuildMissionsPlaceholder`），少挂 Legacy。

---

## 4. MVP 拍板决议

### 4.1 主链五步（固定顺序）

| 顺序 | stepId | 中文名 | 英文名 | 目标屏 | 完成条件（摘要） |
| --- | --- | --- | --- | --- | --- |
| 1 | `ob_formation` | 编成首支开拓战队 | Form a Deck | Formation | 任一解锁卡组 `MemberCount >= 1`（建议引导满 5，验收 ≥1） |
| 2 | `ob_first_battle` | 完成第七前沿首场清剿 | First Battle | Explore → Battle | 任意区域挑战胜利至少 1 次（`world` 进度或战后标记） |
| 3 | `ob_gather` | 启动简易采集设备 | Start Gathering | Explore | 成功开始 `DeckActionType.Gather` 至少一次，**或**本地仓库存在任意采集产物 `quantity >= 1` |
| 4 | `ob_craft` | 在舰船工坊**手动**完成一次制造 | Craft Once | Crafting | 成功 `TryStartRecipe` 且经历完整周期后仓库出现产物（占 `Produce` 编制） |
| 5 | `ob_npc_shop` | 使用市场终端连接星港 | Visit Starport | Market（星港） | 任意一次 `NpcShopService.TryBuy` **或** `TrySell` 成功 |

**链完成后**：主链状态 `Completed`；Missions 显示「新手航线完成——开拓局确认你具备独立作业资格」；可展示可选后续提示（刷取 / 舰船升级），**不**再强制步骤。

### 4.2 完成判定细则

| stepId | 推荐检测点 | 说明 |
| --- | --- | --- |
| `ob_formation` | `DeckService` 任意解锁卡组成员数 | 开局种子若已预填编队，则进游戏即可 `Active`→可 Claim；仍应引导玩家打开 Formation 确认 |
| `ob_first_battle` | 区域 `Progress` 离开 `Locked`/`Available` 进入已通关系，或显式 `onboardingFirstBattleWon` 标志 | 挂机 AutoCombat **不**算首战 |
| `ob_gather` | `actionType == Gather` 曾 Running，或库存含 `GatherNodeCatalog` 任一 `outputDefId` | 避免只开 UI 未行动 |
| `ob_craft` | 制造成功日志 / 库存增量含配方产物 | Debug 直接改数量 **不算**（可用 `craftedOnce` 标志防刷） |
| `ob_npc_shop` | 商店买卖成功回调置 `shopTradeOnce` | 纯打开 Market 不算 |

> 实现可在领域层设 `OnboardingFlags`（布尔）由各服务在成功路径置位，避免 UI 轮询脆弱。

### 4.3 奖励表（MVP 默认；数值可配置）

| stepId | 奖励 | 备注 |
| --- | --- | --- |
| `ob_formation` | 信用点 `+50` | 与新档起步信用叠加无妨 |
| `ob_first_battle` | `mat_scrap` ×10（Q2） | 堆叠键 `(itemDefId, quality)` |
| `ob_gather` | `mat_iron_ore` ×6 或节点对应矿 | 与 Starter Seed 不冲突 |
| `ob_craft` | `con_repair_kit` ×1 | 衔接耐久系统 |
| `ob_npc_shop` | 信用点 `+100` | 巩固商店循环 |

领取时走 `CurrencyService` / `ProductionService.TryAddLocal`；满仓则奖励进 `pendingLoot` 或提示先清理仓库（与离线领取一致）。

### 4.4 软 CTA（P5.4b）

| 所在页 | 行为 |
| --- | --- |
| Bridge | 若主链未完成：摘要条「下一步：{步骤名}」+ 按钮跳转目标屏 |
| Missions | 列表展示五步；`Active` 高亮；Completed 打勾；Claim 按钮 |
| Formation / Explore / Crafting / Market | 当该页是当前步骤目标时，顶栏或提示条显示任务目标一句 |

规则：

- CTA **可关闭本次会话**（内存即可），重进游戏可再出现；  
- **不**禁止玩家自由探索其他屏；  
- 文案双语走 `UiText`。

### 4.5 与现有循环的映射

```text
编队 (P1) → 首战 (P2 Explore/Battle) → 采集 (P3 Gather)
    → 制造 (P3 Crafting，首周期入仓) → 星港 NPC (P4 MarketScreen)
```

- 离线领取、挂机刷取、舰船升级：**不**进主五步，可作为链后「建议」文案。  
- Debug 改物品/信用：**不**自动推进步骤（除非另开 QA 作弊完成，默认关）。

### 4.6 演示节奏（指导，非硬锁）

| 阶段 | 建议玩家时间 | 对应步骤 |
| --- | --- | --- |
| 开局熟悉 UI | 0–15 min | `ob_formation` |
| 首区战斗与战报 | 15–40 min | `ob_first_battle` |
| 开图后资源侧 | 40–75 min | `ob_gather` + `ob_craft` |
| 经济闭环 | 75–100 min | `ob_npc_shop` |
| 自由刷取 / 下一区 | 100–120+ min | 链后；P5.3 曲线负责可读性 |

具体秒数与掉落权重由 `11-sector-and-region-content.md` + P5.5 调整。

---

## 5. UI 契约

### 5.1 Missions 屏（替换占位）

- 入口：`AppScreen.Missions`（现 `BuildMissionsPlaceholder`）  
- 标题：任务 / Missions  
- 内容：主链列表（5 行）+ 当前步骤详情（完成条件说明、奖励预览、Claim）  
- Claim：仅当 `Active && CompleteWhenSatisfied`  
- 链完成：展示完成横幅；可选「前往 Explore / 舰船舱」按钮  

### 5.2 导航与面包屑

- 保持现有 Nav「任务」；F8 等快捷键不变  
- Breadcrumb：`UiText` 已有 Inventory 等；补齐任务相关字符串  

### 5.3 不做

- 独立「教程关」场景  
- 强制全屏遮罩逐步点击  

---

## 6. 领域模型（建议）

```text
OnboardingState
- chainId: "onboarding_v1"
- activeStepId: string
- completedStepIds: List<string>
- flags: OnboardingFlags
- claimedStepIds: List<string>   // 已领奖，防重复发奖

OnboardingFlags
- formationReady
- firstBattleWon
- gatherStartedOrLooted
- craftedOnce
- shopTradedOnce
```

静态配置（只读）：

```text
OnboardingStepDef
- stepId, order, titleEn/Zh, hintEn/Zh
- targetScreen: AppScreen
- rewards: RewardGrant[]
```

服务门面建议：`OnboardingService`（EnsureLoaded / Evaluate / TryClaim / GetView）。

---

## 7. 存档

| 项目 | MVP 规则 |
| --- | --- |
| 文件 | `saves/{playerId}/onboarding.json`（或并入 `idle.json` 的 `onboarding` 字段；**优先独立文件**便于 Migrator） |
| 缺文件 | 新档或旧档升级：初始化 `chainId=onboarding_v1`，`activeStepId=ob_formation`，flags 全 false；若编队已满足则 Evaluate 后可直接 Claim |
| saveVersion | 若独立文件：随 `meta` 升版时 Migrator 增加「缺则创建」；不强制所有旧档立刻重跑玩法 |
| 原子写 | 与 `08-save-and-seed-data.md` 一致 |

领取奖励与 flags 写入须在同一次保存意图中完成（先改内存再 Save），避免领奖丢进度。

---

## 8. 设计验收标准

### 8.1 P5.4a Missions

- 新档进入 MainScene，Missions 显示 5 步，第 1 步为 Active  
- 满足编队条件后可 Claim，步骤 2 变为 Active，奖励入账  
- 按序完成至 `ob_npc_shop`，链标记完成（运行时路径已接线）  
- 杀进程重进，进度写入 `onboarding.json`；已领奖励不重复发放  
- Solo 全文案无「玩家市场 / 挂单」  

### 8.2 P5.4b 软 CTA

- Bridge 显示当前下一步名称与跳转  
- 位于目标屏时有一句任务提示（`OnboardingBanner`）  
- 忽略 CTA 仍可自由游玩，不卡死其他系统  

### 8.3 回归

- 制造 Start 仍即时入仓；商店买卖仍刷新信用点  
- Debug 改数量不单独完成 `ob_craft` / `ob_npc_shop`  

---

## 9. Cursor 实现切片建议

按开发计划 §10 顺序：

1. **本文已完成（P5.0）** → 实现前勿改五步语义，除非升本文主版本。  
2. **P5.4a**：`OnboardingService` + `onboarding.json` + `MissionsScreen` 替换占位。  
3. **P5.4b**：Bridge / 各目标屏软 CTA。  
4. 再开 **P5.3** 星域包装（本链不阻塞，但完成后体验更贴「两小时」）。  

标准提示词入口类：`AppShell.cs`、`DeckService`、`WorldService`、`ProductionService`、`NpcShopService`、`CurrencyService`。

---

## 10. 修订规则

1. 改步骤顺序、完成条件或奖励语义 → 升本文 **主版本**，并更新 `11-mvp-development-plan.md` §5 P5 表。  
2. 纯文案 / 默认数量 → 次版本或配置表，不改 stepId。  
3. 与 `07` / `08` / `11` 冲突时：流通与存档以 `07`/`08` 为准；掉落曲线以 `11` 为准；**引导顺序以本文为准**。  
