# 系统文档：经济流通、NPC 商店与玩家市场

> 文档版本：v1.2
> 文档类型：**规则**
> **实现与验收状态**见 [PRODUCT-STATUS.md](PRODUCT-STATUS.md)；本文仅描述规则与设计标准。

> 上级约束：`02-core-product-design.md` §7.5 / §7.6 / §12 / §16 / §20 / §22（v0.4）  
> 关联：`18-production-and-quality.md`、`14-deck-and-occupation.md`、`17-idle-and-offline.md`、`09-economy`（货币细表，若拆分）  
> 更新日期：2026-08-10  
> 变更：v1.2 P4 落地——`PlayMode`、`CurrencyService`、`NpcShops.json`、`MarketScreen`。

---

## 1. 目标与非目标

### 1.1 目标

- **MVP（单机）**：用货币 + NPC 商人打通「制造 / 掉落 ↔ 购售 / 回收」；  
- **线上（后期）**：全服务器玩家市场（订单簿 + 历史价）+ 普通卡交易；  
- 领域规则可单测；UI 按 `PlayMode` 显隐入口，避免单机出现「假全服市场」。

### 1.2 非目标

- MVP 内模拟全服盘口或假玩家挂单  
- 面对面交易、高复杂度拍卖  
- 完整反作弊图谱（线上再做钩子）  
- 抽卡商店冒充「市场」导航（须拆开：Market → 商店/交易中心按模式切换）

---

## 2. 术语

| 术语 | 定义 |
| --- | --- |
| **PlayMode** | `Solo` \| `Online`；见核心设计 §12 |
| **NPC 商店** | 静态或慢变价货架；单机主流通渠道；叙事上为开拓舰**市场终端**连到的星港认证商人（`03-setting-and-lore.md`） |
| **玩家市场** | 玩家订单簿；**仅 Online**；叙事为公共交易网络进一步恢复后的舰长间贸易 |
| **市场终端** | UI 入口（Market）；Solo 只开 NPC，Online 可切换/并陈玩家市场 |
| **ListingKey** | 资源：`(itemDefId, quality)`；卡牌：见 §4.8（线上） |
| **绑定（Bound）** | 不可上架玩家市场的卡/物 |

---

## 3. 硬约束

1. **`PlayMode.Solo` 禁止打开玩家市场 UI / API**（返回 `MarketDisabledInSolo`）。  
2. Solo 下物资流通 = 掉落 + 制造 + **NPC 购售/回收**（+ 任务发放）。  
3. `PlayMode.Online` 下玩家市场按**全服共享**设计；须有历史价格。  
4. 普通卡仅在 Online 可进玩家市场；绑定卡除外。  
5. 生产品质影响 NPC 回收价与（线上）挂牌键。  
6. 首发核心循环不依赖 Online。  

---

## 4. MVP 拍板决议

### 4.0 模式门控

```text
if PlayMode == Solo:
    enable NpcShop
    disable PlayerMarket
else: // Online — 非 MVP
    enable NpcShop
    enable PlayerMarket
```

导航建议：单机「市场」页改为 **「星港商店 / 商人」**；线上再显示「玩家交易所」页签。

### 4.1 货币（MVP）

| 货币 | Id | MVP | 用途 |
| --- | --- | --- | --- |
| 信用点 | `credits` | **必做** | NPC 购售主币；玩家余额在 `PlayerEntity` |
| 绑定信用 | `credits_bound` | 可选 | 仅 NPC / 兑换；不可（未来）进玩家市场 |
| 活动币 | — | 非 MVP | — |

不引入难以理解的多币种兑换矩阵；第二种货币若做必须在 UI 明确「绑定」。

### 4.2 NPC 商人（MVP 必做）

| 项目 | 规则 |
| --- | --- |
| 配置 | `NpcShops.json`：`shopId`、锚点（星域/区域/舰桥）、货架行 |
| 货架行 | `itemDefId`、品质、单价、限购（可空）、解锁条件（区域通关/舰船等级） |
| 购买 | 扣 `credits`，加物品到本地仓库；库存满则失败 |
| 回收 | 玩家出售材料给 NPC；默认回收价 = `buyPrice * sellBackRatio`（默认 **0.4**） |
| 刷新 | MVP：**不刷新限购**或按日 UTC 刷新（配置）；不做拍卖式波动 |
| 卡牌 | MVP NPC **不收购**角色卡（避免与抽卡/成长冲突）；重复卡走分解/兑换（经济文档） |

### 4.3 玩家市场架构（仅 Online / 后期）

```text
UI (PlayerMarketScreen)  // Solo 隐藏
  → IMarketService
       └─ RemoteMarketService   // 真全服
  → MarketDomain (订单簿、撮合、手续费、护栏)
```

| 项目 | 后期默认 |
| --- | --- |
| 货币 | `credits`（非绑定） |
| 时区 | 历史桶 UTC |
| LocalMock | **不再作为 Solo 冒充全服的方案**；仅测试装配可用 |

以下 §4.4–§4.8 保留为**线上实现契约**（由原 v1.0 拍板延续），MVP 编码可推迟。

### 4.4 订单与撮合（Online）

| 项目 | 默认 |
| --- | --- |
| 卖单 | 指定 `unitPrice`、`quantity`；预扣商品 |
| 买单 | 指定 `unitPrice`、`quantity`；预扣货价（建议一并冻结买方手续费） |
| 撮合价 | **价格优先，时间优先**；成交价取**先挂单方价格** |
| 部分成交 | 允许；剩余保持挂单 |
| 立即买入 | 吃卖盘，从最低卖价起 |
| 立即卖出 | 吃买盘，从最高买价起 |
| 撤单 | 随时；返还未成交预扣 |

#### 手续费

| 侧 | 费率默认 | 收取时机 |
| --- | --- | --- |
| 卖方 | **2%** 成交额 | 成交时从卖方所得扣 |
| 买方 | **1%** 成交额 | 成交时额外扣买方信用（立即买须余额 ≥ 货价+费） |
| 卡牌 | 卖方 **3%** / 买方 **1%**（可配置独立表） | 同 |

买单预扣：预扣货价；买方手续费推荐**一并冻结**以免卡单。

### 4.5 订单数量上限（Online）

| 限额 | MVP 默认 |
| --- | --- |
| 每玩家活动卖单 | **20** |
| 每玩家活动买单 | **20** |
| 单笔资源数量上限 | `min(stack, itemDef.maxOrderQty)` 默认 **9999** |
| 单笔卡牌 | **1** 张实例 / 单 |
| 新玩家（等级 &lt; 5） | 卖单+买单合计 **5** |

超限拒绝并提示。

### 4.6 价格上下限与异常价（Online）

```text
refPrice = max(1, priceHistory.vwap24h || itemDef.baseValue * qualityMult)
minPrice = floor(refPrice * 0.2)
maxPrice = ceil(refPrice * 5.0)
```

| 项目 | MVP |
| --- | --- |
| 挂单越界 | 拒绝 |
| 无历史时 | 用 `baseValue * qualityMult`（生产文档市场基准价） |
| 操纵检测钩子 | 记录「同账号短时自成交」「远离 VWAP 的刷量」；MVP 只打日志 + 可配置拒绝自成交 |
| 自成交 | 默认 **拒绝** 同一 `playerId` 买卖对敲 |

### 4.7 价格历史展示（Online）

| 指标 | 必须 |
| --- | --- |
| 最近成交价 | 是 |
| 区间最高 / 最低 | 是 |
| 均价（成交量加权 VWAP） | 是 |
| 成交量 | 是 |
| 价格曲线 | 是 |

时间范围档：`1H / 24H / 7D / 30D`（MVP 至少实现 **24H + 7D**；其余可灰显）。  
聚合桶：1H 用 1 分钟或 5 分钟桶；7D 用 1 小时桶。

```text
MarketCandle
- startUtc: long
- open, high, low, close: int
- volume: long
- vwap: int
```

### 4.8 新玩家保护（Online）

| 保护 | MVP 规则 |
| --- | --- |
| 等级 &lt; 3 | 不可上架卡牌；资源交易单笔信用上限 `newPlayerMaxTradeCredits`（默认 **5000**） |
| 首次卖卡 | 二次确认 + 手续费与绑定说明 |
| 购买确认 | 单价偏离 24h VWAP ≥ **30%** 时强制确认弹窗 |
| 误操作冷却 | 撤销后同一 ListingKey **10s** 内不可重复挂同向单（防连点） |

通胀控制：手续费sink + 维修/制造消耗为主要 sink；不在本系统做动态税率（留钩子 `IFeePolicy`）。

### 4.9 卡牌交易细则（Online）

关闭 §21 卡牌交易待定项：

| 议题 | MVP 决议 |
| --- | --- |
| 已升级卡能否交易 | **能** |
| 交易后是否保留等级/成长 | **保留**（等级、经验、技能等级随卡） |
| 已装备卡 | 上架前必须**卸下全部装备**到仓库 |
| 任务占用 / 行动占用 | `occupation != Idle` 或任务锁 → **不可上架** |
| 绑定 | `Starter` / `MainStoryEssential` / `AccountBound` → 不可交易；掉落/抽卡普通卡默认可交易 |
| 制造后立即出售 | **允许**；可选 `craftSellCooldownSeconds`（默认 **0**；若反投机需要可调 300） |
| 重复卡用途 | 可交易、可分解（分解非本文件；允许留作同名多开编队） |
| 卡牌手续费 | 见 §4.3 |
| Listing 展示 | 模板名、稀有度、等级、关键技能摘要；**不**按每张做成独立无限 SKU 曲线——历史按 `characterTemplateId + rarity` 聚合，成交仍交割具体 `cardId` |

```text
CardListingKey (历史聚合)
- characterTemplateId
- rarity

CardConsignment (订单)
- cardId
- unitPrice
- sellerId
```

### 4.10 仓库与原子性（Online）

| 操作 | 原子性 |
| --- | --- |
| 挂卖单 | 预扣物品/卡 → 写订单；失败回滚 |
| 挂买单 | 预扣信用（+预估费）→ 写订单 |
| 成交 | 单笔撮合事务：扣双方预扣、入账对方、写成交、更新历史 |
| 领取 | 资源进本地仓；卡进卡池；信用到账 |

LocalMock 亦须保证同一套原子顺序，便于日后换 Remote。

### 4.11 测试用 Mock 流动性（非 Solo 产品路径）

| 手段 | 说明 |
| --- | --- |
| 种子盘口 | 按 `baseValue` 生成双向薄盘，便于新手立即买/卖 |
| 缓慢漂移 | 可选：模拟价随生产 sink 微变 |
| 标明 | UI 调试角标「模拟全服」仅开发包显示；正式 Remote 无此标 |

---

## 5. 数据模型（实现契约）

```text
Order
- orderId: string
- side: Buy | Sell
- sellerOrBuyerId: string
- listingKey: string          // 规范化键
- itemDefId? / quality? / cardId?
- unitPrice: int
- quantity: int               // 卡牌恒为 1
- filledQuantity: int
- reservedCredits?: int
- createdAtUtc: long
- status: Open | Partial | Filled | Cancelled

Trade
- tradeId: string
- orderIdBuy / orderIdSell
- listingKey
- price: int
- quantity: int
- feeBuyer / feeSeller: int
- atUtc: long

IMarketService
- GetOrderBook(listingKey, depth)
- GetHistory(listingKey, range) -> candles + summary
- PlaceSell / PlaceBuy
- Cancel(orderId)
- MarketBuy / MarketSell          // 立即
- ListMyOrders()
- PreviewFees(...)
```

卡牌扩展字段（`CardEntity`）：

```text
- boundReason: None | Starter | MainStoryEssential | AccountBound | Admin
- tradeCooldownUntilUtc?: long
```

---

## 6. 规则细则

### 6.1 上架校验（卖）

1. 玩家订单数未超上限；  
2. 物品/卡存在且数量足够；  
3. 非绑定；卡牌 Idle 且已卸装；  
4. 单价在 `[minPrice, maxPrice]`；  
5. 新玩家规则通过；  
6. 预扣成功。

### 6.2 立即买示例

```text
剩余需求 = qty
从最低卖价起吃单:
  成交价 = 该卖单价
  扣买方 成交额 + 买方费
  给卖方 成交额 - 卖方费
  更新历史
直到需求满足或卖盘耗尽
```

### 6.3 验收用例

| 场景 | 期望 |
| --- | --- |
| 挂卖铁矿 Q2 | 预扣成功，盘口可见 |
| 立即买吃两档 | 部分/全部成交，历史更新 |
| 绑定 starter 卡上架 | 拒绝 |
| 占用中卡上架 | 拒绝 |
| 已升级卡成交 | 买方获得同等级卡 |
| 偏离 VWAP 30%+ | 确认框 |
| 切换 Remote 实现 | UI 无改动（契约测试） |

---

## 7. UI / UX 要求

| 界面 | MVP（Solo） | Online 后期 |
| --- | --- | --- |
| 原 Market 导航 | 改为 **星港商店 / NPC 商人**；禁止进抽卡 | 增加「玩家交易所」页签 |
| NPC 货架 | 分类浏览、购买、回收、限购提示 | 同左 |
| 玩家市场 | **隐藏** | 盘口、挂单、历史曲线、我的订单 |
| 空状态 | 引导采集/制造/靠近商人 | 「暂无订单」引导生产 |

文案：扩展 `UiText`；去掉 Market=抽卡映射。

---


## 9. 设计验收标准

### MVP（Solo）

- Solo 下无法打开玩家市场；API 返回禁用  
- NPC 购买扣信用、加物品；满仓失败提示  
- NPC 回收按 `sellBackRatio` 给信用、扣物品  
- 货架解锁条件（区域/舰船）生效  
- 导航不再进入抽卡商店  

### Online（后期）

- 买单/卖单/立即买/卖、手续费、历史、护栏、卡牌绑定规则同原契约  
- EditMode：撮合与绑定拒绝  

---

## 10. 开放钩子

- 单机 → 线上迁入时的经济快照校验  
- 多账号转移检测（Online）  
- 动态费率 / 通胀监控  
- 拍卖行（非 MVP）  
- NPC 动态库存事件  

若恢复「Solo 内假全服市场」，或取消「Solo 禁用玩家市场」，需修订核心设计 v0.4+ 并升本文主版本。

---

## 11. 参考

- 核心设计：`02-core-product-design.md` §7.5 / §7.6 / §12 / §22（v0.4）  
- 品质与堆叠：`18-production-and-quality.md`  
- 占用：`14-deck-and-occupation.md`  
- 开发计划：`13-mvp-development-plan.md`（P4 修订为 NPC）  
- 现有：`AppShell` Market 导航、`CardDrawingManager`（须解耦）、`CardEntity`、`ItemEntity`
