# Figma UI 重构（全量重写）

> vNext（2026-08-29）：统一反馈层（Dialog / Snackbar / Notification）、功能逐步解锁与导航 grey out、舰桥舰队滑动组件 — 见下文专节。

## 设计来源

- Figma Make：[PC Card Based Idle RPG](https://www.figma.com/make/zBemuUe0jkpQiLxSpSKXRo/PC-Card-Based-Idle-RPG)
- 目标画布：桌面端 `1920 × 1080`
- 实现策略：**按屏重写视图**，不再把 NEXUS 外壳叠在旧 Prefab 面板上做主题涂装

## 战斗决策（全自动）

产品确认：战斗为**全自动回合制**，玩家无需在战斗中点选行动。  
Unity 现有 `BattleController` + `Character` 策略与此一致。

玩家流程：

1. 舰桥 / 编队配置（复用角色立绘、稀有度徽章）；
2. 探索页选择星域（复用 `Images/Planets`）；
3. 进入 `BattleScene` 自动结算（`BattleChrome` + legacy 卡牌/特效/战报）；
4. 返回舰桥查看战情。

复用资源入口：`NexusCardVisual`（角色图、Tier 框、徽章、星球、事件图、UI Battle 图标）。

## 视觉系统

颜色与布局常量集中在：

- `Assets/Resources/Scripts/UI/Nexus/NexusTheme.cs`

与 Figma Make `index.css` 对齐：背景 `#07091a`、表面 `#0d1228` / `#111830`、金色 `#e8a832`、青色 `#38bdf8`。  
导航默认收起宽度 `64 px`，展开 `220 px`；顶栏 `48 px`；面包屑 `32 px`；状态栏 `22 px`。

## 架构

```text
NexusUiBootstrap
  └── AppShell（主导航与内容宿主）
        ├── BridgeScreen / FormationScreen / SettingsScreen（本阶段原生重建）
        ├── LegacyPanelAdapter（Characters / Cards / Inventory / Crafting / Market 临时委托旧面板）
        └── BattleScene → BattleChrome（Figma chrome，战斗逻辑不变）
```

| Figma 页面 | Unity | Phase |
| --- | --- | --- |
| Bridge | `BridgeScreen` | 1 | 顶栏三统计 + 当前卡组 + **舰队滑动组件**（vNext） |
| Formation | `FormationScreen` → `CardListManager` / lineup positions | 1 |
| Settings | `SettingsScreen` | 1 |
| Explore / Auto Battle | `F2` → `ExploreScreen` → `BattleScene` + `BattleChrome` | 1 |
| Formation | `FormationScreen`（legacy 立绘/徽章） | 1 |
| Crafting / Marketplace / Missions | 需新领域系统 + 新视图 | 4 |

## 关键约定

- 新颜色、间距只进 `NexusTheme`；通用构件进 `NexusUiFactory`。
- **禁止**对全场景 Canvas 做全局 `ApplyThemeToScene` 涂装。
- `AppShell` 只做导航与布局组合；卡牌/物品/战斗数据仍由既有 Manager 拥有。
- 玩家可见 UI 默认文案为 **English**；通过 Settings 可切换 **简体中文（zh-CN）**。
- 语言选择保存在 PlayerPrefs 键 `ui_language`（`en` / `zh-CN`）。
- **UI chrome**（导航、按钮、提示）：`Resources/Data/Localization/UiStrings.json`（`id` / `en` / `zh`），由 `LocalizationUtil.Get` / `Format` 解析；`UiText` 仅作强类型键访问器。
- **内容目录**（物品、配方、商店、星域、任务、技能）：继续使用各自的 `displayNameEn`/`displayNameZh` 或 `{en,zh}`（`LocalizedText`），经 `LocalizationUtil.T` / `GetLocalizedText` 解析；**本阶段不迁入 UiStrings**。
- 中文渲染：`CjkFontBootstrap` 将 `Resources/Fonts/NotoSansSC-Regular.otf` 注册为 TMP 动态 fallback（不检入完整 CJK atlas）；缺省时回退系统字体（如微软雅黑）。
- 已安装的 `com.unity.localization` **本阶段不接入游戏代码**（Nexus UI 为代码构建，非 Prefab LocalizedString）。

## 统一反馈层（Dialog / Snackbar / Notification）

游戏内所有需要玩家感知的结果反馈，须走**统一风格**的三类组件，禁止各 Screen 各自拼临时弹窗或裸 `Debug.Log` 替代。

| 组件 | 用途 | 交互 | 典型场景 |
| --- | --- | --- | --- |
| **Dialog** | 需确认或需完整阅读的模态信息 | 遮罩 + 居中面板；主/次按钮；可点遮罩关闭（Destructive 除外） | 功能解锁庆祝、二次确认、错误说明、奖励明细（与 `RewardPopup` 同视觉族） |
| **Snackbar** | 轻量操作结果 | 底部短条，2–4 秒自动消失；可选手动关闭 | 保存成功、材料不足、启动行动失败、分解完成 |
| **Notification** | 非阻塞系统播报 | **舰桥顶栏下方**横向滚动条（marquee / 队列轮播）；不打断操作 | 离线收益可领、区域首通、新任务可用、舰队召回完成 |

### 视觉与实现约定

- 颜色、圆角、边框、字号一律继承 `NexusTheme`；文案走 `UiStrings.json` + `UiText`。
- 建议集中实现：`NexusDialog`（或复用/扩展 `RewardPopup` 基类）、`NexusSnackbar`、`NexusNotificationBar`；由 `AppShell` 或单例宿主挂载，各 Screen 只发「展示请求」。
- **优先级**：Dialog 打开时暂停 Snackbar 入队；Notification 可与 Snackbar 并存，但不得遮挡 Dialog。
- 与现有 `RewardPopup`、`ModuleUpgradePopup` 对齐：同一 scrim 透明度、同一面板 surface 色、同一按钮高度。

### Notification 栏（舰桥）

- 位置：`AppShell` 内容区顶栏与面包屑之间，或 `BridgeScreen` 专属顶带（全 Nexus 可见时挂 AppShell 更优）。
- 行为：多条消息 FIFO 轮播；重要消息（如功能解锁）可同时触发 Dialog + Notification 摘要。
- 持久化：Notification 本身不存档；触发源（如 pending offline）仍读领域状态。

---

## 功能逐步解锁（Feature Unlock）

**产品决议**：游戏中**所有功能**（导航入口、页内子功能、行动类型等）均须**逐步解锁**；不得开局展示完整导航后仅隐藏内容。

### 解锁前（Locked）

| 位置 | 表现 |
| --- | --- |
| **导航栏**（`AppShell` 侧栏） | 对应 `AppScreen` 按钮 **grey out**（降饱和 + `NexusTheme.MutedText`）；**不可进入** |
| 页内入口 / CTA | 同上；点击时 **不跳转**，改为 Snackbar 或 Dialog 说明解锁条件 |
| 卡组槽 / 并行位 / 区域 | 沿用各系统文档锁态；文案统一走解锁表 |

点击已 grey out 的导航项时，优先弹出 **Dialog**：标题「功能未解锁」+ 条件摘要 +「知道了」；条件较长时用 Snackbar 作次要提示。

### 解锁时（Just Unlocked）

1. 领域层检测到条件满足 → 写入 `FeatureUnlockState`（或并入 `onboarding.json` / 独立 `unlocks.json`，须登记 `saveVersion`）。  
2. **必须**弹出 **Dialog**：功能名称、一句话说明、可选插图/icon；主按钮「前往」跳转目标屏，次按钮「稍后」。  
3. 同步 **Notification** 滚动一条摘要（如「市场终端已开放 — 可在舰桥连接星港」）。  
4. 导航栏该项恢复可点击样式；若玩家选「稍后」，不强制跳转。

### 配置与权威

- 解锁条件表：`Resources/Data/FeatureUnlockCatalog.json`（建议）；字段含 `featureId`、`requiredStepId?`、`requiredRegionId?`、`requiredShipLevel?`、`navScreen?`。  
- 与 [17-sector-and-onboarding.md](17-sector-and-onboarding.md) 主链、`11-deck-and-occupation.md` 卡组槽表、`13-region-and-ship.md` 区域门 **对齐**，避免重复魔法数。  
- **实现状态**：已落地 — 见 [PRODUCT-STATUS.md](PRODUCT-STATUS.md)。

---

## 舰桥（Bridge）布局

### 当前实现

顶栏三格统计（战力 / 探索度 / 信用点）；「当前卡组」+ 其下 **舰队水平滑动组件**（旗舰卡 + 预留泊位）。Notification 条挂在 `AppShell` 顶栏与面包屑之间。

### 目标布局（vNext）

```text
[ Notification 滚动条 — 全宽，可选 ]

[ 标题 / 副标题 / 任务 Banner ]

[ 战力 ] [ 探索度 % ] [ 信用点 ₵ ]     ← 仅三格；删除原「并行」统计卡

[ 当前卡组 Active Fleet — 5 人立绘横排，点击进 Formation ]

[ 舰队 Fleet Carousel — 水平滑动，见 21 §3.5 ]

[ 待领取 / 区域条 / CTA / 日志 ... ]
```

| 变更 | 说明 |
| --- | --- |
| **删除** | 与探索度、信用点并列的第四格「并行 / Parallel ops」统计卡 |
| **删除** | 页内其它重复的并行摘要（顶栏已移除后，并行信息只出现在舰队组件内） |
| **保留** | 「当前卡组」面板：展示**主战斗卡组** 5 槽立绘与卡组名 |
| **改造** | 原「并行中行动 / RunningOps」区域 → **舰队组件**：多张**舰队卡片**横排，**水平滑动**（ScrollRect / carousel） |

### 舰队组件（Fleet Carousel）

与 [21-fleet-factions-and-exploration.md](21-fleet-factions-and-exploration.md) §3.5 一致。

**MVP 过渡期**（仅单舰 `ShipEntity`）：仍显示 **1 张舰队卡**，卡片内容 = 舰名 + 绑定卡组 + 当前行动（战斗/采集/制造/空闲）+ 并行占用状态；为多舰 UI 预留滑动容器。

**后 MVP**（`FleetState` 多舰）：每舰一张卡；卡内迷你编制或状态图标；左右滑动浏览整支舰队；点击进 Ship / Formation。

卡片信息字段（最小集）：

- 舰名 / 蓝图图标  
- 绑定卡组名  
- 行动类型 + Running / Idle / Stationed  
- （可选）进度条：当前行动周期  

不再单独在舰桥顶部用数字卡片展示 `busy/max`；并行上限在舰队卡或 Formation 页查看。

---

## 已废弃方向

旧文档中的“隐藏 `MainScrollView` + 把旧面板塞进安全区 + 全局主题扫描”已停止扩展。`NexusShell` 由 `AppShell` 取代。
