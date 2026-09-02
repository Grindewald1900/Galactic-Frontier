using System.Collections.Generic;
using Assets.Resources.Scripts.ChapterQuest.Domain;
using NUnit.Framework;

namespace GalacticFrontier.Tests.EditMode
{
    public class ChapterQuestRulesTests
    {
        [Test]
        public void Catalog_HasSixWaveASteps()
        {
            Assert.AreEqual(6, ChapterQuestCatalog.All.Count);
            Assert.AreEqual(ChapterQuestCatalog.StepPrologue, ChapterQuestCatalog.All[0].stepId);
            Assert.AreEqual(ChapterQuestCatalog.StepMiningSpur, ChapterQuestCatalog.All[5].stepId);
        }

        [Test]
        public void NextAfter_ReturnsFollowingStep()
        {
            var next = ChapterQuestCatalog.NextAfter(ChapterQuestCatalog.StepPrologue);
            Assert.NotNull(next);
            Assert.AreEqual(ChapterQuestCatalog.StepRecruitCole, next.stepId);
        }

        [Test]
        public void FlagRules_Prologue_MetWhenFlagSet()
        {
            var state = NewState();
            Assert.IsFalse(ChapterQuestFlagRules.IsStepSatisfiedByFlags(state.flags, ChapterQuestCatalog.StepPrologue));
            state.flags.prologueWon = true;
            Assert.IsTrue(ChapterQuestFlagRules.IsStepSatisfiedByFlags(state.flags, ChapterQuestCatalog.StepPrologue));
        }

        [Test]
        public void FlagRules_Recruit_MetWhenEmergencyDone()
        {
            var state = NewState();
            state.flags.emergencyRecruitDone = true;
            Assert.IsTrue(ChapterQuestFlagRules.IsStepSatisfiedByFlags(state.flags, ChapterQuestCatalog.StepRecruitCole));
        }

        [Test]
        public void FlagRules_Formation_MetWhenFlagSet()
        {
            var state = NewState();
            state.flags.formationReady = true;
            Assert.IsTrue(ChapterQuestFlagRules.IsStepSatisfiedByFlags(state.flags, ChapterQuestCatalog.StepFormation));
        }

        [Test]
        public void FlagRules_OuterCleanup_MetWhenFlagSet()
        {
            var state = NewState();
            state.flags.outerCleanupWon = true;
            Assert.IsTrue(ChapterQuestFlagRules.IsStepSatisfiedByFlags(state.flags, ChapterQuestCatalog.StepOuterCleanup));
        }

        [Test]
        public void FlagRules_ScanSignal_MetWhenFlagSet()
        {
            var state = NewState();
            state.flags.miningSignalScanned = true;
            Assert.IsTrue(ChapterQuestFlagRules.IsStepSatisfiedByFlags(state.flags, ChapterQuestCatalog.StepScanSignal));
        }

        [Test]
        public void FlagRules_MiningSpur_MetWhenFlagSet()
        {
            var state = NewState();
            state.flags.miningSpurWon = true;
            Assert.IsTrue(ChapterQuestFlagRules.IsStepSatisfiedByFlags(state.flags, ChapterQuestCatalog.StepMiningSpur));
        }

        [Test]
        public void FormationStep_LinksOnboardingFormation()
        {
            var def = ChapterQuestCatalog.Get(ChapterQuestCatalog.StepFormation);
            Assert.AreEqual("ob_formation", def.linkedOnboardingStepId);
        }

        [Test]
        public void OuterCleanupStep_LinksOnboardingFirstBattle()
        {
            var def = ChapterQuestCatalog.Get(ChapterQuestCatalog.StepOuterCleanup);
            Assert.AreEqual("ob_first_battle", def.linkedOnboardingStepId);
        }

        private static ChapterQuestState NewState() =>
            new ChapterQuestState
            {
                activeStepId = ChapterQuestCatalog.StepPrologue,
                flags = new ChapterQuestFlags(),
                completedStepIds = new List<string>(),
                claimedStepIds = new List<string>()
            };
    }
}
