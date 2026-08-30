# 开发、验证与 Agent 工作指南
> 文档版本：v1.0
> 文档类型：**工程**
> 由原 `08-development-guide` + `09-codex-guide` + `28-debug-and-test-mode` 合并。
> 状态见 [PRODUCT-STATUS.md](PRODUCT-STATUS.md)。

---

## 开发与验证

### 修改边界

优先修改：

- `Assets/Resources/Scripts`
- `Assets/Resources/Prefabs`
- `Assets/Resources/Scenes`
- `Assets/Resources/data`
- `Documentation`

第三方目录如 `Assets/Plugins`、`Assets/Packages`、`Assets/TextMesh Pro`、`Assets/LeanTween` 和 `Assets/JMO Assets` 应视为外部代码，除非任务明确要求。

### Unity 序列化规则

- 重命名 Inspector 字段时使用 `[FormerlySerializedAs("oldName")]` 保留场景/Prefab 数据。
- 移动脚本时必须保留对应 `.meta`，否则 GUID 改变，场景组件会丢失。
- 新增 `Assets` 内脚本时必须提交 `.meta`。
- 不要通过文本批量重写 `.unity` 或 `.prefab`，除非已验证 YAML 引用和 Unity 导入结果。
- 将字段移入基类时保持字段名和可序列化性，Unity 才能继续恢复已有数据。

### 新增系统的建议步骤

1. 先定义或扩展 `Entity` 数据模型。
2. 将纯规则放在普通 C# 类或静态工具中。
3. 用一个 Controller/Manager 连接 Unity 生命周期和 UI。
4. 通过 Inspector 引用 Prefab，而不是在业务代码中重复硬编码路径。
5. 若必须使用 `Resources.Load`，将路径集中为常量。
6. 明确保存时机和旧存档兼容方式。
7. 添加 EditMode 或 PlayMode 测试。
8. 更新本文档中受影响的系统说明。

### 新增角色清单

- 在 `CharacterName` 添加枚举值；
- 新建继承 `Character` 的实现；
- 实现普通攻击、特殊攻击、被动和可能稀有度；
- 在 `CardDataManager.InitCharacterList()` 注册；
- 在 `CharacterSkillController` 注册实例；
- 增加技能 JSON 和图片；
- 增加角色专长；
- 验证抽卡、预览、编队和战斗。

### 新增场景清单

- 将场景加入 Build Settings；
- 避免重复创建 `DontDestroyOnLoad` 单例；
- 更新 `SceneLoader.SceneName` 和必要的 `CurrentScene`；
- 验证直接打开该场景时的依赖；
- 验证从正常入口进入时的依赖。

### 验证方式

调试开关、礼品码、Debug 导航页见 **[07-development-guide.md](07-development-guide.md)** v1.1。  
- **Debug Mode**：`DebugModeController` 单例；设置页开启后导航出现「Debug模式」。  
- **礼品码**：仅设置页领奖（`GiftCodeService`），不再用 `000`/`001` 开 Debug。  
- **`DefaultProperty.isDebug`**：只控制存档明文；样例数据仍走 `DevDataSettings`。

#### 静态检查

```powershell
git diff --check
rg -n "TODO|FIXME|FakeData" Assets/Resources/Scripts -g "*.cs"
```

#### EditMode 测试（P0.5）

- 程序集：`Assets/Tests/EditMode/GalacticFrontier.Tests.EditMode.asmdef`
- 领域：`Assets/Resources/Scripts/Battle/Domain`（`noEngineReferences`）
- 在 Unity：**Window → General → Test Runner → EditMode**，运行 `GalacticFrontier.Tests.EditMode`
- 战斗结算路径禁止新增未播种的 `UnityEngine.Random`；用 `BattleRng`

#### 编译

首选让 Unity 完成脚本导入和编译，然后检查 Console。Unity 生成的 `.csproj` 在本项目中包含本机扩展路径，命令行 `dotnet build` 不一定可复现 Unity 编译环境。

#### 测试

项目已包含 `com.unity.test-framework` 与第一方 EditMode 程序集：

```text
Assets/Tests/EditMode/
Assets/Resources/Scripts/Battle/Domain/
```

并使用 `.asmdef` 隔离测试。优先覆盖：

- 伤害、减伤和命中计算；
- 权重随机边界；
- 卡牌升级与属性倍率；
- 背包容量、堆叠和转移；
- 存档往返与旧数据兼容。

### 提交前检查

- Unity Console 无新增错误；
- 目标场景完成冒烟测试；
- `git diff --check` 通过；
- `.cs` 与 `.meta` 成对存在；
- 未提交 `Library`、`Temp`、`Logs`、`.csproj` 或 `.sln`；
- 未混入无关场景、字体、材质或第三方资源变更；
- 文档与当前行为一致。

## Codex / Cursor 工作边界

本文件是 Codex 或其他自动化开发代理处理本项目时的快速上下文。

### 开始任务前

依次读取：

1. `README.md`
2. 与任务对应的专题文档
3. `ProjectSettings/ProjectVersion.txt`
4. `ProjectSettings/EditorBuildSettings.asset`
5. 目标脚本及其直接调用者
6. `git status --short --branch`

如果存在 `AGENTS.md`，其指令优先于本文档。

### 默认工作边界

- 第一方业务代码：`Assets/Resources/Scripts`
- 第一方运行时数据：`Assets/Resources/data`
- 第一方场景与 Prefab：`Assets/Resources/Scenes`、`Assets/Resources/Prefabs`
- 第三方资源默认只读。
- 保留用户现有未提交改动，不要清理或回滚无关文件。

### 修改时必须考虑

#### Unity 引用

- 脚本 GUID 来自 `.meta`；不要删除后重新创建已有 `.meta`。
- 场景/Prefab 中的字段名与脚本字段相连；重命名必须迁移。
- `MonoBehaviour` 初始化顺序可能影响 Singleton。
- `DontDestroyOnLoad` 对象可能与新场景中的重复组件冲突。

#### 数据兼容

- `PlayerEntity`、`CardEntity`、`ItemEntity` 等字段可能已经写入玩家存档。
- 枚举顺序可能直接影响 JSON 中的整数值。
- `JsonUtility` 不保存字典、事件和普通属性。
- 修改存档路径时必须兼容已有 `persistentDataPath/saves`。

#### 当前架构现实

- 多个 Manager 通过静态 `Instance` 紧耦合。
- 主场景承担大量系统，不能只编译单文件后假设运行正常。
- `Resources.Load` 路径是运行时契约。
- 多处 `FakeData()` 属于原型行为；删除前先确认真实数据来源。
- 第一方代码没有 `.asmdef`，会进入大型 `Assembly-CSharp`。

### 推荐任务流程

```text
定位调用链
  → 确认场景/Prefab/存档影响
  → 做最小一致修改
  → Unity 编译
  → 目标场景冒烟测试
  → git diff --check
  → 更新文档
```

### 重点入口索引

| 任务 | 首先阅读 |
| --- | --- |
| 新游戏/加载 | `MainMenu.cs`, `GameLoadManager.cs`, `DataUtil.cs` |
| 主菜单面板 | `MainScrollController.cs`, `GameStatusManager.cs` |
| 卡牌生成 | `CardDataManager.cs`, `CardEntity.cs`, `Character.cs` |
| 卡牌列表 | `CardListManager.cs`, `Card.cs`, `CardPreviewController.cs` |
| 编队 | `LineupManager.cs`, `DropZoneHandler.cs`, `PortraitSlot.cs` |
| 战斗 | `BattleController.cs`, `CharacterSkillController.cs`, 具体角色类 |
| Buff/Debuff | `BuffManager.cs`, `DebuffManager.cs`, `Status.cs` |
| 抽卡 | `CardDrawingManager.cs`, `CardResultManager.cs`, `19-characters-and-progression.md` |
| 物品 | `InventoryItemManagerBase.cs`, `ItemOperationManager.cs`, `ItemSlot.cs` |
| 存档 | `DataUtil.cs`, `Wrappers.cs`, `DefaultProperty.cs` |
| 星球 | `RadarSystem.cs`, `PlanetListManager.cs`, `PlanetDetailManager.cs` |

### 不应自行假设

- 不要假设所有 `Instance` 在任意场景都非空。
- 不要假设 `HomeScene` 是正式流程的一部分。
- 不要假设测试数据可以直接删除。
- 不要假设 Base64 是安全加密。
- 不要假设根目录生成的 `.csproj` 可提交。
- 不要将第三方示例脚本当作第一方架构进行大规模整理。

## Debug 模式与测试工具

### 1. 目标与非目标

#### 1.1 目标

1. 用 **`DebugModeController` 单例** 统一控制 Debug 模式开关；  
2. Debug **开启后**，Nexus 导航栏出现 **「Debug模式」** 入口；  
3. Debug 页可 **修改本地道具数量**、**新增 / 编辑当前卡牌**；  
4. **礼品码放在设置页**，只用于领取奖励道具（每存档每码一次）；  
5. 与 Dev Data（样例 FakeData）、存档明文 `isDebug` 三者分离。

#### 1.2 非目标

- 用礼品码打开 Debug / Dev Data（已废弃 `000` / `001` 旧语义）  
- Release 默认开启 Debug  
- 线上反作弊  

---

### 2. 三层开关（必须分清）

| 层 | 名称 | 控制什么 | 默认 |
| --- | --- | --- | --- |
| **A** | `DefaultProperty.isDebug` | 存档 JSON 明文 vs Base64 | 原型常 true |
| **B** | `DevDataSettings` | 是否允许 `IDevDataProvider` 样例数据 | **关** |
| **C** | **`DebugModeController.IsEnabled`** | 导航「Debug模式」+ Debug 作弊页 | **关** |

```text
正式可玩 = Solo + Debug Mode OFF + Dev Data OFF + 正规存档路径
```

`isDebug` **不得**打开 Debug Mode 或 Dev Data。

---

### 3. Debug Mode 单例

#### 3.1 类

`Assets/Resources/Scripts/Utils/DebugTools/DebugModeController.cs`

- `DontDestroyOnLoad` 单例，`RuntimeInitializeOnLoadMethod(BeforeSceneLoad)` 自举  
- `IsEnabled` / `SetEnabled(bool)` / `Toggle()`  
- `event Action<bool> Changed` → `AppShell` 重建导航  

#### 3.2 开启方式

| 方式 | 说明 |
| --- | --- |
| **设置 → 开发者 → 开启 Debug 模式** | 主路径（Nexus `SettingsScreen`） |
| 命令行 `-debugMode` | 进程级强制开 |
| EditorPrefs `GalacticFrontier.DebugMode.enabled` | Editor 持久（随 `SetEnabled` 写入） |

关闭：设置页再次点击，或 Debug 页「关闭 Debug 模式」。

#### 3.3 导航行为

- `IsEnabled == true`：`BuildNavigation` 在「任务」与「设置」之间插入 `AppScreen.Debug`  
- 标题：`UiText.ScreenDebug` → **Debug Mode / Debug模式**  
- 关闭后若当前停在 Debug 页，自动回 **设置**

---

### 4. Debug 页面能力（`DebugScreen`）

路径：`UI/Nexus/Screens/DebugScreen.cs`  
仅当 Debug Mode 开启时可进入。

| 分区 | 能力 |
| --- | --- |
| **道具（本地仓库）** | 对 Copper / Steel / GoldBar / SteelBar / Water / Wood：输入数量「应用」、或 +10；写入 `ItemManager` / `inventory_local.json` |
| **卡牌** | 列表摘要；新增随机卡；升级第一张；第一张经验升一级；刷新列表 |
| **关闭** | `SetEnabled(false)` 并刷新壳层 |

后续可扩展：选中具体卡编辑、远程仓库、舰船模块等（见开放钩子）。

旧 Prefab `DebugPanelController` 保留但不作为主入口；不再由礼品码打开。

---

### 5. 礼品码（设置页 · 仅领奖）

#### 5.1 入口

- Nexus：**设置 → 礼品码** 输入框 +「领取」  
- 遗留设置面板 `SettingsManager` 输入框同样走 `GiftCodeService.TryRedeem`  
- **不再**用礼品码切换 Debug / Dev Data  

#### 5.2 服务

`GiftCodeService`（`Utils/DebugTools/GiftCodeService.cs`）

- 大小写不敏感  
- 每玩家每码：`PlayerPrefs` 键 `GiftRedeemed_{playerId}_{CODE}`，领过拒绝  
- 可发：材料、信用点、随机卡牌、**头像框**（`GiftReward.FrameId`）

#### 5.3 设计码表（MVP）

| 码 | 中文名 | 奖励 |
| --- | --- | --- |
| `WELCOME` | 指挥官欢迎礼包 | Copper×40、Water×20、信用+1000 |
| `COPPER100` | 铜材补给 | Copper×100 |
| `STEEL50` | 钢材货运 | Steel×50 |
| `GF-BETA` | 星际前线内测礼包 | Copper×30、Steel×20、GoldBar×5、信用+3000 |
| `CREDIT5K` | 信用点投放 | 信用+5000 |
| `RECRUIT` | 紧急征召 | 随机角色卡×1 |
| `WATER20` | 水源箱 | Water×20 |
| `WOOD80` | 木材捆 | Wood×80 |
| `GF-FRAME` | 内测光环头像框 | 解锁并写入 `frame_beta` |
| `EVENT-RIFT` | 裂隙庆典头像框 | 解锁 `frame_event_rift`（活动框的 MVP 占位） |

> 正式上线前可轮换码表；勿在玩家可见商店页展示完整列表（内测可放补丁说明）。

#### 5.4 废弃码

| 旧码 | 旧行为 | 现行为 |
| --- | --- | --- |
| `000` | 打开 Debug 面板 | 忽略并打日志，提示去设置开 Debug |
| `001` | 切换 Dev Data | 同上 |

---

### 6. Dev Data Mode（层 B，联调样例）

仍由 `DevDataSettings` / `IDevDataProvider` 控制，**与 Debug Mode 独立**。

开启：EditorPrefs、`-devData`、或日后 Debug 页子开关（可选）。  
用途：战斗样例敌人、抽卡材料等；**禁止** Provider 内直接 `Save*`。

日常：Debug Mode ON + Dev Data OFF 即可改库存/卡牌；仅缺内容联调时再开 Dev Data。

---

### 7. 日志前缀

| 前缀 | 用途 |
| --- | --- |
| `[DEBUG]` | Debug Mode 开关、面板操作 |
| `[GIFT]` | 礼品码领取 |
| `[DEV-DATA]` | 样例数据 |
| `[SAVE]` | 存档迁移 |

---

### 8. 自动化与冒烟

#### EditMode

`Assets/Tests/EditMode/` — 规则测试不依赖 Debug Mode。

#### 手测冒烟

1. 设置开启 Debug → 导航出现「Debug模式」  
2. 改 Copper 数量 → 进仓库可见（或重进仓库刷新）  
3. 新增随机卡 → 卡牌列表增加  
4. 关闭 Debug → 导航入口消失  
5. 设置领取 `WELCOME` → 成功；再领 → 已领取提示  
6. 输入 `000` → 不打开 Debug  

---

### 9. 实现映射

| 组件 | 路径 |
| --- | --- |
| Debug 单例 | `Utils/DebugTools/DebugModeController.cs` |
| 礼品码 | `Utils/DebugTools/GiftCodeService.cs` |
| Debug 页 | `UI/Nexus/Screens/DebugScreen.cs` |
| 设置页 | `UI/Nexus/Screens/SettingsScreen.cs` |
| 导航 | `AppShell` + `AppScreen.Debug` |
| 库存 SetQty | `InventoryItemManagerBase.SetItemQuantity` |

---

### 10. 设计验收标准

- `DebugModeController.Instance` 场景切换后仍存在  
- 仅设置 / `-debugMode` / EditorPrefs 可开 Debug；礼品码不能开  
- Debug ON 时导航有「Debug模式」；OFF 时无  
- Debug 页可改本地道具数量并持久化  
- Debug 页可新增卡、升级/加经验第一张卡  
- 设置页礼品码可领奖；同档重复领取失败  
- `isDebug` 不影响 Debug / Dev Data  
- Release 默认 Debug OFF  

---

### 11. 开放钩子

- Debug 页增加 Dev Data 子开关、存档路径显示  
- 按 `itemDefId` 自由添加任意道具  
- 选中卡编辑等级/稀有度  
- 礼品码服务端校验（Online）  
- IMGUI 叠加层  

---

### 12. 参考

- 存档：`06-data-and-save.md`  
- 运行模式：`18-online-and-play-modes.md`  
- 开发指南：`07-development-guide.md`  
- MVP 计划：`10-mvp-development-plan.md`
