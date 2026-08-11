using System;
using Assets.Resources.Scripts.Economy.Domain;

namespace Assets.Resources.Scripts.Market.Domain
{
    public static class NpcShopRules
    {
        public static int ResolveSellPrice(NpcShopDef shop, NpcShopOfferDef offer)
        {
            if (offer == null) return 0;
            if (offer.sellPrice >= 0) return offer.sellPrice;
            var ratio = shop != null && shop.sellBackRatio > 0f
                ? shop.sellBackRatio
                : MarketConstants.DefaultSellBackRatio;
            return Math.Max(1, (int)Math.Floor(offer.buyPrice * ratio));
        }

        public static bool IsUnlocked(NpcShopOfferDef offer, ShopUnlockContext ctx)
        {
            if (offer?.unlock == null) return true;
            var u = offer.unlock;
            if (u.minShipLevel > 0 && ctx.ShipLevel < u.minShipLevel)
                return false;
            if (!string.IsNullOrEmpty(u.requireRegionCleared)
                && !ctx.IsRegionCleared(u.requireRegionCleared))
                return false;
            if (!string.IsNullOrEmpty(u.requireModuleId)
                && ctx.ModuleLevel(u.requireModuleId) < Math.Max(1, u.minModuleLevel))
                return false;
            return true;
        }

        public static MarketCommandResult ValidateBuy(
            NpcShopDef shop,
            NpcShopOfferDef offer,
            int quantity,
            int credits,
            int warehouseSlotsUsed,
            int warehouseCapacity,
            int alreadyPurchased,
            ShopUnlockContext ctx)
        {
            if (shop == null)
                return MarketCommandResult.Fail(MarketCommandError.UnknownShop, "Unknown shop.");
            if (offer == null)
                return MarketCommandResult.Fail(MarketCommandError.UnknownOffer, "Unknown offer.");
            if (!offer.canBuy)
                return MarketCommandResult.Fail(MarketCommandError.NotSellable, "Offer not for sale.");
            if (quantity <= 0)
                return MarketCommandResult.Fail(MarketCommandError.InvalidQuantity, "Invalid quantity.");
            if (!IsUnlocked(offer, ctx))
                return MarketCommandResult.Fail(MarketCommandError.Locked, "Offer locked.");
            if (offer.purchaseLimit > 0 && alreadyPurchased + quantity > offer.purchaseLimit)
                return MarketCommandResult.Fail(MarketCommandError.PurchaseLimit, "Purchase limit reached.");

            var cost = offer.buyPrice * quantity;
            if (credits < cost)
                return MarketCommandResult.Fail(MarketCommandError.InsufficientCredits, "Not enough credits.");

            var def = ItemCatalog.Get(offer.itemDefId);
            var needsNewSlot = def == null || !def.stackable || ItemCatalog.IsEquipment(offer.itemDefId);
            // Stackables can merge; only fail if warehouse completely full AND would need new stack.
            // Pure rule: if capacity full, buyer must have existing stack — caller checks merge.
            if (warehouseSlotsUsed >= warehouseCapacity && needsNewSlot)
                return MarketCommandResult.Fail(MarketCommandError.WarehouseFull, "Warehouse full.");

            return MarketCommandResult.Ok();
        }

        public static MarketCommandResult ValidateSell(
            NpcShopDef shop,
            NpcShopOfferDef offer,
            int quantity,
            int ownedQuantity,
            ShopUnlockContext ctx)
        {
            if (shop == null)
                return MarketCommandResult.Fail(MarketCommandError.UnknownShop, "Unknown shop.");
            if (offer == null)
                return MarketCommandResult.Fail(MarketCommandError.UnknownOffer, "Unknown offer.");
            if (!offer.canSell)
                return MarketCommandResult.Fail(MarketCommandError.NotSellable, "Cannot sell this here.");
            if (quantity <= 0)
                return MarketCommandResult.Fail(MarketCommandError.InvalidQuantity, "Invalid quantity.");
            // Selling does not require unlock (player may have mats from elsewhere)
            if (ownedQuantity < quantity)
                return MarketCommandResult.Fail(MarketCommandError.InsufficientItems, "Not enough items.");
            return MarketCommandResult.Ok();
        }

        public static MarketCommandResult PlayerMarketGate(PlayMode mode)
        {
            if (mode == PlayMode.Solo)
            {
                return MarketCommandResult.Fail(
                    MarketCommandError.MarketDisabledInSolo,
                    "Player market is disabled in Solo mode.");
            }

            return MarketCommandResult.Ok();
        }
    }
}
