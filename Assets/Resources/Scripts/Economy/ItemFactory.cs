using System;
using System.Collections.Generic;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Entity;

namespace Assets.Resources.Scripts.Economy
{
    /// <summary>Bridges ItemEntity ↔ InventoryStack / ItemCatalog.</summary>
    public static class ItemFactory
    {
        public static ItemEntity FromDef(string itemDefId, int quantity, int quality = EconomyConstants.DefaultQuality)
        {
            var def = ItemCatalog.Get(itemDefId);
            if (def == null)
            {
                return new ItemEntity(itemDefId, itemDefId, "Steel", 0, ItemType.Material)
                {
                    itemDefId = itemDefId,
                    quality = QualityRules.ClampQuality(quality),
                    quantity = Math.Max(1, quantity)
                };
            }

            var type = def.category switch
            {
                ItemCategory.Equipment => ItemType.Equipment,
                ItemCategory.ShipModule => ItemType.Equipment,
                ItemCategory.Consumable => ItemType.Food,
                _ => ItemType.Material
            };

            var entity = new ItemEntity(def.displayNameEn, def.descriptionEn, ResolveIcon(def.icon), def.baseCost, type)
            {
                itemDefId = def.itemDefId,
                quality = QualityRules.ClampQuality(quality),
                quantity = Math.Max(1, quantity),
                itemName = def.displayNameEn
            };

            if (!def.stackable || def.category == ItemCategory.Equipment || def.category == ItemCategory.ShipModule)
            {
                entity.quantity = 1;
                entity.itemInstanceId = Guid.NewGuid().ToString("N");
                entity.maxDurability = Math.Max(1, def.baseMaxDurability);
                entity.durability = entity.maxDurability;
            }

            return entity;
        }

        public static InventoryStack ToStack(ItemEntity e)
        {
            if (e == null) return null;
            return new InventoryStack
            {
                itemDefId = ResolveDefId(e),
                itemName = e.itemName,
                itemDescription = e.itemDescription,
                itemIcon = e.itemIcon,
                quality = e.quality > 0 ? e.quality : EconomyConstants.DefaultQuality,
                quantity = e.quantity,
                itemInstanceId = e.itemInstanceId ?? "",
                durability = e.durability,
                maxDurability = e.maxDurability,
                equippedToCardId = e.equippedToCardId ?? "",
                itemType = (int)e.itemType,
                itemCost = e.itemCost,
                isRemote = e.isRemote
            };
        }

        public static ItemEntity FromStack(InventoryStack s)
        {
            if (s == null) return null;
            return new ItemEntity(s.itemName, s.itemDescription, s.itemIcon, s.itemCost, (ItemType)s.itemType)
            {
                quantity = s.quantity,
                isRemote = s.isRemote,
                itemDefId = s.itemDefId,
                quality = s.quality,
                itemInstanceId = s.itemInstanceId,
                durability = s.durability,
                maxDurability = s.maxDurability,
                equippedToCardId = s.equippedToCardId
            };
        }

        public static string ResolveDefId(ItemEntity e)
        {
            if (e == null) return "";
            if (!string.IsNullOrEmpty(e.itemDefId)) return e.itemDefId;
            if (string.Equals(e.itemName, "seed_scrap", StringComparison.OrdinalIgnoreCase)
                || string.Equals(e.itemName, "Scrap", StringComparison.OrdinalIgnoreCase))
                return EconomyConstants.ScrapDefId;
            // Match by display name
            foreach (var kv in ItemCatalog.All)
            {
                if (kv.Value != null && string.Equals(kv.Value.displayNameEn, e.itemName, StringComparison.OrdinalIgnoreCase))
                    return kv.Key;
            }

            return e.itemName ?? "";
        }

        public static void NormalizeLegacy(ItemEntity e)
        {
            if (e == null) return;
            if (string.IsNullOrEmpty(e.itemDefId))
                e.itemDefId = ResolveDefId(e);
            if (e.quality < 1 || e.quality > 5)
                e.quality = EconomyConstants.DefaultQuality;

            var def = ItemCatalog.Get(e.itemDefId);
            if (def != null)
            {
                if (string.IsNullOrEmpty(e.itemName))
                    e.itemName = def.displayNameEn;
                e.itemIcon = ResolveIcon(def.icon);
                e.itemType = def.category switch
                {
                    ItemCategory.Equipment => ItemType.Equipment,
                    ItemCategory.ShipModule => ItemType.Equipment,
                    ItemCategory.Consumable => ItemType.Food,
                    _ => ItemType.Material
                };
                if (!def.stackable && string.IsNullOrEmpty(e.itemInstanceId))
                {
                    e.itemInstanceId = Guid.NewGuid().ToString("N");
                    e.maxDurability = Math.Max(1, def.baseMaxDurability);
                    if (e.durability <= 0) e.durability = e.maxDurability;
                    e.quantity = 1;
                }
            }
            else if (e.itemType == ItemType.Unknown)
            {
                e.itemType = ItemType.Material;
            }
        }

        public static string ResolveIcon(string icon)
        {
            if (string.IsNullOrEmpty(icon)) return "Steel";
            return icon switch
            {
                "Crystal" => "SteelBar",
                "Organic" => "Wood",
                _ => icon
            };
        }

        public static List<InventoryStack> ToStacks(IList<ItemEntity> items)
        {
            var list = new List<InventoryStack>();
            if (items == null) return list;
            foreach (var e in items)
            {
                if (e == null) continue;
                NormalizeLegacy(e);
                list.Add(ToStack(e));
            }

            return list;
        }
    }
}
