using System.Collections.Generic;

namespace Assets.Resources.Scripts.Economy.Domain
{
    public static class GatherNodeCatalog
    {
        private static readonly Dictionary<string, GatherNodeDef> ById = new Dictionary<string, GatherNodeDef>();
        private static readonly List<GatherNodeDef> Ordered = new List<GatherNodeDef>();
        private static bool loaded;

        public static IReadOnlyList<GatherNodeDef> All
        {
            get
            {
                Ensure();
                return Ordered;
            }
        }

        public static GatherNodeDef Get(string nodeId)
        {
            Ensure();
            if (string.IsNullOrEmpty(nodeId)) return null;
            return ById.TryGetValue(nodeId, out var n) ? n : null;
        }

        public static GatherNodeDef FindForRegion(string regionId)
        {
            Ensure();
            if (string.IsNullOrEmpty(regionId)) return null;
            foreach (var n in Ordered)
            {
                if (n != null && n.regionId == regionId)
                    return n;
            }

            return null;
        }

        public static System.Collections.Generic.List<GatherNodeDef> ForRegion(string regionId)
        {
            Ensure();
            var list = new List<GatherNodeDef>();
            if (string.IsNullOrEmpty(regionId)) return list;
            foreach (var n in Ordered)
            {
                if (n != null && n.regionId == regionId)
                    list.Add(n);
            }

            return list;
        }

        private static void Ensure()
        {
            if (loaded) return;
            loaded = true;
            Add(new GatherNodeDef
            {
                nodeId = "node_outer_iron",
                regionId = "sec01_outer_belt",
                displayNameEn = "Outer Belt Iron",
                displayNameZh = "外带铁矿",
                outputDefId = "mat_iron_ore",
                outputQty = 2,
                outputQuality = EconomyConstants.DefaultQuality,
                riskLevel = 1
            });
            Add(new GatherNodeDef
            {
                nodeId = "node_spur_crystal",
                regionId = "sec01_mining_spur",
                displayNameEn = "Spur Crystal Sand",
                displayNameZh = "矿刺晶砂",
                outputDefId = "mat_crystal_sand",
                outputQty = 2,
                outputQuality = EconomyConstants.DefaultQuality,
                riskLevel = 2
            });
            Add(new GatherNodeDef
            {
                nodeId = "node_spur_fungal",
                regionId = "sec01_mining_spur",
                displayNameEn = "Spur Fungal Mat",
                displayNameZh = "矿刺菌毯",
                outputDefId = "mat_fungal",
                outputQty = 2,
                outputQuality = EconomyConstants.DefaultQuality,
                riskLevel = 2
            });
        }

        private static void Add(GatherNodeDef n)
        {
            ById[n.nodeId] = n;
            Ordered.Add(n);
        }
    }
}
