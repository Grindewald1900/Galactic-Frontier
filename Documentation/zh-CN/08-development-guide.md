# 开发与验证指南

## 修改边界

优先修改：

- `Assets/Resources/Scripts`
- `Assets/Resources/Prefabs`
- `Assets/Resources/Scenes`
- `Assets/Resources/data`
- `Documentation`

第三方目录如 `Assets/Plugins`、`Assets/Packages`、`Assets/TextMesh Pro`、`Assets/LeanTween` 和 `Assets/JMO Assets` 应视为外部代码，除非任务明确要求。

## Unity 序列化规则

- 重命名 Inspector 字段时使用 `[FormerlySerializedAs("oldName")]` 保留场景/Prefab 数据。
- 移动脚本时必须保留对应 `.meta`，否则 GUID 改变，场景组件会丢失。
- 新增 `Assets` 内脚本时必须提交 `.meta`。
- 不要通过文本批量重写 `.unity` 或 `.prefab`，除非已验证 YAML 引用和 Unity 导入结果。
- 将字段移入基类时保持字段名和可序列化性，Unity 才能继续恢复已有数据。

## 新增系统的建议步骤

1. 先定义或扩展 `Entity` 数据模型。
2. 将纯规则放在普通 C# 类或静态工具中。
3. 用一个 Controller/Manager 连接 Unity 生命周期和 UI。
4. 通过 Inspector 引用 Prefab，而不是在业务代码中重复硬编码路径。
5. 若必须使用 `Resources.Load`，将路径集中为常量。
6. 明确保存时机和旧存档兼容方式。
7. 添加 EditMode 或 PlayMode 测试。
8. 更新本文档中受影响的系统说明。

## 新增角色清单

- 在 `CharacterName` 添加枚举值；
- 新建继承 `Character` 的实现；
- 实现普通攻击、特殊攻击、被动和可能稀有度；
- 在 `CardDataManager.InitCharacterList()` 注册；
- 在 `CharacterSkillController` 注册实例；
- 增加技能 JSON 和图片；
- 增加角色专长；
- 验证抽卡、预览、编队和战斗。

## 新增场景清单

- 将场景加入 Build Settings；
- 避免重复创建 `DontDestroyOnLoad` 单例；
- 更新 `SceneLoader.SceneName` 和必要的 `CurrentScene`；
- 验证直接打开该场景时的依赖；
- 验证从正常入口进入时的依赖。

## 验证方式

调试开关、礼品码、Debug 导航页见 **[28-debug-and-test-mode.md](28-debug-and-test-mode.md)** v1.1。  
- **Debug Mode**：`DebugModeController` 单例；设置页开启后导航出现「Debug模式」。  
- **礼品码**：仅设置页领奖（`GiftCodeService`），不再用 `000`/`001` 开 Debug。  
- **`DefaultProperty.isDebug`**：只控制存档明文；样例数据仍走 `DevDataSettings`。

### 静态检查

```powershell
git diff --check
rg -n "TODO|FIXME|FakeData" Assets/Resources/Scripts -g "*.cs"
```

### EditMode 测试（P0.5）

- 程序集：`Assets/Tests/EditMode/GalacticFrontier.Tests.EditMode.asmdef`
- 领域：`Assets/Resources/Scripts/Battle/Domain`（`noEngineReferences`）
- 在 Unity：**Window → General → Test Runner → EditMode**，运行 `GalacticFrontier.Tests.EditMode`
- 战斗结算路径禁止新增未播种的 `UnityEngine.Random`；用 `BattleRng`

### 编译

首选让 Unity 完成脚本导入和编译，然后检查 Console。Unity 生成的 `.csproj` 在本项目中包含本机扩展路径，命令行 `dotnet build` 不一定可复现 Unity 编译环境。

### 测试

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

## 提交前检查

- Unity Console 无新增错误；
- 目标场景完成冒烟测试；
- `git diff --check` 通过；
- `.cs` 与 `.meta` 成对存在；
- 未提交 `Library`、`Temp`、`Logs`、`.csproj` 或 `.sln`；
- 未混入无关场景、字体、材质或第三方资源变更；
- 文档与当前行为一致。
