# 快速开始

## 1. 环境要求

- 使用 Unity `6000.0.20f1` 打开项目。
- 项目依赖 URP、UGUI、Input System、TextMesh Pro、XCharts、DOTween/LeanTween 等包或导入资源。
- 不要手工维护根目录下的 `.sln` 和 `.csproj`；它们由 Unity 生成，并已被 `.gitignore` 忽略。

## 2. 启动项目

推荐从 `Assets/Resources/Scenes/MainMenuScene.unity` 启动。

Build Settings 当前状态：

| 场景 | 是否启用 | 作用 |
| --- | --- | --- |
| `MainMenuScene` | 是 | 初始化持久化管理器，创建/加载存档 |
| `MainScene` | 是 | 卡牌、编队、抽卡、星球、背包和设置等主界面 |
| `BattleScene` | 是 | 回合制战斗和战报 |
| `HomeScene` | 否 | 早期移动/场景原型，不属于当前构建流程 |

主流程：

```mermaid
flowchart LR
    A["MainMenuScene"] -->|"新游戏 / 加载存档"| B["MainScene"]
    B -->|"选择星球并开始"| C["BattleScene"]
    C -->|"当前仅显示战报"| D["战斗结果面板"]
```

## 3. 第一次阅读代码

建议依次打开：

1. `Main/MainMenu.cs`：新建和加载游戏入口。
2. `Utils/DataUtil.cs`：持久化对象、存档路径和 JSON 读写。
3. `Main/MainScrollController.cs`：主场景菜单切换。
4. `Cards/CardDataManager.cs`：卡牌生成、角色与稀有度概率。
5. `Entity/CardEntity.cs`：卡牌数据模型和属性计算。
6. `Cards/CardListManager.cs` 与 `Battle/LineupManager.cs`：卡牌列表和编队。
7. `Battle/BattleController.cs`：完整战斗协程。

## 4. 修改前检查

```powershell
git status --short --branch
git diff --check
```

本项目可能存在用户正在编辑的 Unity 资源。不要覆盖与当前任务无关的 `.unity`、`.prefab`、材质、字体或导入设置变更。

## 5. 最小冒烟测试

在 Unity 中依次验证：

1. `MainMenuScene` 能创建新存档并进入 `MainScene`。
2. 主菜单各面板能切换。
3. 卡牌列表能显示、筛选、排序和进入预览。
4. 编队卡牌在进入 `BattleScene` 后能正确落位。
5. 战斗能运行到胜负或最大回合，并显示战报。
6. 保存后重新加载，玩家和卡牌数据仍可读取。
