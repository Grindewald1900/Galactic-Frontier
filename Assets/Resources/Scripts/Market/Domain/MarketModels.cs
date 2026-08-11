using System;
using System.Collections.Generic;

namespace Assets.Resources.Scripts.Market.Domain
{
    [Serializable]
    public class NpcShopOfferUnlock
    {
        public string requireRegionCleared = "";
        public int minShipLevel;
        public int minModuleLevel;
        public string requireModuleId = "";
    }

    [Serializable]
    public class NpcShopOfferDef
    {
        public string offerId = "";
        public string itemDefId = "";
        public int quality = 2;
        public int buyPrice = 10;
        public int sellPrice = -1; // <0 → buyPrice * sellBackRatio
        public bool canBuy = true;
        public bool canSell = true;
        public int purchaseLimit; // 0 = unlimited
        public NpcShopOfferUnlock unlock = new NpcShopOfferUnlock();
    }

    [Serializable]
    public class NpcShopDef
    {
        public string shopId = "";
        public string displayNameEn = "Starport Merchant";
        public string displayNameZh = "星港商人";
        public string anchor = "bridge";
        public float sellBackRatio = MarketConstants.DefaultSellBackRatio;
        public List<NpcShopOfferDef> offers = new List<NpcShopOfferDef>();
    }

    [Serializable]
    public class NpcShopsTable
    {
        public int version = 1;
        public List<NpcShopDef> shops = new List<NpcShopDef>();
    }

    public sealed class MarketCommandResult
    {
        public bool Success;
        public MarketCommandError Error = MarketCommandError.None;
        public string Message = "";

        public static MarketCommandResult Ok(string message = "") =>
            new MarketCommandResult { Success = true, Message = message ?? "" };

        public static MarketCommandResult Fail(MarketCommandError error, string message) =>
            new MarketCommandResult
            {
                Success = false,
                Error = error,
                Message = message ?? error.ToString()
            };
    }

    /// <summary>Context for unlock checks (pure; no Unity services).</summary>
    public readonly struct ShopUnlockContext
    {
        public int ShipLevel { get; }
        public Func<string, bool> IsRegionCleared { get; }
        public Func<string, int> ModuleLevel { get; }

        public ShopUnlockContext(
            int shipLevel,
            Func<string, bool> isRegionCleared,
            Func<string, int> moduleLevel)
        {
            ShipLevel = shipLevel;
            IsRegionCleared = isRegionCleared ?? (_ => false);
            ModuleLevel = moduleLevel ?? (_ => 0);
        }
    }
}
