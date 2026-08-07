# Figma UI 重构

## 设计来源

- Figma Make：[PC Card Based Idle RPG](https://www.figma.com/make/zBemuUe0jkpQiLxSpSKXRo/PC-Card-Based-Idle-RPG)
- 目标画布：桌面端 `1920 × 1080`
- Unity 实现：uGUI + TextMesh Pro，运行时装配，不修改已有场景和 Prefab 的序列化引用

## 视觉系统

新界面使用 `NEXUS COMMAND` 风格：深海军蓝背景、低对比度面板边框、金色当前状态，以及青色、绿色和紫色功能状态。所有颜色与固定布局尺寸集中在：

- `Assets/Resources/Scripts/UI/Nexus/NexusTheme.cs`

主要结构尺寸：左侧导航 `72 px`、顶部资源栏 `56 px`、面包屑 `34 px`、底部状态栏 `24 px`。`NexusUiFactory` 统一创建 Canvas、面板、按钮和 TMP 文本。当前 UI 默认语言为英文，并直接使用项目默认 TMP 字体资产。

## 页面映射

| Figma 页面 | Unity 入口 | 实现方式 |
| --- | --- | --- |
| 舰桥 Bridge | `F1` | 新的运行时仪表盘，显示活动、进度、舰队和日志 |
| 探索/卡牌战斗 | `F2` | 加载原有 `BattleScene`，叠加统一 NEXUS 框架 |
| 编队 Formation | `F3` | 映射 `CurrentScene.BATTLE_MENU` |
| 角色 Characters | `F4` | 映射 `CurrentScene.CHARACTER_MENU` |
| 仓库 Inventory | `F5` | 映射 `CurrentScene.INVENTORY_MENU` |
| 制造 Crafting | `F6` | 映射 `CurrentScene.BUILDING_MENU` |
| 市场 Market | `F7` | 映射 `CurrentScene.SHOP_MENU` |
| 任务 Missions | `F8` | 新的任务列表与详情页面 |
| 设置 Settings | 左下角齿轮 | 映射 `CurrentScene.SETTINGS_MENU` |

舰桥和任务是本轮新增页面；已有玩法页面保留控制器、数据绑定和按钮事件，只调整可用内容区域并应用统一主题。这样不会复制业务逻辑，也方便逐页用正式 Figma 布局替换旧 Prefab。

## 运行时装配

入口为 `NexusUiBootstrap`，在 `MainMenuScene`、`MainScene` 和 `BattleScene` 加载完成后自动创建一个 `NexusShell`：

1. 创建背景、顶部栏、导航栏、面包屑和状态栏。
2. 在主场景隐藏旧的横向 `MainScrollView`。
3. 为旧页面预留新的安全内容区域。
4. 对现有 Canvas、按钮、滚动条和 TMP 文本应用 NEXUS 主题。
5. 在战斗与主场景切换时记忆目标页面。

关键代码：

- `Assets/Resources/Scripts/UI/Nexus/NexusUiBootstrap.cs`
- `Assets/Resources/Scripts/UI/Nexus/NexusShell.cs`
- `Assets/Resources/Scripts/UI/Nexus/NexusUiFactory.cs`
- `Assets/Resources/Scripts/UI/Nexus/NexusTheme.cs`

## 后续开发约定

- 新颜色、间距和框架尺寸只加入 `NexusTheme`，不要散落硬编码。
- 通用 uGUI 构件加入 `NexusUiFactory`；业务页面组合保留在独立控制器中。
- 新导航项需要同时更新 `NexusScreen`、`screenTitles`、侧栏按钮和页面映射。
- 保留旧页面业务事件时，优先更换视图层，不复制控制器逻辑。
- 当前阶段所有玩家可见 UI 文案统一使用英文，不在界面中混用中文；未来引入本地化时再配置项目内静态字体及语言表。
- 设计更新后先核对 1920×1080 基准，再测试 16:10 与超宽屏的 Canvas Scaler 表现。

## 当前边界

本轮实现了全局框架、导航、舰桥、任务和旧玩法页面的主题迁移。角色、仓库、制造、市场等旧页面仍沿用原 Prefab 的内部布局；后续可以按照同一设计令牌逐页重建，而无需再次改动场景导航。
