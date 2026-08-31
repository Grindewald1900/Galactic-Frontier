using System;
using System.Collections.Generic;

namespace Assets.Resources.Scripts.World.Domain
{
    [Serializable]
    public class SectorNodeDef
    {
        public string sectorId = "";
        public float x;
        public float y;
        public string displayNameEn = "";
        public string displayNameZh = "";
        public string ringEn = "";
        public string ringZh = "";
        public bool playable;
        public int recommendedExpeditionLv = 1;
        public int danger = 1;
        public int spriteIndex;
    }

    /// <summary>
    /// Universe-layer graph: each node is a sector. Only Frontier VII is playable in Solo.
    /// </summary>
    public static class UniverseMapCatalog
    {
        public const float MapMin = 0f;
        public const float MapMax = 100f;

        private static List<SectorNodeDef> cachedSectors;
        private static List<InnerEdgeDef> cachedEdges;

        public static IReadOnlyList<SectorNodeDef> Sectors
        {
            get
            {
                cachedSectors ??= BuildSectors();
                return cachedSectors;
            }
        }

        public static IReadOnlyList<InnerEdgeDef> Edges
        {
            get
            {
                cachedEdges ??= BuildEdges();
                return cachedEdges;
            }
        }

        public static SectorNodeDef Get(string sectorId)
        {
            if (string.IsNullOrEmpty(sectorId)) return null;
            foreach (var s in Sectors)
            {
                if (s != null && s.sectorId == sectorId)
                    return s;
            }

            return null;
        }

        public static InnerEdgeDef FindEdge(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return null;
            foreach (var e in Edges)
            {
                if (e == null) continue;
                if ((e.a == a && e.b == b) || (e.a == b && e.b == a))
                    return e;
            }

            return null;
        }

        private static List<SectorNodeDef> BuildSectors() => new List<SectorNodeDef>
        {
            Sector(WorldConstants.SectorId, 18f, 82f, "Frontier VII", "第七前沿",
                "Outer Rim", "外缘带", playable: true, recLv: 1, danger: 1, sprite: 0),
            Sector(WorldConstants.SectorMiningId, 40f, 70f, "Mining Belt", "矿业星域",
                "Pioneer", "开拓带", playable: false, recLv: 12, danger: 2, sprite: 3),
            Sector(WorldConstants.SectorRelicId, 22f, 52f, "Relic Reach", "遗迹星域",
                "Pioneer", "开拓带", playable: false, recLv: 14, danger: 3, sprite: 9),
            Sector(WorldConstants.SectorFaultId, 50f, 46f, "Entropy Fault", "熵雾断层",
                "Severance", "断航带", playable: false, recLv: 20, danger: 4, sprite: 12),
            Sector(WorldConstants.SectorTradeId, 70f, 66f, "Trade Relay", "商贸中继",
                "Plane", "位面带", playable: false, recLv: 18, danger: 2, sprite: 6),
            Sector(WorldConstants.SectorInnerId, 72f, 34f, "Inner Ring", "内环星域",
                "Inner", "内环带", playable: false, recLv: 28, danger: 4, sprite: 15),
            Sector(WorldConstants.SectorCoreId, 88f, 16f, "Astral Core", "中枢区域",
                "Core", "中枢区", playable: false, recLv: 40, danger: 5, sprite: 15)
        };

        private static List<InnerEdgeDef> BuildEdges() => new List<InnerEdgeDef>
        {
            Edge("ulane_vii_mining", WorldConstants.SectorId, WorldConstants.SectorMiningId, GridRouteTag.Industry, 0.18f),
            Edge("ulane_vii_relic", WorldConstants.SectorId, WorldConstants.SectorRelicId, GridRouteTag.Military, 0.22f),
            Edge("ulane_mining_fault", WorldConstants.SectorMiningId, WorldConstants.SectorFaultId, GridRouteTag.Rift, 0.40f, rift: true),
            Edge("ulane_relic_fault", WorldConstants.SectorRelicId, WorldConstants.SectorFaultId, GridRouteTag.Rift, 0.38f, rift: true),
            Edge("ulane_mining_trade", WorldConstants.SectorMiningId, WorldConstants.SectorTradeId, GridRouteTag.Trade, 0.20f),
            Edge("ulane_trade_inner", WorldConstants.SectorTradeId, WorldConstants.SectorInnerId, GridRouteTag.Trade, 0.28f),
            Edge("ulane_fault_inner", WorldConstants.SectorFaultId, WorldConstants.SectorInnerId, GridRouteTag.Military, 0.35f),
            Edge("ulane_inner_core", WorldConstants.SectorInnerId, WorldConstants.SectorCoreId, GridRouteTag.Military, 0.55f)
        };

        private static SectorNodeDef Sector(
            string id, float x, float y, string en, string zh,
            string ringEn, string ringZh, bool playable, int recLv, int danger, int sprite) =>
            new SectorNodeDef
            {
                sectorId = id,
                x = x,
                y = y,
                displayNameEn = en,
                displayNameZh = zh,
                ringEn = ringEn,
                ringZh = ringZh,
                playable = playable,
                recommendedExpeditionLv = recLv,
                danger = danger,
                spriteIndex = sprite
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
