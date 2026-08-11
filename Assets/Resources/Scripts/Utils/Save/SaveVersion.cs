namespace Assets.Resources.Scripts.Utils.Save
{
    /// <summary>Schema version constants for player save directories.</summary>
    public static class SaveVersion
    {
        /// <summary>Current on-disk schema (P3: itemDef inventory + idle.json).</summary>
        public const int Current = 4;

        public const int SplitInventory = 1;
        public const int Decks = 2;
        public const int WorldAndShip = 3;
        public const int EconomyIdle = 4;
    }
}
