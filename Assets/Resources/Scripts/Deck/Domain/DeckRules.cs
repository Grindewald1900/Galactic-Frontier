using System.Collections.Generic;

namespace Assets.Resources.Scripts.Deck.Domain
{
    /// <summary>Pure deck membership and occupation rules (systems/01 §6).</summary>
    public static class DeckRules
    {
        public static DeckEntity FindDeck(PlayerDeckState state, string deckId)
        {
            if (state?.decks == null || string.IsNullOrEmpty(deckId))
                return null;
            foreach (var deck in state.decks)
            {
                if (deck != null && deck.deckId == deckId)
                    return deck;
            }

            return null;
        }

        public static DeckCommandResult TryAssignSlot(
            PlayerDeckState state,
            string deckId,
            int slotIndex,
            string cardId)
        {
            var deck = FindDeck(state, deckId);
            if (deck == null)
                return DeckCommandResult.Fail(DeckCommandError.DeckNotFound, "Deck not found.");
            if (!deck.unlocked)
                return DeckCommandResult.Fail(DeckCommandError.DeckLocked, "Deck slot is locked.");
            if (deck.IsActionBusy)
                return DeckCommandResult.Fail(DeckCommandError.DeckBusy, "Stop the action before editing the deck.");
            if (slotIndex < 0 || slotIndex >= DeckConstants.SlotsPerDeck)
                return DeckCommandResult.Fail(DeckCommandError.InvalidSlot, "Invalid slot index.");
            if (deck.slotCardIds == null || deck.slotCardIds.Length != DeckConstants.SlotsPerDeck)
                deck.slotCardIds = DeckEntity.CreateEmptySlots();

            if (!string.IsNullOrEmpty(cardId))
            {
                for (var i = 0; i < deck.slotCardIds.Length; i++)
                {
                    if (i != slotIndex && deck.slotCardIds[i] == cardId)
                        return DeckCommandResult.Fail(DeckCommandError.DuplicateInDeck, "Card already in this deck.");
                }
            }

            deck.slotCardIds[slotIndex] = cardId ?? "";
            return DeckCommandResult.Ok();
        }

        public static DeckCommandResult TryClearSlot(PlayerDeckState state, string deckId, int slotIndex) =>
            TryAssignSlot(state, deckId, slotIndex, "");

        public static DeckCommandResult TryRename(PlayerDeckState state, string deckId, string displayName)
        {
            var deck = FindDeck(state, deckId);
            if (deck == null)
                return DeckCommandResult.Fail(DeckCommandError.DeckNotFound, "Deck not found.");
            if (!deck.unlocked)
                return DeckCommandResult.Fail(DeckCommandError.DeckLocked, "Deck slot is locked.");
            if (deck.IsActionBusy)
                return DeckCommandResult.Fail(DeckCommandError.DeckBusy, "Stop the action before renaming.");
            if (string.IsNullOrWhiteSpace(displayName))
                return DeckCommandResult.Fail(DeckCommandError.InvalidSlot, "Name is required.");

            deck.displayName = displayName.Trim();
            return DeckCommandResult.Ok();
        }

        public static DeckCommandResult TrySetPurpose(PlayerDeckState state, string deckId, DeckPurpose purpose)
        {
            var deck = FindDeck(state, deckId);
            if (deck == null)
                return DeckCommandResult.Fail(DeckCommandError.DeckNotFound, "Deck not found.");
            if (!deck.unlocked)
                return DeckCommandResult.Fail(DeckCommandError.DeckLocked, "Deck slot is locked.");
            if (deck.IsActionBusy)
                return DeckCommandResult.Fail(DeckCommandError.DeckBusy, "Stop the action before changing purpose.");

            deck.purpose = purpose;
            return DeckCommandResult.Ok();
        }

        public static DeckCommandResult TryStart(
            PlayerDeckState state,
            string deckId,
            DeckActionType actionType,
            string targetId,
            long nowUtc,
            ICollection<string> ownedCardIds)
        {
            var deck = FindDeck(state, deckId);
            if (deck == null)
                return DeckCommandResult.Fail(DeckCommandError.DeckNotFound, "Deck not found.");
            if (!deck.unlocked)
                return DeckCommandResult.Fail(DeckCommandError.DeckLocked, "Deck slot is locked.");
            if (deck.IsActionBusy)
                return DeckCommandResult.Fail(DeckCommandError.DeckBusy, "Deck is already running an action.");
            if (deck.MemberCount < 1)
                return DeckCommandResult.Fail(DeckCommandError.EmptyDeck, "Deck has no members.");
            if (actionType == DeckActionType.None)
                return DeckCommandResult.Fail(DeckCommandError.DeckBusy, "Action type is required.");

            var busyCount = DeckOccupationMap.CountBusyDecks(state);
            if (busyCount >= state.maxParallelActions)
            {
                return DeckCommandResult.Fail(
                    DeckCommandError.ParallelLimit,
                    $"Parallel action limit reached ({state.maxParallelActions}).");
            }

            var occupation = DeckOccupationMap.Build(state);
            var conflictCards = new List<string>();
            var conflictDecks = new List<string>();

            foreach (var cardId in deck.slotCardIds)
            {
                if (string.IsNullOrEmpty(cardId))
                    continue;
                if (ownedCardIds != null && !ownedCardIds.Contains(cardId))
                {
                    return DeckCommandResult.Fail(
                        DeckCommandError.CardMissing,
                        $"Card {cardId} is not in the player collection.");
                }

                if (occupation.TryGetValue(cardId, out var occ))
                {
                    conflictCards.Add(cardId);
                    if (!conflictDecks.Contains(occ.DeckId))
                        conflictDecks.Add(occ.DeckId);
                }
            }

            if (conflictCards.Count > 0)
            {
                return DeckCommandResult.Fail(
                    DeckCommandError.CardOccupied,
                    "One or more cards are occupied by another running deck.",
                    conflictCards,
                    conflictDecks);
            }

            deck.action.status = DeckActionStatus.Running;
            deck.action.actionType = actionType;
            deck.action.targetId = targetId ?? "";
            deck.action.startedAtUtc = nowUtc;
            deck.action.lastSettledAtUtc = nowUtc;
            return DeckCommandResult.Ok();
        }

        public static DeckCommandResult TryStop(PlayerDeckState state, string deckId, long nowUtc)
        {
            var deck = FindDeck(state, deckId);
            if (deck == null)
                return DeckCommandResult.Fail(DeckCommandError.DeckNotFound, "Deck not found.");
            if (!deck.IsActionBusy && deck.action.status != DeckActionStatus.Completing)
                return DeckCommandResult.Fail(DeckCommandError.NotBusy, "Deck is not running.");

            deck.action.status = DeckActionStatus.Idle;
            deck.action.actionType = DeckActionType.None;
            deck.action.targetId = "";
            deck.action.lastSettledAtUtc = nowUtc;
            deck.action.progressPayload = "";
            return DeckCommandResult.Ok();
        }
    }
}
