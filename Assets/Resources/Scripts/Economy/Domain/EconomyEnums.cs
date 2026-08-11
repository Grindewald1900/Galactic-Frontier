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
        public const int CraftCycleSeconds = 30;
        public const int OnlineFarmCycleSeconds = 30;
        public const int OfflineCapBaseSeconds = 7200;
        public const int OfflineCapSoftSeconds = 43200;
        public const int OfflineCapHardSeconds = 86400;
        public const float OfflineYieldBase = 0.50f;
        public const float OfflineYieldSoft = 0.90f;
        public const float OfflineYieldHard = 1.00f;
        public const int AutoRepairThresholdPercent = 30;
        public const int BaseCombatWear = 3;
        public const string ScrapDefId = "mat_scrap";
        public const string RepairKitDefId = "con_repair_kit";
    }
}
