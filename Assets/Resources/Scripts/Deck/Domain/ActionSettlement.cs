namespace Assets.Resources.Scripts.Deck.Domain
{
    /// <summary>Outcome of settling a deck action before stop/complete (systems/01 §4.6).</summary>
    public sealed class ActionSettlement
    {
        public string DeckId;
        public DeckActionType ActionType;
        public string TargetId;
        public bool WasPlayerStop;
        public bool DiscardedUnsettledProgress;
        public string ProgressPayloadSnapshot;
        public long SettledAtUtc;
    }
}
