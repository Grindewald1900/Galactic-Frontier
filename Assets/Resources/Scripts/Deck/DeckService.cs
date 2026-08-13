using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Resources.Scripts.Cards;
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
        /// <summary>UI-selected deck for Formation editing (not persisted).</summary>
        private static string editingDeckId = "";

        public static PlayerDeckState State { get; private set; }

        public static bool IsLoaded => State != null;

        public static void Clear()
        {
            State = null;
            editingDeckId = "";
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

            if (string.IsNullOrEmpty(editingDeckId) || DeckRules.FindDeck(State, editingDeckId) == null)
                editingDeckId = State.activeCombatDeckId;

            SyncLegacyLineupPositions(cards);
        }

        /// <summary>Creates the default multi-deck state for a brand-new player save.</summary>
        public static void CreateForNewPlayer(DataUtil dataUtil)
        {
            State = DeckStateFactory.CreateNewPlayerState();
            editingDeckId = State.activeCombatDeckId;
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

        public static DeckEntity GetEditingDeck()
        {
            if (State == null) return null;
            var deck = DeckRules.FindDeck(State, editingDeckId);
            if (deck != null)
                return deck;
            return GetActiveCombatDeck();
        }

        public static string EditingDeckId => GetEditingDeck()?.deckId ?? "";

        public static IReadOnlyList<DeckEntity> GetDecks() =>
            (IReadOnlyList<DeckEntity>)(State?.decks ?? new List<DeckEntity>());

        public static int CountBusyDecks() => DeckOccupationMap.CountBusyDecks(State);

        public static int MaxParallelActions => State?.maxParallelActions ?? DeckConstants.DefaultMaxParallelActions;

        public static List<DeckEntity> GetBusyDecks()
        {
            var list = new List<DeckEntity>();
            if (State?.decks == null) return list;
            foreach (var deck in State.decks)
            {
                if (deck != null && deck.unlocked && deck.IsActionBusy)
                    list.Add(deck);
            }

            return list;
        }

        public static DeckCommandResult TrySelectDeck(string deckId)
        {
            EnsureState();
            var deck = DeckRules.FindDeck(State, deckId);
            if (deck == null)
                return DeckCommandResult.Fail(DeckCommandError.DeckNotFound, "Deck not found.");
            editingDeckId = deck.deckId;
            return DeckCommandResult.Ok();
        }

        /// <summary>Marks an unlocked deck as the Explore / main-combat default.</summary>
        public static DeckCommandResult TrySetActiveCombatDeck(string deckId, IList<CardEntity> cards = null)
        {
            EnsureState();
            var deck = DeckRules.FindDeck(State, deckId);
            if (deck == null)
                return DeckCommandResult.Fail(DeckCommandError.DeckNotFound, "Deck not found.");
            if (!deck.unlocked)
                return DeckCommandResult.Fail(DeckCommandError.DeckLocked, "Deck slot is locked.");

            State.activeCombatDeckId = deck.deckId;
            editingDeckId = deck.deckId;
            SyncLegacyLineupPositions(cards ?? CardListFallback());
            Save();
            return DeckCommandResult.Ok();
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

        public static CardEntity FindMemberInSlot(string deckId, int slotIndex, IList<CardEntity> cards)
        {
            var deck = DeckRules.FindDeck(State, deckId);
            if (deck?.slotCardIds == null || slotIndex < 0 || slotIndex >= deck.slotCardIds.Length)
                return null;
            var id = deck.slotCardIds[slotIndex];
            if (string.IsNullOrEmpty(id) || cards == null)
                return null;
            return cards.FirstOrDefault(c => c != null && c.id == id);
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

        public static DeckCommandResult TryAssignToDeck(string deckId, int slotIndex, string cardId, IList<CardEntity> allCards)
        {
            EnsureState();
            var result = DeckRules.TryAssignSlot(State, deckId, slotIndex, cardId);
            if (result.Success)
            {
                SyncLegacyLineupPositions(allCards);
                Save();
                if (GetDecks() != null)
                {
                    foreach (var d in GetDecks())
                    {
                        if (d != null && d.unlocked && d.MemberCount >= 1)
                        {
                            Assets.Resources.Scripts.Onboarding.OnboardingService.NotifyFormationReady();
                            break;
                        }
                    }
                }
            }

            return result;
        }

        public static DeckCommandResult TryAssignToActiveCombat(int slotIndex, string cardId, IList<CardEntity> allCards)
        {
            var deck = GetActiveCombatDeck();
            if (deck == null)
                return DeckCommandResult.Fail(DeckCommandError.DeckNotFound, "No active combat deck.");
            return TryAssignToDeck(deck.deckId, slotIndex, cardId, allCards);
        }

        public static DeckCommandResult TryAssignToEditing(int slotIndex, string cardId, IList<CardEntity> allCards)
        {
            var deck = GetEditingDeck();
            if (deck == null)
                return DeckCommandResult.Fail(DeckCommandError.DeckNotFound, "No deck selected.");
            return TryAssignToDeck(deck.deckId, slotIndex, cardId, allCards);
        }

        public static DeckCommandResult TryRename(string deckId, string displayName)
        {
            EnsureState();
            var result = DeckRules.TryRename(State, deckId, displayName);
            if (result.Success)
                Save();
            return result;
        }

        public static DeckCommandResult TrySetPurpose(string deckId, DeckPurpose purpose)
        {
            EnsureState();
            var result = DeckRules.TrySetPurpose(State, deckId, purpose);
            if (result.Success)
                Save();
            return result;
        }

        public static DeckCommandResult TrySetCombatStrategy(string deckId, string strategyId)
        {
            EnsureState();
            var deck = DeckRules.FindDeck(State, deckId);
            if (deck == null)
                return DeckCommandResult.Fail(DeckCommandError.DeckNotFound, "Deck not found.");
            if (!deck.unlocked)
                return DeckCommandResult.Fail(DeckCommandError.DeckLocked, "Deck slot is locked.");
            deck.combatStrategyId = strategyId ?? "Balanced";
            Save();
            return DeckCommandResult.Ok();
        }

        public static DeckCommandResult TryStart(
            string deckId,
            DeckActionType actionType,
            string targetId,
            IList<CardEntity> cards)
        {
            EnsureState();
            var owned = CollectOwnedIds(cards);
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var result = ActionScheduler.TryStart(State, deckId, actionType, targetId, now, owned);
            if (result.Success)
                Save();
            return result;
        }

        public static DeckCommandResult TryStop(string deckId)
        {
            EnsureState();
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var result = ActionScheduler.TryStop(State, deckId, now);
            if (result.Success)
                Save();
            return result;
        }

        public static DeckCommandResult TryComplete(string deckId)
        {
            EnsureState();
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var result = ActionScheduler.TryComplete(State, deckId, now);
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
                    ActionScheduler.TryStop(State, deck.deckId, now);
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

        private static HashSet<string> CollectOwnedIds(IList<CardEntity> cards)
        {
            var owned = new HashSet<string>();
            if (cards == null) return owned;
            foreach (var c in cards)
            {
                if (c != null && !string.IsNullOrEmpty(c.id))
                    owned.Add(c.id);
            }

            return owned;
        }

        private static IList<CardEntity> CardListFallback() =>
            CardListManager.Instance?.GetCardEntities();

        public static void EnsureReady()
        {
            EnsureState();
        }

        private static void EnsureState()
        {
            if (State == null)
            {
                State = DeckStateFactory.CreateNewPlayerState();
                editingDeckId = State.activeCombatDeckId;
            }
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

            for (var i = 0; i < state.decks.Count; i++)
            {
                var deck = state.decks[i];
                if (deck == null) continue;
                if (deck.slotCardIds == null || deck.slotCardIds.Length != DeckConstants.SlotsPerDeck)
                    deck.slotCardIds = DeckEntity.CreateEmptySlots();
                deck.action ??= new DeckActionState();
                deck.unlocked = i < state.unlockedDeckSlots;
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
