using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Resources.Scripts.Economy;
using Assets.Resources.Scripts.Economy.Domain;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.UI;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.Utils.Save;
using TMPro;
using UnityEngine;

namespace Assets.Resources.Scripts.Inventory
{
    /// <summary>
    /// Shared item collection and slot presentation logic for local and remote inventories.
    /// </summary>
    /// <remarks>
    /// Derived managers only define inventory ownership through IsRemote. Items are copied during
    /// transfer so the local and remote inventories never share the same mutable ItemEntity instance.
    /// Each side loads/saves its own inventory_*.json file (saveVersion ≥ 1).
    /// </remarks>
    public abstract class InventoryItemManagerBase : MonoBehaviour
    {
        private readonly List<ItemSlot> itemSlots = new();
        private List<ItemEntity> items = new();
        private int selectedSlotIndex;
        private int typeFilterIndex;
        private int qualityFilterIndex;

        /// <summary>True after the first load from disk (or sample inject).</summary>
        public bool HasLoaded { get; private set; }

        public GameObject itemPrefab;
        public Transform gridParent;
        public int inventorySize = 60;

        protected abstract bool IsRemote { get; }

        private InventoryStore Store => IsRemote ? InventoryStore.Remote : InventoryStore.Local;

        protected virtual void Start()
        {
            InitializeItemList();
        }

        /// <summary>
        /// Replaces the in-memory inventory with Dev sample items and persists them.
        /// No-op when Dev Data Mode is off.
        /// </summary>
        public bool TryInjectSampleInventory(bool persist = true)
        {
            if (!DevData.IsActive)
            {
                DevData.LogSkipped(nameof(InventoryItemManagerBase) + ".TryInjectSampleInventory");
                return false;
            }

            var samples = DevData.Current.CreateSampleInventory(IsRemote);
            items = new List<ItemEntity>(samples);
            HasLoaded = true;
            Debug.Log($"[DEV-DATA] Injected {items.Count} sample items (isRemote={IsRemote}, persist={persist}).");

            if (persist)
                PersistInventory();

            UpdateItemList();
            return true;
        }

        private void InitializeItemList()
        {
            items = DataUtil.Instance != null
                ? DataUtil.Instance.LoadInventory(Store) ?? new List<ItemEntity>()
                : new List<ItemEntity>();
            foreach (var item in items)
                ItemFactory.NormalizeLegacy(item);
            HasLoaded = true;
            WireFilterDropdowns();
            itemSlots.Clear();

            for (var i = 0; i < inventorySize; i++)
            {
                var itemObject = Instantiate(itemPrefab, gridParent);
                itemObject.SetActive(true);

                var itemSlot = itemObject.GetComponent<ItemSlot>();
                itemSlot.slotIndex = i;
                itemSlot.isRemote = IsRemote;
                itemSlots.Add(itemSlot);
            }

            UpdateItemList();
        }

        /// <summary>Adds or stacks an item and refreshes the slot projection.</summary>
        /// <returns>False only when a new stack cannot fit in the inventory.</returns>
        public bool AddItem(ItemEntity newItem)
        {
            if (newItem == null)
                throw new ArgumentNullException(nameof(newItem));

            ItemFactory.NormalizeLegacy(newItem);
            var defId = ItemFactory.ResolveDefId(newItem);
            var quality = newItem.quality > 0 ? newItem.quality : EconomyConstants.DefaultQuality;
            var stackable = ItemCatalog.IsStackable(defId) && !ItemCatalog.IsEquipment(defId)
                            && string.IsNullOrEmpty(newItem.itemInstanceId);

            if (stackable)
            {
                var index = items.FindIndex(item =>
                {
                    if (item == null) return false;
                    ItemFactory.NormalizeLegacy(item);
                    return string.Equals(ItemFactory.ResolveDefId(item), defId, StringComparison.Ordinal)
                           && item.quality == quality
                           && string.IsNullOrEmpty(item.itemInstanceId);
                });

                if (index >= 0)
                {
                    items[index].quantity += newItem.quantity;
                    UpdateItemList();
                    PersistInventory();
                    NotifyLocalChanged();
                    return true;
                }
            }

            if (items.Count >= inventorySize)
            {
                Debug.Log("Inventory is full.");
                return false;
            }

            var copy = CopyForThisInventory(newItem);
            ItemFactory.NormalizeLegacy(copy);
            items.Add(copy);
            UpdateItemList();
            PersistInventory();
            NotifyLocalChanged();
            return true;
        }

        /// <summary>Consumes quantity of a def at min quality (highest quality first).</summary>
        public bool TryConsumeDef(string itemDefId, int quantity, int minQuality = 1)
        {
            if (string.IsNullOrEmpty(itemDefId) || quantity <= 0) return false;
            var stacks = ItemFactory.ToStacks(items);
            if (!InventoryRules.TryConsume(stacks, itemDefId, quantity, minQuality))
                return false;

            // Rebuild from stacks
            items.Clear();
            foreach (var s in stacks)
                items.Add(ItemFactory.FromStack(s));
            UpdateItemList();
            PersistInventory();
            NotifyLocalChanged();
            return true;
        }

        public int CountDef(string itemDefId, int minQuality = 1)
        {
            return InventoryRules.CountOf(ItemFactory.ToStacks(items), itemDefId, minQuality);
        }

        public bool IsFull => items.Count >= inventorySize;

        /// <summary>Consumes up to the requested quantity and removes empty stacks.</summary>
        public void UseItem(ItemEntity itemEntity, int quantity = 1)
        {
            if (itemEntity == null || quantity <= 0)
                return;

            var item = items.Find(candidate =>
                candidate != null &&
                string.Equals(candidate.itemName, itemEntity.itemName, StringComparison.Ordinal));

            if (item == null || item.quantity <= 0)
                return;

            var usedQuantity = Mathf.Min(quantity, item.quantity);
            item.quantity -= usedQuantity;
            Debug.Log($"Used {usedQuantity} {item.itemName}. Remaining: {item.quantity}");

            if (item.quantity <= 0)
            {
                items.Remove(item);
                Debug.Log($"{item.itemName} was removed from the inventory.");
            }

            UpdateItemList();
            PersistInventory();
        }

        public List<ItemEntity> GetItems()
        {
            return items;
        }

        public void SelectItem(int index)
        {
            if (index < 0 || index >= itemSlots.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (selectedSlotIndex >= 0 && selectedSlotIndex < itemSlots.Count)
                itemSlots[selectedSlotIndex].SetSelected(false);

            itemSlots[index].SetSelected(true);
            selectedSlotIndex = index;
            Debug.Log($"Selected item at index {index}");
        }

        public void SortItems(SortType sortType)
        {
            switch (sortType)
            {
                case SortType.ITEM_NAME:
                    items = items
                        .Where(item => item != null)
                        .OrderBy(item => item.itemName)
                        .ToList();
                    break;
                case SortType.ITEM_COST:
                    items = items
                        .Where(item => item != null)
                        .OrderByDescending(item => item.itemCost)
                        .ToList();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sortType), sortType, null);
            }

            UpdateItemList();
            PersistInventory();
            Debug.Log("Inventory sorted.");
        }

        public List<ItemEntity> FilterItemsByType(ItemType itemType)
        {
            return items
                .Where(item => item != null && item.itemType == itemType)
                .ToList();
        }

        public List<ItemEntity> FilterItemsByQuality(int quality)
        {
            return items
                .Where(item => item != null && item.quality == quality)
                .ToList();
        }

        public List<ItemEntity> FilterItemsByDefCategory(ItemCategory category)
        {
            return items
                .Where(item =>
                {
                    if (item == null) return false;
                    var def = ItemCatalog.Get(ItemFactory.ResolveDefId(item));
                    return def != null && def.category == category;
                })
                .ToList();
        }

        /// <summary>Sets absolute quantity for a named stack (0 removes). Persists.</summary>
        public bool SetItemQuantity(string itemName, int quantity)
        {
            if (string.IsNullOrWhiteSpace(itemName))
                return false;

            var index = items.FindIndex(item =>
                item != null && string.Equals(item.itemName, itemName, StringComparison.Ordinal));

            if (quantity <= 0)
            {
                if (index < 0)
                    return false;
                items.RemoveAt(index);
                UpdateItemList();
                PersistInventory();
                return true;
            }

            if (index >= 0)
            {
                items[index].quantity = quantity;
            }
            else
            {
                if (items.Count >= inventorySize)
                    return false;
                items.Add(new ItemEntity(itemName, itemName, itemName, 0, ItemType.Material)
                {
                    quantity = quantity,
                    isRemote = IsRemote
                });
            }

            UpdateItemList();
            PersistInventory();
            return true;
        }

        public void RefreshSlotsFromMemory()
        {
            UpdateItemList();
            NotifyLocalChanged();
        }

        private void NotifyLocalChanged()
        {
            if (!IsRemote)
                ProductionService.NotifyWarehouseChanged();
        }

        private void PersistInventory()
        {
            if (DataUtil.Instance == null)
                return;
            DataUtil.Instance.SaveInventory(Store, items);
        }

        /// <summary>Creates an ownership-specific copy for safe transfer between inventories.</summary>
        private ItemEntity CopyForThisInventory(ItemEntity source)
        {
            return source.CloneOwnership(IsRemote);
        }

        public bool SetItemQuantityByDef(string itemDefId, int quantity, int quality = EconomyConstants.DefaultQuality)
        {
            if (string.IsNullOrWhiteSpace(itemDefId))
                return false;

            quantity = Mathf.Max(0, quantity);
            quality = QualityRules.ClampQuality(quality);
            var index = items.FindIndex(item =>
            {
                if (item == null) return false;
                ItemFactory.NormalizeLegacy(item);
                return string.Equals(ItemFactory.ResolveDefId(item), itemDefId, StringComparison.Ordinal)
                       && item.quality == quality
                       && string.IsNullOrEmpty(item.itemInstanceId);
            });

            if (quantity <= 0)
            {
                if (index < 0)
                    return false;
                items.RemoveAt(index);
                UpdateItemList();
                PersistInventory();
                return true;
            }

            if (index >= 0)
            {
                items[index].quantity = quantity;
            }
            else
            {
                if (items.Count >= inventorySize)
                    return false;
                var created = ItemFactory.FromDef(itemDefId, quantity, quality);
                created.isRemote = IsRemote;
                items.Add(created);
            }

            UpdateItemList();
            PersistInventory();
            return true;
        }

        private void WireFilterDropdowns()
        {
            var dropdowns = GetComponentsInParent<TMP_Dropdown>(true);
            if (dropdowns == null || dropdowns.Length == 0)
                dropdowns = transform.parent != null
                    ? transform.parent.GetComponentsInChildren<TMP_Dropdown>(true)
                    : Array.Empty<TMP_Dropdown>();

            TMP_Dropdown typeDrop = null;
            TMP_Dropdown qualityDrop = null;
            foreach (var d in dropdowns)
            {
                if (d == null) continue;
                if (d.gameObject.name.IndexOf("One", StringComparison.OrdinalIgnoreCase) >= 0)
                    typeDrop = d;
                else if (d.gameObject.name.IndexOf("Two", StringComparison.OrdinalIgnoreCase) >= 0)
                    qualityDrop = d;
            }

            if (typeDrop == null && dropdowns.Length > 0)
                typeDrop = dropdowns[0];
            if (qualityDrop == null && dropdowns.Length > 1)
                qualityDrop = dropdowns[1];

            if (typeDrop != null)
            {
                typeDrop.ClearOptions();
                typeDrop.AddOptions(new List<string> { "All", "Equipment", "Material", "Consumable" });
                typeDrop.SetValueWithoutNotify(0);
                typeDrop.onValueChanged.RemoveAllListeners();
                typeDrop.onValueChanged.AddListener(idx =>
                {
                    typeFilterIndex = idx;
                    UpdateItemList();
                });
            }

            if (qualityDrop != null)
            {
                qualityDrop.ClearOptions();
                qualityDrop.AddOptions(new List<string> { "All Q", "Q1", "Q2", "Q3", "Q4", "Q5" });
                qualityDrop.SetValueWithoutNotify(0);
                qualityDrop.onValueChanged.RemoveAllListeners();
                qualityDrop.onValueChanged.AddListener(idx =>
                {
                    qualityFilterIndex = idx;
                    UpdateItemList();
                });
            }
        }

        private List<ItemEntity> VisibleItems()
        {
            IEnumerable<ItemEntity> query = items.Where(item => item != null);
            if (typeFilterIndex == 1)
                query = query.Where(item => item.itemType == ItemType.Equipment);
            else if (typeFilterIndex == 2)
                query = query.Where(item => item.itemType == ItemType.Material);
            else if (typeFilterIndex == 3)
                query = query.Where(item => item.itemType == ItemType.Food);

            if (qualityFilterIndex >= 1 && qualityFilterIndex <= 5)
                query = query.Where(item => item.quality == qualityFilterIndex);

            return query.ToList();
        }

        private void UpdateItemList()
        {
            var visible = VisibleItems();
            var populatedSlotCount = Mathf.Min(visible.Count, itemSlots.Count);
            for (var i = 0; i < populatedSlotCount; i++)
                itemSlots[i].SetItem(visible[i]);

            for (var i = populatedSlotCount; i < itemSlots.Count; i++)
                itemSlots[i].SetItem(null);
        }
    }

    public enum SortType
    {
        ITEM_NAME,
        ITEM_COST
    }
}
