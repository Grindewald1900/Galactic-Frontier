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

    /// <summary>In-sector Astral Grid visibility / transit (doc 20 §3). Not a region lock.</summary>
    public enum GridNodeState
    {
        Unobserved = 0,
        Fogged = 1,
        Located = 2,
        Provisional = 3,
        Stable = 4
    }

    public enum GridEdgeTier
    {
        Unknown = 0,
        Provisional = 1,
        Stable = 2
    }

    public enum GridRouteTag
    {
        None = 0,
        Military = 1,
        Industry = 2,
        Trade = 3,
        Rift = 4
    }

    public static class WorldConstants
    {
        public const string SectorId = "sector_frontier_vii";
        public const string SectorMiningId = "sector_mining_belt";
        public const string SectorRelicId = "sector_relic_reach";
        public const string SectorFaultId = "sector_entropy_fault";
        public const string SectorTradeId = "sector_trade_relay";
        public const string SectorInnerId = "sector_inner_ring";
        public const string SectorCoreId = "sector_astral_core";
        public const string OuterHavenBodyId = "body_outer_haven";
        public const string CapitalBodyId = "body_frontier_anchor";
        public const string FrontierChartId = "chart_frontier_vii";
        public const int ChartCreditCost = 60;
        public const int StabilizeScrapCost = 8;
        public const int StabilizeCreditCost = 15;
        public const int HubChartExploreMin = 50;
        public const string OuterBeltId = "sec01_outer_belt";
        public const string MiningSpurId = "sec01_mining_spur";
        public const string QuantumRiftId = "sec01_quantum_rift";
        public const string AbyssalEdgeId = "sec01_abyssal_edge";
        public const string ConvoyLaneId = "sec01_convoy_lane";
        public const string FrontierBossId = "sec01_frontier_boss";

        public const int FarmCycleSeconds = 45;
        public const int FarmWearPerCycle = 1;
        public const int FarmWearPauseThreshold = 10;
        public const string FarmLootItemName = "seed_scrap";
        public const int FarmLootQuantity = 2;
    }
}
