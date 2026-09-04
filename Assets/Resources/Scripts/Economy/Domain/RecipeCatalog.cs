using System.Collections.Generic;
using Assets.Resources.Scripts.Progression.Domain;

namespace Assets.Resources.Scripts.Economy.Domain
{
    /// <summary>Three production chains: metal, energy, synth (systems/05 §4.2).</summary>
    public static class RecipeCatalog
    {
        private static readonly Dictionary<string, RecipeDef> ById = new Dictionary<string, RecipeDef>();
        private static readonly List<RecipeDef> Ordered = new List<RecipeDef>();
        private static bool loaded;

        public static IReadOnlyList<RecipeDef> All
        {
            get
            {
                Ensure();
                return Ordered;
            }
        }

        public static RecipeDef Get(string recipeId)
        {
            Ensure();
            if (string.IsNullOrEmpty(recipeId)) return null;
            return ById.TryGetValue(recipeId, out var r) ? r : null;
        }

        private static void Ensure()
        {
            if (loaded) return;
            loaded = true;

            // chain_metal
            Add(new RecipeDef
            {
                recipeId = "rcp_smelt_ingot",
                chainId = "chain_metal",
                displayNameEn = "Smelt Ingot",
                displayNameZh = "精炼锭",
                kind = RecipeKind.Process,
                inputs = new[]
                {
                    new RecipeInput { itemDefId = "mat_iron_ore", quantity = 3, minQuality = 1 },
                    new RecipeInput { itemDefId = "mat_scrap", quantity = 1, minQuality = 1 }
                },
                outputDefId = "int_refined_ingot",
                outputQty = 1,
                facilityModuleId = "mod_armor",
                cycleSeconds = EconomyConstants.CraftCycleSeconds
            });
            Add(new RecipeDef
            {
                recipeId = "rcp_forge_frame",
                chainId = "chain_metal",
                displayNameEn = "Forge Armor Frame",
                displayNameZh = "锻造装甲框架",
                kind = RecipeKind.Process,
                inputs = new[]
                {
                    new RecipeInput { itemDefId = "int_refined_ingot", quantity = 2, minQuality = 1 },
                    new RecipeInput { itemDefId = "mat_alloy_plate", quantity = 1, minQuality = 1 }
                },
                outputDefId = "int_armor_frame",
                outputQty = 1,
                facilityModuleId = "mod_armor",
                cycleSeconds = EconomyConstants.CraftCycleSeconds,
                requiredProfession = ProfessionSkill.Craft,
                requiredSkillLevel = 5
            });
            Add(new RecipeDef
            {
                recipeId = "rcp_make_composite",
                chainId = "chain_metal",
                displayNameEn = "Assemble Composite Armor",
                displayNameZh = "组装复合装甲",
                kind = RecipeKind.Manufacture,
                inputs = new[]
                {
                    new RecipeInput { itemDefId = "int_armor_frame", quantity = 1, minQuality = 1 },
                    new RecipeInput { itemDefId = "mat_alloy_plate", quantity = 2, minQuality = 1 }
                },
                outputDefId = "eq_composite_armor",
                outputQty = 1,
                outputIsEquipment = true,
                facilityModuleId = "mod_armor",
                cycleSeconds = EconomyConstants.CraftCycleSeconds,
                requiredProfession = ProfessionSkill.Craft,
                requiredSkillLevel = 10
            });

            // chain_energy
            Add(new RecipeDef
            {
                recipeId = "rcp_charge_core",
                chainId = "chain_energy",
                displayNameEn = "Charge Core",
                displayNameZh = "充能核心",
                kind = RecipeKind.Process,
                inputs = new[]
                {
                    new RecipeInput { itemDefId = "mat_crystal_sand", quantity = 3, minQuality = 1 },
                    new RecipeInput { itemDefId = "mat_energy_cell", quantity = 1, minQuality = 1 }
                },
                outputDefId = "int_charged_core",
                outputQty = 1,
                facilityModuleId = "mod_reactor",
                cycleSeconds = EconomyConstants.CraftCycleSeconds
            });
            Add(new RecipeDef
            {
                recipeId = "rcp_wind_coil",
                chainId = "chain_energy",
                displayNameEn = "Wind Reactor Coil",
                displayNameZh = "绕制反应线圈",
                kind = RecipeKind.Process,
                inputs = new[]
                {
                    new RecipeInput { itemDefId = "int_charged_core", quantity = 2, minQuality = 1 },
                    new RecipeInput { itemDefId = "mat_scrap", quantity = 2, minQuality = 1 }
                },
                outputDefId = "int_reactor_coil",
                outputQty = 1,
                facilityModuleId = "mod_reactor",
                cycleSeconds = EconomyConstants.CraftCycleSeconds,
                requiredProfession = ProfessionSkill.Craft,
                requiredSkillLevel = 5
            });
            Add(new RecipeDef
            {
                recipeId = "rcp_make_rifle",
                chainId = "chain_energy",
                displayNameEn = "Assemble Pulse Rifle",
                displayNameZh = "组装脉冲步枪",
                kind = RecipeKind.Manufacture,
                inputs = new[]
                {
                    new RecipeInput { itemDefId = "int_reactor_coil", quantity = 1, minQuality = 1 },
                    new RecipeInput { itemDefId = "mat_alloy_plate", quantity = 1, minQuality = 1 }
                },
                outputDefId = "eq_pulse_rifle",
                outputQty = 1,
                outputIsEquipment = true,
                facilityModuleId = "mod_reactor",
                cycleSeconds = EconomyConstants.CraftCycleSeconds,
                requiredProfession = ProfessionSkill.Craft,
                requiredSkillLevel = 10
            });

            // chain_synth
            Add(new RecipeDef
            {
                recipeId = "rcp_weave_mesh",
                chainId = "chain_synth",
                displayNameEn = "Weave Synth Mesh",
                displayNameZh = "编织合成网",
                kind = RecipeKind.Process,
                inputs = new[]
                {
                    new RecipeInput { itemDefId = "mat_fungal", quantity = 3, minQuality = 1 },
                    new RecipeInput { itemDefId = "mat_biofiber", quantity = 1, minQuality = 1 }
                },
                outputDefId = "int_synth_mesh",
                outputQty = 1,
                facilityModuleId = "mod_synth",
                cycleSeconds = EconomyConstants.CraftCycleSeconds
            });
            Add(new RecipeDef
            {
                recipeId = "rcp_spin_thread",
                chainId = "chain_synth",
                displayNameEn = "Spin Nano Thread",
                displayNameZh = "抽丝纳米丝",
                kind = RecipeKind.Process,
                inputs = new[]
                {
                    new RecipeInput { itemDefId = "int_synth_mesh", quantity = 2, minQuality = 1 },
                    new RecipeInput { itemDefId = "mat_crystal_sand", quantity = 1, minQuality = 1 }
                },
                outputDefId = "int_nano_thread",
                outputQty = 1,
                facilityModuleId = "mod_synth",
                cycleSeconds = EconomyConstants.CraftCycleSeconds,
                requiredProfession = ProfessionSkill.Craft,
                requiredSkillLevel = 5
            });
            Add(new RecipeDef
            {
                recipeId = "rcp_make_cloak",
                chainId = "chain_synth",
                displayNameEn = "Tailor Synth Cloak",
                displayNameZh = "裁制合成斗篷",
                kind = RecipeKind.Manufacture,
                inputs = new[]
                {
                    new RecipeInput { itemDefId = "int_nano_thread", quantity = 1, minQuality = 1 },
                    new RecipeInput { itemDefId = "mat_biofiber", quantity = 2, minQuality = 1 }
                },
                outputDefId = "eq_synth_cloak",
                outputQty = 1,
                outputIsEquipment = true,
                facilityModuleId = "mod_synth",
                cycleSeconds = EconomyConstants.CraftCycleSeconds,
                requiredProfession = ProfessionSkill.Craft,
                requiredSkillLevel = 10
            });

            // Repair kit from scrap + parts
            Add(new RecipeDef
            {
                recipeId = "rcp_make_repair_kit",
                chainId = "chain_metal",
                displayNameEn = "Assemble Repair Kit",
                displayNameZh = "组装维修包",
                kind = RecipeKind.Manufacture,
                inputs = new[]
                {
                    new RecipeInput { itemDefId = "mat_scrap", quantity = 5, minQuality = 1 },
                    new RecipeInput { itemDefId = "mat_repair_parts", quantity = 1, minQuality = 1 }
                },
                outputDefId = "con_repair_kit",
                outputQty = 1,
                facilityModuleId = "mod_armor",
                cycleSeconds = EconomyConstants.CraftCycleSeconds
            });
        }

        private static void Add(RecipeDef r)
        {
            ById[r.recipeId] = r;
            Ordered.Add(r);
        }
    }
}
