using System;
using System.Collections.Generic;

namespace Assets.Resources.Scripts.World.Domain
{
    /// <summary>Pure loot resolution for FirstClear / Repeat / Farm tables (systems/11).</summary>
    public static class RewardRules
    {
        public static List<LootGrant> Resolve(RewardTable table, Random rng)
        {
            var grants = new List<LootGrant>();
            if (table == null) return grants;
            rng ??= new Random(1);

            if (table.guaranteed != null)
            {
                foreach (var entry in table.guaranteed)
                    TryAdd(grants, entry, rng, force: true);
            }

            if (table.weighted != null)
            {
                foreach (var entry in table.weighted)
                    TryAdd(grants, entry, rng, force: false);
            }

            return grants;
        }

        private static void TryAdd(List<LootGrant> grants, LootEntry entry, Random rng, bool force)
        {
            if (entry == null || string.IsNullOrEmpty(entry.itemDefId))
                return;

            if (!force)
            {
                var chance = entry.chance;
                if (chance <= 0f) return;
                if (chance < 1f && rng.NextDouble() > chance)
                    return;
            }

            var min = Math.Max(0, entry.qtyMin);
            var max = Math.Max(min, entry.qtyMax);
            var qty = min == max ? min : rng.Next(min, max + 1);
            if (qty <= 0) return;

            grants.Add(new LootGrant
            {
                itemDefId = entry.itemDefId,
                quantity = qty,
                quality = entry.quality > 0 ? entry.quality : 2
            });
        }
    }
}
