# Figma UI 重构（全量重写）

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
| Bridge | `BridgeScreen` | 1 |
| Formation | `FormationScreen` → `CardListManager` / lineup positions | 1 |
| Settings | `SettingsScreen` | 1 |
| Explore / Auto Battle | `F2` → `ExploreScreen` → `BattleScene` + `BattleChrome` | 1 |
| Formation | `FormationScreen`（legacy 立绘/徽章） | 1 |
| Crafting / Marketplace / Missions | 需新领域系统 + 新视图 | 4 |

## 关键约定

- 新颜色、间距只进 `NexusTheme`；通用构件进 `NexusUiFactory`。
- **禁止**对全场景 Canvas 做全局 `ApplyThemeToScene` 涂装。
- `AppShell` 只做导航与布局组合；卡牌/物品/战斗数据仍由既有 Manager 拥有。
- 玩家可见 UI 默认文案为 **English**；通过 Settings 可切换 **简体中文（zh-CN）**，由 `LocalizationUtil` + `UiText` 解析。
- 语言选择保存在 PlayerPrefs 键 `ui_language`（`en` / `zh-CN`）。
- 本地化表后续可迁到外部 JSON；当前 AppShell 字符串集中在 `UiText.cs`。

## 已废弃方向

旧文档中的“隐藏 `MainScrollView` + 把旧面板塞进安全区 + 全局主题扫描”已停止扩展。`NexusShell` 由 `AppShell` 取代。
