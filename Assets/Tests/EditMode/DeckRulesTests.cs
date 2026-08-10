using System.Collections.Generic;
using Assets.Resources.Scripts.Deck.Domain;
using NUnit.Framework;

namespace GalacticFrontier.Tests.EditMode
{
    public class DeckRulesTests
    {
        private static PlayerDeckState TwoDeckState()
        {
            var state = DeckStateFactory.CreateNewPlayerState();
            state.decks[0].slotCardIds[0] = "card-a";
            state.decks[0].slotCardIds[1] = "card-b";
            state.decks[1].slotCardIds[0] = "card-c";
            state.maxParallelActions = 2;
            return state;
        }

        private static HashSet<string> Owned(params string[] ids) => new HashSet<string>(ids);

        [Test]
        public void TryStart_RejectsWhenCardOccupiedByOtherRunningDeck()
        {
            var state = TwoDeckState();
            state.decks[1].slotCardIds[1] = "card-a"; // share card-a in preset (allowed while idle)

            var first = DeckRules.TryStart(
                state, state.decks[0].deckId, DeckActionType.AutoCombat, "region:1", 100, Owned("card-a", "card-b", "card-c"));
            Assert.IsTrue(first.Success);

            var second = DeckRules.TryStart(
                state, state.decks[1].deckId, DeckActionType.Gather, "node:1", 101, Owned("card-a", "card-b", "card-c"));
            Assert.IsFalse(second.Success);
            Assert.AreEqual(DeckCommandError.CardOccupied, second.Error);
            CollectionAssert.Contains(second.ConflictCardIds, "card-a");
        }

        [Test]
        public void IdlePresetMayShareCardId_WithoutOccupation()
        {
            var state = TwoDeckState();
            state.decks[1].slotCardIds[0] = "card-a";
            Assert.AreEqual(CardOccupationState.Idle, DeckOccupationMap.GetState(state, "card-a"));
        }

        [Test]
        public void TryStart_RespectsParallelLimit()
        {
            var state = TwoDeckState();
            state.maxParallelActions = 1;
            state.decks[1].slotCardIds[0] = "card-x";

            Assert.IsTrue(DeckRules.TryStart(
                state, state.decks[0].deckId, DeckActionType.Gather, "a", 1, Owned("card-a", "card-b", "card-x")).Success);

            var blocked = DeckRules.TryStart(
                state, state.decks[1].deckId, DeckActionType.Gather, "b", 2, Owned("card-a", "card-b", "card-x"));
            Assert.IsFalse(blocked.Success);
            Assert.AreEqual(DeckCommandError.ParallelLimit, blocked.Error);
        }

        [Test]
        public void TryStop_ReleasesOccupationImmediately()
        {
            var state = TwoDeckState();
            Assert.IsTrue(DeckRules.TryStart(
                state, state.decks[0].deckId, DeckActionType.MainCombat, "r", 10, Owned("card-a", "card-b")).Success);
            Assert.AreEqual(CardOccupationState.MainCombat, DeckOccupationMap.GetState(state, "card-a"));

            Assert.IsTrue(DeckRules.TryStop(state, state.decks[0].deckId, 11).Success);
            Assert.AreEqual(CardOccupationState.Idle, DeckOccupationMap.GetState(state, "card-a"));
        }

        [Test]
        public void DifferentCardIdsSameTemplate_CanRunInParallel()
        {
            var state = TwoDeckState();
            state.decks[0].slotCardIds[0] = "asra-1";
            state.decks[1].slotCardIds[0] = "asra-2";

            Assert.IsTrue(DeckRules.TryStart(
                state, state.decks[0].deckId, DeckActionType.AutoCombat, "r1", 1, Owned("asra-1", "asra-2")).Success);
            Assert.IsTrue(DeckRules.TryStart(
                state, state.decks[1].deckId, DeckActionType.Gather, "n1", 2, Owned("asra-1", "asra-2")).Success);
        }

        [Test]
        public void CannotEditMembershipWhileRunning()
        {
            var state = TwoDeckState();
            Assert.IsTrue(DeckRules.TryStart(
                state, state.decks[0].deckId, DeckActionType.Gather, "n", 1, Owned("card-a", "card-b")).Success);

            var edit = DeckRules.TryAssignSlot(state, state.decks[0].deckId, 2, "card-z");
            Assert.IsFalse(edit.Success);
            Assert.AreEqual(DeckCommandError.DeckBusy, edit.Error);
        }

        [Test]
        public void CreateNewPlayerState_HasTwoUnlockedDecks()
        {
            var state = DeckStateFactory.CreateNewPlayerState();
            Assert.AreEqual(DeckConstants.MaxDeckSlots, state.decks.Count);
            Assert.AreEqual(2, state.unlockedDeckSlots);
            Assert.AreEqual(2, state.maxParallelActions);
            Assert.IsTrue(state.decks[0].unlocked);
            Assert.IsTrue(state.decks[1].unlocked);
            Assert.IsFalse(state.decks[2].unlocked);
        }

        [Test]
        public void IdleBackupDeck_CanShareCardAndBeRenamed()
        {
            var state = TwoDeckState();
            state.decks[1].slotCardIds[0] = "card-a";
            Assert.AreEqual(CardOccupationState.Idle, DeckOccupationMap.GetState(state, "card-a"));

            var rename = DeckRules.TryRename(state, state.decks[1].deckId, "Backup Alpha");
            Assert.IsTrue(rename.Success);
            Assert.AreEqual("Backup Alpha", state.decks[1].displayName);
        }
    }
}
