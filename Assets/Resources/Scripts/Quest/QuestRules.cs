using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Inventory;
using Assets.Resources.Scripts.Quest.Domain;
using Assets.Resources.Scripts.World;
using Assets.Resources.Scripts.World.Domain;

namespace Assets.Resources.Scripts.Quest
{
    /// <summary>
    /// Acceptance checks. Linear chains only offer the next step after
    /// <see cref="PrerequisiteSatisfied"/>. Which tracks may run together, and the
    /// global expedition cap, live in <see cref="QuestPacingRules"/>.
    /// </summary>
    public static class QuestRules
    {
        public static bool IsClaimed(QuestState state, string questId) =>
            state?.claimedIds != null && !string.IsNullOrEmpty(questId) && state.claimedIds.Contains(questId);

        public static bool DependsMet(QuestState state, QuestDef def)
        {
            if (def?.dependsOn == null || def.dependsOn.Length == 0) return true;
            foreach (var id in def.dependsOn)
            {
                if (!PrerequisiteSatisfied(state, id))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Accepts the next quest once the prerequisite is done. Claiming its reward is separate.
        /// </summary>
        public static bool PrerequisiteSatisfied(QuestState state, string questId)
        {
            if (IsClaimed(state, questId)) return true;
            var prior = QuestCatalog.Get(questId);
            return prior != null && IsCompleteWhen(state, prior, null);
        }

        public static bool IsCompleteWhen(QuestState state, QuestDef def, IList<CardEntity> cards)
        {
            if (state == null || def == null) return false;
            var f = state.flags ?? new QuestFlags();
            switch (def.type)
            {
                case QuestType.Onboarding:
                    return def.questId switch
                    {
                        QuestIds.ObFormation => f.formationReady || HasFormationMember(),
                        QuestIds.ObFirstBattle => f.firstBattleWon,
                        // Starter seed already holds every gather output; only a real gather action counts.
                        QuestIds.ObGather => f.gatherStartedOrLooted,
                        QuestIds.ObCraft => f.craftedOnce,
                        QuestIds.ObNpcShop => f.shopTradedOnce,
                        _ => QuestFlagRules.IsOnboardingFlagMet(f, def.questId)
                    };
                case QuestType.Story:
                    return def.questId switch
                    {
                        QuestIds.ChPrologue => f.prologueWon,
                        QuestIds.ChRecruitCole => f.emergencyRecruitDone || Owns(cards, CharacterName.Cole),
                        QuestIds.ChFormation => f.chapterFormationReady || f.coleFrontAdjusted,
                        QuestIds.ChOuterCleanup => f.outerCleanupWon || BeaconRestored(WorldConstants.OuterBeltId) || StageAtLeast(WorldConstants.OuterBeltId, 1),
                        QuestIds.ChScanSignal => f.miningSignalScanned || BodyAtLeast(QuestIds.BodyMining, GridNodeState.Fogged),
                        QuestIds.ChMiningSpur => f.miningSpurWon || StageAtLeast(WorldConstants.MiningSpurId, 1),
                        QuestIds.ChIdleGather => f.gatherCycleSettled,
                        QuestIds.ChHullLeak => f.craftedOnce && (f.heldRepairKit || HasItem(EconomyConstants.RepairKitDefId)),
                        QuestIds.ChTwoRoutes => f.routeChoiceMade && BothRoutesFogged(),
                        QuestIds.ChExpeditionPrep => f.pendingLootClaimed && (HasTwoUnlockedDecks() || ShipMeetsRegion(WorldConstants.QuantumRiftId)),
                        QuestIds.ChOpenTheLock => f.frontierBossWon || BeaconRestored(WorldConstants.FrontierBossId),
                        _ => QuestFlagRules.IsStoryFlagMet(f, def.questId)
                    };
                case QuestType.Expedition:
                    return ExpeditionComplete(def);
                case QuestType.Ship:
                    return ShipMeetsRegion(def.regionId);
                case QuestType.Loop:
                    return QuestFlagRules.LoopReady(LoopCount(state, def.questId), def.loopTarget);
                default:
                    return false;
            }
        }

        public static bool IsTriggered(QuestState state, QuestDef def)
        {
            if (state == null || def == null) return false;
            if (!DependsMet(state, def)) return false;
            switch (def.type)
            {
                case QuestType.Onboarding:
                    return !state.onboardingComplete;
                case QuestType.Story:
                    if (state.chapterComplete || state.chapterSkipped) return false;
                    if (def.questId == QuestIds.ChIdleGather)
                        return StageAtLeast(WorldConstants.OuterBeltId, 2) || StageAtLeast(WorldConstants.MiningSpurId, 2);
                    if (def.questId == QuestIds.ChOpenTheLock)
                        return BeaconRestored(WorldConstants.ConvoyLaneId) || IsClaimed(state, QuestIds.ExConvoyR3);
                    return true;
                case QuestType.Expedition:
                    return CanChallenge(def.regionId) || StageAtLeast(def.regionId, 1) || BeaconRestored(def.regionId);
                case QuestType.Ship:
                    return BodyOnMap(def.bodyId);
                case QuestType.Loop:
                    if (def.loopKind == QuestLoopKind.Shop) return IsClaimed(state, QuestIds.ObNpcShop);
                    if (def.loopKind == QuestLoopKind.Craft) return IsClaimed(state, QuestIds.ObCraft);
                    if (def.loopKind == QuestLoopKind.Gather)
                        return IsClaimed(state, RingQuest(def.regionId, 1));
                    if (def.loopKind == QuestLoopKind.Farm)
                        return IsClaimed(state, RingQuest(def.regionId, 2));
                    return false;
                default:
                    return false;
            }
        }

        public static int LoopCount(QuestState state, string questId)
        {
            if (state?.loops == null || string.IsNullOrEmpty(questId)) return 0;
            foreach (var row in state.loops)
            {
                if (row != null && row.questId == questId)
                    return row.count;
            }

            return 0;
        }

        public static void AddLoopCount(QuestState state, string questId, int delta)
        {
            if (state == null || string.IsNullOrEmpty(questId) || delta <= 0) return;
            state.loops ??= new List<QuestLoopProgress>();
            foreach (var row in state.loops)
            {
                if (row != null && row.questId == questId)
                {
                    row.count += delta;
                    return;
                }
            }

            state.loops.Add(new QuestLoopProgress { questId = questId, count = delta });
        }

        public static void ResetLoopCount(QuestState state, string questId)
        {
            if (state?.loops == null) return;
            foreach (var row in state.loops)
            {
                if (row != null && row.questId == questId)
                    row.count = 0;
            }
        }

        public static bool ExpeditionComplete(QuestDef def)
        {
            if (def == null || string.IsNullOrEmpty(def.regionId)) return false;
            WorldService.EnsureReady();
            var rt = WorldRules.FindRuntime(WorldService.State, def.regionId);
            var campaign = NodeCampaignCatalog.Get(def.regionId);
            int stage = rt?.highestClearedStage ?? 0;
            int gather = NodeCampaignCatalog.GatherIdleMinStage(campaign);
            int combat = NodeCampaignCatalog.CombatIdleMinStage(campaign);
            int count = campaign?.StageCount ?? 0;
            bool beacon = BeaconRestored(def.regionId);
            return QuestFlagRules.ExpeditionRingComplete(def.ring, stage, gather, combat, count, beacon);
        }

        public static bool HasFormationMember()
        {
            if (!DeckService.IsLoaded) return false;
            foreach (var deck in DeckService.GetDecks())
            {
                if (deck != null && deck.unlocked && deck.MemberCount >= 1)
                    return true;
            }

            return false;
        }

        public static bool HasTwoUnlockedDecks()
        {
            if (!DeckService.IsLoaded) return false;
            int n = 0;
            foreach (var deck in DeckService.GetDecks())
            {
                if (deck != null && deck.unlocked)
                    n++;
            }

            return n >= 2;
        }

        /// <summary>Front row is slot 0–1. Used by the formation lesson; quest completion also requires the post-lesson flag.</summary>
        public static bool ColeInFrontRow(IList<CardEntity> cards)
        {
            if (!DeckService.IsLoaded) return false;
            var deck = DeckService.GetActiveCombatDeck();
            if (deck?.slotCardIds == null) return false;
            for (int i = 0; i < deck.slotCardIds.Length; i++)
            {
                if (i >= 2) break;
                var id = deck.slotCardIds[i];
                if (string.IsNullOrEmpty(id)) continue;
                var entity = FindCard(cards, id);
                if (entity != null && entity.characterName == CharacterName.Cole)
                    return true;
            }

            return false;
        }

        public static bool MeetsFormationLayout(IList<CardEntity> cards) => ColeInFrontRow(cards);

        public static bool HasItem(string defId)
        {
            if (string.IsNullOrEmpty(defId)) return false;
            var items = ProductionService.GetLocalItems();
            if (items == null) return false;
            return InventoryRules.CountOf(ItemFactory.ToStacks(items), defId, 1) > 0;
        }

        public static bool StageAtLeast(string regionId, int stage)
        {
            if (string.IsNullOrEmpty(regionId) || stage <= 0) return false;
            WorldService.EnsureReady();
            var rt = WorldRules.FindRuntime(WorldService.State, regionId);
            return (rt?.highestClearedStage ?? 0) >= stage;
        }

        public static bool BeaconRestored(string regionId)
        {
            if (string.IsNullOrEmpty(regionId)) return false;
            WorldService.EnsureReady();
            return WorldRules.IsRegionBeaconRestored(WorldService.State, regionId);
        }

        public static bool CanChallenge(string regionId)
        {
            var cfg = RegionCatalog.Get(regionId);
            if (cfg == null) return false;
            WorldService.EnsureReady();
            return WorldRules.CanChallengeRegion(WorldService.State, cfg);
        }

        public static bool BodyAtLeast(string bodyId, GridNodeState min)
        {
            if (string.IsNullOrEmpty(bodyId)) return false;
            GridService.EnsureReady();
            return GridService.GetState(bodyId) >= min;
        }

        public static bool BothRoutesFogged() =>
            BodyAtLeast(QuestIds.BodyConvoy, GridNodeState.Fogged)
            && BodyAtLeast(QuestIds.BodyRift, GridNodeState.Fogged);

        public static bool BodyOnMap(string bodyId)
        {
            if (string.IsNullOrEmpty(bodyId)) return false;
            WorldService.EnsureReady();
            return GridVisibility.IsCombatBodyOnMap(WorldService.State, bodyId);
        }

        public static bool ShipMeetsRegion(string regionId)
        {
            var cfg = RegionCatalog.Get(regionId);
            if (cfg == null) return false;
            ShipService.EnsureReady();
            return ShipService.MeetsGate(cfg.shipGate).Ok;
        }

        public static bool ShipGateFailing(string regionId) =>
            !ShipMeetsRegion(regionId);

        private static bool Owns(IList<CardEntity> cards, CharacterName name)
        {
            if (cards == null) return false;
            foreach (var c in cards)
            {
                if (c != null && c.characterName == name)
                    return true;
            }

            return false;
        }

        private static CardEntity FindCard(IList<CardEntity> cards, string cardId)
        {
            if (cards == null || string.IsNullOrEmpty(cardId)) return null;
            foreach (var c in cards)
            {
                if (c != null && c.id == cardId)
                    return c;
            }

            return null;
        }

        private static string RingQuest(string regionId, int ring)
        {
            if (regionId == WorldConstants.OuterBeltId) return ring == 1 ? QuestIds.ExOuterR1 : QuestIds.ExOuterR2;
            if (regionId == WorldConstants.MiningSpurId) return ring == 1 ? QuestIds.ExMiningR1 : QuestIds.ExMiningR2;
            if (regionId == WorldConstants.QuantumRiftId) return ring == 1 ? QuestIds.ExRiftR1 : QuestIds.ExRiftR2;
            if (regionId == WorldConstants.AbyssalEdgeId) return ring == 1 ? QuestIds.ExAbyssR1 : QuestIds.ExAbyssR2;
            if (regionId == WorldConstants.ConvoyLaneId) return ring == 1 ? QuestIds.ExConvoyR1 : QuestIds.ExConvoyR2;
            if (regionId == WorldConstants.FrontierBossId) return ring == 1 ? QuestIds.ExAnchorR1 : QuestIds.ExAnchorR2;
            return "";
        }
    }
}
