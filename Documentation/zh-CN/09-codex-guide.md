# Codex 工作指南

本文件是 Codex 或其他自动化开发代理处理本项目时的快速上下文。

## 开始任务前

依次读取：

1. `README.md`
2. 与任务对应的专题文档
3. `ProjectSettings/ProjectVersion.txt`
4. `ProjectSettings/EditorBuildSettings.asset`
5. 目标脚本及其直接调用者
6. `git status --short --branch`

如果存在 `AGENTS.md`，其指令优先于本文档。

## 默认工作边界

- 第一方业务代码：`Assets/Resources/Scripts`
- 第一方运行时数据：`Assets/Resources/data`
- 第一方场景与 Prefab：`Assets/Resources/Scenes`、`Assets/Resources/Prefabs`
- 第三方资源默认只读。
- 保留用户现有未提交改动，不要清理或回滚无关文件。

## 修改时必须考虑

### Unity 引用

- 脚本 GUID 来自 `.meta`；不要删除后重新创建已有 `.meta`。
- 场景/Prefab 中的字段名与脚本字段相连；重命名必须迁移。
- `MonoBehaviour` 初始化顺序可能影响 Singleton。
- `DontDestroyOnLoad` 对象可能与新场景中的重复组件冲突。

### 数据兼容

- `PlayerEntity`、`CardEntity`、`ItemEntity` 等字段可能已经写入玩家存档。
- 枚举顺序可能直接影响 JSON 中的整数值。
- `JsonUtility` 不保存字典、事件和普通属性。
- 修改存档路径时必须兼容已有 `persistentDataPath/saves`。

### 当前架构现实

- 多个 Manager 通过静态 `Instance` 紧耦合。
- 主场景承担大量系统，不能只编译单文件后假设运行正常。
- `Resources.Load` 路径是运行时契约。
- 多处 `FakeData()` 属于原型行为；删除前先确认真实数据来源。
- 第一方代码没有 `.asmdef`，会进入大型 `Assembly-CSharp`。

## 推荐任务流程

```text
定位调用链
  → 确认场景/Prefab/存档影响
  → 做最小一致修改
  → Unity 编译
  → 目标场景冒烟测试
  → git diff --check
  → 更新文档
```

## 重点入口索引

| 任务 | 首先阅读 |
| --- | --- |
| 新游戏/加载 | `MainMenu.cs`, `GameLoadManager.cs`, `DataUtil.cs` |
| 主菜单面板 | `MainScrollController.cs`, `GameStatusManager.cs` |
| 卡牌生成 | `CardDataManager.cs`, `CardEntity.cs`, `Character.cs` |
| 卡牌列表 | `CardListManager.cs`, `Card.cs`, `CardPreviewController.cs` |
| 编队 | `LineupManager.cs`, `DropZoneHandler.cs`, `PortraitSlot.cs` |
| 战斗 | `BattleController.cs`, `CharacterSkillController.cs`, 具体角色类 |
| Buff/Debuff | `BuffManager.cs`, `DebuffManager.cs`, `Status.cs` |
| 抽卡 | `CardDrawingManager.cs`, `CardResultManager.cs`, `27-gacha-and-progression.md` |
| 物品 | `InventoryItemManagerBase.cs`, `ItemOperationManager.cs`, `ItemSlot.cs` |
| 存档 | `DataUtil.cs`, `Wrappers.cs`, `DefaultProperty.cs` |
| 星球 | `RadarSystem.cs`, `PlanetListManager.cs`, `PlanetDetailManager.cs` |

## 不应自行假设

- 不要假设所有 `Instance` 在任意场景都非空。
- 不要假设 `HomeScene` 是正式流程的一部分。
- 不要假设测试数据可以直接删除。
- 不要假设 Base64 是安全加密。
- 不要假设根目录生成的 `.csproj` 可提交。
- 不要将第三方示例脚本当作第一方架构进行大规模整理。
