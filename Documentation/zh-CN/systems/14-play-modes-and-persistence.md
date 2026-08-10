# 系统文档：运行模式与存档互通（方向稿）

> 文档版本：v0.1  
> 状态：**方向已写入核心设计 v0.4；细则待线上立项时拍板**  
> 上级约束：`Documentation/01-core-product-design.md` §12 / §16 / §22  
> 关联：`08-save-and-seed-data.md`（本地存档契约）、`07-market-and-card-trade.md`（Solo 禁玩家市场）  
> 更新日期：2026-08-09

---

## 1. 目标

锁定两种运行模式的产品边界，避免 MVP 误做联网经济：

| 模式 | MVP | 存档 | 经济 |
| --- | --- | --- | --- |
| **Solo** | 必做 | 本地 `saves/` | NPC only |
| **Online** | 不做 | 账号 / 服务端权威（待定） | NPC + 全服玩家市场 |

---

## 2. 已拍板

1. MVP **只交付 Solo**。  
2. Solo **不能**访问玩家市场。  
3. 存档互通：**不做**未验证的双向实时同步。  
4. 候选方案（线上立项二选一或分阶段）：  
   - **单向迁入**：Solo 进度申请导入 Online 角色（须经济隔离 / 冷却 / 审核规则）；  
   - **双轨分存**：两套进度互不影响，UI 明确标识。  

---

## 3. 待拍板（线上立项时）

* 账号体系、设备绑定、封禁；  
* 迁入白名单字段（卡牌、信用、舰船模块是否全量）；  
* 反刷：迁入后绑定交易冷却；  
* 服务端权威战斗/挂机是否必须（建议挂机结算服务端校验）。  

---

## 4. 实现钩子（MVP 可先埋）

```text
enum PlayMode { Solo, Online }

IPlayModeService.Current
IPlayModeService.IsPlayerMarketEnabled => Current == Online
```

Solo 构建中 `Online` 入口灰显或隐藏。

---

## 5. 参考

- 核心设计 §12  
- `08-save-and-seed-data.md`  
- `07-market-and-card-trade.md`
