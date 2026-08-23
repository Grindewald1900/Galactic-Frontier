# 系统文档：Debug 模式与测试工具

> 文档版本：v1.1
> 文档类型：**规则**
> **实现与验收状态**见 [PRODUCT-STATUS.md](../PRODUCT-STATUS.md)；本文仅描述规则与设计标准。

> 上级约束：核心设计 v0.4；`08-save-and-seed-data.md`（FakeData / Dev Data 边界）  
> 关联实现：`DebugModeController`、`GiftCodeService`、`DebugScreen`、`SettingsScreen`、`DevDataSettings`、`Assets/Tests/EditMode`  
> 更新日期：2026-08-09  
> 变更：v1.1 — Debug 由单例控制；导航栏「Debug模式」页；礼品码仅用于设置页领奖。

---

## 1. 目标与非目标

### 1.1 目标

1. 用 **`DebugModeController` 单例** 统一控制 Debug 模式开关；  
2. Debug **开启后**，Nexus 导航栏出现 **「Debug模式」** 入口；  
3. Debug 页可 **修改本地道具数量**、**新增 / 编辑当前卡牌**；  
4. **礼品码放在设置页**，只用于领取奖励道具（每存档每码一次）；  
5. 与 Dev Data（样例 FakeData）、存档明文 `isDebug` 三者分离。

### 1.2 非目标

- 用礼品码打开 Debug / Dev Data（已废弃 `000` / `001` 旧语义）  
- Release 默认开启 Debug  
- 线上反作弊  

---

## 2. 三层开关（必须分清）

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

## 3. Debug Mode 单例

### 3.1 类

`Assets/Resources/Scripts/Utils/DebugTools/DebugModeController.cs`

- `DontDestroyOnLoad` 单例，`RuntimeInitializeOnLoadMethod(BeforeSceneLoad)` 自举  
- `IsEnabled` / `SetEnabled(bool)` / `Toggle()`  
- `event Action<bool> Changed` → `AppShell` 重建导航  

### 3.2 开启方式

| 方式 | 说明 |
| --- | --- |
| **设置 → 开发者 → 开启 Debug 模式** | 主路径（Nexus `SettingsScreen`） |
| 命令行 `-debugMode` | 进程级强制开 |
| EditorPrefs `GalacticFrontier.DebugMode.enabled` | Editor 持久（随 `SetEnabled` 写入） |

关闭：设置页再次点击，或 Debug 页「关闭 Debug 模式」。

### 3.3 导航行为

- `IsEnabled == true`：`BuildNavigation` 在「任务」与「设置」之间插入 `AppScreen.Debug`  
- 标题：`UiText.ScreenDebug` → **Debug Mode / Debug模式**  
- 关闭后若当前停在 Debug 页，自动回 **设置**

---

## 4. Debug 页面能力（`DebugScreen`）

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

## 5. 礼品码（设置页 · 仅领奖）

### 5.1 入口

- Nexus：**设置 → 礼品码** 输入框 +「领取」  
- 遗留设置面板 `SettingsManager` 输入框同样走 `GiftCodeService.TryRedeem`  
- **不再**用礼品码切换 Debug / Dev Data  

### 5.2 服务

`GiftCodeService`（`Utils/DebugTools/GiftCodeService.cs`）

- 大小写不敏感  
- 每玩家每码：`PlayerPrefs` 键 `GiftRedeemed_{playerId}_{CODE}`，领过拒绝  
- 可发：材料、信用点、随机卡牌  

### 5.3 设计码表（MVP）

| 码 | 中文名 | 奖励 |
| --- | --- | --- |
| `WELCOME` | 指挥官欢迎礼包 | Copper×40、Water×20、信用+1000 |
| `COPPER100` | 铜材补给 | Copper×100 |
| `STEEL50` | 钢材货运 | Steel×50 |
| `NEXUS-BETA` | NEXUS 内测礼包 | Copper×30、Steel×20、GoldBar×5、信用+3000 |
| `CREDIT5K` | 信用点投放 | 信用+5000 |
| `RECRUIT` | 紧急征召 | 随机角色卡×1 |
| `WATER20` | 水源箱 | Water×20 |
| `WOOD80` | 木材捆 | Wood×80 |

> 正式上线前可轮换码表；勿在玩家可见商店页展示完整列表（内测可放补丁说明）。

### 5.4 废弃码

| 旧码 | 旧行为 | 现行为 |
| --- | --- | --- |
| `000` | 打开 Debug 面板 | 忽略并打日志，提示去设置开 Debug |
| `001` | 切换 Dev Data | 同上 |

---

## 6. Dev Data Mode（层 B，联调样例）

仍由 `DevDataSettings` / `IDevDataProvider` 控制，**与 Debug Mode 独立**。

开启：EditorPrefs、`-devData`、或日后 Debug 页子开关（可选）。  
用途：战斗样例敌人、抽卡材料等；**禁止** Provider 内直接 `Save*`。

日常：Debug Mode ON + Dev Data OFF 即可改库存/卡牌；仅缺内容联调时再开 Dev Data。

---

## 7. 日志前缀

| 前缀 | 用途 |
| --- | --- |
| `[DEBUG]` | Debug Mode 开关、面板操作 |
| `[GIFT]` | 礼品码领取 |
| `[DEV-DATA]` | 样例数据 |
| `[SAVE]` | 存档迁移 |

---

## 8. 自动化与冒烟

### EditMode

`Assets/Tests/EditMode/` — 规则测试不依赖 Debug Mode。

### 手测冒烟

1. 设置开启 Debug → 导航出现「Debug模式」  
2. 改 Copper 数量 → 进仓库可见（或重进仓库刷新）  
3. 新增随机卡 → 卡牌列表增加  
4. 关闭 Debug → 导航入口消失  
5. 设置领取 `WELCOME` → 成功；再领 → 已领取提示  
6. 输入 `000` → 不打开 Debug  

---

## 9. 实现映射

| 组件 | 路径 |
| --- | --- |
| Debug 单例 | `Utils/DebugTools/DebugModeController.cs` |
| 礼品码 | `Utils/DebugTools/GiftCodeService.cs` |
| Debug 页 | `UI/Nexus/Screens/DebugScreen.cs` |
| 设置页 | `UI/Nexus/Screens/SettingsScreen.cs` |
| 导航 | `AppShell` + `AppScreen.Debug` |
| 库存 SetQty | `InventoryItemManagerBase.SetItemQuantity` |

---

## 10. 设计验收标准

- `DebugModeController.Instance` 场景切换后仍存在  
- 仅设置 / `-debugMode` / EditorPrefs 可开 Debug；礼品码不能开  
- Debug ON 时导航有「Debug模式」；OFF 时无  
- Debug 页可改本地道具数量并持久化  
- Debug 页可新增卡、升级/加经验第一张卡  
- 设置页礼品码可领奖；同档重复领取失败  
- `isDebug` 不影响 Debug / Dev Data  
- Release 默认 Debug OFF  

---

## 11. 开放钩子

- Debug 页增加 Dev Data 子开关、存档路径显示  
- 按 `itemDefId` 自由添加任意道具  
- 选中卡编辑等级/稀有度  
- 礼品码服务端校验（Online）  
- IMGUI 叠加层  

---

## 12. 参考

- 存档：`08-save-and-seed-data.md`  
- 运行模式：`14-play-modes-and-persistence.md`  
- 开发指南：`../06-development-guide.md`  
- MVP 计划：`../11-mvp-development-plan.md`
