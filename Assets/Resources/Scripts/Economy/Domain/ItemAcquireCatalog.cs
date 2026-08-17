using System;
using System.Collections.Generic;

namespace Assets.Resources.Scripts.Economy.Domain
{
    /// <summary>How a catalog item can be obtained in the MVP loop.</summary>
    /// <remarks>Used by warehouse / upgrade hover tips for jump links.</remarks>
    public enum ItemAcquireKind
    {
        Gather = 0,
        Craft = 1,
        Combat = 2,
        Market = 3,
        Recruit = 4,
        Mission = 5
    }

    [Serializable]
    public class ItemAcquireSourceDef
    {
        public string sourceId = "";
        public string itemDefId = "";
        public ItemAcquireKind kind = ItemAcquireKind.Market;
        public string labelEn = "";
        public string labelZh = "";
        /// <summary>Explore / gather gate region when relevant.</summary>
        public string regionId = "";
        /// <summary>Crafting recipe when <see cref="kind"/> is Craft.</summary>
        public string recipeId = "";
        /// <summary>App shell target: Battle, Crafting, Market, Recruit, Missions.</summary>
        public string targetScreen = "";
    }

    /// <summary>Content map: itemDefId → acquisition channels (hover tip / upgrade costs).</summary>
    public static class ItemAcquireCatalog
    {
        private static readonly Dictionary<string, List<ItemAcquireSourceDef>> ByItem =
            new Dictionary<string, List<ItemAcquireSourceDef>>();
        private static bool loaded;

        public static IReadOnlyList<ItemAcquireSourceDef> ForItem(string itemDefId)
        {
            Ensure();
            if (string.IsNullOrEmpty(itemDefId) || !ByItem.TryGetValue(itemDefId, out var list))
                return Array.Empty<ItemAcquireSourceDef>();
            return list;
        }

        private static void Ensure()
        {
            if (loaded) return;
            loaded = true;

            // Materials
            Add("mat_scrap",
                Gather("src_scrap_abyss", "Abyssal Scrap Field", "深渊废料场采集", "sec01_abyssal_edge", "node_abyss_scrap"),
                Combat("src_scrap_combat", "Combat salvage", "战斗打捞", "sec01_outer_belt"),
                Market("src_scrap_shop", "Starport trade", "星港交易"));
            Add("mat_iron_ore",
                Gather("src_iron_gather", "Outer Belt Iron", "外带铁矿采集", "sec01_outer_belt", "node_outer_iron"));
            Add("mat_crystal_sand",
                Gather("src_crystal_gather", "Spur Crystal Sand", "矿刺晶砂采集", "sec01_mining_spur", "node_spur_crystal"));
            Add("mat_fungal",
                Gather("src_fungal_gather", "Spur Fungal Mat", "矿刺菌毯采集", "sec01_mining_spur", "node_spur_fungal"));
            Add("mat_alloy_plate",
                Combat("src_alloy_combat", "Combat drop", "战斗掉落", "sec01_mining_spur"),
                Market("src_alloy_shop", "Starport trade", "星港交易"));
            Add("mat_energy_cell",
                Gather("src_energy_gather", "Rift Energy Cell", "裂隙能量芯采集", "sec01_quantum_rift", "node_rift_energy"),
                Market("src_energy_shop", "Starport trade", "星港交易"));
            Add("mat_biofiber",
                Gather("src_bio_gather", "Convoy Biofiber Cache", "护航生物纤维库", "sec01_convoy_lane", "node_convoy_bio"),
                Market("src_bio_shop", "Starport trade", "星港交易"));
            Add("mat_repair_parts",
                Combat("src_parts_combat", "Combat salvage", "战斗打捞", "sec01_outer_belt"),
                Market("src_parts_shop", "Starport trade", "星港交易"));

            // Intermediates → craft
            Add("int_refined_ingot", Craft("src_ingot", "Smelt Ingot", "精炼锭配方", "rcp_smelt_ingot"));
            Add("int_armor_frame", Craft("src_frame", "Forge Armor Frame", "锻造装甲框架", "rcp_forge_frame"));
            Add("int_charged_core", Craft("src_core", "Charge Core", "充能核心配方", "rcp_charge_core"));
            Add("int_reactor_coil", Craft("src_coil", "Wind Reactor Coil", "绕制反应线圈", "rcp_wind_coil"));
            Add("int_synth_mesh", Craft("src_mesh", "Weave Synth Mesh", "编织合成网", "rcp_weave_mesh"));
            Add("int_nano_thread", Craft("src_thread", "Spin Nano Thread", "抽丝纳米丝", "rcp_spin_thread"));

            // Consumables
            Add("con_repair_kit",
                Craft("src_kit_craft", "Assemble Repair Kit", "组装维修包", "rcp_make_repair_kit"),
                Market("src_kit_shop", "Starport trade", "星港交易"));
            Add("con_field_ration", Market("src_ration_shop", "Starport trade", "星港交易"));
            Add("con_stim", Market("src_stim_shop", "Starport trade", "星港交易"));
            Add("con_nano_paste", Market("src_paste_shop", "Starport trade", "星港交易"));
            Add("con_recruit_ticket",
                Market("src_ticket_shop", "Starport trade", "星港交易"),
                Mission("src_ticket_mission", "Mission rewards", "任务奖励"));

            // Equipment
            Add("eq_pulse_rifle", Craft("src_rifle", "Assemble Pulse Rifle", "组装脉冲步枪", "rcp_make_rifle"));
            Add("eq_plasma_blade", Combat("src_blade_combat", "Combat drop", "战斗掉落", "sec01_abyssal_edge"));
            Add("eq_composite_armor", Craft("src_armor", "Assemble Composite Armor", "组装复合装甲", "rcp_make_composite"));
            Add("eq_shield_vest", Combat("src_vest_combat", "Combat drop", "战斗掉落", "sec01_quantum_rift"));
            Add("eq_scope", Market("src_scope_shop", "Starport trade", "星港交易"));
            Add("eq_gather_drill", Market("src_drill_shop", "Starport trade", "星港交易"));
            Add("eq_energy_pack", Market("src_pack_shop", "Starport trade", "星港交易"));
            Add("eq_synth_cloak", Craft("src_cloak", "Tailor Synth Cloak", "裁制合成斗篷", "rcp_make_cloak"));

            // Ship module items (warehouse instances)
            Add("mod_armor", Craft("src_mod_armor", "Armor workshop", "装甲工坊制造", "rcp_make_composite"));
            Add("mod_reactor", Market("src_mod_reactor", "Starport trade", "星港交易"));
            Add("mod_cargo", Market("src_mod_cargo", "Starport trade", "星港交易"));
            Add("mod_synth", Market("src_mod_synth", "Starport trade", "星港交易"));
        }

        private static void Add(string itemDefId, params ItemAcquireSourceDef[] sources)
        {
            var list = new List<ItemAcquireSourceDef>();
            foreach (var s in sources)
            {
                if (s == null) continue;
                s.itemDefId = itemDefId;
                list.Add(s);
            }

            ByItem[itemDefId] = list;
        }

        private static ItemAcquireSourceDef Gather(
            string id, string en, string zh, string regionId, string nodeId) =>
            new ItemAcquireSourceDef
            {
                sourceId = id,
                kind = ItemAcquireKind.Gather,
                labelEn = en,
                labelZh = zh,
                regionId = regionId,
                recipeId = nodeId,
                targetScreen = "Battle"
            };

        private static ItemAcquireSourceDef Craft(string id, string en, string zh, string recipeId) =>
            new ItemAcquireSourceDef
            {
                sourceId = id,
                kind = ItemAcquireKind.Craft,
                labelEn = en,
                labelZh = zh,
                recipeId = recipeId,
                targetScreen = "Crafting"
            };

        private static ItemAcquireSourceDef Combat(string id, string en, string zh, string regionId) =>
            new ItemAcquireSourceDef
            {
                sourceId = id,
                kind = ItemAcquireKind.Combat,
                labelEn = en,
                labelZh = zh,
                regionId = regionId,
                targetScreen = "Battle"
            };

        private static ItemAcquireSourceDef Market(string id, string en, string zh) =>
            new ItemAcquireSourceDef
            {
                sourceId = id,
                kind = ItemAcquireKind.Market,
                labelEn = en,
                labelZh = zh,
                targetScreen = "Market"
            };

        private static ItemAcquireSourceDef Mission(string id, string en, string zh) =>
            new ItemAcquireSourceDef
            {
                sourceId = id,
                kind = ItemAcquireKind.Mission,
                labelEn = en,
                labelZh = zh,
                targetScreen = "Missions"
            };
    }
}
