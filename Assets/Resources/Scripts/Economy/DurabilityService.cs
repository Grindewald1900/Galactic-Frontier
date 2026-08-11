using System.Collections.Generic;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Inventory;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.Utils.Save;
using UnityEngine;

namespace Assets.Resources.Scripts.Economy
{
    public static class DurabilityService
    {
        public static void ApplyCombatWearToEquipped(IList<CardEntity> cards)
        {
            var items = ProductionService.GetLocalItems();
            if (items == null) return;
            var changed = false;
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.equippedToCardId)) continue;
                if (!ItemCatalog.IsEquipment(ItemFactory.ResolveDefId(item))) continue;
                item.durability = DurabilityRules.ApplyCombatWear(item.durability, item.maxDurability);
                changed = true;
            }

            // Heuristic: if nothing equipped, wear first weapon/armor in bag
            if (!changed)
            {
                foreach (var item in items)
                {
                    if (item == null) continue;
                    var def = ItemCatalog.Get(ItemFactory.ResolveDefId(item));
                    if (def == null || def.category != ItemCategory.Equipment) continue;
                    if (def.equipSlot != EquipSlot.Weapon && def.equipSlot != EquipSlot.Armor) continue;
                    item.durability = DurabilityRules.ApplyCombatWear(item.durability, item.maxDurability);
                    changed = true;
                    break;
                }
            }

            if (changed)
                Persist(items);
        }

        public static void ApplyGatherWear(int riskLevel)
        {
            var items = ProductionService.GetLocalItems();
            if (items == null) return;
            var changed = false;
            foreach (var item in items)
            {
                if (item == null) continue;
                var def = ItemCatalog.Get(ItemFactory.ResolveDefId(item));
                if (def == null || def.equipSlot != EquipSlot.Tool) continue;
                item.durability = DurabilityRules.ApplyGatherWear(item.durability, item.maxDurability, riskLevel);
                changed = true;
            }

            if (!changed)
            {
                // Soft wear on any equipped gear
                foreach (var item in items)
                {
                    if (item == null || string.IsNullOrEmpty(item.equippedToCardId)) continue;
                    item.durability = DurabilityRules.ApplyGatherWear(item.durability, item.maxDurability, riskLevel);
                    changed = true;
                }
            }

            if (changed)
                Persist(items);
        }

        public static bool HasCriticalBrokenEquipped()
        {
            var items = ProductionService.GetLocalItems();
            if (items == null) return false;
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.equippedToCardId)) continue;
                var def = ItemCatalog.Get(ItemFactory.ResolveDefId(item));
                if (def == null) continue;
                if (def.equipSlot != EquipSlot.Weapon && def.equipSlot != EquipSlot.Armor) continue;
                if (DurabilityRules.IsBroken(item.durability))
                    return true;
            }

            return false;
        }

        public static int TryAutoRepairAll()
        {
            var items = ProductionService.GetLocalItems();
            if (items == null) return 0;
            var kits = 0;
            foreach (var item in items)
            {
                if (item == null) continue;
                if (ItemFactory.ResolveDefId(item) == EconomyConstants.RepairKitDefId)
                    kits += item.quantity;
            }

            var repaired = 0;
            foreach (var item in items)
            {
                if (item == null) continue;
                if (!ItemCatalog.IsEquipment(ItemFactory.ResolveDefId(item))) continue;
                if (!DurabilityRules.NeedsAutoRepair(item.durability, item.maxDurability)) continue;
                var d = item.durability;
                if (DurabilityRules.TryRepair(ref d, item.maxDurability, ref kits))
                {
                    item.durability = d;
                    repaired++;
                }
            }

            if (repaired > 0)
            {
                // Rewrite kit count
                var stacks = ItemFactory.ToStacks(items);
                // Remove all kits then add remaining
                InventoryRules.TryConsume(stacks, EconomyConstants.RepairKitDefId, InventoryRules.CountOf(stacks, EconomyConstants.RepairKitDefId), 1);
                if (kits > 0)
                {
                    var kit = ItemFactory.ToStack(ItemFactory.FromDef(EconomyConstants.RepairKitDefId, kits));
                    InventoryRules.TryMerge(stacks, kit, EconomyConstants.WarehouseCapacity);
                }

                var list = new List<ItemEntity>();
                foreach (var s in stacks)
                    list.Add(ItemFactory.FromStack(s));
                Persist(list);
            }

            return repaired;
        }

        public static bool TryRepairInstance(string itemInstanceId)
        {
            var items = ProductionService.GetLocalItems();
            if (items == null || string.IsNullOrEmpty(itemInstanceId)) return false;
            ItemEntity target = null;
            foreach (var item in items)
            {
                if (item != null && item.itemInstanceId == itemInstanceId)
                {
                    target = item;
                    break;
                }
            }

            if (target == null || target.maxDurability <= 0 || target.durability >= target.maxDurability)
                return false;

            var cost = DurabilityRules.RepairCostKits(target.durability, target.maxDurability);
            var stacks = ItemFactory.ToStacks(items);
            if (!InventoryRules.TryConsume(stacks, EconomyConstants.RepairKitDefId, cost, 1))
                return false;

            foreach (var s in stacks)
            {
                if (s != null && s.itemInstanceId == itemInstanceId)
                    s.durability = s.maxDurability;
            }

            var list = new List<ItemEntity>();
            foreach (var s in stacks)
                list.Add(ItemFactory.FromStack(s));
            Persist(list);
            return true;
        }

        public static bool TryEquip(string itemInstanceId, string cardId)
        {
            var items = ProductionService.GetLocalItems();
            if (items == null || string.IsNullOrEmpty(itemInstanceId) || string.IsNullOrEmpty(cardId))
                return false;

            ItemEntity target = null;
            foreach (var item in items)
            {
                if (item != null && item.itemInstanceId == itemInstanceId)
                {
                    target = item;
                    break;
                }
            }

            if (target == null) return false;
            var def = ItemCatalog.Get(ItemFactory.ResolveDefId(target));
            if (def == null || def.category != ItemCategory.Equipment) return false;

            // Unequip same slot on this card
            foreach (var item in items)
            {
                if (item == null || item.equippedToCardId != cardId) continue;
                var other = ItemCatalog.Get(ItemFactory.ResolveDefId(item));
                if (other != null && other.equipSlot == def.equipSlot)
                    item.equippedToCardId = "";
            }

            target.equippedToCardId = cardId;
            Persist(items);
            return true;
        }

        private static void Persist(List<ItemEntity> items)
        {
            if (ItemManager.Instance != null)
            {
                var current = ItemManager.Instance.GetItems();
                if (!ReferenceEquals(current, items))
                {
                    current.Clear();
                    current.AddRange(items);
                }

                ItemManager.Instance.RefreshSlotsFromMemory();
            }

            DataUtil.Instance?.SaveInventory(InventoryStore.Local, items, touchMeta: true);
        }
    }
}
