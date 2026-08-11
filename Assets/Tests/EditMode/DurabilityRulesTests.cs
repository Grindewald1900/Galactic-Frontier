using Assets.Resources.Scripts.Economy.Domain;
using NUnit.Framework;

namespace GalacticFrontier.Tests.EditMode
{
    public class DurabilityRulesTests
    {
        [Test]
        public void CombatWear_NeverDestroysBelowZero()
        {
            Assert.AreEqual(0, DurabilityRules.ApplyCombatWear(1, 100, 3));
            Assert.AreEqual(97, DurabilityRules.ApplyCombatWear(100, 100, 3));
        }

        [Test]
        public void GatherWear_ScalesWithRisk()
        {
            Assert.AreEqual(99, DurabilityRules.ApplyGatherWear(100, 100, 1));
            Assert.AreEqual(98, DurabilityRules.ApplyGatherWear(100, 100, 2));
            Assert.AreEqual(97, DurabilityRules.ApplyGatherWear(100, 100, 3));
        }

        [Test]
        public void TryRepair_SpendsKits()
        {
            var current = 10;
            var kits = 5;
            Assert.IsTrue(DurabilityRules.TryRepair(ref current, 100, ref kits));
            Assert.AreEqual(100, current);
            Assert.Less(kits, 5);
        }

        [Test]
        public void OfflineYield_StartsNearHalf()
        {
            var ratio = OfflineRules.YieldRatio(60, EconomyConstants.OfflineCapBaseSeconds);
            Assert.GreaterOrEqual(ratio, 0.49f);
            Assert.LessOrEqual(ratio, 0.55f);
        }
    }
}
