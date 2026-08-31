using System;
using System.Collections.Generic;

namespace Assets.Resources.Scripts.World.Domain
{
    public enum StellarBodyType
    {
        Planet = 0,
        Station = 1,
        Anomaly = 2,
        Hub = 3,
        Beacon = 4
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
        public int recommendedExpeditionLv = 1;
        public GridRouteTag route = GridRouteTag.None;
        public int danger = 1;
        public bool capitalHub;
    }

    /// <summary>
    /// Frontier VII in-sector map: bodies + inner lanes (military / industry / trade / rift).
    /// Universe-layer graph is M3+. Coordinates are sector-local (0–100).
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

        private static List<StellarBodyDef> cachedBodies;
        private static List<InnerEdgeDef> cachedEdges;

        public static IReadOnlyList<StellarBodyDef> Bodies
        {
            get
            {
                cachedBodies ??= BuildFrontierVii();
                return cachedBodies;
            }
        }

        public static IReadOnlyList<InnerEdgeDef> Edges
        {
            get
            {
                cachedEdges ??= BuildInnerEdges();
                return cachedEdges;
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

        public static InnerEdgeDef GetEdge(string edgeId)
        {
            if (string.IsNullOrEmpty(edgeId)) return null;
            foreach (var e in Edges)
            {
                if (e != null && e.edgeId == edgeId)
                    return e;
            }

            return null;
        }

        public static InnerEdgeDef FindEdge(string bodyA, string bodyB)
        {
            if (string.IsNullOrEmpty(bodyA) || string.IsNullOrEmpty(bodyB)) return null;
            foreach (var e in Edges)
            {
                if (e == null) continue;
                if ((e.a == bodyA && e.b == bodyB) || (e.a == bodyB && e.b == bodyA))
                    return e;
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
                StellarBodyType.Planet, WorldConstants.OuterBeltId, 0,
                recLv: 1, route: GridRouteTag.Trade, danger: 1),
            Body("body_mining_spur", 40f, 70f, "Mining Spur", "矿脉支线",
                StellarBodyType.Planet, WorldConstants.MiningSpurId, 3,
                recLv: 3, route: GridRouteTag.Industry, danger: 2),
            Body("body_convoy_lane", 30f, 56f, "Convoy Lane", "护航航道",
                StellarBodyType.Station, WorldConstants.ConvoyLaneId, 6,
                recLv: 5, route: GridRouteTag.Military, danger: 2),
            Body("body_quantum_rift", 58f, 60f, "Quantum Rift", "量子裂隙",
                StellarBodyType.Anomaly, WorldConstants.QuantumRiftId, 9,
                recLv: 8, route: GridRouteTag.Rift, danger: 4),
            Body("body_abyssal_edge", 50f, 38f, "Abyssal Edge", "深渊边界",
                StellarBodyType.Planet, WorldConstants.AbyssalEdgeId, 12,
                recLv: 10, route: GridRouteTag.Industry, danger: 3),
            Body("body_frontier_anchor", 74f, 46f, "Frontier Anchor", "边境锚点",
                StellarBodyType.Hub, WorldConstants.FrontierBossId, 15,
                recLv: 14, route: GridRouteTag.Military, danger: 5, capitalHub: true)
        };

        private static List<InnerEdgeDef> BuildInnerEdges() => new List<InnerEdgeDef>
        {
            Edge("lane_haven_mining", "body_outer_haven", "body_mining_spur", GridRouteTag.Industry, 0.12f),
            Edge("lane_haven_convoy", "body_outer_haven", "body_convoy_lane", GridRouteTag.Trade, 0.10f),
            Edge("lane_mining_convoy", "body_mining_spur", "body_convoy_lane", GridRouteTag.Military, 0.18f),
            Edge("lane_mining_abyss", "body_mining_spur", "body_abyssal_edge", GridRouteTag.Industry, 0.22f),
            Edge("lane_convoy_rift", "body_convoy_lane", "body_quantum_rift", GridRouteTag.Military, 0.28f),
            Edge("lane_rift_abyss", "body_quantum_rift", "body_abyssal_edge", GridRouteTag.Rift, 0.40f, rift: true),
            Edge("lane_rift_anchor", "body_quantum_rift", "body_frontier_anchor", GridRouteTag.Rift, 0.55f, rift: true),
            Edge("lane_abyss_anchor", "body_abyssal_edge", "body_frontier_anchor", GridRouteTag.Industry, 0.30f),
            Edge("lane_haven_rift", "body_outer_haven", "body_quantum_rift", GridRouteTag.Rift, 0.62f, rift: true)
        };

        private static StellarBodyDef Body(
            string id, float x, float y, string en, string zh,
            StellarBodyType type, string regionId, int sprite,
            int recLv, GridRouteTag route, int danger, bool capitalHub = false) =>
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
                planetSpriteIndex = sprite,
                recommendedExpeditionLv = recLv,
                route = route,
                danger = danger,
                capitalHub = capitalHub
            };

        private static InnerEdgeDef Edge(
            string id, string a, string b, GridRouteTag route, float entropy, bool rift = false) =>
            new InnerEdgeDef
            {
                edgeId = id,
                a = a,
                b = b,
                route = route,
                entropy = entropy,
                rift = rift
            };
    }
}
