# 核心系统实现与类职责
> 文档版本：v1.0
> 文档类型：**工程**
> 由原 `06-core-systems` + `12-core-classes` 合并。
> 状态见 [PRODUCT-STATUS.md](PRODUCT-STATUS.md)。

---

## 核心系统实现

### 1. 玩家与主菜单

入口为 `Main/MainMenu.cs`：

- 新游戏：`DataUtil.CreatePlayerData()` 创建带 GUID 的 `PlayerEntity`（`playerID` 此后不可改），保存后加载 `MainScene`。
- 加载游戏：显示存档面板，`GameLoadManager.LoadGame()` 从每个玩家目录读取数据并创建 `GameLoadSlot`。
- 选中存档：`DataUtil.SetCurrentPlayer()` 更新当前玩家及相关文件路径。
- **玩家资料**：`playerName` 与 `avatar.png` 可自定义；`playerID` 只读 — 见 [06-data-and-save.md](06-data-and-save.md)。

`MainScrollController` 在 `MainScene` 中创建菜单项，使用 `CurrentScene` 枚举切换各子面板，并同步更新 `GameStatusManager.CurrentScene`。

### 2. 卡牌数据生成

`CardDataManager` 在 `Awake` 初始化：

1. 角色实现列表：`Asra`、`Magki`、`Sernia`；
2. `SkillData_encrypted.bytes` 中的技能；
3. `BaseAttributes.json` 中的等级基础属性；
4. 职业属性权重；
5. 卡牌稀有度基础概率。

生成流程：

```mermaid
flowchart LR
    A["GetCharacter()"] --> B["按 Character.weight 加权抽角色"]
    B --> C["GetCardTier(character)"]
    C --> D["GetCardEntity(character)"]
    D --> E["基础属性 + 角色专长 + 随机专长"]
    E --> F["CardEntity"]
```

`GetAdjustedTierProbabilities(level)` 会随等级增加高稀有度概率、降低低稀有度概率。`WeightedRandom` 按字典权重进行选择。

### 3. CardEntity 属性模型

`CardEntity` 同时保存：

- 身份：GUID、卡名、角色、职业、稀有度；
- 成长：等级、经验、进化状态、战力；
- 基础战斗属性：生命、攻击、防御、命中、闪避、暴击、暴伤、减伤、能量恢复、速度；
- 编队位置；
- 普通专长、角色专长和技能。

属性按三个倍率字典分层：

```text
面板属性 = 基础值 × characterAttrs × panelAttrs
战斗属性 = 面板属性 × battleAttrs
```

- `characterAttrs`：角色固有专长；
- `panelAttrs`：卡牌专长等常驻修正；
- `battleAttrs`：战斗期间 Buff/Debuff 修正。

属性 Setter 会重新计算战力。升级和数据变更通过 `OnDataChanged`、`OnCardUpgraded` 事件通知视图。

### 4. 卡牌列表、预览与编队

`CardListManager`：

- 从 `DataUtil.LoadCardData()` 初始化列表；
- 根据实体数量增减 `Card` GameObject；
- 支持排序、职业过滤、查找、添加和删除；
- `GetInLineCardEntities()` 返回已设置编队位置的卡牌；
- 更新列表后调用 `DataUtil.SaveCardData()`。

`LineupManager` 管理 5 个 `PortraitSlot`：

- 点击槽位后记录 `selectedIndex`；
- `DropZoneHandler` 接收拖入的卡牌；
- `AddLineupCard` 更新目标槽位及 `CardEntity.position`；
- 战斗场景按 `LineupPosition` 将卡牌放到相同索引。

### 5. 抽卡

当前抽卡分为两个阶段：

1. `CardDrawingManager` 选择 1 次或 10 次抽取，预扣材料；
2. `CardResultManager.InitCards(drawCount)` 生成结果并展示统计；翻牌结束后 `ShowReport` → `CardListManager.AddCardEntity` 入池存档。

`CardDrawingManager` 材料仅在 **Dev Data Mode** 下由 `IDevDataProvider.FillSampleGachaMaterials` 注入（内存，不写档）；正式模式材料列表为空，待接真实背包。`InitCards` 在 Dev OFF 时目前不会调用 `CardDataManager` 生成（空结果）——正式路径见系统文档拍板。

完整规则、代价表、软保底、绑定/分解与落地顺序：见 [19-characters-and-progression.md](19-characters-and-progression.md)。

### 6. 战斗

`BattleController` 的主要流程：

1. 从持久化的 `CardListManager` 或存档读取玩家编队；
2. 敌方：仅 Dev Data Mode 下由 `IDevDataProvider.CreateSampleEnemyParty` 生成最多 5 张；正式模式空槽，待 P2.3 遭遇表；
3. 隐藏未使用位置并初始化双方 `Card`；
4. 每回合按 `Speed` 降序行动；
5. 能量满时使用 `SpecialAttack`，否则使用 `NormalAttack`；
6. 判断任一方是否全部阵亡；
7. 达到胜负或最大回合数后显示战报；Confirm 返回 MainScene Explore（`AppScreen.Battle`），Esc 中途返回舰桥。  
8. 伤害命中/暴击使用 `BattleRng`（`BattleController.BattleSeed`）；公式在 `CombatMath`，EditMode 可回归。

伤害核心：

```text
命中率 = clamp(攻击方命中 - 防守方闪避, 0, 1)
减伤率 = clamp(log₁.₄(max(防御, 1)) × 0.01 + 固定减伤, 0, 1)
伤害 = 战斗攻击 × 暴击倍率 × (1 - 减伤率) × 技能倍率
```

`Character` 是抽象策略：

- `NormalAttack(Card, List<Card>)`
- `SpecialAttack(Card, List<Card>)`
- `PassiveSkill(Card, List<Card>)`
- `GetPossibleTiers()`

新增角色时应实现以上行为，并同步更新 `CharacterName`、`CardDataManager.InitCharacterList()`、技能数据和图片资源。

### 7. Buff、Debuff 与特效

- `BuffManager` 合并或追加 Buff，并在回合更新时减少持续时间。
- `DebuffManager` 添加 Debuff、修改目标卡牌并刷新 UI。
- `CardEffectManager` 在启动时通过固定 Resources 路径载入特效 Prefab。
- 角色攻击协程负责播放效果、等待攻击间隔并应用伤害/状态。

### 8. 物品与远程仓库

`InventoryItemManagerBase` 封装本地和远程物品共有实现：

- 创建 Slot；
- 添加、消耗、排序和过滤；
- 容量与索引检查；
- 刷新 Slot；
- 转移时复制实体，避免两个背包共享同一个可变对象。

派生类：

- `ItemManager`：`IsRemote == false`；
- `RemoteItemManager`：`IsRemote == true`。

`ItemOperationManager` 负责删除和延迟传送。本地/远程已分文件（`inventory_local.json` / `inventory_remote.json`）；启动不再写 FakeData。详见 `06-data-and-save.md`。

**P3**：堆叠键为 `(itemDefId, quality)`；装备为不可堆叠实例（`itemInstanceId` + 耐久）。采集/制造由 `ProductionService` + `IdleEconomyTicker` 驱动；离线进 `idle.json` Pending，舰桥 Claim。

**P4**：Nexus「市场」→ `MarketScreen` 星港 NPC 商店（`NpcShops.json`）；`PlayMode.Solo` 禁用玩家市场 API；货币经 `CurrencyService`。

### 9. 星球与事件

`RadarSystem` 在 UI 范围内生成星球和星点，负责焦点与坐标映射。`PlanetListManager`/`PlanetDetailManager` 展示列表与详情，开始按钮加载 `BattleScene`。

`EventManager` 根据事件实体创建 `EventSlot`。当前星球和事件内容仍含原型/模拟数据。

## 核心类职责与关系

本文从“谁拥有数据、谁协调流程、谁只负责显示”的角度描述项目核心类。修改功能前，先找到数据所有者，再沿调用关系修改协调器和视图，避免把同一份状态复制到多个 Manager。

### 总体关系

```mermaid
flowchart TB
    Menu["MainMenu / GameLoadManager"] --> Data["DataUtil\n当前玩家与存档"]
    Data --> Player["PlayerEntity"]
    Data --> CardModel["CardEntity 集合"]
    Data --> ItemModel["ItemEntity 集合"]

    CardData["CardDataManager\n生成规则与静态配置"] --> CardModel
    CardData --> Strategy["Character 策略"]
    CardList["CardListManager\n玩家卡牌集合"] <--> CardModel
    CardList --> CardView["Card\n视图与战斗实例"]
    Lineup["LineupManager\n编队位置"] --> CardList

    Battle["BattleController\n回合状态机"] --> CardView
    Battle --> Registry["CharacterSkillController"]
    Registry --> Strategy
    Battle --> Report["BattleReportManager"]

    Inventory["InventoryItemManagerBase"] --> ItemModel
    ItemOps["ItemOperationManager"] --> Inventory

    Nexus["AppShell"] --> MainScroll["MainScrollController\n(legacy adapter only)"]
    MainScroll --> Status["GameStatusManager"]
    Battle --> Status
    Loader["SceneLoader"] --> UnityScenes["Unity SceneManager"]
    BattleChrome["BattleChrome"] --> Battle
```

图中的箭头表示主要调用或数据依赖，不代表对象一定由上游创建。大量组件仍由场景或 Prefab 通过 Inspector 装配。

### 核心类索引

| 类 | 层级 | 数据/状态所有权 | 主要协作者 | 关键约束 |
| --- | --- | --- | --- | --- |
| `DataUtil` | 基础设施 | 当前玩家、玩家存档路径、序列化入口 | `PlayerEntity`、`CardListManager`、`CharacterInfoManager` | 先选择 `currentPlayer`，再调用玩家范围的读写方法 |
| `GameStatusManager` | 全局状态 | 当前页面、是否战斗、是否抽卡 | `MainScrollController`、`BattleController` | 跨场景保留；不直接切换页面或场景 |
| `MainMenu` | 入口 UI | 无持久数据 | `DataUtil`、`GameLoadManager` | 新游戏成功后进入 `MainScene` |
| `GameLoadManager` | 入口协调 | 存档列表 UI 与当前焦点 | `DataUtil`、`GameLoadSlot` | 选中 Slot 时必须同步 `DataUtil.currentPlayer` |
| `MainScrollController` | 主界面协调 | 当前激活的旧版面板 | `GameStatusManager`、`AppShell` / `LegacyPanelAdapter` | `panels` 顺序与 `CurrentScene` 数值绑定；有 AppShell 时不建旧导航 |
| `AppShell` | UI 外壳 | 全局导航、Bridge/Formation/Settings/Missions 原生页、跨场景目标页 | `LegacyPanelAdapter`、`SceneManager` | 新页面自己渲染；未重写页委托旧面板；**规划**中：功能解锁 grey out、Notification 栏、Snackbar 宿主 |
| `BattleChrome` | 战斗外壳 | 无战斗数据 | `BattleController.CurrentRound` | Option A：只改 chrome，不改手牌战斗 |
| `CardEntity` | 领域模型 | 卡牌身份、成长、基础属性、专长、编队位置 | `CardDataManager`、`DataUtil`、`Card` | 字段参与 JSON；属性变化可能触发整组卡牌保存 |
| `CardDataManager` | 领域服务 | 角色目录、技能、等级属性、概率表 | `Character`、`Resources`、`CardEntity` | 负责“生成”，不自动把卡加入玩家集合 |
| `CardListManager` | 集合协调 | 当前玩家的卡牌列表及 Card 视图池 | `DataUtil`、`Card`、`CardPreviewController` | 跨场景保留；编队卡不会显示在普通卡牌网格中 |
| `Card` | 表现/战斗实例 | 临时生命、能量、动画、战报统计引用 | `CardEntity`、Buff/Debuff、特效系统 | 使用前调用 `InitCard`；不应成为持久成长数据源 |
| `LineupManager` | 编队协调 | Slot 选择状态 | `PortraitSlot`、`CardListManager` | Slot 索引等于 `LineupPosition` 和战斗站位索引 |
| `Character` | 战斗策略 | 单个角色的攻击行为与生成权重 | `BattleController`、`CardDataManager` | 新角色需要同时注册枚举、策略、资源和技能数据 |
| `CharacterSkillController` | 策略注册表 | `CharacterName → Character` 映射 | `BattleController` | 每场战斗初始化后才能查询 |
| `BattleController` | 战斗协调 | 回合、行动顺序、战斗结束条件 | `Card`、`Character`、`GameStatusManager` | 使用玩家卡牌的深拷贝，避免污染持久数据 |
| `InventoryItemManagerBase` | 集合协调 | 本地或远程物品列表及 Slot | `DataUtil`、`ItemSlot` | 派生类通过 `IsRemote` 区分所有权；转移时复制实体 |
| `ItemOperationManager` | 物品流程 | 当前操作对象与传送进度 | `ItemManager`、`RemoteItemManager` | 不直接拥有物品集合 |
| `SceneLoader` | 基础设施 | 可选过场动画状态 | Unity `SceneManager` | 跨场景保留；场景名必须存在于 Build Settings |

### 1. 启动与存档链路

```mermaid
sequenceDiagram
    participant Menu as MainMenu
    participant Load as GameLoadManager
    participant Data as DataUtil
    participant Disk as persistentDataPath/saves
    participant Main as MainScene

    alt New game
        Menu->>Data: CreatePlayerData()
        Data->>Data: 创建 PlayerEntity 并绑定路径
        Data->>Disk: playerData.json
        Menu->>Main: LoadScene("MainScene")
    else Load game
        Menu->>Load: LoadGame()
        Load->>Data: LoadPlayerEntities()
        Data->>Disk: 枚举玩家目录并读取 profile
        Load->>Data: SetCurrentPlayer(selectedPlayer)
        Data->>Data: UpdatePaths()
    end
```

`DataUtil` 是唯一应负责拼接玩家存档路径的类。业务 Manager 应调用 `SaveCardData`、`SaveInventory` 等明确入口，不应自行假设目录结构。

### 2. 卡牌生成、持有与显示

这三个概念必须分开：

- `CardDataManager` 根据概率和静态配置生成一张新的 `CardEntity`。
- `CardListManager` 持有玩家当前的 `CardEntity` 集合，并负责保存。
- `Card` 是场景里的 MonoBehaviour，用于渲染、交互和战斗临时状态。

```mermaid
flowchart LR
    Static["技能 / 等级属性 / 概率"] --> Generator["CardDataManager"]
    Character["Character 元数据"] --> Generator
    Generator -->|返回新对象| Entity["CardEntity"]
    Entity -->|AddCardEntity| Collection["CardListManager"]
    Collection -->|InitCard| View["Card"]
    Collection -->|SaveCardData| Data["DataUtil"]
```

调用 `GetCardEntity()` 不代表玩家已经获得该卡；抽卡确认后仍需显式调用 `CardListManager.AddCardEntity()`。

#### CardEntity 属性层

```text
PanelAttribute = BaseValue × CharacterMultiplier × PanelMultiplier
BattleAttribute = PanelAttribute × BattleMultiplier
```

- `characterAttrs`：角色固有专长。
- `panelAttrs`：卡牌专长等持久修正。
- `battleAttrs`：Buff/Debuff 等战斗临时修正。

`OnDataChanged` 用于刷新视图，`OnCardUpgraded` 用于进阶后的专门反馈。当前 `CalculatePower()` 还会通过 `CardListManager` 保存整个集合，因此在初始化阶段批量设置属性时会产生较强的单例与 I/O 耦合。

### 3. 编队到战斗

```mermaid
sequenceDiagram
    participant UI as PortraitSlot / DropZone
    participant Lineup as LineupManager
    participant Cards as CardListManager
    participant Battle as BattleController
    participant Registry as CharacterSkillController
    participant Strategy as Character

    UI->>Lineup: AddLineupCard(entity, slotIndex)
    Lineup->>Lineup: entity.position = slotIndex
    Lineup->>Cards: UpdateCardList()
    Cards->>Cards: 保存卡牌集合
    Battle->>Cards: GetInLineCardEntities()
    Battle->>Battle: 深拷贝并放入同索引 Card
    Battle->>Registry: GetCharacter(characterName)
    Registry-->>Battle: Character strategy
    Battle->>Strategy: NormalAttack / SpecialAttack
```

`LineupPosition.None` 表示未编队。有效枚举值会直接转换为 `playerCards` 的索引，因此修改枚举或 Slot 数量时必须同时检查场景中的卡牌槽位。

### 4. 战斗职责边界

`BattleController` 负责：

- 初始化双方卡牌；
- 每回合按速度排序；
- 选择普通攻击或特殊攻击；
- 检查存活与最大回合数；
- 打开战报。

`Character` 负责某个角色一次行动中的具体行为，包括目标选择、动画等待、伤害和状态效果。`Card` 负责显示与临时生命/能量。不要把角色特有逻辑继续堆入 `BattleController`。

### 5. 主界面与 AppShell UI

```mermaid
flowchart LR
    Bootstrap["NexusUiBootstrap"] --> Shell["AppShell"]
    Shell -->|Bridge / Formation / Settings / Missions| NewViews["原生重建页面"]
    Shell -->|Characters / Cards / Inventory...| Adapter["LegacyPanelAdapter"]
    Adapter --> MSC["MainScrollController"]
    MSC --> Panels["场景内现有 Panels"]
    MSC --> Status["GameStatusManager"]
    Shell -->|Battle| BattleScene["BattleScene + BattleChrome"]
```

`AppShell` 是表现层宿主，不是卡牌/物品数据所有者。战斗采用 Option A：保留 `BattleController` 自动战斗，仅由 `BattleChrome` 提供 Figma 风格边框与 HUD。

### 6. 物品系统

`InventoryItemManagerBase` 同时承担集合与 Slot 显示逻辑：

- `ItemManager` 表示玩家本地背包；
- `RemoteItemManager` 表示远程仓库；
- `ItemOperationManager` 负责删除和延迟传送操作。

物品转移使用新的 `ItemEntity` 副本，避免两个背包共享可变对象。本地/远程分文件持久化（P0.2）；`ItemManager` ↔ `inventory_local.json`，`RemoteItemManager` ↔ `inventory_remote.json`。

P3 字段：`itemDefId`、`quality`、`itemInstanceId`、`durability` / `maxDurability`、`equippedToCardId`。领域规则在 `Economy/Domain`；运行时 `ProductionService` / `DurabilityService` / `IdleSettlementService`。

### 生命周期和单例注意事项

| 阶段 | 典型行为 | 风险 |
| --- | --- | --- |
| `Awake` | 建立 `Instance`、读取静态配置、初始化路径 | 同一阶段不同 GameObject 的顺序未保证 |
| `OnEnable` | 发布当前场景状态、加载或刷新列表 | 对象重复启用可能重复执行 I/O |
| `Start` | 绑定按钮、创建 Slot、启动战斗 | 依赖对象的 `Start` 可能尚未运行 |
| 场景切换 | `DontDestroyOnLoad` 对象继续存在 | 场景内重复单例必须销毁或复用 |

跨系统调用前应验证可选单例是否存在。不要仅因为某组件存在于一个场景，就假设从其他场景直接进入时它也已初始化。

### 修改入口速查

| 需求 | 数据所有者 | 主要协调器 | 视图/外壳 |
| --- | --- | --- | --- |
| 新增玩家字段 | `PlayerEntity` / `DataUtil` | `MainMenu`、角色信息 Manager | 对应 UI |
| 新增角色 | `CharacterName`、具体 `Character` | `CardDataManager`、`CharacterSkillController` | 图片、技能和卡牌 Prefab |
| 修改卡牌成长 | `CardEntity`、等级配置 | `CardDataManager`、`CardListManager` | `CardPreviewController` |
| 修改编队规模 | `LineupPosition` | `LineupManager`、`BattleController` | `PortraitSlot`、战斗槽位 |
| 修改战斗回合 | `Card` 临时状态 | `BattleController`、具体 `Character` | `BattleInfo`、战报 |
| 修改物品存档 | `ItemEntity`、`DataUtil` | 本地/远程 Item Manager、`ProductionService` | `ItemSlot` / `CraftingScreen` |
| 采集/制造/离线 | `ItemCatalog` / `idle.json` | `IdleEconomyTicker`、`IdleSettlementService` | Explore / Crafting / Bridge Claim |
| NPC 商店 | `NpcShops.json` / `credits` | `NpcShopService`、`CurrencyService` | `MarketScreen` |
| 新增主导航页 | 对应业务模型 | 对应业务 Controller | `AppShell`、必要时 `LegacyPanelAdapter` |

### 面向 Codex 的检查清单

1. 找到领域实体或集合 Manager，确认真正的数据所有者。
2. 检查目标类是否跨场景保留，以及重复单例如何处理。
3. 搜索事件订阅、Inspector 字段、`Resources.Load` 路径和枚举到索引的转换。
4. 修改实体字段前检查已有 JSON 存档兼容性。
5. 修改编队或主菜单枚举前检查场景数组顺序。
6. 修改流程后同步更新本文档和对应专题文档。
7. 至少执行 C# 编译、`git diff --check` 和目标场景冒烟测试。
