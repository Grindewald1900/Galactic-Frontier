using System;
using System.Collections.Generic;

namespace Assets.Resources.Scripts.World.Domain
{
    [Serializable]
    public class ShipGate
    {
        public int level = 1;
        public int range;
        public int energy;
        public int hull;
        public int entropyResist;
        public int cargo;
        public int scan;
        public int lifeSupport;
    }

    [Serializable]
    public class ShipStats
    {
        public int range;
        public int energy;
        public int hull;
        public int entropyResist;
        public int cargo;
        public int scan;
        public int lifeSupport;
    }

    [Serializable]
    public class ShipModuleState
    {
        public string moduleId = "";
        public int level;
    }

    [Serializable]
    public class ShipEntity
    {
        public string shipId = "";
        public string displayName = "Frontier Skiff";
        public int level = 1;
        public ShipStats stats = new ShipStats();
        public List<ShipModuleState> modules = new List<ShipModuleState>();
        /// <summary>Card the player picked to represent the ship; empty when none is chosen.</summary>
        public string mascotCardId = "";
        public int count;
    }

    [Serializable]
    public class RegionRuntimeState
    {
        public string regionId = "";
        public bool cleared;
        public int clearCount;
        public bool bossDefeated;
        public long firstClearedAtUtc;
        public long lastFarmAtUtc;
    }

    [Serializable]
    public class PlayerWorldState
    {
        public string currentSectorId = WorldConstants.SectorId;
        public List<RegionRuntimeState> regions = new List<RegionRuntimeState>();
        public int explorationProgress;
        public int count;
    }

    [Serializable]
    public class RegionConfig
    {
        public string regionId = "";
        public string sectorId = WorldConstants.SectorId;
        public int sortOrder;
        public string displayNameEn = "";
        public string displayNameZh = "";
        public string blurbEn = "";
        public string blurbZh = "";
        public string factionTag = "";
        public string[] prereqRegionIds = Array.Empty<string>();
        public ShipGate shipGate = new ShipGate();
        public string mainEncounterId = "";
        public string farmEncounterId = "";
        public string[] gatherNodeIds = Array.Empty<string>();
        public string firstClearRewardId = "";
        public string repeatClearRewardId = "";
        public string farmRewardId = "";
        public int recommendedPower;
        public bool bossRegion;
    }

    [Serializable]
    public class EncounterEnemySlot
    {
        public string characterKey = "Asra";
        public int level = 1;
        public int weight = 1;
    }

    [Serializable]
    public class EncounterConfig
    {
        public string encounterId = "";
        public string displayName = "";
        public string displayNameZh = "";
        public string factionTag = "";
        public bool isBoss;
        public EncounterEnemySlot[] enemies = Array.Empty<EncounterEnemySlot>();
        public int lootScrap = 1;
        public string rewardTableId = "";
    }

    [Serializable]
    public class ShipModuleDef
    {
        public string moduleId = "";
        public string displayNameEn = "";
        public string displayNameZh = "";
        public string primaryStat = "range";
        public int scrapCostPerLevel = 5;
        public int creditCostPerLevel = 10;
        public int statPerLevel = 1;
    }

    /// <summary>UI/service view of one region for the current player.</summary>
    public sealed class RegionView
    {
        public RegionConfig Config;
        public RegionRuntimeState Runtime;
        public RegionProgressState Progress;
        public bool CanEnter;
        public bool FarmUnlocked;
        public List<string> BlockReasons = new List<string>();
        public List<string> ShipGaps = new List<string>();
    }

    public sealed class GateCheckResult
    {
        public bool Ok;
        public List<string> Missing = new List<string>();
    }

    public sealed class WorldCommandResult
    {
        public bool Success;
        public string Message = "";
        public bool WasFirstClear;
        public string RegionId = "";
        public string EncounterId = "";
        public static WorldCommandResult Ok(bool wasFirstClear = false, string regionId = "", string encounterId = "") =>
            new WorldCommandResult
            {
                Success = true,
                WasFirstClear = wasFirstClear,
                RegionId = regionId ?? "",
                EncounterId = encounterId ?? ""
            };
        public static WorldCommandResult Fail(string message) =>
            new WorldCommandResult { Success = false, Message = message ?? "" };
    }
}
