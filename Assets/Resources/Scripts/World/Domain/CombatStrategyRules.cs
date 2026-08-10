using System.Collections.Generic;

namespace Assets.Resources.Scripts.World.Domain
{
    /// <summary>Minimal combatant snapshot for strategy / AFK resolution (no Unity types).</summary>
    public sealed class CombatantSnapshot
    {
        public string Id;
        public float CurrentHp;
        public float MaxHp;
        public float Power;
        public bool IsPlayer;
        public bool Alive => CurrentHp > 0f;
    }

    public static class CombatStrategyRules
    {
        public static CombatStrategyId Parse(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return CombatStrategyId.Balanced;
            return System.Enum.TryParse(raw, true, out CombatStrategyId id) ? id : CombatStrategyId.Balanced;
        }

        /// <summary>Picks an enemy target index for the given strategy.</summary>
        public static int PickEnemyTargetIndex(CombatStrategyId strategy, IList<CombatantSnapshot> enemies)
        {
            if (enemies == null || enemies.Count == 0)
                return -1;

            var best = -1;
            for (var i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] == null || !enemies[i].Alive)
                    continue;
                if (best < 0)
                {
                    best = i;
                    continue;
                }

                if (IsBetterTarget(strategy, enemies[i], enemies[best]))
                    best = i;
            }

            return best;
        }

        private static bool IsBetterTarget(CombatStrategyId strategy, CombatantSnapshot candidate, CombatantSnapshot current)
        {
            switch (strategy)
            {
                case CombatStrategyId.FocusLowestHp:
                    return candidate.CurrentHp < current.CurrentHp;
                case CombatStrategyId.FocusHighestThreat:
                    return candidate.Power > current.Power;
                case CombatStrategyId.PreferAoe:
                    // Prefer fuller HP clusters (proxy: higher max hp) for splash value.
                    return candidate.MaxHp > current.MaxHp;
                default:
                    // Balanced: lowest remaining HP ratio.
                    var cr = candidate.MaxHp > 0 ? candidate.CurrentHp / candidate.MaxHp : 1f;
                    var cur = current.MaxHp > 0 ? current.CurrentHp / current.MaxHp : 1f;
                    return cr < cur;
            }
        }
    }

    /// <summary>Deterministic AFK farm cycle outcome without full battle simulation.</summary>
    public static class FarmCombatResolver
    {
        public sealed class FarmCycleResult
        {
            public bool Victory;
            public int LootScrap;
            public long Seed;
        }

        public static FarmCycleResult Resolve(
            float playerPowerSum,
            float enemyPowerSum,
            int baseLoot,
            long seed)
        {
            // Simple seeded bias: player wins if power * (0.85..1.15) >= enemy.
            var roll = (seed & 0xFFFF) / 65535f;
            var factor = 0.85f + roll * 0.3f;
            var victory = playerPowerSum * factor >= enemyPowerSum * 0.9f;
            return new FarmCycleResult
            {
                Victory = victory,
                LootScrap = victory ? System.Math.Max(0, baseLoot) : 0,
                Seed = seed
            };
        }

        public static float EstimateEncounterPower(EncounterConfig encounter)
        {
            if (encounter?.enemies == null) return 50f;
            float sum = 0f;
            foreach (var e in encounter.enemies)
            {
                if (e == null) continue;
                sum += 40f + e.level * 25f;
            }

            return sum <= 0 ? 50f : sum;
        }
    }
}
