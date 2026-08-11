using System.Collections.Generic;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Market.Domain;
using Assets.Resources.Scripts.Props;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using Assets.Resources.Scripts.World.Domain;
using UnityEngine;

namespace Assets.Resources.Scripts.Market
{
    /// <summary>NPC buy / sell against local warehouse + credits (P4).</summary>
    public static class NpcShopService
    {
        private static readonly Dictionary<string, int> SessionPurchases = new Dictionary<string, int>();
        private static bool catalogReady;

        public static void EnsureCatalogLoaded()
        {
            if (catalogReady) return;
            catalogReady = true;
            var asset = UnityEngine.Resources.Load<TextAsset>(DefaultProperty.NPC_SHOPS_PATH);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
            {
                Debug.Log("[NPC-SHOP] Using embedded catalog (no NpcShops.json).");
                return;
            }

            try
            {
                var table = JsonUtility.FromJson<NpcShopsTable>(asset.text);
                NpcShopCatalog.LoadOverlay(table);
                Debug.Log($"[NPC-SHOP] Loaded overlay shops={table?.shops?.Count ?? 0}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[NPC-SHOP] Failed to parse NpcShops.json: " + ex.Message);
            }
        }

        public static ShopUnlockContext BuildUnlockContext()
        {
            WorldService.EnsureReady();
            ShipService.EnsureReady();
            var shipLevel = ShipService.State?.level ?? 1;
            return new ShopUnlockContext(
                shipLevel,
                regionId =>
                {
                    if (string.IsNullOrEmpty(regionId)) return true;
                    foreach (var v in WorldService.GetAllRegionViews())
                    {
                        if (v.Config == null || v.Config.regionId != regionId) continue;
                        return v.Progress == RegionProgressState.Cleared
                               || v.Progress == RegionProgressState.BossAvailable
                               || v.Progress == RegionProgressState.BossDefeated
                               || v.FarmUnlocked;
                    }

                    return false;
                },
                moduleId =>
                {
                    if (ShipService.State?.modules == null || string.IsNullOrEmpty(moduleId))
                        return 0;
                    foreach (var m in ShipService.State.modules)
                    {
                        if (m != null && m.moduleId == moduleId)
                            return m.level;
                    }

                    return 0;
                });
        }

        public static int GetSessionPurchased(string shopId, string offerId)
        {
            var key = shopId + "|" + offerId;
            return SessionPurchases.TryGetValue(key, out var n) ? n : 0;
        }

        public static MarketCommandResult TryBuy(string shopId, string offerId, int quantity = 1)
        {
            EnsureCatalogLoaded();
            var shop = NpcShopCatalog.Get(shopId) ?? NpcShopCatalog.DefaultShop();
            var offer = NpcShopCatalog.FindOffer(shop?.shopId ?? shopId, offerId);
            var ctx = BuildUnlockContext();
            var items = ProductionService.GetLocalItems();
            var slots = items?.Count ?? 0;
            var purchased = GetSessionPurchased(shop?.shopId ?? "", offerId);

            var validate = NpcShopRules.ValidateBuy(
                shop, offer, quantity, CurrencyService.Credits, slots,
                EconomyConstants.WarehouseCapacity, purchased, ctx);
            if (!validate.Success)
                return validate;

            // Stackable merge check when full
            var def = ItemCatalog.Get(offer.itemDefId);
            var stackable = def != null && def.stackable && !ItemCatalog.IsEquipment(offer.itemDefId);
            if (slots >= EconomyConstants.WarehouseCapacity && stackable)
            {
                var idx = InventoryRules.FindStackIndex(
                    ItemFactory.ToStacks(items), offer.itemDefId, offer.quality);
                if (idx < 0)
                    return MarketCommandResult.Fail(MarketCommandError.WarehouseFull, "Warehouse full.");
            }

            var cost = offer.buyPrice * quantity;
            if (!CurrencyService.TrySpendCredits(cost, out var payErr))
                return MarketCommandResult.Fail(MarketCommandError.InsufficientCredits, payErr);

            var granted = ItemFactory.FromDef(offer.itemDefId, quantity, offer.quality);
            if (!ProductionService.TryAddLocal(granted))
            {
                CurrencyService.AddCredits(cost); // refund
                return MarketCommandResult.Fail(MarketCommandError.WarehouseFull, "Warehouse full.");
            }

            var key = (shop?.shopId ?? shopId) + "|" + offerId;
            SessionPurchases[key] = purchased + quantity;
            return MarketCommandResult.Ok($"Bought {quantity}× {offer.itemDefId} for {cost}₵");
        }

        public static MarketCommandResult TrySell(string shopId, string offerId, int quantity = 1)
        {
            EnsureCatalogLoaded();
            var shop = NpcShopCatalog.Get(shopId) ?? NpcShopCatalog.DefaultShop();
            var offer = NpcShopCatalog.FindOffer(shop?.shopId ?? shopId, offerId);
            var ctx = BuildUnlockContext();
            var owned = InventoryRules.CountOf(
                ItemFactory.ToStacks(ProductionService.GetLocalItems()),
                offer?.itemDefId ?? "",
                offer?.quality ?? 1);

            var validate = NpcShopRules.ValidateSell(shop, offer, quantity, owned, ctx);
            if (!validate.Success)
                return validate;

            var stacks = ItemFactory.ToStacks(ProductionService.GetLocalItems());
            if (!InventoryRules.TryConsume(stacks, offer.itemDefId, quantity, offer.quality))
                return MarketCommandResult.Fail(MarketCommandError.InsufficientItems, "Not enough items.");

            // Prefer exact quality: TryConsume uses minQuality — ok for MVP
            ReplaceInventory(stacks);
            var payout = NpcShopRules.ResolveSellPrice(shop, offer) * quantity;
            CurrencyService.AddCredits(payout);
            return MarketCommandResult.Ok($"Sold {quantity}× {offer.itemDefId} for {payout}₵");
        }

        private static void ReplaceInventory(List<InventoryStack> stacks)
        {
            var list = new List<Entity.ItemEntity>();
            foreach (var s in stacks)
                list.Add(ItemFactory.FromStack(s));

            if (Inventory.ItemManager.Instance != null && Inventory.ItemManager.Instance.HasLoaded)
            {
                var current = Inventory.ItemManager.Instance.GetItems();
                current.Clear();
                current.AddRange(list);
                Inventory.ItemManager.Instance.RefreshSlotsFromMemory();
            }

            DataUtil.Instance?.SaveInventory(
                Utils.Save.InventoryStore.Local, list, touchMeta: true);
        }
    }
}
