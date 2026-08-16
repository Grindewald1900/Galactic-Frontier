using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Economy.Domain;

namespace Assets.Resources.Scripts.Market.Domain
{
    /// <summary>Embedded NPC shop defaults; optional JSON overlay replaces when loaded.</summary>
    public static class NpcShopCatalog
    {
        private static readonly Dictionary<string, NpcShopDef> ById = new Dictionary<string, NpcShopDef>();
        private static readonly List<NpcShopDef> Ordered = new List<NpcShopDef>();
        private static bool loaded;

        public static IReadOnlyList<NpcShopDef> All
        {
            get
            {
                Ensure();
                return Ordered;
            }
        }

        public static NpcShopDef Get(string shopId)
        {
            Ensure();
            if (string.IsNullOrEmpty(shopId)) return null;
            return ById.TryGetValue(shopId, out var s) ? s : null;
        }

        public static NpcShopDef DefaultShop()
        {
            Ensure();
            return Get(MarketConstants.DefaultShopId) ?? (Ordered.Count > 0 ? Ordered[0] : null);
        }

        public static NpcShopOfferDef FindOffer(string shopId, string offerId)
        {
            var shop = Get(shopId);
            if (shop?.offers == null) return null;
            foreach (var o in shop.offers)
            {
                if (o != null && o.offerId == offerId)
                    return o;
            }

            return null;
        }

        /// <summary>Replace catalog from deserialized table (Resources JSON).</summary>
        public static void LoadOverlay(NpcShopsTable table)
        {
            if (table?.shops == null || table.shops.Count == 0)
                return;
            ById.Clear();
            Ordered.Clear();
            foreach (var shop in table.shops)
            {
                if (shop == null || string.IsNullOrEmpty(shop.shopId)) continue;
                ById[shop.shopId] = shop;
                Ordered.Add(shop);
            }

            loaded = true;
            MergeMissingDefaultOffers();
        }

        public static void ResetToDefaultsForTests()
        {
            loaded = false;
            ById.Clear();
            Ordered.Clear();
            Ensure();
        }

        private static void Ensure()
        {
            if (loaded) return;
            loaded = true;
            Add(BuildStarportShop());
        }

        private static void MergeMissingDefaultOffers()
        {
            var defaults = BuildStarportShop();
            var shop = Get(defaults.shopId);
            if (shop == null)
            {
                Add(defaults);
                return;
            }

            shop.offers ??= new List<NpcShopOfferDef>();
            foreach (var offer in defaults.offers)
            {
                if (offer == null || string.IsNullOrEmpty(offer.offerId)) continue;
                if (FindOffer(shop.shopId, offer.offerId) != null) continue;
                shop.offers.Add(offer);
            }
        }

        private static void Add(NpcShopDef shop)
        {
            ById[shop.shopId] = shop;
            Ordered.Add(shop);
        }

        private static NpcShopDef BuildStarportShop()
        {
            return new NpcShopDef
            {
                shopId = MarketConstants.DefaultShopId,
                displayNameEn = "Starport Merchant",
                displayNameZh = "星港商人",
                anchor = "bridge",
                sellBackRatio = MarketConstants.DefaultSellBackRatio,
                offers = new List<NpcShopOfferDef>
                {
                    Offer("offer_scrap", "mat_scrap", 2, 5, true, true, 0, null),
                    Offer("offer_iron", "mat_iron_ore", 2, 8, true, true, 0,
                        Unlock("sec01_outer_belt", 1)),
                    Offer("offer_crystal", "mat_crystal_sand", 2, 10, true, true, 0,
                        Unlock("sec01_mining_spur", 1)),
                    Offer("offer_fungal", "mat_fungal", 2, 9, true, true, 0,
                        Unlock("sec01_mining_spur", 1)),
                    Offer("offer_alloy", "mat_alloy_plate", 2, 24, true, true, 20,
                        Unlock("sec01_outer_belt", 2)),
                    Offer("offer_energy", "mat_energy_cell", 2, 30, true, true, 20,
                        Unlock("sec01_mining_spur", 2)),
                    Offer("offer_biofiber", "mat_biofiber", 2, 18, true, true, 20,
                        Unlock("sec01_mining_spur", 2)),
                    Offer("offer_repair_kit", EconomyConstants.RepairKitDefId, 2, 28, true, true, 10,
                        Unlock("", 1)),
                    Offer("offer_repair_parts", "mat_repair_parts", 2, 11, true, true, 0, null),
                    Offer("offer_ration", "con_field_ration", 2, 6, true, true, 0, null),
                    Offer("offer_ticket", GachaRules.TicketDefId, 2, 40, true, true, 0, null),
                }
            };
        }

        private static NpcShopOfferDef Offer(
            string id, string defId, int q, int buy, bool canBuy, bool canSell, int limit,
            NpcShopOfferUnlock unlock) =>
            new NpcShopOfferDef
            {
                offerId = id,
                itemDefId = defId,
                quality = q,
                buyPrice = buy,
                sellPrice = -1,
                canBuy = canBuy,
                canSell = canSell,
                purchaseLimit = limit,
                unlock = unlock ?? new NpcShopOfferUnlock()
            };

        private static NpcShopOfferUnlock Unlock(string region, int shipLevel) =>
            new NpcShopOfferUnlock
            {
                requireRegionCleared = region ?? "",
                minShipLevel = shipLevel
            };
    }
}
