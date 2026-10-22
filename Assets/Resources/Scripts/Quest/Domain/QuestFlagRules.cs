namespace Assets.Resources.Scripts.Quest.Domain
{
    /// <summary>Flag-only and numeric checks that EditMode tests can run without Unity services.</summary>
    public static class QuestFlagRules
    {
        public static bool IsOnboardingFlagMet(QuestFlags flags, string questId)
        {
            if (flags == null || string.IsNullOrEmpty(questId)) return false;
            return questId switch
            {
                QuestIds.ObFormation => flags.formationReady,
                QuestIds.ObFirstBattle => flags.firstBattleWon,
                QuestIds.ObGather => flags.gatherStartedOrLooted,
                QuestIds.ObCraft => flags.craftedOnce,
                QuestIds.ObNpcShop => flags.shopTradedOnce,
                _ => false
            };
        }

        public static bool IsStoryFlagMet(QuestFlags flags, string questId)
        {
            if (flags == null || string.IsNullOrEmpty(questId)) return false;
            return questId switch
            {
                QuestIds.ChPrologue => flags.prologueWon,
                QuestIds.ChRecruitCole => flags.emergencyRecruitDone,
                QuestIds.ChFormation => flags.chapterFormationReady || flags.coleFrontAdjusted,
                QuestIds.ChOuterCleanup => flags.outerCleanupWon,
                QuestIds.ChScanSignal => flags.miningSignalScanned,
                QuestIds.ChMiningSpur => flags.miningSpurWon,
                QuestIds.ChIdleGather => flags.gatherCycleSettled,
                QuestIds.ChHullLeak => flags.craftedOnce && flags.heldRepairKit,
                QuestIds.ChTwoRoutes => flags.routeChoiceMade,
                QuestIds.ChExpeditionPrep => flags.pendingLootClaimed,
                QuestIds.ChOpenTheLock => flags.frontierBossWon,
                _ => false
            };
        }

        public static bool ExpeditionRingComplete(int ring, int highestStage, int gatherMin, int combatMin, int stageCount, bool beaconRestored)
        {
            if (ring <= 1) return highestStage >= gatherMin;
            if (ring == 2) return highestStage >= combatMin;
            return beaconRestored || (stageCount > 0 && highestStage >= stageCount);
        }

        public static bool LoopReady(int count, int target) =>
            target > 0 && count >= target;
    }
}
