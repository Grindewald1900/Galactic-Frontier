using System.Collections.Generic;

namespace Assets.Resources.Scripts.World.Domain
{
    /// <summary>Frontier VII FirstClear / Repeat / Farm tables (systems/11 §8–9).</summary>
    public static class RewardCatalog
    {
        private static Dictionary<string, RewardTable> cached;

        public static RewardTable Get(string tableId)
        {
            Ensure();
            if (string.IsNullOrEmpty(tableId)) return null;
            return cached.TryGetValue(tableId, out var t) ? t : null;
        }

        public static void ResetForTests()
        {
            cached = null;
        }

        private static void Ensure()
        {
            if (cached != null) return;
            cached = new Dictionary<string, RewardTable>();
            foreach (var t in BuildDefaults())
                cached[t.tableId] = t;
        }

        public static List<RewardTable> BuildDefaults()
        {
            return new List<RewardTable>
            {
                Table("reward_outer_first",
                    G("credit", 40), G("mat_scrap", 12), G("mat_iron_ore", 8), G("mat_repair_parts", 2),
                    W("mat_alloy_plate", 1, 0.35f)),
                Table("reward_outer_repeat",
                    G("credit", 8), G("mat_scrap", 3)),
                Table("reward_outer_farm",
                    G("mat_scrap", 2), G("credit", 3),
                    W("mat_iron_ore", 1, 0.45f)),

                Table("reward_mining_first",
                    G("credit", 60), G("mat_scrap", 10), G("mat_crystal_sand", 8), G("mat_fungal", 6),
                    G("mat_alloy_plate", 2),
                    W("mat_energy_cell", 1, 0.40f)),
                Table("reward_mining_repeat",
                    G("credit", 12), G("mat_crystal_sand", 2), G("mat_fungal", 2)),
                Table("reward_mining_farm",
                    G("mat_scrap", 2), G("credit", 4),
                    W("mat_crystal_sand", 1, 0.50f), W("mat_fungal", 1, 0.35f), W("mat_alloy_plate", 1, 0.12f)),

                Table("reward_rift_first",
                    G("credit", 90), G("mat_energy_cell", 6), G("mat_crystal_sand", 10), G("mat_scrap", 14),
                    G("int_charged_core", 1),
                    W("eq_energy_pack", 1, 0.25f)),
                Table("reward_rift_repeat",
                    G("credit", 16), G("mat_energy_cell", 2), G("mat_scrap", 4)),
                Table("reward_rift_farm",
                    G("mat_scrap", 3), G("credit", 5),
                    W("mat_energy_cell", 1, 0.40f), W("mat_crystal_sand", 1, 0.35f), W("int_charged_core", 1, 0.08f)),

                Table("reward_abyss_first",
                    G("credit", 120), G("mat_scrap", 18), G("mat_repair_parts", 5), G("con_repair_kit", 2),
                    G("int_armor_frame", 1),
                    W("eq_shield_vest", 1, 0.30f, 3)),
                Table("reward_abyss_repeat",
                    G("credit", 20), G("mat_repair_parts", 2), G("mat_scrap", 5)),
                Table("reward_abyss_farm",
                    G("mat_scrap", 3), G("credit", 6),
                    W("mat_repair_parts", 1, 0.40f), W("con_repair_kit", 1, 0.10f), W("mat_alloy_plate", 1, 0.18f)),

                Table("reward_convoy_first",
                    G("credit", 150), G("mat_biofiber", 8), G("mat_alloy_plate", 4), G("int_synth_mesh", 1),
                    G("con_repair_kit", 2),
                    W("eq_gather_drill", 1, 0.35f)),
                Table("reward_convoy_repeat",
                    G("credit", 24), G("mat_biofiber", 3), G("mat_alloy_plate", 1)),
                Table("reward_convoy_farm",
                    G("mat_scrap", 3), G("credit", 8),
                    W("mat_biofiber", 1, 0.45f), W("mat_alloy_plate", 1, 0.20f), W("con_field_ration", 1, 0.15f)),

                Table("reward_boss_first",
                    G("credit", 300), G("mat_scrap", 40), G("mat_alloy_plate", 8), G("mat_energy_cell", 8),
                    G("mat_biofiber", 8), G("con_repair_kit", 5), G("eq_pulse_rifle", 1, 3), G("mod_cargo", 1, 3),
                    W("eq_synth_cloak", 1, 0.40f, 3)),
                Table("reward_boss_repeat",
                    G("credit", 40), G("mat_scrap", 10), G("mat_alloy_plate", 2), G("mat_energy_cell", 2),
                    G("mat_biofiber", 2)),
                Table("reward_frontier_farm",
                    G("mat_scrap", 4), G("credit", 10),
                    W("mat_energy_cell", 1, 0.30f), W("mat_biofiber", 1, 0.30f),
                    W("mat_alloy_plate", 1, 0.25f), W("eq_scope", 1, 0.03f)),
            };
        }

        private static RewardTable Table(string id, params LootEntry[] entries)
        {
            var table = new RewardTable { tableId = id };
            foreach (var e in entries)
            {
                if (e == null) continue;
                if (e.chance >= 1f)
                    table.guaranteed.Add(e);
                else
                    table.weighted.Add(e);
            }

            return table;
        }

        private static LootEntry G(string id, int qty, int quality = 2) =>
            new LootEntry { itemDefId = id, qtyMin = qty, qtyMax = qty, quality = quality, chance = 1f };

        private static LootEntry W(string id, int qty, float chance, int quality = 2) =>
            new LootEntry { itemDefId = id, qtyMin = qty, qtyMax = qty, quality = quality, chance = chance };
    }
}
