using System.Collections.Generic;

namespace Assets.Resources.Scripts.Economy.Domain
{
    /// <summary>Embedded item catalog: stackable resources + equipment/modules (P3 §4.2).</summary>
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
            Add(Mat(
                "mat_scrap", "Scrap", "废料",
                "Twisted hull fragments and entropy-scored plating salvaged from Severance wrecks. Cheap filler for early smelting.",
                "断航残骸上拆下的扭曲壳体与被熵雾灼蚀的板材。廉价填料，适合早期精炼。",
                "Steel", 0));
            Add(Mat(
                "mat_iron_ore", "Iron Ore", "铁矿",
                "Dense ore pulled from Frontier VII outer-belt veins. The backbone of the metal workshop chain.",
                "从第七前沿外缘带矿脉采出的致密矿石。金属工坊链的基础原料。",
                "Steel", 2));
            Add(Mat(
                "mat_crystal_sand", "Crystal Sand", "晶砂",
                "Fine refractive grit that still resonates with dead Astral Grid harmonics. Feeds energy cores.",
                "仍微微共振着航网残波的折射细砂。充能与能源链的关键原料。",
                "Crystal", 3));
            Add(Mat(
                "mat_fungal", "Fungal Mat", "菌毯",
                "Spore-rich biomass mats harvested from mining-spur caverns. Feedstock for synth weaving.",
                "矿脉支线洞穴中采收的富孢子生物毯。合成编织链的原料。",
                "Organic", 2));
            Add(Mat(
                "mat_alloy_plate", "Alloy Plate", "合金板",
                "Pre-forged frontier plating traded through starports. Reinforces frames before final armor assembly.",
                "经星港流通的边境预制合金板。锻造装甲框架时的加固材料。",
                "Steel", 8));
            Add(Mat(
                "mat_energy_cell", "Energy Cell", "能量芯",
                "Compact cells that hold a short-lived charge. Recovered from rift debris or sold by certified vendors.",
                "能短暂储能的紧凑芯体。多来自裂隙残骸或认证商人货架。",
                "Crystal", 10));
            Add(Mat(
                "mat_biofiber", "Biofiber", "生物纤维",
                "Tough organic strands used to bind synth meshes. Common convoy salvage across the frontier lanes.",
                "用于固定合成网的强韧有机纤维。护航航道上常见的打捞物。",
                "Organic", 6));
            Add(Mat(
                "mat_repair_parts", "Repair Parts", "维修零件",
                "Assorted fasteners, seals, and micro-actuators for field maintenance. Essential for repair kits.",
                "野外维修用的紧固件、密封圈与微型执行器。组装维修包的必备零件。",
                "Steel", 5));

            // --- Intermediates (6) ---
            Add(Inter(
                "int_refined_ingot", "Refined Ingot", "精炼锭",
                "Smelted metal purged of entropy grit. Ready to be shaped into armor frames.",
                "去除熵雾杂质后的精炼金属锭。可继续锻造成装甲框架。",
                "Steel", 12));
            Add(Inter(
                "int_charged_core", "Charged Core", "充能核心",
                "A crystal lattice holding a stable charge. The heart of reactor coils and pulse weapons.",
                "保持稳定电荷的晶格核心。反应线圈与脉冲武器的心脏。",
                "Crystal", 15));
            Add(Inter(
                "int_synth_mesh", "Synth Mesh", "合成网",
                "Woven biofiber mesh with light adaptive weave. Intermediate for nano-thread spinning.",
                "带轻度自适应编织的生物纤维网。抽丝纳米丝前的中间件。",
                "Organic", 14));
            Add(Inter(
                "int_armor_frame", "Armor Frame", "装甲框架",
                "Rigid chassis ready for composite plating. Marks the last step before wearable armor.",
                "可覆复合板材的刚性骨架。进入可穿戴装甲前的最后中间件。",
                "Steel", 20));
            Add(Inter(
                "int_reactor_coil", "Reactor Coil", "反应线圈",
                "Wound coil that meters power from charged cores into ship systems or weapons.",
                "将充能核心的能量导入舰船系统或武器的绕制线圈。",
                "Crystal", 22));
            Add(Inter(
                "int_nano_thread", "Nano Thread", "纳米丝",
                "Ultra-fine synth filament spun for cloaks, seals, and precision fittings.",
                "抽丝得到的超细合成丝线，用于斗篷、密封与精密装配。",
                "Organic", 18));

            // --- Consumables ---
            Add(Con(
                "con_repair_kit", "Repair Kit", "维修包",
                "Field kit that restores equipment durability. Standard Bureau issue for pioneer crews.",
                "恢复装备耐久的野外维修包。开拓局发给舰长编制的标准补给。",
                "Steel", 15));
            Add(Con(
                "con_field_ration", "Field Ration", "军粮",
                "Compressed rations for long sorties. Keeps crews working when the galley is offline.",
                "适合长途出动的压缩口粮。厨房停摆时维持编制运转。",
                "Organic", 4));
            Add(Con(
                "con_stim", "Combat Stim", "战剂",
                "Short-burst combat stimulant sealed against entropy fog. Reserved for future battle buffs.",
                "抗熵雾封装的短时战剂。预留给后续战斗增益效果。",
                "Crystal", 12));
            Add(Con(
                "con_nano_paste", "Nano Paste", "纳米膏",
                "Self-binding paste used as a rework reagent in advanced crafting recipes.",
                "可自结合的纳米膏体，用作高阶再加工配方的试剂。",
                "Organic", 10));
            Add(Con(
                "con_recruit_ticket", "Recruit Ticket", "招募券",
                "Bureau voucher authorizing one pull from the Frontier recruit pool.",
                "开拓局签发的招募凭证，可兑换一次边境征募抽取。",
                "Crystal", 40));

            // --- Equipment (8) ---
            Add(Eq(
                "eq_pulse_rifle", "Pulse Rifle", "脉冲步枪",
                "Standard-issue energy rifle fed by reactor coils. Reliable mid-range firepower for frontier decks.",
                "以反应线圈供能的制式能量步枪。边境卡组可靠的中距火力。",
                EquipSlot.Weapon, 80));
            Add(Eq(
                "eq_plasma_blade", "Plasma Blade", "等离子刃",
                "Close-quarters blade sheathed in a plasma edge. Favored by shock troopers boarding wrecks.",
                "刃缘覆等离子的近战武器。登舰清残骸时突击队员的偏爱。",
                EquipSlot.Weapon, 70));
            Add(Eq(
                "eq_composite_armor", "Composite Armor", "复合装甲",
                "Layered alloy plating on a forged frame. Core product of the metal workshop chain.",
                "锻框覆层合金板的复合装甲。金属工坊链的核心成品。",
                EquipSlot.Armor, 100));
            Add(Eq(
                "eq_shield_vest", "Shield Vest", "护盾背心",
                "Personal vest with a short-lived kinetic barrier. Light protection for scouts.",
                "带短时动能屏障的个人背心。适合侦察编制的轻防护。",
                EquipSlot.Armor, 90));
            Add(Eq(
                "eq_scope", "Combat Scope", "战斗瞄具",
                "Stabilized optic that cuts through fog haze. Improves aim discipline on long engagements.",
                "可穿透熵雾霾的稳定瞄具。长线交火时提升瞄准纪律。",
                EquipSlot.Accessory, 40));
            Add(Eq(
                "eq_gather_drill", "Gather Drill", "采集钻",
                "Handheld mining drill. Higher quality shortens gather cycle time on resource nodes.",
                "手持采矿钻。品质越高，资源节点的采集周期越短。",
                EquipSlot.Tool, 50));
            Add(Eq(
                "eq_energy_pack", "Energy Pack", "能量背包",
                "Portable cell rack that tops up personal gear mid-sortie.",
                "可在出动途中为个人装备补能的便携芯架。",
                EquipSlot.Accessory, 45));
            Add(Eq(
                "eq_synth_cloak", "Synth Cloak", "合成斗篷",
                "Adaptive cloak spun from nano-thread. Signature finish of the synth workshop chain.",
                "由纳米丝织成的自适应斗篷。合成工坊链的标志成品。",
                EquipSlot.Accessory, 55));

            // --- Ship modules (4) ---
            Add(Mod(
                "mod_armor", "Armor Module", "装甲模块",
                "Workshop module that unlocks metal processing and hardens the hull against entropy abrasion.",
                "解锁金属加工并强化舰体抗熵磨损的工坊模块。",
                120));
            Add(Mod(
                "mod_reactor", "Reactor Module", "反应堆模块",
                "Power plant module for energy-chain crafting and high-draw region gates.",
                "支撑能源链制造与高耗能区域门槛的动力模块。",
                130));
            Add(Mod(
                "mod_cargo", "Cargo Module", "货仓模块",
                "Expanded hold plating that raises warehouse capacity for long frontier runs.",
                "扩大货舱容积的模块板材，提升长途开拓时的仓库容量。",
                110));
            Add(Mod(
                "mod_synth", "Synth Bay", "合成舱",
                "Bio-synth bay required for mesh weaving, nano-thread spinning, and cloak manufacture.",
                "合成网编织、纳米抽丝与斗篷制造所需的生物合成舱。",
                125));
        }

        private static ItemDef Mat(
            string id, string en, string zh, string enDesc, string zhDesc, string icon, int cost) =>
            new ItemDef
            {
                itemDefId = id,
                displayNameEn = en,
                displayNameZh = zh,
                descriptionEn = enDesc,
                descriptionZh = zhDesc,
                category = ItemCategory.Material,
                icon = icon,
                stackable = true,
                maxStack = 999,
                baseCost = cost
            };

        private static ItemDef Inter(
            string id, string en, string zh, string enDesc, string zhDesc, string icon, int cost) =>
            new ItemDef
            {
                itemDefId = id,
                displayNameEn = en,
                displayNameZh = zh,
                descriptionEn = enDesc,
                descriptionZh = zhDesc,
                category = ItemCategory.Intermediate,
                icon = icon,
                stackable = true,
                maxStack = 999,
                baseCost = cost
            };

        private static ItemDef Con(
            string id, string en, string zh, string enDesc, string zhDesc, string icon, int cost) =>
            new ItemDef
            {
                itemDefId = id,
                displayNameEn = en,
                displayNameZh = zh,
                descriptionEn = enDesc,
                descriptionZh = zhDesc,
                category = ItemCategory.Consumable,
                icon = icon,
                stackable = true,
                maxStack = 99,
                baseCost = cost
            };

        private static ItemDef Eq(
            string id, string en, string zh, string enDesc, string zhDesc, EquipSlot slot, int dura) =>
            new ItemDef
            {
                itemDefId = id,
                displayNameEn = en,
                displayNameZh = zh,
                descriptionEn = enDesc,
                descriptionZh = zhDesc,
                category = ItemCategory.Equipment,
                icon = "Steel",
                stackable = false,
                maxStack = 1,
                baseMaxDurability = dura,
                equipSlot = slot,
                baseCost = dura
            };

        private static ItemDef Mod(string id, string en, string zh, string enDesc, string zhDesc, int dura) =>
            new ItemDef
            {
                itemDefId = id,
                displayNameEn = en,
                displayNameZh = zh,
                descriptionEn = enDesc,
                descriptionZh = zhDesc,
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
