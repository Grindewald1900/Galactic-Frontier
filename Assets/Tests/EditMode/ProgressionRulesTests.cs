using Assets.Resources.Scripts.Progression.Domain;
using NUnit.Framework;

namespace GalacticFrontier.Tests.EditMode
{
    public class ProgressionRulesTests
    {
        [Test]
        public void Caps_MatchDesignTable()
        {
            Assert.AreEqual(10, ProgressionRules.CombatCap(EnergyRank.F));
            Assert.AreEqual(5, ProgressionRules.SkillCap(EnergyRank.F));
            Assert.AreEqual(100, ProgressionRules.CombatCap(EnergyRank.S));
            Assert.AreEqual(60, ProgressionRules.SkillCap(EnergyRank.S));
        }

        [Test]
        public void CombatXp_BanksWhenAtCap()
        {
            var state = new CombatXpState
            {
                Level = ProgressionRules.MaxCombatLevel,
                ExpToNext = ProgressionRules.CombatExpToNext(ProgressionRules.MaxCombatLevel)
            };
            state = ProgressionRules.ApplyCombatXp(state, EnergyRank.S, 250f);
            Assert.AreEqual(ProgressionRules.MaxCombatLevel, state.Level);
            Assert.AreEqual(0f, state.StoredXp);
            Assert.Greater(state.CurrentXp, 0f);
            Assert.IsTrue(state.AtCap);
        }

        [Test]
        public void CombatXp_OverflowLevelsPastEnergyRankCap()
        {
            var state = new CombatXpState
            {
                Level = 10,
                CurrentXp = 5000f,
                ExpToNext = ProgressionRules.CombatExpToNext(10)
            };
            state = ProgressionRules.ApplyCombatXp(state, EnergyRank.F, 0f);
            Assert.Greater(state.Level, 10);
            Assert.Less(state.CurrentXp, ProgressionRules.CombatExpToNext(state.Level));
        }

        [Test]
        public void CombatXp_DumpsStoredOnAscend()
        {
            var state = new CombatXpState
            {
                Level = 10,
                StoredXp = 12480f,
                ExpToNext = ProgressionRules.CombatExpToNext(10)
            };
            state = ProgressionRules.DumpStoredCombatXp(state, EnergyRank.E);
            Assert.Greater(state.Level, 10);
            Assert.LessOrEqual(state.Level, 20);
            Assert.Greater(state.LevelsGained, 0);
        }

        [Test]
        public void CommanderXp_AppliesOnceNotPerCard()
        {
            var once = ProgressionRules.ApplyCommanderXp(
                new CommanderXpState { Level = 1, ExpToNext = ProgressionRules.CommanderExpToNext(1) },
                ProgressionCatalog.CommanderBattleXp);
            var perCard = ProgressionRules.ApplyCommanderXp(
                new CommanderXpState { Level = 1, ExpToNext = ProgressionRules.CommanderExpToNext(1) },
                ProgressionCatalog.CommanderBattleXp * 5f);
            Assert.AreEqual(ProgressionCatalog.CommanderBattleXp, once.CurrentXp, 0.01f);
            Assert.Greater(perCard.CurrentXp + perCard.LevelsGained, once.CurrentXp + once.LevelsGained);
        }

        [Test]
        public void JobShare_SplitsLeaderAssistantOther()
        {
            Assert.AreEqual(1f, ProgressionRules.JobShare(0, false, false));
            Assert.AreEqual(0.6f, ProgressionRules.JobShare(1, false, false));
            Assert.AreEqual(0.35f, ProgressionRules.JobShare(2, false, false));
            Assert.AreEqual(1f, ProgressionRules.JobShare(0, true, false));
            Assert.AreEqual(0.8f, ProgressionRules.JobShare(0, true, true));
        }

        [Test]
        public void MatchCurve_FollowsDesignBands()
        {
            Assert.AreEqual(1.2f, ProgressionRules.MatchMultiplier(5, 8));
            Assert.AreEqual(1.0f, ProgressionRules.MatchMultiplier(10, 10));
            Assert.AreEqual(0.7f, ProgressionRules.MatchMultiplier(12, 10));
            Assert.AreEqual(0.3f, ProgressionRules.MatchMultiplier(18, 10));
            Assert.AreEqual(0.05f, ProgressionRules.MatchMultiplier(25, 10));
        }

        [Test]
        public void TeamPower_LeaderPlusTopTwoAssistantsAndFacility()
        {
            var power = ProgressionRules.TeamPower(new[] { 12, 6, 3 }, 4);
            Assert.AreEqual(17.8f, power, 0.01f);
        }

        [Test]
        public void CatchUp_UsesHighestAndCommanderBand()
        {
            Assert.AreEqual(14, ProgressionRules.CatchUpCombatLevel(20, 5));
            Assert.AreEqual(20, ProgressionRules.CatchUpCombatLevel(5, 25));
            Assert.AreEqual(1, ProgressionRules.CatchUpCombatLevel(1, 1));
        }

        [Test]
        public void LeaderGate_WorkshopCorrectionClosesSmallGaps()
        {
            Assert.IsFalse(ProgressionRules.LeaderMeetsGate(8, 10, 0));
            Assert.IsTrue(ProgressionRules.LeaderMeetsGate(8, 10, 2));
            Assert.AreEqual(2, ProgressionRules.WorkshopThresholdCorrection(4));
            Assert.AreEqual(1, ProgressionRules.WorkshopThresholdCorrection(2));
        }

        [Test]
        public void AdjustedCycle_FasterWithTeamPower()
        {
            var baseCycle = 30f;
            var adjusted = ProgressionRules.AdjustedCycleSeconds(30, 20f);
            Assert.Less(adjusted, baseCycle);
            Assert.Greater(adjusted, baseCycle * 0.25f);
        }

        [Test]
        public void CommanderOfflineBonus_StepsAtFiveTenFifteenTwenty()
        {
            Assert.AreEqual(0, ProgressionRules.CommanderOfflineBonusSeconds(1));
            Assert.AreEqual(1800, ProgressionRules.CommanderOfflineBonusSeconds(5));
            Assert.AreEqual(3600, ProgressionRules.CommanderOfflineBonusSeconds(10));
            Assert.AreEqual(7200, ProgressionRules.CommanderOfflineBonusSeconds(15));
            Assert.AreEqual(10800, ProgressionRules.CommanderOfflineBonusSeconds(20));
        }
    }
}
