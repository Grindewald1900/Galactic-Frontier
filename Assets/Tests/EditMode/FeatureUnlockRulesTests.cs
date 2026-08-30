using Assets.Resources.Scripts.Unlock.Domain;
using NUnit.Framework;

namespace GalacticFrontier.Tests.EditMode
{
    public class FeatureUnlockRulesTests
    {
        [Test]
        public void StartsUnlocked_IsMet_WithoutContext()
        {
            var def = new FeatureUnlockDef { featureId = "a", startsUnlocked = true };
            Assert.IsTrue(FeatureUnlockRules.IsConditionMet(def, null));
        }

        [Test]
        public void RequiredStep_NeedsClaim()
        {
            var def = new FeatureUnlockDef
            {
                featureId = "explore",
                requiredStepId = "ob_formation"
            };
            var ctx = new FeatureUnlockContext();
            Assert.IsFalse(FeatureUnlockRules.IsConditionMet(def, ctx));
            ctx.ClaimedStepIds.Add("ob_formation");
            Assert.IsTrue(FeatureUnlockRules.IsConditionMet(def, ctx));
        }

        [Test]
        public void ChainCompleted_UnlocksAllGated()
        {
            var def = new FeatureUnlockDef { featureId = "market", requiredStepId = "ob_craft" };
            var ctx = new FeatureUnlockContext { ChainCompleted = true };
            Assert.IsTrue(FeatureUnlockRules.IsConditionMet(def, ctx));
        }

        [Test]
        public void Seed_MarksAnnounced_WithoutPending()
        {
            var state = new FeatureUnlockState();
            var catalog = new System.Collections.Generic.List<FeatureUnlockDef>
            {
                new FeatureUnlockDef { featureId = "bridge", startsUnlocked = true }
            };
            var newly = FeatureUnlockRules.ApplyNewlyMet(state, catalog, new FeatureUnlockContext(), seed: true);
            Assert.AreEqual(0, newly.Count);
            Assert.IsTrue(state.seeded);
            Assert.IsTrue(FeatureUnlockRules.ContainsId(state.unlockedFeatureIds, "bridge"));
            Assert.AreEqual(0, FeatureUnlockRules.PendingAnnouncements(state).Count);
        }
    }
}
