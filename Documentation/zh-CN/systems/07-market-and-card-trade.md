# 系统文档：全服市场与卡牌交易

> 文档版本：v1.0  
> 状态：**MVP 规则已拍板，可供 P4 实现**  
> 上级约束：`Documentation/01-core-product-design.md` §7.5 / §7.6 / §16.1 / §20 / §21 / §22  
> 关联：`05-production-and-quality.md`（品质堆叠键与价值）、`01-deck-and-occupation.md`（占用中卡不可上架）、`04-idle-and-offline.md`（仓库/Pending 入账）  
> 实现阶段：开发计划 P4.1–P4.6（见 `../11-mvp-development-plan.md`）  
> 更新日期：2026-08-09

---

## 1. 目标与非目标

### 1.1 目标

建立**按全服务器规模设计**的玩家市场，使资源与普通卡牌可流通，并提供可信的价格历史：

- 买单 / 卖单 / 立即买入 / 立即卖出；
- 盘口与历史（最近价、高低、均价、量、曲线）；
- 普通角色卡可交易；初始/主线关键卡绑定；
- 手续费、订单上限、价格护栏、新玩家保护钩子；
- 领域层可单测；UI 通过 `IMarketService` 切换 LocalMock / Remote。

### 1.2 非目标

- 面对面交易、高复杂度拍卖（设计排除）  
- 完整反作弊/多账号图谱（仅留检测钩子与基础规则）  
- 跨服贸易、公会订单（非 MVP）  
- 抽卡商店 UI 冒充市场（当前 Market 导航指向抽卡——必须拆开）

---

## 2. 术语

| 术语 | 定义 |
| --- | --- |
| **商品键（ListingKey）** | 资源：`(itemDefId, quality)`；卡牌：见 §4.8 |
| **卖单（Sell Order）** | 挂出商品等买家，指定单价与数量 |
| **买单（Buy Order）** | 挂出信用等卖家，指定单价与数量 |
| **立即成交** | 按对手盘最优价吃单，直到数量满足或盘口耗尽 |
| **手续费** | 成交时从金额中抽取的费用 |
| **价格历史** | 成交聚合后的时序统计 |
| **绑定（Bound）** | 不可上架、不可被市场转移的卡牌/物品 |
| **LocalMock** | 单机模拟全服盘口（NPC 做市或回放脚本），接口与 Remote 一致 |

---

## 3. 硬约束（不可违背）

1. 市场按**全服务器共享**设计（实现可先 Mock，契约按全服）。  
2. **普通卡牌必须能进市场**；关键绑定卡除外。  
3. 界面必须提供**历史价格与成交信息**。  
4. 生产品质影响挂牌商品键与价值认知（不同品质分盘）。  
5. 首发核心循环不依赖强制多人；玩家可与 Mock/系统流动性完成教程式交易。  
6. 不改第三方包；Market Screen 与抽卡商店分离。

---

## 4. MVP 拍板决议

> 关闭核心设计 §21「全服务器市场」与「卡牌交易」待定项。

### 4.1 市场架构

```text
UI (MarketScreen)
  → IMarketService
       ├─ LocalMockMarketService   // P4 默认可玩
       └─ RemoteMarketService      // 真全服
  → MarketDomain (订单簿、撮合、手续费、护栏)  // 纯 C# 可单测
```

| 项目 | MVP |
| --- | --- |
| 默认实现 | `LocalMock` + 本地订单簿持久化（`market_local.json`） |
| 切换 | 配置 `marketBackend = LocalMock \| Remote` |
| 货币 | 信用点 `credits`（整数）；暂不引入第二货币 |
| 时区 | 历史桶用 UTC |

### 4.2 可交易商品

| 类型 | 可交易 | ListingKey |
| --- | --- | --- |
| 材料 / 中间件 / 消耗品 | 是 | `itemDefId + quality` |
| 装备 / 模块实例 | 是（MVP 开放） | 盘口聚合键 `itemDefId + quality`；交割具体 `itemInstanceId`（耐久一并转移） |
| 普通角色卡 | 是 | 见 §4.8 |
| 绑定卡 / 任务物 / 货币本身 | 否 | — |

装备上架时：**当前耐久随实例走**；买家可在详情见耐久百分比。

### 4.3 订单与撮合

| 项目 | MVP 默认 |
| --- | --- |
| 卖单 | 指定 `unitPrice`、`quantity`；预扣商品 |
| 买单 | 指定 `unitPrice`、`quantity`；预扣 `unitPrice * quantity`（不含买方手续费预留见下） |
| 撮合价 | **价格优先，时间优先**；成交价取**先挂单方价格**（经典盘口） |
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

买单预扣：MVP 预扣货价；买方手续费在成交时扣，若不足则该笔撮合跳过并提示补款（或预扣时一并冻结费，推荐**一并冻结**以免卡单）。

### 4.4 订单数量上限

| 限额 | MVP 默认 |
| --- | --- |
| 每玩家活动卖单 | **20** |
| 每玩家活动买单 | **20** |
| 单笔资源数量上限 | `min(stack, itemDef.maxOrderQty)` 默认 **9999** |
| 单笔卡牌 | **1** 张实例 / 单 |
| 新玩家（等级 &lt; 5） | 卖单+买单合计 **5** |

超限拒绝并提示。

### 4.5 价格上下限与异常价

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

### 4.6 价格历史展示

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

### 4.7 新玩家保护

| 保护 | MVP 规则 |
| --- | --- |
| 等级 &lt; 3 | 不可上架卡牌；资源交易单笔信用上限 `newPlayerMaxTradeCredits`（默认 **5000**） |
| 首次卖卡 | 二次确认 + 手续费与绑定说明 |
| 购买确认 | 单价偏离 24h VWAP ≥ **30%** 时强制确认弹窗 |
| 误操作冷却 | 撤销后同一 ListingKey **10s** 内不可重复挂同向单（防连点） |

通胀控制：手续费sink + 维修/制造消耗为主要 sink；不在本系统做动态税率（留钩子 `IFeePolicy`）。

### 4.8 卡牌交易细则

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

### 4.9 仓库与原子性

| 操作 | 原子性 |
| --- | --- |
| 挂卖单 | 预扣物品/卡 → 写订单；失败回滚 |
| 挂买单 | 预扣信用（+预估费）→ 写订单 |
| 成交 | 单笔撮合事务：扣双方预扣、入账对方、写成交、更新历史 |
| 领取 | 资源进本地仓；卡进卡池；信用到账 |

LocalMock 亦须保证同一套原子顺序，便于日后换 Remote。

### 4.10 LocalMock 流动性

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

| 界面 | 要求 |
| --- | --- |
| Market Screen | **独立**于抽卡；浏览分类：资源 / 装备 / 卡牌 |
| 商品页 | 盘口深度、挂单表单、立即买/卖、历史曲线与四指标 |
| 我的订单 | 撤单、部分成交状态 |
| 卡牌上架 | 展示绑定原因；未卸装/占用时禁用并说明 |
| 空盘 | Mock 种子或「暂无订单」引导制造/采集 |

文案：扩展 `UiText`；去掉 Market=抽卡的导航映射。

---

## 8. 与现有代码的差距

| 现有实现 | 差距 |
| --- | --- |
| Market 导航 → 抽卡商店 | 必须拆分为真正 Market Screen |
| 无订单簿 | 新建 Domain + `IMarketService` |
| `CardEntity` 无绑定/交易字段 | 需扩展 |
| 无价格历史 | 需成交写入与聚合 |
| 物品无品质键 | 依赖 P3 物品模型 |

**建议落地顺序（P4）：**

1. `MarketDomain` 撮合单测 + 手续费/护栏  
2. `LocalMockMarketService` + 本地持久化  
3. Market Screen 盘口/历史/挂单  
4. 资源交易接通仓库  
5. 卡牌绑定字段 + 上架/成交  
6. `RemoteMarketService` 空实现或 staging API  

---

## 9. 验收清单

- [ ] 买单/卖单/立即买/立即卖可用，预扣与到账一致  
- [ ] 手续费按表扣除；订单数有上限  
- [ ] 历史至少 24H/7D：最近价、高低、均价、量、曲线  
- [ ] 资源按 `itemDefId+quality` 分盘  
- [ ] 普通卡可交易；Starter/主线关键卡不可  
- [ ] 升级卡成交保留成长；上架需卸装且 Idle  
- [ ] 价格护栏与新玩家确认生效  
- [ ] `IMarketService` 可切换 Mock/Remote  
- [ ] Market UI 不再进入抽卡商店  
- [ ] EditMode：撮合顺序、手续费、自成交拒绝、绑定拒绝  

---

## 10. 开放钩子

- 多账号转移检测服务  
- 动态费率 / 通胀监控仪表盘  
- 拍卖行模式（非 MVP）  
- 卡牌按精确技能词条分盘（慎做，防流动性碎裂）  
- `craftSellCooldownSeconds` 反投机  

若取消全服共享改为个人商店，或禁止升级卡交易，需修订核心设计并升本文主版本。

---

## 11. 参考

- 核心设计：`Documentation/01-core-product-design.md` §7.5 / §7.6 / §21  
- 品质与堆叠：`05-production-and-quality.md`  
- 占用：`01-deck-and-occupation.md`  
- 开发计划：`Documentation/zh-CN/11-mvp-development-plan.md` P4  
- 现有：`AppShell` Market 导航、`CardDrawingManager`（须解耦）、`CardEntity`、`ItemEntity`
