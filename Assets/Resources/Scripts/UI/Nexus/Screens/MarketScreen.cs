using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Market;
using Assets.Resources.Scripts.Market.Domain;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.World;
using TMPro;
using UnityEngine;

namespace Assets.Resources.Scripts.UI.Nexus
{
    /// <summary>Starport NPC shop (Solo Market nav). Not gacha; player market hidden in Solo.</summary>
    internal sealed class MarketScreen
    {
        private readonly Transform root;
        private string selectedOfferId = "";
        private string statusMessage = "";

        private MarketScreen(Transform root)
        {
            this.root = root;
        }

        public GameObject Root => root.gameObject;

        public static MarketScreen Build(Transform parent)
        {
            var panel = NexusUiFactory.CreatePanel(
                parent, "Market Screen", NexusTheme.Background,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var screen = new MarketScreen(panel.transform);
            screen.Rebuild();
            return screen;
        }

        public void Rebuild()
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(root.GetChild(i).gameObject);

            if (DataUtil.Instance != null)
            {
                WorldService.EnsureLoaded(DataUtil.Instance);
                ShipService.EnsureLoaded(DataUtil.Instance);
            }

            NpcShopService.EnsureCatalogLoaded();
            var shop = NpcShopCatalog.DefaultShop();
            var ctx = NpcShopService.BuildUnlockContext();

            NexusUiFactory.CreateText(
                root, "Title", UiText.StarportShopTitle,
                new Vector2(28f, 20f), new Vector2(700f, 36f), 22f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            var shopName = shop != null
                ? UiText.T(shop.displayNameEn, shop.displayNameZh)
                : UiText.StarportShopTitle;
            NexusUiFactory.CreateText(
                root, "Subtitle",
                $"{shopName} · {UiText.CreditsLabel(CurrencyService.Credits)}" +
                (CurrencyService.CreditsBound > 0
                    ? $" · {UiText.BoundCreditsLabel(CurrencyService.CreditsBound)}"
                    : ""),
                new Vector2(28f, 56f), new Vector2(1100f, 28f), 13f, NexusTheme.MutedText);

            NexusUiFactory.CreateText(
                root, "Hint", UiText.StarportShopHint,
                new Vector2(28f, 84f), new Vector2(1100f, 28f), 12f, NexusTheme.DimText);
            OnboardingBanner.TryDraw(root, AppScreen.Market, new Vector2(28f, 108f));

            if (PlayModeService.IsSolo)
            {
                NexusUiFactory.CreateText(
                    root, "SoloBadge", UiText.PlayerMarketDisabledSolo,
                    new Vector2(1200f, 20f), new Vector2(480f, 28f), 12f, NexusTheme.Gold,
                    TextAlignmentOptions.Right, FontStyles.Bold);
            }
            else
            {
                NexusUiFactory.CreateButton(
                    root, "PlayerMarket", UiText.OpenPlayerMarket,
                    new Vector2(1400f, 16f), new Vector2(280f, 40f),
                    () =>
                    {
                        var r = PlayerMarketService.Open();
                        statusMessage = r.Success ? UiText.PlayerMarketSoon : r.Message;
                        Rebuild();
                    },
                    NexusTheme.WithAlpha(NexusTheme.Purple, 0.16f), NexusTheme.Purple, 12f);
            }

            float y = 130f;
            if (shop?.offers != null)
            {
                foreach (var offer in shop.offers)
                {
                    if (offer == null) continue;
                    var unlocked = NpcShopRules.IsUnlocked(offer, ctx);
                    var def = ItemCatalog.Get(offer.itemDefId);
                    var name = def != null
                        ? UiText.T(def.displayNameEn, def.displayNameZh)
                        : offer.itemDefId;
                    var sell = NpcShopRules.ResolveSellPrice(shop, offer);
                    var owned = InventoryRules.CountOf(
                        ItemFactory.ToStacks(ProductionService.GetLocalItems()),
                        offer.itemDefId, offer.quality);
                    var limit = offer.purchaseLimit > 0
                        ? $" · lim {NpcShopService.GetSessionPurchased(shop.shopId, offer.offerId)}/{offer.purchaseLimit}"
                        : "";
                    var lockTag = unlocked ? "" : $" · {UiText.OfferLocked}";
                    var selected = offer.offerId == selectedOfferId;
                    var captured = offer.offerId;
                    NexusUiFactory.CreateButton(
                        root, "Offer " + offer.offerId,
                        $"{name} Q{offer.quality}  buy {offer.buyPrice}₵  sell {sell}₵  have {owned}{limit}{lockTag}",
                        new Vector2(28f, y), new Vector2(900f, 40f),
                        () =>
                        {
                            selectedOfferId = captured;
                            Rebuild();
                        },
                        selected
                            ? NexusTheme.WithAlpha(NexusTheme.Gold, 0.22f)
                            : (unlocked ? NexusTheme.SurfaceRaised : NexusTheme.Surface),
                        unlocked ? (selected ? NexusTheme.Gold : NexusTheme.Text) : NexusTheme.DimText,
                        12f);
                    y += 46f;
                }
            }

            BuildActions(shop, ctx);
            if (!string.IsNullOrEmpty(statusMessage))
            {
                NexusUiFactory.CreateText(
                    root, "Status", statusMessage,
                    new Vector2(980f, 200f), new Vector2(700f, 120f), 14f, NexusTheme.Cyan);
            }
        }

        private void BuildActions(NpcShopDef shop, ShopUnlockContext ctx)
        {
            GameObject box = NexusUiFactory.CreateBox(
                root, "Actions", new Vector2(980f, 130f), new Vector2(700f, 280f),
                NexusTheme.Surface, NexusTheme.BorderSoft);

            var offer = shop != null ? NpcShopCatalog.FindOffer(shop.shopId, selectedOfferId) : null;
            if (offer == null)
            {
                NexusUiFactory.CreateText(
                    box.transform, "Empty", UiText.SelectShopOffer,
                    new Vector2(24f, 24f), new Vector2(640f, 40f), 14f, NexusTheme.MutedText);
                return;
            }

            var def = ItemCatalog.Get(offer.itemDefId);
            var name = def != null
                ? UiText.T(def.displayNameEn, def.displayNameZh)
                : offer.itemDefId;
            var unlocked = NpcShopRules.IsUnlocked(offer, ctx);
            NexusUiFactory.CreateText(
                box.transform, "Name", $"{name} · Q{offer.quality}",
                new Vector2(24f, 20f), new Vector2(640f, 32f), 16f, NexusTheme.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);

            NexusUiFactory.CreateText(
                box.transform, "Prices",
                UiText.ShopPrices(offer.buyPrice, NpcShopRules.ResolveSellPrice(shop, offer)),
                new Vector2(24f, 56f), new Vector2(640f, 28f), 13f, NexusTheme.MutedText);

            NexusUiFactory.CreateButton(
                box.transform, "Buy", UiText.ShopBuy,
                new Vector2(24f, 120f), new Vector2(200f, 48f),
                () =>
                {
                    if (!unlocked)
                    {
                        statusMessage = UiText.OfferLocked;
                        Rebuild();
                        return;
                    }

                    var r = NpcShopService.TryBuy(shop.shopId, offer.offerId, 1);
                    statusMessage = r.Success ? r.Message : r.Message;
                    Rebuild();
                },
                NexusTheme.WithAlpha(NexusTheme.Gold, 0.18f), NexusTheme.Gold, 14f);

            NexusUiFactory.CreateButton(
                box.transform, "Sell", UiText.ShopSell,
                new Vector2(240f, 120f), new Vector2(200f, 48f),
                () =>
                {
                    var r = NpcShopService.TrySell(shop.shopId, offer.offerId, 1);
                    statusMessage = r.Message;
                    Rebuild();
                },
                NexusTheme.WithAlpha(NexusTheme.Cyan, 0.16f), NexusTheme.Cyan, 14f);

            NexusUiFactory.CreateButton(
                box.transform, "Buy5", UiText.ShopBuyX(5),
                new Vector2(24f, 184f), new Vector2(200f, 44f),
                () =>
                {
                    var r = NpcShopService.TryBuy(shop.shopId, offer.offerId, 5);
                    statusMessage = r.Message;
                    Rebuild();
                },
                NexusTheme.SurfaceRaised, NexusTheme.Text, 13f);

            NexusUiFactory.CreateButton(
                box.transform, "Sell5", UiText.ShopSellX(5),
                new Vector2(240f, 184f), new Vector2(200f, 44f),
                () =>
                {
                    var r = NpcShopService.TrySell(shop.shopId, offer.offerId, 5);
                    statusMessage = r.Message;
                    Rebuild();
                },
                NexusTheme.SurfaceRaised, NexusTheme.Text, 13f);
        }
    }
}
