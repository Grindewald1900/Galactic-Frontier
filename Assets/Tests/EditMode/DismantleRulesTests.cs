using Assets.Resources.Scripts.Economy.Domain;
using NUnit.Framework;

namespace GalacticFrontier.Tests.EditMode
{
    public class DismantleRulesTests
    {
        [Test]
        public void Normalize_Clamps_None_And_Overshoot()
        {
            Assert.AreEqual(1, DismantleRules.NormalizeTier(0));
            Assert.AreEqual(7, DismantleRules.NormalizeTier(99));
            Assert.AreEqual(5, DismantleRules.NormalizeTier(5));
        }

        [Test]
        public void Credits_Scale_With_Tier()
        {
            Assert.Greater(DismantleRules.CreditsForTier(7), DismantleRules.CreditsForTier(1));
            Assert.AreEqual(8, DismantleRules.CreditsForTier(1));
        }

        [Test]
        public void High_Tier_Grants_Recruit_Tickets()
        {
            var low = DismantleRules.ItemGrantsForTier(1);
            var high = DismantleRules.ItemGrantsForTier(5);
            Assert.IsFalse(Contains(low, GachaRules.TicketDefId));
            Assert.IsTrue(Contains(high, GachaRules.TicketDefId));
            Assert.IsTrue(Contains(low, EconomyConstants.ScrapDefId));
        }

        private static bool Contains(System.Collections.Generic.IReadOnlyList<DismantleGrant> grants, string defId)
        {
            foreach (var grant in grants)
            {
                if (grant != null && grant.ItemDefId == defId)
                    return true;
            }

            return false;
        }
    }
}
