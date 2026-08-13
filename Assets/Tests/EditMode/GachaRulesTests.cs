using Assets.Resources.Scripts.Economy.Domain;
using NUnit.Framework;

namespace GalacticFrontier.Tests.EditMode
{
    public class GachaRulesTests
    {
        [Test]
        public void TicketsRequired_Scales_With_Pull_Count()
        {
            Assert.AreEqual(0, GachaRules.TicketsRequired(0));
            Assert.AreEqual(1, GachaRules.TicketsRequired(1));
            Assert.AreEqual(10, GachaRules.TicketsRequired(10));
        }

        [Test]
        public void Pity_Forces_After_Threshold()
        {
            Assert.IsFalse(GachaRules.ShouldForceHighTier(GachaRules.PityThreshold - 1));
            Assert.IsTrue(GachaRules.ShouldForceHighTier(GachaRules.PityThreshold));
        }

        [Test]
        public void Pity_Resets_On_High_Tier_Hit()
        {
            Assert.AreEqual(0, GachaRules.AdvancePity(39, true));
            Assert.AreEqual(40, GachaRules.AdvancePity(39, false));
        }
    }
}
