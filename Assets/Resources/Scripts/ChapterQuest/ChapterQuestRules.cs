using System.Collections.Generic;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.ChapterQuest.Domain;
using Assets.Resources.Scripts.CharacterPanel;
using Assets.Resources.Scripts.Deck;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.World;
using Assets.Resources.Scripts.World.Domain;

namespace Assets.Resources.Scripts.ChapterQuest
{
    public static class ChapterQuestRules
    {
        public static bool IsConditionMet(ChapterQuestState state, string stepId, IList<CardEntity> cards)
        {
            if (state == null || string.IsNullOrEmpty(stepId))
                return false;
            var f = state.flags ?? new ChapterQuestFlags();
            return stepId switch
            {
                ChapterQuestCatalog.StepPrologue => f.prologueWon,
                ChapterQuestCatalog.StepRecruitCole => f.emergencyRecruitDone || OwnsCharacter(cards, CharacterName.Cole),
                ChapterQuestCatalog.StepFormation => f.formationReady,
                ChapterQuestCatalog.StepOuterCleanup => f.outerCleanupWon || RegionCleared(WorldConstants.OuterBeltId),
                ChapterQuestCatalog.StepScanSignal => f.miningSignalScanned || MiningBodyLocated(),
                ChapterQuestCatalog.StepMiningSpur => f.miningSpurWon || RegionCleared(WorldConstants.MiningSpurId),
                ChapterQuestCatalog.StepIdleGather => f.gatherCycleSettled,
                ChapterQuestCatalog.StepHullLeak => (f.craftedOnce && f.heldRepairKit),
                ChapterQuestCatalog.StepTwoRoutes => f.routeChoiceMade,
                ChapterQuestCatalog.StepExpeditionPrep => f.pendingLootClaimed,
                ChapterQuestCatalog.StepOpenTheLock => f.frontierBossWon || RegionCleared(WorldConstants.FrontierBossId),
                _ => false
            };
        }

        public static bool MeetsFormationLayout(IList<CardEntity> cards)
        {
            if (!DeckService.IsLoaded) return false;
            var deck = DeckService.GetActiveCombatDeck();
            if (deck?.slotCardIds == null) return false;
            for (int i = 0; i < deck.slotCardIds.Length && i < 2; i++)
            {
                var id = deck.slotCardIds[i];
                if (string.IsNullOrEmpty(id)) continue;
                var entity = FindCard(cards, id);
                if (entity != null && entity.characterName == CharacterName.Cole)
                    return true;
            }

            return false;
        }

        public static bool OwnsCharacter(IList<CardEntity> cards, CharacterName name)
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

        private static bool RegionCleared(string regionId)
        {
            if (string.IsNullOrEmpty(regionId)) return false;
            WorldService.EnsureReady();
            var rt = WorldRules.FindRuntime(WorldService.State, regionId);
            return rt != null && (rt.cleared || rt.bossDefeated);
        }

        private static bool MiningBodyLocated()
        {
            GridService.EnsureReady();
            return GridService.GetState("body_mining_spur") >= GridNodeState.Located;
        }
    }
}
