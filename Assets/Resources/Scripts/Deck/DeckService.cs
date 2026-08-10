using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Resources.Scripts.Deck.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Utils;
using UnityEngine;

namespace Assets.Resources.Scripts.Deck
{
    /// <summary>
    /// Runtime façade over <see cref="PlayerDeckState"/>: load/save decks.json, membership, occupation.
    /// </summary>
    public static class DeckService
    {
        public static PlayerDeckState State { get; private set; }

        public static bool IsLoaded => State != null;

        public static void Clear()
        {
            State = null;
        }

        /// <summary>Loads decks.json or creates/migrates from legacy lineup positions.</summary>
        public static void EnsureLoaded(DataUtil dataUtil, IList<CardEntity> cards)
        {
            if (dataUtil == null)
                throw new ArgumentNullException(nameof(dataUtil));

            var loaded = dataUtil.LoadDeckState();
            if (loaded != null && loaded.decks != null && loaded.decks.Count > 0)
            {
                State = Normalize(loaded);
            }
            else
            {
                State = DeckStateFactory.CreateFromLegacyLineup(CollectLegacyLineup(cards));
                dataUtil.SaveDeckState(State, touchMeta: true);
                Debug.Log($"[DECK] Created decks.json from legacy lineup ({State.decks[0].MemberCount} members).");
            }

            SyncLegacyLineupPositions(cards);
        }

        /// <summary>Creates the default multi-deck state for a brand-new player save.</summary>
        public static void CreateForNewPlayer(DataUtil dataUtil)
        {
            State = DeckStateFactory.CreateNewPlayerState();
            dataUtil.SaveDeckState(State, touchMeta: false);
        }

        public static void Save(DataUtil dataUtil = null)
        {
            if (State == null) return;
            var util = dataUtil ?? DataUtil.Instance;
            if (util == null) return;
            State.count = State.decks?.Count ?? 0;
            util.SaveDeckState(State, touchMeta: true);
        }

        public static DeckEntity GetActiveCombatDeck()
        {
            if (State == null) return null;
            var deck = DeckRules.FindDeck(State, State.activeCombatDeckId);
            if (deck != null && deck.unlocked)
                return deck;
            return State.decks?.FirstOrDefault(d => d != null && d.unlocked);
        }

        public static List<CardEntity> GetOrderedMembers(string deckId, IList<CardEntity> cards)
        {
            var result = new List<CardEntity>();
            var deck = DeckRules.FindDeck(State, deckId);
            if (deck?.slotCardIds == null || cards == null)
                return result;

            for (var i = 0; i < deck.slotCardIds.Length; i++)
            {
                var id = deck.slotCardIds[i];
                if (string.IsNullOrEmpty(id))
                    continue;
                var entity = cards.FirstOrDefault(c => c != null && c.id == id);
                if (entity != null)
                    result.Add(entity);
            }

            return result;
        }

        /// <summary>Members of the active combat deck, with LineupPosition mirrored for battle slotting.</summary>
        public static List<CardEntity> GetActiveCombatMembers(IList<CardEntity> cards)
        {
            var deck = GetActiveCombatDeck();
            if (deck == null)
                return new List<CardEntity>();

            var members = new List<CardEntity>();
            for (var i = 0; i < DeckConstants.SlotsPerDeck; i++)
            {
                var id = deck.slotCardIds != null && i < deck.slotCardIds.Length ? deck.slotCardIds[i] : "";
                if (string.IsNullOrEmpty(id) || cards == null)
                    continue;
                var entity = cards.FirstOrDefault(c => c != null && c.id == id);
                if (entity == null)
                    continue;
                entity.SetLineupPosition(i);
                members.Add(entity);
            }

            return members;
        }

        public static CardOccupationState GetOccupation(string cardId) =>
            DeckOccupationMap.GetState(State, cardId);

        public static DeckCommandResult TryAssignToActiveCombat(int slotIndex, string cardId, IList<CardEntity> allCards)
        {
            EnsureState();
            var deck = GetActiveCombatDeck();
            if (deck == null)
                return DeckCommandResult.Fail(DeckCommandError.DeckNotFound, "No active combat deck.");

            var result = DeckRules.TryAssignSlot(State, deck.deckId, slotIndex, cardId);
            if (result.Success)
            {
                SyncLegacyLineupPositions(allCards);
                Save();
            }

            return result;
        }

        public static DeckCommandResult TryStart(
            string deckId,
            DeckActionType actionType,
            string targetId,
            IList<CardEntity> cards)
        {
            EnsureState();
            var owned = new HashSet<string>();
            if (cards != null)
            {
                foreach (var c in cards)
                {
                    if (c != null && !string.IsNullOrEmpty(c.id))
                        owned.Add(c.id);
                }
            }

            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var result = DeckRules.TryStart(State, deckId, actionType, targetId, now, owned);
            if (result.Success)
                Save();
            return result;
        }

        public static DeckCommandResult TryStop(string deckId)
        {
            EnsureState();
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var result = DeckRules.TryStop(State, deckId, now);
            if (result.Success)
                Save();
            return result;
        }

        /// <summary>Stops any deck currently in MainCombat (battle exit / Esc).</summary>
        public static void StopAllMainCombat()
        {
            if (State?.decks == null) return;
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var changed = false;
            foreach (var deck in State.decks)
            {
                if (deck?.action == null) continue;
                if (deck.IsActionBusy && deck.action.actionType == DeckActionType.MainCombat)
                {
                    DeckRules.TryStop(State, deck.deckId, now);
                    changed = true;
                }
            }

            if (changed)
                Save();
        }

        public static void SyncLegacyLineupPositions(IList<CardEntity> cards)
        {
            if (cards == null || State == null)
                return;

            foreach (var card in cards)
            {
                if (card != null)
                    card.SetLineupPosition(LineupPosition.None);
            }

            var deck = GetActiveCombatDeck();
            if (deck?.slotCardIds == null)
                return;

            for (var i = 0; i < deck.slotCardIds.Length; i++)
            {
                var id = deck.slotCardIds[i];
                if (string.IsNullOrEmpty(id))
                    continue;
                var entity = cards.FirstOrDefault(c => c != null && c.id == id);
                entity?.SetLineupPosition(i);
            }
        }

        private static void EnsureState()
        {
            if (State == null)
                State = DeckStateFactory.CreateNewPlayerState();
        }

        private static PlayerDeckState Normalize(PlayerDeckState state)
        {
            state.decks ??= new List<DeckEntity>();
            if (state.unlockedDeckSlots <= 0)
                state.unlockedDeckSlots = DeckConstants.DefaultUnlockedDeckSlots;
            if (state.maxParallelActions <= 0)
                state.maxParallelActions = DeckConstants.DefaultMaxParallelActions;

            while (state.decks.Count < DeckConstants.MaxDeckSlots)
            {
                var i = state.decks.Count;
                state.decks.Add(new DeckEntity
                {
                    deckId = Guid.NewGuid().ToString("N"),
                    displayName = "Locked " + (i + 1),
                    sortOrder = i,
                    unlocked = i < state.unlockedDeckSlots,
                    slotCardIds = DeckEntity.CreateEmptySlots(),
                    action = new DeckActionState()
                });
            }

            foreach (var deck in state.decks)
            {
                if (deck.slotCardIds == null || deck.slotCardIds.Length != DeckConstants.SlotsPerDeck)
                    deck.slotCardIds = DeckEntity.CreateEmptySlots();
                deck.action ??= new DeckActionState();
            }

            if (string.IsNullOrEmpty(state.activeCombatDeckId) ||
                DeckRules.FindDeck(state, state.activeCombatDeckId) == null)
            {
                state.activeCombatDeckId = state.decks.FirstOrDefault(d => d != null && d.unlocked)?.deckId ?? "";
            }

            state.count = state.decks.Count;
            return state;
        }

        private static IEnumerable<LegacyLineupEntry> CollectLegacyLineup(IList<CardEntity> cards)
        {
            if (cards == null)
                yield break;
            foreach (var card in cards)
            {
                if (card == null) continue;
                var pos = card.GetLineupPosition();
                if (pos == LineupPosition.None) continue;
                yield return new LegacyLineupEntry(card.id, (int)pos);
            }
        }
    }
}
