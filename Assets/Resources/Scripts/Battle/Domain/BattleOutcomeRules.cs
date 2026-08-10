namespace Assets.Resources.Scripts.Battle.Domain
{
    /// <summary>Result of a finished auto-battle.</summary>
    public enum BattleOutcome
    {
        None = 0,
        Victory = 1,
        Defeat = 2
    }

    /// <summary>Pure win/loss rules from systems/02-auto-battle.md §4.8.</summary>
    public static class BattleOutcomeRules
    {
        /// <summary>
        /// Enemy wiped → Victory; player wiped → Defeat; round cap with both alive → Defeat.
        /// </summary>
        public static BattleOutcome Resolve(bool playerAlive, bool enemyAlive, bool reachedRoundCap)
        {
            if (!playerAlive && !enemyAlive)
                return BattleOutcome.Defeat;
            if (!enemyAlive && playerAlive)
                return BattleOutcome.Victory;
            if (!playerAlive)
                return BattleOutcome.Defeat;
            if (reachedRoundCap)
                return BattleOutcome.Defeat;
            return BattleOutcome.None;
        }
    }
}
