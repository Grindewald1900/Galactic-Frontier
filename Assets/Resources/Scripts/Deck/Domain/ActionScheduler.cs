using System.Collections.Generic;

namespace Assets.Resources.Scripts.Deck.Domain
{
    /// <summary>
    /// Starts / stops / completes deck actions with low stop penalty (systems/01 §4.6 / §6.3).
    /// Inventory rewards are left to action subsystems; this layer only settles progress flags and occupation.
    /// </summary>
    public static class ActionScheduler
    {
        public static DeckCommandResult TryStart(
            PlayerDeckState state,
            string deckId,
            DeckActionType actionType,
            string targetId,
            long nowUtc,
            ICollection<string> ownedCardIds) =>
            DeckRules.TryStart(state, deckId, actionType, targetId, nowUtc, ownedCardIds);

        /// <summary>
        /// Player stop: settle partial progress (discard unsettled cycle), then release occupation immediately.
        /// </summary>
        public static DeckCommandResult TryStop(PlayerDeckState state, string deckId, long nowUtc)
        {
            var deck = DeckRules.FindDeck(state, deckId);
            if (deck == null)
                return DeckCommandResult.Fail(DeckCommandError.DeckNotFound, "Deck not found.");
            if (!deck.IsActionBusy && deck.action?.status != DeckActionStatus.Completing)
                return DeckCommandResult.Fail(DeckCommandError.NotBusy, "Deck is not running.");

            var settlement = SettlePartial(deck, nowUtc, wasPlayerStop: true);
            var stop = DeckRules.TryStop(state, deckId, nowUtc);
            return stop.Success ? DeckCommandResult.Ok(settlement) : stop;
        }

        /// <summary>Normal completion: keep full period rewards conceptually, then idle.</summary>
        public static DeckCommandResult TryComplete(PlayerDeckState state, string deckId, long nowUtc)
        {
            var deck = DeckRules.FindDeck(state, deckId);
            if (deck == null)
                return DeckCommandResult.Fail(DeckCommandError.DeckNotFound, "Deck not found.");
            if (!deck.IsActionBusy && deck.action?.status != DeckActionStatus.Completing)
                return DeckCommandResult.Fail(DeckCommandError.NotBusy, "Deck is not running.");

            deck.action.status = DeckActionStatus.Completing;
            var settlement = SettleFull(deck, nowUtc);
            var stop = DeckRules.TryStop(state, deckId, nowUtc);
            return stop.Success ? DeckCommandResult.Ok(settlement) : stop;
        }

        /// <summary>Offline / soft cap pause — members remain occupied until continue or stop.</summary>
        public static DeckCommandResult TryPauseAtCap(PlayerDeckState state, string deckId, long nowUtc)
        {
            var deck = DeckRules.FindDeck(state, deckId);
            if (deck == null)
                return DeckCommandResult.Fail(DeckCommandError.DeckNotFound, "Deck not found.");
            if (deck.action == null || deck.action.status != DeckActionStatus.Running)
                return DeckCommandResult.Fail(DeckCommandError.NotBusy, "Only Running actions can pause at cap.");

            deck.action.status = DeckActionStatus.PausedCap;
            deck.action.lastSettledAtUtc = nowUtc;
            return DeckCommandResult.Ok();
        }

        /// <summary>Resume a PausedCap deck without re-checking membership conflicts (already occupied).</summary>
        public static DeckCommandResult TryResume(PlayerDeckState state, string deckId, long nowUtc)
        {
            var deck = DeckRules.FindDeck(state, deckId);
            if (deck == null)
                return DeckCommandResult.Fail(DeckCommandError.DeckNotFound, "Deck not found.");
            if (deck.action == null || deck.action.status != DeckActionStatus.PausedCap)
                return DeckCommandResult.Fail(DeckCommandError.NotBusy, "Deck is not paused at cap.");

            deck.action.status = DeckActionStatus.Running;
            deck.action.lastSettledAtUtc = nowUtc;
            return DeckCommandResult.Ok();
        }

        private static ActionSettlement SettlePartial(DeckEntity deck, long nowUtc, bool wasPlayerStop)
        {
            var payload = deck.action?.progressPayload ?? "";
            var settlement = new ActionSettlement
            {
                DeckId = deck.deckId,
                ActionType = deck.action?.actionType ?? DeckActionType.None,
                TargetId = deck.action?.targetId ?? "",
                WasPlayerStop = wasPlayerStop,
                // MVP low penalty: keep bagged rewards; drop only unsettled cycle progress.
                DiscardedUnsettledProgress = !string.IsNullOrEmpty(payload),
                ProgressPayloadSnapshot = payload,
                SettledAtUtc = nowUtc
            };
            if (deck.action != null)
            {
                deck.action.progressPayload = "";
                deck.action.lastSettledAtUtc = nowUtc;
            }

            return settlement;
        }

        private static ActionSettlement SettleFull(DeckEntity deck, long nowUtc)
        {
            var payload = deck.action?.progressPayload ?? "";
            var settlement = new ActionSettlement
            {
                DeckId = deck.deckId,
                ActionType = deck.action?.actionType ?? DeckActionType.None,
                TargetId = deck.action?.targetId ?? "",
                WasPlayerStop = false,
                DiscardedUnsettledProgress = false,
                ProgressPayloadSnapshot = payload,
                SettledAtUtc = nowUtc
            };
            if (deck.action != null)
            {
                deck.action.progressPayload = "";
                deck.action.lastSettledAtUtc = nowUtc;
            }

            return settlement;
        }
    }
}
