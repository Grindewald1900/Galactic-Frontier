namespace Assets.Resources.Scripts.World.Domain
{
    public enum RegionProgressState
    {
        Locked = 0,
        Challengeable = 1,
        Cleared = 2,
        BossAvailable = 3,
        BossDefeated = 4
    }

    public enum CombatStrategyId
    {
        Balanced = 0,
        FocusLowestHp = 1,
        FocusHighestThreat = 2,
        PreferAoe = 3
    }

    public static class WorldConstants
    {
        public const string SectorId = "sector_frontier_vii";
        public const string OuterBeltId = "sec01_outer_belt";
        public const string MiningSpurId = "sec01_mining_spur";
        public const string QuantumRiftId = "sec01_quantum_rift";
        public const string AbyssalEdgeId = "sec01_abyssal_edge";
        public const string ConvoyLaneId = "sec01_convoy_lane";
        public const string FrontierBossId = "sec01_frontier_boss";

        public const int FarmCycleSeconds = 30;
        public const int FarmWearPerCycle = 1;
        public const int FarmWearPauseThreshold = 10;
        public const string FarmLootItemName = "seed_scrap";
        public const int FarmLootQuantity = 2;
    }
}
