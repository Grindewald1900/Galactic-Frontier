using System;

namespace Assets.Resources.Scripts.Economy.Domain
{
    /// <summary>Pure gacha cost / pity rules (systems/15). No Unity dependency.</summary>
    public static class GachaRules
    {
        public const string StandardPoolId = "pool_standard";
        public const string TicketDefId = "con_recruit_ticket";
        public const int CostPerPull = 1;
        public const int TenPullMultiplier = 10;
        public const int PityThreshold = 40;

        public static int TicketsRequired(int pullCount)
        {
            if (pullCount <= 0) return 0;
            return pullCount * CostPerPull;
        }

        public static bool ShouldForceHighTier(int pityCounter) => pityCounter >= PityThreshold;

        public static int AdvancePity(int pityCounter, bool hitPityMinTier)
        {
            if (hitPityMinTier) return 0;
            return Math.Max(0, pityCounter) + 1;
        }
    }
}
