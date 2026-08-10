namespace Assets.Resources.Scripts.Deck.Domain
{
    public enum DeckPurpose
    {
        Flexible = 0,
        Combat = 1,
        Gather = 2,
        Produce = 3,
        Research = 4,
        Transit = 5
    }

    public enum DeckActionStatus
    {
        Idle = 0,
        Running = 1,
        PausedCap = 2,
        Completing = 3
    }

    public enum DeckActionType
    {
        None = 0,
        MainCombat = 1,
        AutoCombat = 2,
        Gather = 3,
        Process = 4,
        Manufacture = 5,
        Research = 6,
        Transit = 7
    }

    public enum CardOccupationState
    {
        Idle = 0,
        MainCombat = 1,
        AutoCombat = 2,
        Gathering = 3,
        Processing = 4,
        Manufacturing = 5,
        Researching = 6,
        InTransit = 7
    }
}
