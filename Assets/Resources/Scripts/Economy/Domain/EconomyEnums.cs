namespace Assets.Resources.Scripts.Economy.Domain
{
    public enum ItemCategory
    {
        Material = 0,
        Intermediate = 1,
        Consumable = 2,
        Equipment = 3,
        ShipModule = 4
    }

    public enum QualityTier
    {
        Q1 = 1,
        Q2 = 2,
        Q3 = 3,
        Q4 = 4,
        Q5 = 5
    }

    /// <summary>Display letters for Q1–Q5 (F → S). Used by the crafting list and filters.</summary>
    public static class QualityGrade
    {
        public static string Letter(int quality) => quality switch
        {
            1 => "F",
            2 => "D",
            3 => "C",
            4 => "B",
            5 => "S",
            _ => "—"
        };

        public static int FromLetter(string letter)
        {
            if (string.IsNullOrEmpty(letter)) return 0;
            return letter.Trim().ToUpperInvariant() switch
            {
                "F" => 1,
                "D" => 2,
                "C" => 3,
                "B" => 4,
                "S" => 5,
                _ => 0
            };
        }
    }

    public enum RecipeKind
    {
        Process = 0,
        Manufacture = 1
    }

    public enum EquipSlot
    {
        None = 0,
        Weapon = 1,
        Armor = 2,
        Accessory = 3,
        Tool = 4
    }

    public static class EconomyConstants
    {
        public const int WarehouseCapacity = 60;
        public const int DefaultQuality = 2;
        public const int GatherCycleSeconds = 30; // playtest; design default 300
        /// <summary>Units a gather job may bank before it stalls waiting to be collected.</summary>
        public const int GatherBankCap = 200;
        public const int CraftCycleSeconds = 30;
        public const int OnlineFarmCycleSeconds = 45;
        public const int OfflineCapBaseSeconds = 7200;
        public const int OfflineCapSoftSeconds = 43200;
        public const int OfflineCapHardSeconds = 86400;
        public const float OfflineYieldBase = 1.00f;
        public const float OfflineYieldSoft = 1.00f;
        public const float OfflineYieldHard = 1.00f;
        public const int AutoRepairThresholdPercent = 30;
        public const int BaseCombatWear = 2;
        public const string ScrapDefId = "mat_scrap";
        public const string RepairKitDefId = "con_repair_kit";
    }
}
