using System.Collections.Generic;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Props;
using UnityEngine;

namespace Assets.Resources.Scripts.Utils.Save
{
    /// <summary>
    /// Applies deterministic starter inventory when creating a new player save.
    /// </summary>
    public static class StarterSeedApplier
    {
        public static string ResourcePath => DefaultProperty.STARTER_SEED_PATH;

        /// <summary>
        /// Writes local starter items when the local inventory file is missing or empty.
        /// Returns the seed table version written (0 if table missing / skipped).
        /// </summary>
        public static int ApplyNewPlayerLocalInventory(DataUtil dataUtil)
        {
            if (dataUtil == null)
                throw new System.ArgumentNullException(nameof(dataUtil));

            var existing = dataUtil.LoadInventory(InventoryStore.Local);
            if (existing != null && existing.Count > 0)
            {
                Debug.Log("[SAVE] Starter seed skipped; local inventory already has items.");
                return dataUtil.LoadMeta()?.starterSeedTableVersion ?? 0;
            }

            var table = LoadTable();
            if (table == null)
            {
                Debug.LogWarning("[SAVE] StarterSeed table missing; writing empty local inventory.");
                dataUtil.SaveInventory(InventoryStore.Local, new List<ItemEntity>(), touchMeta: false);
                return 0;
            }

            var items = new List<ItemEntity>();
            if (table.localItems != null)
            {
                foreach (var source in table.localItems)
                {
                    if (source == null)
                        continue;
                    items.Add(new ItemEntity(
                        source.itemName,
                        source.itemDescription,
                        source.itemIcon,
                        source.itemCost,
                        source.itemType)
                    {
                        quantity = Mathf.Max(1, source.quantity),
                        isRemote = false,
                        itemDefId = source.itemDefId ?? "",
                        quality = source.quality > 0 ? source.quality : 2,
                        itemInstanceId = source.itemInstanceId ?? "",
                        durability = source.durability,
                        maxDurability = source.maxDurability
                    });
                }
            }

            foreach (var item in items)
                ItemFactory.NormalizeLegacy(item);

            dataUtil.SaveInventory(InventoryStore.Local, items, touchMeta: false);
            Debug.Log($"[SAVE] Applied starter seed v{table.seedTableVersion} ({items.Count} local items).");
            return table.seedTableVersion;
        }

        public static StarterSeedTable LoadTable()
        {
            // Fully qualify: this file lives under namespace Assets.Resources.*, which shadows UnityEngine.Resources.
            var asset = UnityEngine.Resources.Load<TextAsset>(ResourcePath);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                return null;

            try
            {
                return JsonUtility.FromJson<StarterSeedTable>(asset.text);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[SAVE] Failed to parse StarterSeed.json: " + ex.Message);
                return null;
            }
        }
    }
}
