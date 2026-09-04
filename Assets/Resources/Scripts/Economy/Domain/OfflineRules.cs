using System;
using Assets.Resources.Scripts.Progression.Domain;

namespace Assets.Resources.Scripts.Economy.Domain
{
    public static class OfflineRules
    {
        public static int EffectiveCapSeconds(int shipLevel, int cargoModuleLevel, int commanderLevel = 1)
        {
            var baseCap = EconomyConstants.OfflineCapBaseSeconds;
            var bonus = Math.Max(0, shipLevel - 1) * 600
                        + Math.Max(0, cargoModuleLevel) * 900
                        + ProgressionRules.CommanderOfflineBonusSeconds(commanderLevel);
            return Math.Min(EconomyConstants.OfflineCapHardSeconds, baseCap + bonus);
        }

        public static float YieldRatio(long elapsedSeconds, int capSeconds)
        {
            if (elapsedSeconds <= 0) return 0f;
            return 1f;
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
