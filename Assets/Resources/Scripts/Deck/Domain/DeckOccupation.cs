using System.Collections.Generic;

namespace Assets.Resources.Scripts.Deck.Domain
{
    public readonly struct CardOccupation
    {
        public string CardId { get; }
        public string DeckId { get; }
        public CardOccupationState State { get; }

        public CardOccupation(string cardId, string deckId, CardOccupationState state)
        {
            CardId = cardId;
            DeckId = deckId;
            State = state;
        }
    }

    public static class DeckOccupationMap
    {
        public static CardOccupationState ToOccupation(DeckActionType actionType) =>
            actionType switch
            {
                DeckActionType.MainCombat => CardOccupationState.MainCombat,
                DeckActionType.AutoCombat => CardOccupationState.AutoCombat,
                DeckActionType.Gather => CardOccupationState.Gathering,
                DeckActionType.Process => CardOccupationState.Processing,
                DeckActionType.Manufacture => CardOccupationState.Manufacturing,
                DeckActionType.Research => CardOccupationState.Researching,
                DeckActionType.Transit => CardOccupationState.InTransit,
                _ => CardOccupationState.Idle
            };

        /// <summary>
        /// Builds cardId → first occupying Running/PausedCap deck (earliest startedAtUtc wins).
        /// </summary>
        public static Dictionary<string, CardOccupation> Build(PlayerDeckState state)
        {
            var map = new Dictionary<string, CardOccupation>();
            if (state?.decks == null)
                return map;

            var busy = new List<DeckEntity>();
            foreach (var deck in state.decks)
            {
                if (deck != null && deck.unlocked && deck.IsActionBusy)
                    busy.Add(deck);
            }

            busy.Sort((a, b) => a.action.startedAtUtc.CompareTo(b.action.startedAtUtc));

            foreach (var deck in busy)
            {
                if (deck.slotCardIds == null)
                    continue;
                var occ = ToOccupation(deck.action.actionType);
                foreach (var cardId in deck.slotCardIds)
                {
                    if (string.IsNullOrEmpty(cardId))
                        continue;
                    if (!map.ContainsKey(cardId))
                        map[cardId] = new CardOccupation(cardId, deck.deckId, occ);
                }
            }

            return map;
        }

        public static CardOccupationState GetState(PlayerDeckState state, string cardId)
        {
            if (string.IsNullOrEmpty(cardId))
                return CardOccupationState.Idle;
            var map = Build(state);
            return map.TryGetValue(cardId, out var occ) ? occ.State : CardOccupationState.Idle;
        }

        public static int CountBusyDecks(PlayerDeckState state)
        {
            if (state?.decks == null) return 0;
            var n = 0;
            foreach (var deck in state.decks)
            {
                if (deck != null && deck.unlocked && deck.IsActionBusy)
                    n++;
            }

            return n;
        }
    }
}
