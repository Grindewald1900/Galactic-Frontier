# 系统文档：运行模式与存档互通（方向稿）

> 文档版本：v0.2  
> 状态：**方向已写入核心设计；Online 细则待立项拍板**  
> 上级约束：`Documentation/01-core-product-design.md` §12 / §16 / §22  
> 关联：`08-save-and-seed-data.md`、`07-market-and-card-trade.md`、`17-online-multiverse-cooperation.md`、`00-setting-and-lore.md`  
> 更新日期：2026-08-14  
> 变更：v0.2 — 明确「当前无多人保证」；挂接多元宇宙位面 / Hub 市场方向。

---

## 1. 目标

锁定两种运行模式的产品边界，避免 MVP 误做联网经济：

| 模式 | MVP | 存档 | 经济 | 社交 |
| --- | --- | --- | --- | --- |
| **Solo** | 必做 | 本地 `saves/` | NPC only | 无 |
| **Online** | 不做 | 账号 / 服务端权威（待定） | NPC + **Hub 玩家市场** | 位面隔离 + 公会（见 `17`） |

**现状结论：** 现行构建**不能保证多人体验**；多人体验以 `17-online-multiverse-cooperation.md` 为方向，立项后实现。

---

## 2. 已拍板

1. MVP **只交付 Solo**。  
2. Solo **不能**访问玩家市场。  
3. 存档互通：**不做**未验证的双向实时同步。  
4. 候选方案（线上立项二选一或分阶段）：  
   - **单向迁入**：Solo 进度申请导入 Online 角色（须经济隔离 / 冷却 / 审核规则）；  
   - **双轨分存**：两套进度互不影响，UI 明确标识。  
5. Online 探索默认采用 **私有多元宇宙位面**，避免资源点冲突；市场放在 **中转站 Hub**（`17`）。

---

## 3. 待拍板（线上立项时）

* 账号体系、设备绑定、封禁；  
* 迁入白名单字段（卡牌、信用、舰船模块是否全量）；  
* 反刷：迁入后绑定交易冷却；  
* 服务端权威战斗/挂机是否必须（建议挂机结算服务端校验）；  
* 位面 Affinity 生成与重组规则；  
* Hub 单服 vs 跨服经济；  
* 公会与断航压力数值（`17` §11）。  

---

## 4. 实现钩子（MVP 可先埋）

```text
enum PlayMode { Solo, Online }

IPlayModeService.Current
IPlayModeService.IsPlayerMarketEnabled => Current == Online
IPlayModeService.IsGuildEnabled => Current == Online
```

Solo 构建中 `Online` 入口灰显或隐藏。  
位面 Id / Hub 会话接口可留空实现，禁止 Solo 误调用。

---

## 5. 与多人方向的关系

| 文档 | 职责 |
| --- | --- |
| 本文 | Solo / Online 边界、存档互通 |
| `17-online-multiverse-cooperation.md` | 位面、偏置产量、Hub 市场、公会压力 |
| `07-market-and-card-trade.md` | 订单簿与货币规则 |
| `00-setting-and-lore.md` | 中转站 / 相位航道叙事 |

---

## 6. 参考

- 核心设计 §12  
- `08-save-and-seed-data.md`  
- `07-market-and-card-trade.md`  
- `17-online-multiverse-cooperation.md`
