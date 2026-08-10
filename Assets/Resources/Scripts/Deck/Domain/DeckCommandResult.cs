using System.Collections.Generic;

namespace Assets.Resources.Scripts.Deck.Domain
{
    public enum DeckCommandError
    {
        None = 0,
        DeckNotFound,
        DeckLocked,
        DeckBusy,
        EmptyDeck,
        ParallelLimit,
        CardMissing,
        CardOccupied,
        InvalidSlot,
        DuplicateInDeck,
        NotBusy
    }

    public sealed class DeckCommandResult
    {
        public bool Success { get; private set; }
        public DeckCommandError Error { get; private set; }
        public string Message { get; private set; }
        public List<string> ConflictCardIds { get; private set; }
        public List<string> ConflictDeckIds { get; private set; }

        public static DeckCommandResult Ok() => new DeckCommandResult
        {
            Success = true,
            Error = DeckCommandError.None,
            Message = string.Empty,
            ConflictCardIds = new List<string>(),
            ConflictDeckIds = new List<string>()
        };

        public static DeckCommandResult Fail(
            DeckCommandError error,
            string message,
            List<string> conflictCardIds = null,
            List<string> conflictDeckIds = null) =>
            new DeckCommandResult
            {
                Success = false,
                Error = error,
                Message = message ?? error.ToString(),
                ConflictCardIds = conflictCardIds ?? new List<string>(),
                ConflictDeckIds = conflictDeckIds ?? new List<string>()
            };
    }
}
