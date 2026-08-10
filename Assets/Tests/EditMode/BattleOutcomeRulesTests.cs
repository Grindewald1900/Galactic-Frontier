using Assets.Resources.Scripts.Battle.Domain;
using NUnit.Framework;

namespace GalacticFrontier.Tests.EditMode
{
    public class BattleOutcomeRulesTests
    {
        [Test]
        public void EnemyWiped_IsVictory()
        {
            Assert.AreEqual(
                BattleOutcome.Victory,
                BattleOutcomeRules.Resolve(playerAlive: true, enemyAlive: false, reachedRoundCap: false));
        }

        [Test]
        public void PlayerWiped_IsDefeat()
        {
            Assert.AreEqual(
                BattleOutcome.Defeat,
                BattleOutcomeRules.Resolve(playerAlive: false, enemyAlive: true, reachedRoundCap: false));
        }

        [Test]
        public void RoundCapWithBothAlive_IsDefeat()
        {
            Assert.AreEqual(
                BattleOutcome.Defeat,
                BattleOutcomeRules.Resolve(playerAlive: true, enemyAlive: true, reachedRoundCap: true));
        }

        [Test]
        public void OngoingBattle_IsNone()
        {
            Assert.AreEqual(
                BattleOutcome.None,
                BattleOutcomeRules.Resolve(playerAlive: true, enemyAlive: true, reachedRoundCap: false));
        }
    }
}
