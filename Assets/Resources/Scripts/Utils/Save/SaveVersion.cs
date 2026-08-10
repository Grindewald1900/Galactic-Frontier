namespace Assets.Resources.Scripts.Utils.Save
{
    /// <summary>Schema version constants for player save directories.</summary>
    public static class SaveVersion
    {
        /// <summary>Current on-disk schema (P1.1: decks.json + occupation).</summary>
        public const int Current = 2;

        public const int SplitInventory = 1;
        public const int Decks = 2;
    }
}
