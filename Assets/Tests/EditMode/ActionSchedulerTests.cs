using System.Collections.Generic;
using Assets.Resources.Scripts.Deck.Domain;
using NUnit.Framework;

namespace GalacticFrontier.Tests.EditMode
{
    public class ActionSchedulerTests
    {
        private static PlayerDeckState BusyDeckState()
        {
            var state = DeckStateFactory.CreateNewPlayerState();
            state.decks[0].slotCardIds[0] = "card-a";
            Assert.IsTrue(ActionScheduler.TryStart(
                state, state.decks[0].deckId, DeckActionType.Gather, "node:1", 10,
                new HashSet<string> { "card-a" }).Success);
            state.decks[0].action.progressPayload = "cycle-partial";
            return state;
        }

        [Test]
        public void TryStop_DiscardsUnsettledProgressAndReleasesOccupation()
        {
            var state = BusyDeckState();
            Assert.AreEqual(CardOccupationState.Gathering, DeckOccupationMap.GetState(state, "card-a"));

            var stop = ActionScheduler.TryStop(state, state.decks[0].deckId, 20);
            Assert.IsTrue(stop.Success);
            Assert.IsNotNull(stop.Settlement);
            Assert.IsTrue(stop.Settlement.WasPlayerStop);
            Assert.IsTrue(stop.Settlement.DiscardedUnsettledProgress);
            Assert.AreEqual("cycle-partial", stop.Settlement.ProgressPayloadSnapshot);
            Assert.AreEqual(CardOccupationState.Idle, DeckOccupationMap.GetState(state, "card-a"));
            Assert.IsFalse(state.decks[0].IsActionBusy);
            Assert.AreEqual("", state.decks[0].action.progressPayload);
        }

        [Test]
        public void TryComplete_KeepsFullSettlementWithoutDiscardFlag()
        {
            var state = BusyDeckState();
            var complete = ActionScheduler.TryComplete(state, state.decks[0].deckId, 30);
            Assert.IsTrue(complete.Success);
            Assert.IsNotNull(complete.Settlement);
            Assert.IsFalse(complete.Settlement.WasPlayerStop);
            Assert.IsFalse(complete.Settlement.DiscardedUnsettledProgress);
            Assert.AreEqual(CardOccupationState.Idle, DeckOccupationMap.GetState(state, "card-a"));
        }

        [Test]
        public void TryPauseAtCap_KeepsOccupationUntilStop()
        {
            var state = BusyDeckState();
            Assert.IsTrue(ActionScheduler.TryPauseAtCap(state, state.decks[0].deckId, 15).Success);
            Assert.AreEqual(DeckActionStatus.PausedCap, state.decks[0].action.status);
            Assert.AreEqual(CardOccupationState.Gathering, DeckOccupationMap.GetState(state, "card-a"));

            Assert.IsTrue(ActionScheduler.TryResume(state, state.decks[0].deckId, 16).Success);
            Assert.AreEqual(DeckActionStatus.Running, state.decks[0].action.status);

            Assert.IsTrue(ActionScheduler.TryStop(state, state.decks[0].deckId, 17).Success);
            Assert.AreEqual(CardOccupationState.Idle, DeckOccupationMap.GetState(state, "card-a"));
        }

        [Test]
        public void AfterStop_CardCanStartOnAnotherDeck()
        {
            var state = DeckStateFactory.CreateNewPlayerState();
            state.decks[0].slotCardIds[0] = "card-a";
            state.decks[1].slotCardIds[0] = "card-a"; // backup preset share
            var owned = new HashSet<string> { "card-a" };

            Assert.IsTrue(ActionScheduler.TryStart(
                state, state.decks[0].deckId, DeckActionType.Gather, "a", 1, owned).Success);
            Assert.IsTrue(ActionScheduler.TryStop(state, state.decks[0].deckId, 2).Success);
            Assert.IsTrue(ActionScheduler.TryStart(
                state, state.decks[1].deckId, DeckActionType.AutoCombat, "r", 3, owned).Success);
        }
    }
}
