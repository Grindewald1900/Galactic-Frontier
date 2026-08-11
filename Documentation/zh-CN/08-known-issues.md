# 已知问题与技术债

本页记录当前代码可观察到的限制，用于规划工作；它不是已修复清单。

## 高优先级

### 原型数据覆盖真实数据 — **P0.1 已隔离**

- ~~`InventoryItemManagerBase` 启动时生成并保存随机物品。~~ → 启动只 `Load`；样例注入需 Dev Data Mode + `TryInjectSampleInventory`。
- 抽卡材料 / 战斗敌人 / 雷达星球 / 事件 Fake 已迁入 `IDevDataProvider`，默认关闭。
- 开启方式：EditorPrefs `GalacticFrontier.DevData.enabled`、启动参数 `-devData`、或礼品码 `001`（会话开关）。
- **未完成**：遭遇表替换敌人（P2.3）、`saves/_dev` 隔离写档。

> 契约：见 [systems/08-save-and-seed-data.md](systems/08-save-and-seed-data.md)。`DefaultProperty.isDebug` 仍只控制明文/Base64，不打开 FakeData。

### 本地与远程物品共用存档 — **P0.2 已拆分**

~~共用 `itemData.json`~~ → `inventory_local.json` / `inventory_remote.json`；旧档由 `SaveMigrator` 0→1 拆分。

### 存档可靠性 — **P0.2 部分完成**

- ~~直接覆盖~~ → 原子写（tmp → Replace）；
- ~~无版本号~~ → `meta.json` + `SaveVersion.Current = 4` + Migrator 0→1→2→3→4；
- Base64 仍不是加密；
- 迁移失败可复制到 `saves/_corrupt/`；完整滚动备份仍未做。

## 中优先级

### Singleton 实现不一致

部分组件会销毁重复对象，部分不会；部分按需查找实例。建议统一生命周期策略，并逐步以显式依赖替代全局访问。

### MainScene 体积和职责过大

大量系统同时挂在 `MainScene`，使初始化顺序、场景合并和回归测试复杂。可考虑按功能拆分 Prefab/子场景或引入明确的 Composition Root。

### 缺少第一方程序集与测试 — **P0.5 已起步**

已有：

- `GalacticFrontier.BattleDomain`（纯 C#：`BattleRng` / `CombatMath` / `BattleOutcomeRules`）
- `GalacticFrontier.DeckDomain`（纯 C#：`DeckRules` / `DeckOccupationMap` / `DeckStateFactory`）
- `GalacticFrontier.WorldDomain`（区域/舰船规则）
- `GalacticFrontier.EconomyDomain`（物品目录、库存/品质/耐久/离线规则）
- `GalacticFrontier.MarketDomain`（NPC 商店规则 / PlayMode 门控）
- `GalacticFrontier.Tests.EditMode`（战斗 + 卡组 + 库存堆叠 + 品质/耐久 + NPC 商店）

仍待：更广的 Runtime asmdef 拆分、时间回拨护栏、PlayMode Online 玩家市场、PlayMode 集成测试。

### 字符串路径和场景名

`Resources.Load` 与 `SceneManager.LoadScene` 多处使用裸字符串。资源移动或场景重命名不会得到编译器保护。

### 本地化数据编码

`SkillData.json` 中可观察到中文乱码。需要确认文件原始编码、重新导出中文文本，并验证生成的 `.bytes` 数据。

## 低优先级或未完成功能

- `MainMenu.OnSettingsButtonClick()` 为空。
- ~~战斗结束后目前显示战报，但返回 `MainScene` 的调用被注释。~~ → **P0.3**：战报 Confirm → Explore；Esc → Bridge。
- `HomeScene` 未加入构建。
- 设置、礼品码、成就、事件和星球系统仍有占位逻辑。
- 多个公开 Inspector 字段缺少统一命名与空值验证。
- `CardEntity` 同时承担持久化模型、属性计算和事件通知，职责较重。

## 推荐演进顺序

1. ~~将所有 `FakeData()` 放入明确的开发数据提供器。~~（P0.1）
2. ~~为存档增加版本、备份和迁移。~~（P0.2：版本 + 原子写 + 0→1；滚动备份仍后置）
3. ~~为伤害…增加 EditMode 测试 / 引入 asmdef~~ → **P0.4/P0.5 起步**（战斗公式）；扩展占用/离线/存档测试  
4. 继续拆分 Runtime asmdef，隔离更多领域逻辑与 Unity 表现层。
5. 统一 Singleton/场景生命周期。
6. 用 ScriptableObject、Addressables 或集中配置逐步替代字符串资源路径。
