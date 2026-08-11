namespace Assets.Resources.Scripts.Market.Domain
{
    public enum PlayMode
    {
        Solo = 0,
        Online = 1
    }

    public enum MarketCommandError
    {
        None = 0,
        MarketDisabledInSolo = 1,
        UnknownShop = 2,
        UnknownOffer = 3,
        Locked = 4,
        InsufficientCredits = 5,
        InsufficientItems = 6,
        WarehouseFull = 7,
        PurchaseLimit = 8,
        InvalidQuantity = 9,
        NotSellable = 10
    }

    public static class MarketConstants
    {
        public const float DefaultSellBackRatio = 0.40f;
        public const string CurrencyCredits = "credits";
        public const string CurrencyCreditsBound = "credits_bound";
        public const string DefaultShopId = "shop_starport";
    }
}
