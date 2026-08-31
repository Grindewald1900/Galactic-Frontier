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
        public string factionTag = "";
        public string resourceProfile = "";
        public int difficulty;
    }

    /// <summary>
    /// Universe-layer graph: seeded procedural sectors (doc 23) with pinned Frontier VII + Core.
    /// </summary>
    public static class UniverseMapCatalog
    {
        public const float MapMin = 0f;
        public const float MapMax = 100f;

        private static int configuredSeed;
        private static GeneratedUniverse cachedUniverse;
        private static List<SectorNodeDef> cachedSectors;
        private static List<InnerEdgeDef> cachedEdges;

        public static int ActiveSeed => configuredSeed;

        public static GeneratedUniverse ActiveUniverse => cachedUniverse;

        public static IReadOnlyList<SectorNodeDef> Sectors
        {
            get
            {
                EnsureConfigured();
                return cachedSectors;
            }
        }

        public static IReadOnlyList<InnerEdgeDef> Edges
        {
            get
            {
                EnsureConfigured();
                return cachedEdges;
            }
        }

        public static void Configure(int seed, PlaneModifiersState plane = null, UniverseGenOptions options = null)
        {
            if (seed == 0)
                seed = 1;
            if (configuredSeed == seed && cachedUniverse != null)
            {
                if (plane != null)
                    cachedUniverse.planeModifiers = plane;
                return;
            }

            configuredSeed = seed;
            cachedUniverse = UniverseGenerator.Generate(seed, options ?? new UniverseGenOptions());
            if (plane != null)
                cachedUniverse.planeModifiers = plane;

            cachedSectors = MapSectors(cachedUniverse);
            cachedEdges = MapEdges(cachedUniverse);
        }

        public static void ResetCache()
        {
            configuredSeed = 0;
            cachedUniverse = null;
            cachedSectors = null;
            cachedEdges = null;
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

        private static void EnsureConfigured()
        {
            if (cachedSectors != null && cachedEdges != null)
                return;
            Configure(UniverseSeedUtil.DefaultServerSeed ^ UniverseSeedUtil.DefaultSeasonId);
        }

        private static List<SectorNodeDef> MapSectors(GeneratedUniverse universe)
        {
            var list = new List<SectorNodeDef>();
            if (universe?.sectors == null) return list;
            foreach (var s in universe.sectors)
            {
                if (s == null) continue;
                RingLabels(s.ring, out var ringEn, out var ringZh);
                list.Add(new SectorNodeDef
                {
                    sectorId = s.sectorId,
                    x = s.x,
                    y = s.y,
                    displayNameEn = s.displayNameEn,
                    displayNameZh = s.displayNameZh,
                    ringEn = ringEn,
                    ringZh = ringZh,
                    playable = s.playable,
                    recommendedExpeditionLv = s.recommendedExpeditionLv,
                    danger = s.danger,
                    spriteIndex = s.spriteIndex,
                    factionTag = s.factionTag ?? "",
                    resourceProfile = s.resourceProfile ?? "",
                    difficulty = s.difficulty
                });
            }

            return list;
        }

        private static List<InnerEdgeDef> MapEdges(GeneratedUniverse universe)
        {
            var list = new List<InnerEdgeDef>();
            if (universe?.routes == null) return list;
            foreach (var r in universe.routes)
            {
                if (r == null) continue;
                list.Add(new InnerEdgeDef
                {
                    edgeId = r.edgeId,
                    a = r.fromId,
                    b = r.toId,
                    route = r.route,
                    entropy = r.entropy,
                    rift = r.rift || r.wormhole
                });
            }

            return list;
        }

        private static void RingLabels(UniverseRingId ring, out string en, out string zh)
        {
            switch (ring)
            {
                case UniverseRingId.OuterRim:
                    en = "Outer Rim"; zh = "外缘带"; return;
                case UniverseRingId.Pioneer:
                    en = "Pioneer"; zh = "开拓带"; return;
                case UniverseRingId.Severance:
                    en = "Severance"; zh = "断航带"; return;
                case UniverseRingId.Plane:
                    en = "Plane"; zh = "位面带"; return;
                case UniverseRingId.Inner:
                    en = "Inner"; zh = "内环带"; return;
                case UniverseRingId.Core:
                    en = "Core"; zh = "中枢区"; return;
                default:
                    en = "Unknown"; zh = "未知"; return;
            }
        }
    }
}
