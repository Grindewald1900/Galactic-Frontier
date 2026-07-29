# 项目结构

## 根目录

```text
Galactic Frontier/
├─ Assets/
│  ├─ Resources/              # 第一方运行时代码、场景、预制体和数据
│  ├─ Plugins/                # 第三方插件
│  ├─ Packages/               # 导入的美术/工具资源，不等同于 UPM Packages
│  ├─ TextMesh Pro/           # TMP 示例和资源
│  ├─ LeanTween/              # Tween 库和示例
│  └─ JMO Assets/             # Cartoon FX 等第三方资源
├─ Packages/                  # Unity Package Manager 配置
├─ ProjectSettings/           # Unity 项目设置
├─ Documentation/zh-CN/       # 本文档
└─ README.md                  # 原项目展示说明
```

一般业务修改应限制在 `Assets/Resources`。除非任务明确要求，不要重构第三方资源目录。

## 第一方脚本

| 目录 | 职责 | 代表类型 |
| --- | --- | --- |
| `Achievement` | 成就和徽章 UI | `AchievementController` |
| `Battle` | 战斗循环、Buff/Debuff、编队、战报 | `BattleController`, `LineupManager` |
| `Cards` | 卡牌生成、展示、列表、抽卡结果 | `CardDataManager`, `CardListManager`, `Card` |
| `CharacterPanel` | 角色面板、技能、移动 | `CharacterInfoManager`, `SpellController` |
| `Characters` | 各角色战斗行为 | `Character`, `Asra`, `Magki`, `Sernia` |
| `Entity` | 可序列化领域模型 | `CardEntity`, `PlayerEntity`, `ItemEntity` |
| `Inventory` | 本地/远程物品和背包 UI | `InventoryItemManagerBase`, `ItemManager` |
| `Main` | 主菜单、加载存档、主界面导航和全局状态 | `MainMenu`, `GameStatusManager` |
| `Planet` | 星球列表和详情 | `PlanetListManager`, `PlanetDetailManager` |
| `Scene` | 场景加载与场景效果 | `SceneLoader` |
| `Settings` | 保存、清空卡牌、礼品码和设置面板 | `SettingsManager` |
| `Shop` | 抽卡材料选择 | `CardDrawingManager` |
| `UI` | 可复用 Slot、交互和面板控制器 | `ItemSlot`, `PortraitSlot`, `RadarSystem` |
| `Utils` | 存档、图片、加密、定位和选择工具 | `DataUtil`, `ImageUtil`, `TargetSelector` |

## Resources 约定

因为 `Assets/Resources` 是 Unity 的特殊目录，运行时调用 `Resources.Load` 时路径：

- 从 `Resources/` 的下一层开始；
- 不包含扩展名；
- 大小写和目录名应与资源一致。

当前示例：

```csharp
Resources.Load<TextAsset>("data/BaseAttributes");
Resources.Load<GameObject>("Prefabs/Effect/Bleeding");
Resources.Load<Sprite>("Images/Default");
```

## 场景中的主要第一方组件

### MainMenuScene

`DataUtil`、`GameStatusManager` 和 `SceneLoader` 在这里创建并跨场景保留；`MainMenu` 与 `GameLoadManager` 负责入口 UI。

### MainScene

包含绝大多数玩法控制器：卡牌列表与预览、编队、抽卡、背包、星球雷达、角色信息、事件、设置和调试面板。

### BattleScene

包含 10 个 `Card` 视图（双方各 5 个位置）、`BattleController`、`BattleInfo`、`BattleReportManager`、`CardDataManager` 和效果管理器。

### HomeScene

包含移动、漂浮物和背景循环等早期原型组件；当前未加入构建。
