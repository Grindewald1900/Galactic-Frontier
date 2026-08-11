using Assets.Resources.Scripts.Market.Domain;

namespace Assets.Resources.Scripts.Market
{
    /// <summary>Player order-book API. Solo always disabled (P4.1 / P4.6 Online-only).</summary>
    public static class PlayerMarketService
    {
        public static bool IsAvailable => PlayModeService.IsOnline;

        public static MarketCommandResult Open()
        {
            return NpcShopRules.PlayerMarketGate(PlayModeService.Current);
        }

        public static MarketCommandResult PlaceSellOrder(string listingKey, int unitPrice, int quantity)
        {
            var gate = Open();
            if (!gate.Success) return gate;
            return MarketCommandResult.Fail(
                MarketCommandError.None,
                "Player market not implemented (Online post-MVP).");
        }

        public static MarketCommandResult PlaceBuyOrder(string listingKey, int unitPrice, int quantity)
        {
            var gate = Open();
            if (!gate.Success) return gate;
            return MarketCommandResult.Fail(
                MarketCommandError.None,
                "Player market not implemented (Online post-MVP).");
        }
    }
}
