using System;
using System.Collections.Generic;

namespace Assets.Resources.Scripts.World.Domain
{
    public enum StellarBodyType
    {
        Planet = 0,
        Station = 1,
        Anomaly = 2,
        Hub = 3
    }

    [Serializable]
    public class StellarBodyDef
    {
        public string bodyId = "";
        public string sectorId = WorldConstants.SectorId;
        public float x;
        public float y;
        public string displayNameEn = "";
        public string displayNameZh = "";
        public StellarBodyType bodyType = StellarBodyType.Planet;
        public string[] regionIds = Array.Empty<string>();
        public int planetSpriteIndex;
    }

    /// <summary>
    /// M1 layout: Frontier VII as one dense cluster of bodies (one region per body).
    /// Coordinates are sector-local (0–100). Ship spawns at the outer edge.
    /// </summary>
    public static class SectorMapCatalog
    {
        public const float MapMin = 0f;
        public const float MapMax = 100f;
        public const float SectorCenterX = 48f;
        public const float SectorCenterY = 55f;
        public const float SectorRadius = 42f;
        public const float SpawnX = 12f;
        public const float SpawnY = 88f;
        public const float DockingRange = 10f;

        private static List<StellarBodyDef> cached;

        public static IReadOnlyList<StellarBodyDef> Bodies
        {
            get
            {
                cached ??= BuildFrontierVii();
                return cached;
            }
        }

        public static StellarBodyDef Get(string bodyId)
        {
            if (string.IsNullOrEmpty(bodyId)) return null;
            foreach (var b in Bodies)
            {
                if (b != null && b.bodyId == bodyId)
                    return b;
            }

            return null;
        }

        public static StellarBodyDef FindByRegion(string regionId)
        {
            if (string.IsNullOrEmpty(regionId)) return null;
            foreach (var b in Bodies)
            {
                if (b?.regionIds == null) continue;
                foreach (var id in b.regionIds)
                {
                    if (id == regionId)
                        return b;
                }
            }

            return null;
        }

        private static List<StellarBodyDef> BuildFrontierVii() => new List<StellarBodyDef>
        {
            Body("body_outer_haven", 22f, 78f, "Outer Haven", "外缘港",
                StellarBodyType.Planet, WorldConstants.OuterBeltId, 0),
            Body("body_mining_spur", 40f, 70f, "Mining Spur", "矿脉支线",
                StellarBodyType.Planet, WorldConstants.MiningSpurId, 3),
            Body("body_convoy_lane", 30f, 56f, "Convoy Lane", "护航航道",
                StellarBodyType.Station, WorldConstants.ConvoyLaneId, 6),
            Body("body_quantum_rift", 58f, 60f, "Quantum Rift", "量子裂隙",
                StellarBodyType.Anomaly, WorldConstants.QuantumRiftId, 9),
            Body("body_abyssal_edge", 50f, 38f, "Abyssal Edge", "深渊边界",
                StellarBodyType.Planet, WorldConstants.AbyssalEdgeId, 12),
            Body("body_frontier_anchor", 74f, 46f, "Frontier Anchor", "边境锚点",
                StellarBodyType.Hub, WorldConstants.FrontierBossId, 15)
        };

        private static StellarBodyDef Body(
            string id, float x, float y, string en, string zh,
            StellarBodyType type, string regionId, int sprite) =>
            new StellarBodyDef
            {
                bodyId = id,
                sectorId = WorldConstants.SectorId,
                x = x,
                y = y,
                displayNameEn = en,
                displayNameZh = zh,
                bodyType = type,
                regionIds = new[] { regionId },
                planetSpriteIndex = sprite
            };
    }
}
