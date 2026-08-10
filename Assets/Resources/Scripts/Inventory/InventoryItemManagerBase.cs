using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.UI;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.Utils.Save;
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

            var index = items.FindIndex(item =>
                item != null && string.Equals(item.itemName, newItem.itemName, StringComparison.Ordinal));

            if (index >= 0)
            {
                Debug.Log(
                    $"Item {newItem.itemName} already exists in the inventory at index {index}. " +
                    $"Adding quantity {newItem.quantity}.");
                items[index].quantity += newItem.quantity;
            }
            else
            {
                if (items.Count >= inventorySize)
                {
                    Debug.Log("Inventory is full.");
                    return false;
                }

                Debug.Log($"Adding {newItem.itemName} to the inventory.");
                items.Add(CopyForThisInventory(newItem));
            }

            UpdateItemList();
            PersistInventory();
            return true;
        }

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

        private void PersistInventory()
        {
            if (DataUtil.Instance == null)
                return;
            DataUtil.Instance.SaveInventory(Store, items);
        }

        /// <summary>Creates an ownership-specific copy for safe transfer between inventories.</summary>
        private ItemEntity CopyForThisInventory(ItemEntity source)
        {
            return new ItemEntity(
                source.itemName,
                source.itemDescription,
                source.itemIcon,
                source.itemCost,
                source.itemType)
            {
                quantity = source.quantity,
                isRemote = IsRemote
            };
        }

        private void UpdateItemList()
        {
            var populatedSlotCount = Mathf.Min(items.Count, itemSlots.Count);
            for (var i = 0; i < populatedSlotCount; i++)
                itemSlots[i].SetItem(items[i]);

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
