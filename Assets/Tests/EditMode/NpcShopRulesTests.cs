using Assets.Resources.Scripts.Market.Domain;
using NUnit.Framework;

namespace GalacticFrontier.Tests.EditMode
{
    public class NpcShopRulesTests
    {
        [Test]
        public void PlayerMarket_Disabled_In_Solo()
        {
            var r = NpcShopRules.PlayerMarketGate(PlayMode.Solo);
            Assert.IsFalse(r.Success);
            Assert.AreEqual(MarketCommandError.MarketDisabledInSolo, r.Error);
        }

        [Test]
        public void PlayerMarket_Allowed_Online_GateOnly()
        {
            var r = NpcShopRules.PlayerMarketGate(PlayMode.Online);
            Assert.IsTrue(r.Success);
        }

        [Test]
        public void SellBack_Uses_Ratio()
        {
            var shop = new NpcShopDef { sellBackRatio = 0.4f };
            var offer = new NpcShopOfferDef { buyPrice = 10, sellPrice = -1 };
            Assert.AreEqual(4, NpcShopRules.ResolveSellPrice(shop, offer));
        }

        [Test]
        public void Unlock_Requires_Region_And_Ship()
        {
            var offer = new NpcShopOfferDef
            {
                unlock = new NpcShopOfferUnlock
                {
                    requireRegionCleared = "sec01_outer_belt",
                    minShipLevel = 2
                }
            };
            var locked = new ShopUnlockContext(1, _ => true, _ => 0);
            Assert.IsFalse(NpcShopRules.IsUnlocked(offer, locked));

            var unlocked = new ShopUnlockContext(2, id => id == "sec01_outer_belt", _ => 0);
            Assert.IsTrue(NpcShopRules.IsUnlocked(offer, unlocked));
        }

        [Test]
        public void Buy_Fails_On_Insufficient_Credits()
        {
            NpcShopCatalog.ResetToDefaultsForTests();
            var shop = NpcShopCatalog.DefaultShop();
            var offer = NpcShopCatalog.FindOffer(shop.shopId, "offer_scrap");
            var ctx = new ShopUnlockContext(99, _ => true, _ => 0);
            var r = NpcShopRules.ValidateBuy(shop, offer, 1, credits: 0, 0, 60, 0, ctx);
            Assert.IsFalse(r.Success);
            Assert.AreEqual(MarketCommandError.InsufficientCredits, r.Error);
        }

        [Test]
        public void Buy_Fails_When_Warehouse_Full_NonStack_Path()
        {
            NpcShopCatalog.ResetToDefaultsForTests();
            var shop = NpcShopCatalog.DefaultShop();
            // repair kit is stackable consumable — use a crafted equipment offer if any;
            // force equipment-like by temporary offer
            var offer = new NpcShopOfferDef
            {
                offerId = "tmp_eq",
                itemDefId = "eq_pulse_rifle",
                quality = 2,
                buyPrice = 1,
                canBuy = true,
                unlock = new NpcShopOfferUnlock()
            };
            var ctx = new ShopUnlockContext(99, _ => true, _ => 0);
            var r = NpcShopRules.ValidateBuy(shop, offer, 1, 100, 60, 60, 0, ctx);
            Assert.IsFalse(r.Success);
            Assert.AreEqual(MarketCommandError.WarehouseFull, r.Error);
        }
    }
}
