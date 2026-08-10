using System;
using System.Collections.Generic;

namespace Assets.Resources.Scripts.Deck.Domain
{
    public static class DeckStateFactory
    {
        /// <summary>Creates the MVP default: 6 deck slots, first 2 unlocked, parallel=2.</summary>
        public static PlayerDeckState CreateNewPlayerState()
        {
            var state = new PlayerDeckState
            {
                unlockedDeckSlots = DeckConstants.DefaultUnlockedDeckSlots,
                maxParallelActions = DeckConstants.DefaultMaxParallelActions,
                decks = new List<DeckEntity>(DeckConstants.MaxDeckSlots)
            };

            for (var i = 0; i < DeckConstants.MaxDeckSlots; i++)
            {
                var unlocked = i < state.unlockedDeckSlots;
                var deck = new DeckEntity
                {
                    deckId = Guid.NewGuid().ToString("N"),
                    displayName = unlocked
                        ? (i == 0 ? "Combat Deck" : "Deck " + (i + 1))
                        : "Locked " + (i + 1),
                    purpose = i == 0 ? DeckPurpose.Combat : DeckPurpose.Flexible,
                    sortOrder = i,
                    unlocked = unlocked,
                    slotCardIds = DeckEntity.CreateEmptySlots(),
                    action = new DeckActionState()
                };
                state.decks.Add(deck);
            }

            state.activeCombatDeckId = state.decks[0].deckId;
            state.count = state.decks.Count;
            return state;
        }

        /// <summary>
        /// Builds default state and fills combat deck slots from legacy LineupPosition indices.
        /// </summary>
        public static PlayerDeckState CreateFromLegacyLineup(IEnumerable<LegacyLineupEntry> lineup)
        {
            var state = CreateNewPlayerState();
            var combat = state.decks[0];
            if (lineup != null)
            {
                foreach (var entry in lineup)
                {
                    if (entry.SlotIndex < 0 || entry.SlotIndex >= DeckConstants.SlotsPerDeck)
                        continue;
                    if (string.IsNullOrEmpty(entry.CardId))
                        continue;
                    combat.slotCardIds[entry.SlotIndex] = entry.CardId;
                }
            }

            state.activeCombatDeckId = combat.deckId;
            return state;
        }
    }

    public readonly struct LegacyLineupEntry
    {
        public string CardId { get; }
        public int SlotIndex { get; }

        public LegacyLineupEntry(string cardId, int slotIndex)
        {
            CardId = cardId;
            SlotIndex = slotIndex;
        }
    }
}
