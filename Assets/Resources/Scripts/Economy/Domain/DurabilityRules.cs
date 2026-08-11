using System;

namespace Assets.Resources.Scripts.Economy.Domain
{
    public static class DurabilityRules
    {
        public static int ApplyCombatWear(int current, int max, int baseLoss = EconomyConstants.BaseCombatWear)
        {
            if (max <= 0) return 0;
            var loss = Math.Max(0, baseLoss);
            return Math.Max(0, current - loss);
        }

        public static int ApplyGatherWear(int current, int max, int riskLevel)
        {
            if (max <= 0) return 0;
            var loss = riskLevel <= 1 ? 1 : riskLevel == 2 ? 2 : 3;
            return Math.Max(0, current - loss);
        }

        public static bool IsBroken(int current) => current <= 0;

        public static bool NeedsAutoRepair(int current, int max, int thresholdPercent = EconomyConstants.AutoRepairThresholdPercent)
        {
            if (max <= 0) return false;
            return current * 100 < max * thresholdPercent;
        }

        public static int RepairCostKits(int current, int max)
        {
            if (max <= 0 || current >= max) return 0;
            var missing = max - current;
            return Math.Max(1, (missing + 24) / 25);
        }

        public static bool TryRepair(ref int current, int max, ref int kitCount)
        {
            if (max <= 0 || current >= max) return false;
            var cost = RepairCostKits(current, max);
            if (kitCount < cost) return false;
            kitCount -= cost;
            current = max;
            return true;
        }
    }
}
