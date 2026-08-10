using System.Collections.Generic;

namespace Assets.Resources.Scripts.World.Domain
{
    /// <summary>Static MVP region graph (systems/03 §4.1–4.4). No Unity dependency.</summary>
    public static class RegionCatalog
    {
        private static List<RegionConfig> cached;

        public static IReadOnlyList<RegionConfig> All
        {
            get
            {
                cached ??= BuildDefaults();
                return cached;
            }
        }

        public static RegionConfig Get(string regionId)
        {
            if (string.IsNullOrEmpty(regionId)) return null;
            foreach (var r in All)
            {
                if (r.regionId == regionId)
                    return r;
            }

            return null;
        }

        public static void ReplaceAll(List<RegionConfig> regions)
        {
            cached = regions ?? BuildDefaults();
        }

        public static List<RegionConfig> BuildDefaults()
        {
            return new List<RegionConfig>
            {
                Region(WorldConstants.OuterBeltId, 0, "Outer Belt", "外缘带",
                    null, Gate(1, 0, 0, 0, 0, 0, 0, 0), "enc_outer_main", "enc_outer_farm", 80, false),
                Region(WorldConstants.MiningSpurId, 1, "Mining Spur", "矿脉支线",
                    new[] { WorldConstants.OuterBeltId }, Gate(1, 1, 0, 0, 0, 1, 0, 0),
                    "enc_mining_main", "enc_mining_farm", 120, false),
                Region(WorldConstants.QuantumRiftId, 2, "Quantum Rift", "量子裂隙",
                    new[] { WorldConstants.MiningSpurId }, Gate(2, 2, 2, 1, 2, 1, 1, 1),
                    "enc_rift_main", "enc_rift_farm", 200, false),
                Region(WorldConstants.AbyssalEdgeId, 3, "Abyssal Edge", "深渊边界",
                    new[] { WorldConstants.QuantumRiftId }, Gate(3, 3, 2, 3, 3, 2, 2, 2),
                    "enc_abyss_main", "enc_abyss_farm", 280, false),
                Region(WorldConstants.ConvoyLaneId, 4, "Convoy Lane", "护航航道",
                    new[] { WorldConstants.AbyssalEdgeId }, Gate(3, 4, 3, 3, 2, 3, 2, 2),
                    "enc_convoy_main", "enc_convoy_farm", 320, false),
                Region(WorldConstants.FrontierBossId, 5, "Frontier Anchor", "边境锚点",
                    new[] { WorldConstants.ConvoyLaneId }, Gate(4, 4, 4, 4, 4, 3, 3, 3),
                    "enc_frontier_boss", "enc_frontier_farm", 450, true)
            };
        }

        private static RegionConfig Region(
            string id, int sort, string en, string zh, string[] prereq, ShipGate gate,
            string mainEnc, string farmEnc, int power, bool boss) =>
            new RegionConfig
            {
                regionId = id,
                sectorId = WorldConstants.SectorId,
                sortOrder = sort,
                displayNameEn = en,
                displayNameZh = zh,
                prereqRegionIds = prereq ?? System.Array.Empty<string>(),
                shipGate = gate,
                mainEncounterId = mainEnc,
                farmEncounterId = farmEnc,
                recommendedPower = power,
                bossRegion = boss
            };

        private static ShipGate Gate(
            int level, int range, int energy, int hull, int entropy, int cargo, int scan, int life) =>
            new ShipGate
            {
                level = level,
                range = range,
                energy = energy,
                hull = hull,
                entropyResist = entropy,
                cargo = cargo,
                scan = scan,
                lifeSupport = life
            };
    }
}
