using System.Collections.Generic;
using Assets.Resources.Scripts.Quest.Domain;
using Assets.Resources.Scripts.World.Domain;
using NUnit.Framework;

namespace GalacticFrontier.Tests.EditMode
{
    public class QuestCatalogTests
    {
        [Test]
        public void Catalog_HasFiftyOneDefs()
        {
            Assert.AreEqual(51, QuestCatalog.All.Count);
        }

        [Test]
        public void Catalog_CountsByType()
        {
            Assert.AreEqual(5, Count(QuestType.Onboarding));
            Assert.AreEqual(11, Count(QuestType.Story));
            Assert.AreEqual(18, Count(QuestType.Expedition));
            Assert.AreEqual(4, Count(QuestType.Ship));
            Assert.AreEqual(13, Count(QuestType.Loop));
        }

        [Test]
        public void Story_OpenTheLock_DependsOnConvoyBeaconRing()
        {
            var def = QuestCatalog.Get(QuestIds.ChOpenTheLock);
            Assert.NotNull(def);
            CollectionAssert.Contains(def.dependsOn, QuestIds.ChExpeditionPrep);
            CollectionAssert.Contains(def.dependsOn, QuestIds.ExConvoyR3);
        }

        [Test]
        public void IdleGather_LinksOnboardingGather()
        {
            var def = QuestCatalog.Get(QuestIds.ChIdleGather);
            Assert.AreEqual(QuestIds.ObGather, def.linkedOnboardingStepId);
        }

        [Test]
        public void HullLeak_LinksOnboardingCraft()
        {
            var def = QuestCatalog.Get(QuestIds.ChHullLeak);
            Assert.AreEqual(QuestIds.ObCraft, def.linkedOnboardingStepId);
        }

        [Test]
        public void ExpeditionRing_Complete_UsesStageAndBeacon()
        {
            Assert.IsFalse(QuestFlagRules.ExpeditionRingComplete(1, 1, 2, 3, 6, false));
            Assert.IsTrue(QuestFlagRules.ExpeditionRingComplete(1, 2, 2, 3, 6, false));
            Assert.IsFalse(QuestFlagRules.ExpeditionRingComplete(2, 2, 2, 3, 6, false));
            Assert.IsTrue(QuestFlagRules.ExpeditionRingComplete(2, 3, 2, 3, 6, false));
            Assert.IsFalse(QuestFlagRules.ExpeditionRingComplete(3, 5, 2, 3, 6, false));
            Assert.IsTrue(QuestFlagRules.ExpeditionRingComplete(3, 6, 2, 3, 6, false));
            Assert.IsTrue(QuestFlagRules.ExpeditionRingComplete(3, 4, 2, 3, 6, true));
        }

        [Test]
        public void LoopReady_RequiresTargetCount()
        {
            Assert.IsFalse(QuestFlagRules.LoopReady(2, 3));
            Assert.IsTrue(QuestFlagRules.LoopReady(3, 3));
        }

        [Test]
        public void FlagRules_StoryWaveB()
        {
            var flags = new QuestFlags();
            Assert.IsFalse(QuestFlagRules.IsStoryFlagMet(flags, QuestIds.ChIdleGather));
            flags.gatherCycleSettled = true;
            Assert.IsTrue(QuestFlagRules.IsStoryFlagMet(flags, QuestIds.ChIdleGather));
            flags.craftedOnce = true;
            flags.heldRepairKit = true;
            Assert.IsTrue(QuestFlagRules.IsStoryFlagMet(flags, QuestIds.ChHullLeak));
            flags.routeChoiceMade = true;
            Assert.IsTrue(QuestFlagRules.IsStoryFlagMet(flags, QuestIds.ChTwoRoutes));
            flags.pendingLootClaimed = true;
            Assert.IsTrue(QuestFlagRules.IsStoryFlagMet(flags, QuestIds.ChExpeditionPrep));
            flags.frontierBossWon = true;
            Assert.IsTrue(QuestFlagRules.IsStoryFlagMet(flags, QuestIds.ChOpenTheLock));
        }

        [Test]
        public void FormationFlag_NeedsPlayerAdjustmentOrLegacyReady()
        {
            var flags = new QuestFlags();
            Assert.IsFalse(QuestFlagRules.IsStoryFlagMet(flags, QuestIds.ChFormation));
            flags.formationLessonSeen = true;
            Assert.IsFalse(QuestFlagRules.IsStoryFlagMet(flags, QuestIds.ChFormation));
            flags.coleFrontAdjusted = true;
            Assert.IsTrue(QuestFlagRules.IsStoryFlagMet(flags, QuestIds.ChFormation));
            flags.coleFrontAdjusted = false;
            flags.chapterFormationReady = true;
            Assert.IsTrue(QuestFlagRules.IsStoryFlagMet(flags, QuestIds.ChFormation));
        }

        [Test]
        public void Pacing_StoryAndOnboardingAreLinear_OtherTracksRunBesideThem()
        {
            Assert.IsTrue(QuestPacingRules.IsLinearChain(QuestType.Story));
            Assert.IsTrue(QuestPacingRules.IsLinearChain(QuestType.Onboarding));
            Assert.IsFalse(QuestPacingRules.IsLinearChain(QuestType.Expedition));
            Assert.IsFalse(QuestPacingRules.IsLinearChain(QuestType.Ship));
            Assert.IsFalse(QuestPacingRules.IsLinearChain(QuestType.Loop));

            Assert.IsTrue(QuestPacingRules.MayRunBeside(QuestType.Story, QuestType.Onboarding));
            Assert.IsTrue(QuestPacingRules.MayRunBeside(QuestType.Story, QuestType.Expedition));
            Assert.IsTrue(QuestPacingRules.MayRunBeside(QuestType.Expedition, QuestType.Loop));
            Assert.IsFalse(QuestPacingRules.MayRunBeside(QuestType.Story, QuestType.Story));
            Assert.IsTrue(QuestPacingRules.MayRunBeside(QuestType.Loop, QuestType.Loop));
            Assert.AreEqual(2, QuestPacingRules.MaxActiveLoops);
            Assert.AreEqual(1, QuestPacingRules.MaxActiveExpeditions);
        }

        [Test]
        public void Pacing_UnfinishedLinearSuccessorStaysOffTheBoard()
        {
            Assert.IsTrue(QuestPacingRules.StaysOffBoard(QuestType.Story, false, false));
            Assert.IsFalse(QuestPacingRules.StaysOffBoard(QuestType.Story, true, false));
            Assert.IsFalse(QuestPacingRules.StaysOffBoard(QuestType.Story, false, true));
            Assert.IsTrue(QuestPacingRules.StaysOffBoard(QuestType.Onboarding, false, false));
        }

        [Test]
        public void StoryChain_EachStepDependsOnThePrevious()
        {
            Assert.IsTrue(DependsOn(QuestIds.ChRecruitCole, QuestIds.ChPrologue));
            Assert.IsTrue(DependsOn(QuestIds.ChFormation, QuestIds.ChRecruitCole));
            Assert.IsTrue(DependsOn(QuestIds.ChOuterCleanup, QuestIds.ChFormation));
            Assert.IsFalse(DependsOn(QuestIds.ChOuterCleanup, QuestIds.ChPrologue));
        }

        [Test]
        public void Expedition_SameNodeRingsAreLinear_OnlyOneRingIsCurrent()
        {
            Assert.IsTrue(DependsOn(QuestIds.ExOuterR2, QuestIds.ExOuterR1));
            Assert.IsTrue(DependsOn(QuestIds.ExOuterR3, QuestIds.ExOuterR2));
            Assert.IsFalse(DependsOn(QuestIds.ExOuterR2, QuestIds.ExMiningR1));

            var outer = QuestCatalog.Get(QuestIds.ExOuterR1);
            var mining = QuestCatalog.Get(QuestIds.ExMiningR1);
            Assert.AreNotEqual(outer.regionId, mining.regionId);

            var offers = new List<ExpeditionOffer>
            {
                Offer(outer, triggered: true),
                Offer(mining, triggered: true)
            };
            Assert.AreEqual(
                QuestIds.ExOuterR1,
                QuestPacingRules.SelectCurrentExpedition(offers, "", WorldConstants.ConvoyLaneId, WorldConstants.QuantumRiftId));
        }

        [Test]
        public void Expedition_RoutePreferenceStillSelectsASingleRing()
        {
            var offers = new List<ExpeditionOffer>
            {
                new ExpeditionOffer { QuestId = QuestIds.ExOuterR1, RegionId = WorldConstants.OuterBeltId, Triggered = true },
                new ExpeditionOffer { QuestId = QuestIds.ExRiftR1, RegionId = WorldConstants.QuantumRiftId, Triggered = true }
            };
            Assert.AreEqual(
                QuestIds.ExRiftR1,
                QuestPacingRules.SelectCurrentExpedition(offers, "rift", WorldConstants.ConvoyLaneId, WorldConstants.QuantumRiftId));
        }

        [Test]
        public void RegionForNode_MatchesWorldConstants()
        {
            Assert.AreEqual(WorldConstants.OuterBeltId, QuestIds.RegionForNode("outer"));
            Assert.AreEqual(WorldConstants.FrontierBossId, QuestIds.RegionForNode("anchor"));
        }

        private static bool DependsOn(string questId, string prerequisite)
        {
            var def = QuestCatalog.Get(questId);
            return def?.dependsOn != null && System.Array.IndexOf(def.dependsOn, prerequisite) >= 0;
        }

        private static ExpeditionOffer Offer(QuestDef def, bool triggered) =>
            new ExpeditionOffer
            {
                QuestId = def.questId,
                RegionId = def.regionId,
                Triggered = triggered
            };

        private static int Count(QuestType type)
        {
            int n = 0;
            foreach (var def in QuestCatalog.OfType(type))
                n++;
            return n;
        }
    }
}
