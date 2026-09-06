using System.Collections.Generic;

namespace Assets.Resources.Scripts.Economy.Domain
{
    /// <summary>
    /// Automated production line blueprints, one per production chain (economy/15 §4.10.8 MVP+).
    /// Allowed recipes mirror <see cref="RecipeCatalog"/> chains; tier gates which ones are runnable.
    /// </summary>
    public static class ProductionLineCatalog
    {
        private static readonly Dictionary<string, ProductionLineDef> ById = new Dictionary<string, ProductionLineDef>();
        private static readonly List<ProductionLineDef> Ordered = new List<ProductionLineDef>();
        private static bool loaded;

        public static IReadOnlyList<ProductionLineDef> All
        {
            get
            {
                Ensure();
                return Ordered;
            }
        }

        public static ProductionLineDef Get(string lineId)
        {
            Ensure();
            if (string.IsNullOrEmpty(lineId)) return null;
            return ById.TryGetValue(lineId, out var d) ? d : null;
        }

        private static void Ensure()
        {
            if (loaded) return;
            loaded = true;

            Add(new ProductionLineDef
            {
                lineId = "line_metallurgy",
                lineFamilyId = "chain_metal",
                displayNameEn = "Metallurgy Line",
                displayNameZh = "冶金流水线",
                descriptionEn = "Automates the metal chain: ingots, armor frames and composite plating.",
                descriptionZh = "自动化金属链：精炼锭、装甲框架与复合装甲。",
                icon = "Steel",
                tierCap = 3,
                allowedRecipeIds = new[]
                {
                    "rcp_smelt_ingot", "rcp_forge_frame", "rcp_make_composite", "rcp_make_repair_kit"
                },
                buildCost = new[]
                {
                    new RecipeInput { itemDefId = "mat_iron_ore", quantity = 10, minQuality = 1 },
                    new RecipeInput { itemDefId = "mat_scrap", quantity = 5, minQuality = 1 }
                }
            });

            Add(new ProductionLineDef
            {
                lineId = "line_energy",
                lineFamilyId = "chain_energy",
                displayNameEn = "Reactor Line",
                displayNameZh = "反应堆流水线",
                descriptionEn = "Automates the energy chain: charged cores, reactor coils and pulse rifles.",
                descriptionZh = "自动化能源链：充能核心、反应线圈与脉冲步枪。",
                icon = "Reactor",
                tierCap = 3,
                allowedRecipeIds = new[]
                {
                    "rcp_charge_core", "rcp_wind_coil", "rcp_make_rifle"
                },
                buildCost = new[]
                {
                    new RecipeInput { itemDefId = "mat_crystal_sand", quantity = 10, minQuality = 1 },
                    new RecipeInput { itemDefId = "mat_energy_cell", quantity = 5, minQuality = 1 }
                }
            });

            Add(new ProductionLineDef
            {
                lineId = "line_synthesis",
                lineFamilyId = "chain_synth",
                displayNameEn = "Synthesis Line",
                displayNameZh = "合成流水线",
                descriptionEn = "Automates the synth chain: woven mesh, nano thread and synth cloaks.",
                descriptionZh = "自动化合成链：合成网、纳米丝与合成斗篷。",
                icon = "Synth",
                tierCap = 3,
                allowedRecipeIds = new[]
                {
                    "rcp_weave_mesh", "rcp_spin_thread", "rcp_make_cloak"
                },
                buildCost = new[]
                {
                    new RecipeInput { itemDefId = "mat_fungal", quantity = 10, minQuality = 1 },
                    new RecipeInput { itemDefId = "mat_biofiber", quantity = 5, minQuality = 1 }
                }
            });
        }

        private static void Add(ProductionLineDef d)
        {
            ById[d.lineId] = d;
            Ordered.Add(d);
        }
    }
}
