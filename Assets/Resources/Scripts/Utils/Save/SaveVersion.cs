namespace Assets.Resources.Scripts.Utils.Save
{
    /// <summary>Schema version constants for player save directories.</summary>
    public static class SaveVersion
    {
        /// <summary>Current on-disk schema (P2: world.json + ship.json).</summary>
        public const int Current = 3;

        public const int SplitInventory = 1;
        public const int Decks = 2;
        public const int WorldAndShip = 3;
    }
}
