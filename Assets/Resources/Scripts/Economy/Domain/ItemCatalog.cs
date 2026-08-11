using System.Collections.Generic;

namespace Assets.Resources.Scripts.Economy.Domain
{
    /// <summary>Embedded item catalog: 18 resources + 12 equipment/modules (P3 §4.2).</summary>
    public static class ItemCatalog
    {
        private static readonly Dictionary<string, ItemDef> ById = new Dictionary<string, ItemDef>();
        private static bool loaded;

        public static IReadOnlyDictionary<string, ItemDef> All
        {
            get
            {
                Ensure();
                return ById;
            }
        }

        public static ItemDef Get(string itemDefId)
        {
            Ensure();
            if (string.IsNullOrEmpty(itemDefId)) return null;
            return ById.TryGetValue(itemDefId, out var d) ? d : null;
        }

        public static bool IsStackable(string itemDefId)
        {
            var d = Get(itemDefId);
            return d == null || d.stackable;
        }

        public static bool IsEquipment(string itemDefId)
        {
            var d = Get(itemDefId);
            return d != null && (d.category == ItemCategory.Equipment || d.category == ItemCategory.ShipModule);
        }

        private static void Ensure()
        {
            if (loaded) return;
            loaded = true;
            void Add(ItemDef d) => ById[d.itemDefId] = d;

            // --- Raw / materials (8) ---
            Add(Mat("mat_scrap", "Scrap", "废料", "Steel", 0));
            Add(Mat("mat_iron_ore", "Iron Ore", "铁矿", "Steel", 2));
            Add(Mat("mat_crystal_sand", "Crystal Sand", "晶砂", "Crystal", 3));
            Add(Mat("mat_fungal", "Fungal Mat", "菌毯", "Organic", 2));
            Add(Mat("mat_alloy_plate", "Alloy Plate", "合金板", "Steel", 8));
            Add(Mat("mat_energy_cell", "Energy Cell", "能量芯", "Crystal", 10));
            Add(Mat("mat_biofiber", "Biofiber", "生物纤维", "Organic", 6));
            Add(Mat("mat_repair_parts", "Repair Parts", "维修零件", "Steel", 5));

            // --- Intermediates (6) ---
            Add(Inter("int_refined_ingot", "Refined Ingot", "精炼锭", "Steel", 12));
            Add(Inter("int_charged_core", "Charged Core", "充能核心", "Crystal", 15));
            Add(Inter("int_synth_mesh", "Synth Mesh", "合成网", "Organic", 14));
            Add(Inter("int_armor_frame", "Armor Frame", "装甲框架", "Steel", 20));
            Add(Inter("int_reactor_coil", "Reactor Coil", "反应线圈", "Crystal", 22));
            Add(Inter("int_nano_thread", "Nano Thread", "纳米丝", "Organic", 18));

            // --- Consumables (4) ---
            Add(Con("con_repair_kit", "Repair Kit", "维修包", "Steel", 15));
            Add(Con("con_field_ration", "Field Ration", "军粮", "Organic", 4));
            Add(Con("con_stim", "Combat Stim", "战剂", "Crystal", 12));
            Add(Con("con_nano_paste", "Nano Paste", "纳米膏", "Organic", 10));

            // --- Equipment (8) ---
            Add(Eq("eq_pulse_rifle", "Pulse Rifle", "脉冲步枪", EquipSlot.Weapon, 80));
            Add(Eq("eq_plasma_blade", "Plasma Blade", "等离子刃", EquipSlot.Weapon, 70));
            Add(Eq("eq_composite_armor", "Composite Armor", "复合装甲", EquipSlot.Armor, 100));
            Add(Eq("eq_shield_vest", "Shield Vest", "护盾背心", EquipSlot.Armor, 90));
            Add(Eq("eq_scope", "Combat Scope", "战斗瞄具", EquipSlot.Accessory, 40));
            Add(Eq("eq_gather_drill", "Gather Drill", "采集钻", EquipSlot.Tool, 50));
            Add(Eq("eq_energy_pack", "Energy Pack", "能量背包", EquipSlot.Accessory, 45));
            Add(Eq("eq_synth_cloak", "Synth Cloak", "合成斗篷", EquipSlot.Accessory, 55));

            // --- Ship modules (4) ---
            Add(Mod("mod_armor", "Armor Module", "装甲模块", 120));
            Add(Mod("mod_reactor", "Reactor Module", "反应堆模块", 130));
            Add(Mod("mod_cargo", "Cargo Module", "货仓模块", 110));
            Add(Mod("mod_synth", "Synth Bay", "合成舱", 125));
        }

        private static ItemDef Mat(string id, string en, string zh, string icon, int cost) =>
            new ItemDef
            {
                itemDefId = id,
                displayNameEn = en,
                displayNameZh = zh,
                category = ItemCategory.Material,
                icon = icon,
                stackable = true,
                maxStack = 999,
                baseCost = cost
            };

        private static ItemDef Inter(string id, string en, string zh, string icon, int cost) =>
            new ItemDef
            {
                itemDefId = id,
                displayNameEn = en,
                displayNameZh = zh,
                category = ItemCategory.Intermediate,
                icon = icon,
                stackable = true,
                maxStack = 999,
                baseCost = cost
            };

        private static ItemDef Con(string id, string en, string zh, string icon, int cost) =>
            new ItemDef
            {
                itemDefId = id,
                displayNameEn = en,
                displayNameZh = zh,
                category = ItemCategory.Consumable,
                icon = icon,
                stackable = true,
                maxStack = 99,
                baseCost = cost
            };

        private static ItemDef Eq(string id, string en, string zh, EquipSlot slot, int dura) =>
            new ItemDef
            {
                itemDefId = id,
                displayNameEn = en,
                displayNameZh = zh,
                category = ItemCategory.Equipment,
                icon = "Steel",
                stackable = false,
                maxStack = 1,
                baseMaxDurability = dura,
                equipSlot = slot,
                baseCost = dura
            };

        private static ItemDef Mod(string id, string en, string zh, int dura) =>
            new ItemDef
            {
                itemDefId = id,
                displayNameEn = en,
                displayNameZh = zh,
                category = ItemCategory.ShipModule,
                icon = "Crystal",
                stackable = false,
                maxStack = 1,
                baseMaxDurability = dura,
                equipSlot = EquipSlot.None,
                baseCost = dura
            };
    }
}
