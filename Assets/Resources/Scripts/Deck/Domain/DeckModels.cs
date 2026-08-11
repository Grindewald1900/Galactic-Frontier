using System;
using System.Collections.Generic;

namespace Assets.Resources.Scripts.Deck.Domain
{
    /// <summary>One runnable 5-slot formation unit.</summary>
    [Serializable]
    public class DeckEntity
    {
        public string deckId = "";
        public string displayName = "";
        public DeckPurpose purpose = DeckPurpose.Flexible;
        /// <summary>Exactly <see cref="DeckConstants.SlotsPerDeck"/> entries; empty string = vacant.</summary>
        public string[] slotCardIds = CreateEmptySlots();
        public DeckActionState action = new DeckActionState();
        public string combatStrategyId = "";
        public int sortOrder;
        public bool unlocked;

        public static string[] CreateEmptySlots()
        {
            var slots = new string[DeckConstants.SlotsPerDeck];
            for (var i = 0; i < slots.Length; i++)
                slots[i] = "";
            return slots;
        }

        public int MemberCount
        {
            get
            {
                var n = 0;
                if (slotCardIds == null) return 0;
                for (var i = 0; i < slotCardIds.Length; i++)
                {
                    if (!string.IsNullOrEmpty(slotCardIds[i]))
                        n++;
                }

                return n;
            }
        }

        public bool IsActionBusy =>
            action != null &&
            (action.status == DeckActionStatus.Running
             || action.status == DeckActionStatus.PausedCap
             || action.status == DeckActionStatus.PausedBlock);
    }

    [Serializable]
    public class DeckActionState
    {
        public DeckActionStatus status = DeckActionStatus.Idle;
        public DeckActionType actionType = DeckActionType.None;
        public string targetId = "";
        public long startedAtUtc;
        public long lastSettledAtUtc;
        public string progressPayload = "";
    }

    /// <summary>Player-owned deck collection persisted as decks.json.</summary>
    [Serializable]
    public class PlayerDeckState
    {
        public List<DeckEntity> decks = new List<DeckEntity>();
        public int unlockedDeckSlots = DeckConstants.DefaultUnlockedDeckSlots;
        public int maxParallelActions = DeckConstants.DefaultMaxParallelActions;
        public string activeCombatDeckId = "";
        public int count;
    }

    public static class DeckConstants
    {
        public const int SlotsPerDeck = 5;
        public const int MaxDeckSlots = 6;
        public const int DefaultUnlockedDeckSlots = 2;
        public const int DefaultMaxParallelActions = 2;
    }
}
