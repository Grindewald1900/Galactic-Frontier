using System.Collections.Generic;

namespace Assets.Resources.Scripts.World.Domain
{
    /// <summary>Static MVP region graph (systems/03 §4.1–4.4 + systems/11 P5.3).</summary>
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
                    "Clear the junk belt for your first scrap haul.", "清理航道垃圾带，取得第一桶金。",
                    FactionTags.FrontierGuard,
                    null, Gate(1, 0, 0, 0, 0, 0, 0, 0),
                    "enc_outer_main", "enc_outer_farm",
                    new[] { "node_outer_iron" },
                    "reward_outer_first", "reward_outer_repeat", "reward_outer_farm",
                    80, false),
                Region(WorldConstants.MiningSpurId, 1, "Mining Spur", "矿脉支线",
                    "Dual harvest: crystal sand and fungal mats.", "双原料区：晶砂与菌毯。",
                    FactionTags.FrontierGuard,
                    new[] { WorldConstants.OuterBeltId }, Gate(1, 1, 0, 0, 0, 1, 0, 0),
                    "enc_mining_main", "enc_mining_farm",
                    new[] { "node_spur_crystal", "node_spur_fungal" },
                    "reward_mining_first", "reward_mining_repeat", "reward_mining_farm",
                    120, false),
                Region(WorldConstants.QuantumRiftId, 2, "Quantum Rift", "量子裂隙",
                    "Unstable rift — energy pressure and entropy fog.", "不稳定裂隙，能源与熵雾加压。",
                    FactionTags.RiftSyndicate,
                    new[] { WorldConstants.MiningSpurId }, Gate(2, 2, 2, 1, 2, 1, 1, 1),
                    "enc_rift_main", "enc_rift_farm",
                    new[] { "node_rift_energy" },
                    "reward_rift_first", "reward_rift_repeat", "reward_rift_farm",
                    200, false),
                Region(WorldConstants.AbyssalEdgeId, 3, "Abyssal Edge", "深渊边界",
                    "High pressure combat; repair kits become essential.", "高压战区，维修包成为刚需。",
                    FactionTags.RiftSyndicate,
                    new[] { WorldConstants.QuantumRiftId }, Gate(3, 3, 2, 3, 3, 2, 2, 2),
                    "enc_abyss_main", "enc_abyss_farm",
                    new[] { "node_abyss_scrap" },
                    "reward_abyss_first", "reward_abyss_repeat", "reward_abyss_farm",
                    280, false),
                Region(WorldConstants.ConvoyLaneId, 4, "Convoy Lane", "护航航道",
                    "Logistics sprint before the frontier assault.", "进攻边境锚点前的后勤冲刺。",
                    FactionTags.FrontierGuard,
                    new[] { WorldConstants.AbyssalEdgeId }, Gate(3, 4, 3, 3, 2, 3, 2, 2),
                    "enc_convoy_main", "enc_convoy_farm",
                    new[] { "node_convoy_bio" },
                    "reward_convoy_first", "reward_convoy_repeat", "reward_convoy_farm",
                    320, false),
                Region(WorldConstants.FrontierBossId, 5, "Frontier Anchor", "边境锚点",
                    "Sector apex threat. Clear it to finish Frontier VII.", "星域顶点威胁。击杀即完成第七前沿首圈。",
                    FactionTags.RiftSyndicate,
                    new[] { WorldConstants.ConvoyLaneId }, Gate(4, 4, 4, 4, 4, 3, 3, 3),
                    "enc_frontier_boss", "enc_frontier_farm",
                    System.Array.Empty<string>(),
                    "reward_boss_first", "reward_boss_repeat", "reward_frontier_farm",
                    450, true)
            };
        }

        private static RegionConfig Region(
            string id, int sort, string en, string zh, string blurbEn, string blurbZh, string faction,
            string[] prereq, ShipGate gate,
            string mainEnc, string farmEnc, string[] gatherNodes,
            string firstReward, string repeatReward, string farmReward,
            int power, bool boss) =>
            new RegionConfig
            {
                regionId = id,
                sectorId = WorldConstants.SectorId,
                sortOrder = sort,
                displayNameEn = en,
                displayNameZh = zh,
                blurbEn = blurbEn,
                blurbZh = blurbZh,
                factionTag = faction ?? "",
                prereqRegionIds = prereq ?? System.Array.Empty<string>(),
                shipGate = gate,
                mainEncounterId = mainEnc,
                farmEncounterId = farmEnc,
                gatherNodeIds = gatherNodes ?? System.Array.Empty<string>(),
                firstClearRewardId = firstReward,
                repeatClearRewardId = repeatReward,
                farmRewardId = farmReward,
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
