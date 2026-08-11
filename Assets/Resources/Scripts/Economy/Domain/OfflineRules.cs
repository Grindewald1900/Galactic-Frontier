using System;

namespace Assets.Resources.Scripts.Economy.Domain
{
    public static class OfflineRules
    {
        public static int EffectiveCapSeconds(int shipLevel, int cargoModuleLevel)
        {
            var baseCap = EconomyConstants.OfflineCapBaseSeconds;
            var bonus = Math.Max(0, shipLevel - 1) * 600 + Math.Max(0, cargoModuleLevel) * 900;
            return Math.Min(EconomyConstants.OfflineCapHardSeconds, baseCap + bonus);
        }

        public static float YieldRatio(long elapsedSeconds, int capSeconds)
        {
            if (elapsedSeconds <= 0) return 0f;
            var soft = EconomyConstants.OfflineCapSoftSeconds;
            var hard = Math.Max(capSeconds, EconomyConstants.OfflineCapHardSeconds);
            if (elapsedSeconds <= soft)
                return EconomyConstants.OfflineYieldBase
                       + (EconomyConstants.OfflineYieldSoft - EconomyConstants.OfflineYieldBase)
                       * (elapsedSeconds / (float)soft);
            if (elapsedSeconds <= hard)
                return EconomyConstants.OfflineYieldSoft
                       + (EconomyConstants.OfflineYieldHard - EconomyConstants.OfflineYieldSoft)
                       * ((elapsedSeconds - soft) / (float)Math.Max(1, hard - soft));
            return EconomyConstants.OfflineYieldHard;
        }

        public static int ScaleReward(int baseQty, float yieldRatio)
        {
            if (baseQty <= 0 || yieldRatio <= 0f) return 0;
            return Math.Max(0, (int)Math.Floor(baseQty * yieldRatio));
        }

        public static int MasteryFor(PlayerIdleState idle, string recipeId)
        {
            if (idle?.mastery == null || string.IsNullOrEmpty(recipeId)) return 0;
            foreach (var m in idle.mastery)
            {
                if (m != null && m.recipeId == recipeId)
                    return m.successCount;
            }

            return 0;
        }

        public static void AddMastery(PlayerIdleState idle, string recipeId)
        {
            if (idle == null || string.IsNullOrEmpty(recipeId)) return;
            if (idle.mastery == null) idle.mastery = new System.Collections.Generic.List<RecipeMasteryEntry>();
            foreach (var m in idle.mastery)
            {
                if (m != null && m.recipeId == recipeId)
                {
                    m.successCount++;
                    return;
                }
            }

            idle.mastery.Add(new RecipeMasteryEntry { recipeId = recipeId, successCount = 1 });
        }
    }
}
