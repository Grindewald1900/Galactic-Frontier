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
                ChapterQuestCatalog.StepFormation => f.formationReady || MeetsFormationLayout(cards),
                ChapterQuestCatalog.StepOuterCleanup => f.outerCleanupWon || RegionCleared(WorldConstants.OuterBeltId),
                ChapterQuestCatalog.StepScanSignal => f.miningSignalScanned || MiningBodyLocated(),
                ChapterQuestCatalog.StepMiningSpur => f.miningSpurWon || RegionCleared(WorldConstants.MiningSpurId),
                _ => false
            };
        }

        public static bool MeetsFormationLayout(IList<CardEntity> cards)
        {
            if (!DeckService.IsLoaded) return false;
            var deck = DeckService.GetActiveCombatDeck();
            if (deck?.slotCardIds == null) return false;

            bool asraFront = false;
            bool coleBack = false;
            bool strategyChanged = !string.IsNullOrEmpty(deck.combatStrategyId)
                && deck.combatStrategyId != "Balanced";

            for (int i = 0; i < deck.slotCardIds.Length; i++)
            {
                var id = deck.slotCardIds[i];
                if (string.IsNullOrEmpty(id)) continue;
                var entity = FindCard(cards, id);
                if (entity == null) continue;
                if (entity.characterName == CharacterName.Asra && i < 2)
                    asraFront = true;
                if (entity.characterName == CharacterName.Cole && i >= 2)
                    coleBack = true;
            }

            return asraFront && coleBack && strategyChanged;
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
