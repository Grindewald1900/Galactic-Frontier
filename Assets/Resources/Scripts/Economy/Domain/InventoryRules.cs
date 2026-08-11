using System;
using System.Collections.Generic;

namespace Assets.Resources.Scripts.Economy.Domain
{
    public static class InventoryRules
    {
        public static bool CanStack(InventoryStack a, InventoryStack b)
        {
            if (a == null || b == null) return false;
            if (string.IsNullOrEmpty(a.itemDefId) || string.IsNullOrEmpty(b.itemDefId))
                return false;
            if (!string.Equals(a.itemDefId, b.itemDefId, StringComparison.Ordinal))
                return false;
            if (ItemCatalog.IsEquipment(a.itemDefId) || !ItemCatalog.IsStackable(a.itemDefId))
                return false;
            if (!string.IsNullOrEmpty(a.itemInstanceId) || !string.IsNullOrEmpty(b.itemInstanceId))
                return false;
            return a.quality == b.quality;
        }

        public static int FindStackIndex(IList<InventoryStack> items, string itemDefId, int quality)
        {
            if (items == null || string.IsNullOrEmpty(itemDefId)) return -1;
            for (var i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it == null) continue;
                if (!string.Equals(it.itemDefId, itemDefId, StringComparison.Ordinal)) continue;
                if (it.quality != quality) continue;
                if (!ItemCatalog.IsStackable(itemDefId)) continue;
                if (!string.IsNullOrEmpty(it.itemInstanceId)) continue;
                return i;
            }

            return -1;
        }

        public static bool TryMerge(IList<InventoryStack> items, InventoryStack incoming, int capacity)
        {
            if (items == null || incoming == null) return false;
            if (ItemCatalog.IsEquipment(incoming.itemDefId) || !ItemCatalog.IsStackable(incoming.itemDefId))
            {
                if (items.Count >= capacity) return false;
                items.Add(incoming);
                return true;
            }

            var idx = FindStackIndex(items, incoming.itemDefId, incoming.quality);
            if (idx >= 0)
            {
                items[idx].quantity += incoming.quantity;
                return true;
            }

            if (items.Count >= capacity) return false;
            items.Add(incoming);
            return true;
        }

        public static bool IsWarehouseFull(IList<InventoryStack> items, int capacity) =>
            items != null && items.Count >= capacity;

        public static int CountOf(IList<InventoryStack> items, string itemDefId, int minQuality = 1)
        {
            if (items == null || string.IsNullOrEmpty(itemDefId)) return 0;
            var total = 0;
            foreach (var it in items)
            {
                if (it == null) continue;
                if (!string.Equals(it.itemDefId, itemDefId, StringComparison.Ordinal)) continue;
                if (it.quality < minQuality) continue;
                total += it.quantity;
            }

            return total;
        }

        public static bool TryConsume(IList<InventoryStack> items, string itemDefId, int quantity, int minQuality = 1)
        {
            if (items == null || quantity <= 0) return false;
            if (CountOf(items, itemDefId, minQuality) < quantity) return false;

            var remaining = quantity;
            for (var i = items.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var it = items[i];
                if (it == null) continue;
                if (!string.Equals(it.itemDefId, itemDefId, StringComparison.Ordinal)) continue;
                if (it.quality < minQuality) continue;
                var take = Math.Min(remaining, it.quantity);
                it.quantity -= take;
                remaining -= take;
                if (it.quantity <= 0)
                    items.RemoveAt(i);
            }

            return remaining == 0;
        }

        public static int MinInputQuality(IList<InventoryStack> items, IList<RecipeInput> inputs)
        {
            if (inputs == null || inputs.Count == 0) return EconomyConstants.DefaultQuality;
            var min = 5;
            foreach (var input in inputs)
            {
                if (input == null) continue;
                var found = false;
                var best = 1;
                if (items != null)
                {
                    foreach (var it in items)
                    {
                        if (it == null) continue;
                        if (!string.Equals(it.itemDefId, input.itemDefId, StringComparison.Ordinal)) continue;
                        if (it.quality < input.minQuality) continue;
                        found = true;
                        if (it.quality > best) best = it.quality;
                    }
                }

                if (!found) return input.minQuality;
                if (best < min) min = best;
            }

            return Math.Max(1, Math.Min(5, min));
        }
    }
}
