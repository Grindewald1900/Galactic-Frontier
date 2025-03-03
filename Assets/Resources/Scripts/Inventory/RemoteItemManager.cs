/**
 * Manages the player's inventory and handles adding and using items.
 * Provides a singleton instance for easy access throughout the game.
 * Focus on item logic e.g. adding, using, removing or filtering items.
 */

using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.UI;
using Assets.Resources.Scripts.Utils;

namespace Assets.Resources.Scripts.Inventory
{
    public class RemoteItemManager : MonoBehaviour
    {
        // List all of items player has
        private List<ItemEntity> items = new();
        private List<ItemEntity> filteredItems = new();
        private List<ItemSlot> itemSlots = new();
        private int selectedSlotIndex = 0;
        public GameObject itemPrefab;
        public Transform gridParent;
        public int inventorySize = 60;
        public static RemoteItemManager Instance;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        void Start()
        {
            //TODO:Fake data for testing
            FakeData();
            InitItemList();
        }

        private void FakeData()
        {
            List<string> itemNames = new() { "Copper", "Steel", "GoldBar", "SteelBar", "Water", "Wood" };
            for (int i = 0; i < 20; i++)
            {
                ItemEntity item = new("Item " + i, "Description " + i, itemNames[Random.Range(0, itemNames.Count)], 10 * i, ItemType.Material)
                {
                    quantity = Random.Range(1, 111),
                    isRemote = true
                };
                items.Add(item);
            }
            DataUtil.Instance.SaveItemData(items);
        }

        private void InitItemList()
        {
            items = DataUtil.Instance.LoadItemData();
            for (int i = 0; i < inventorySize; i++)
            {
                GameObject itemGO = Instantiate(itemPrefab, gridParent);
                itemGO.SetActive(true);
                ItemSlot itemSlot = itemGO.GetComponent<ItemSlot>();
                itemSlot.slotIndex = i;
                itemSlot.isRemote = true;
                itemSlots.Add(itemSlot);
            }
            foreach (int i in Enumerable.Range(0, items.Count))
            {
                itemSlots[i].SetItem(items[i]);
            }
        }

        public bool AddItem(ItemEntity newItem)
        {
            int index = items.FindIndex(item => item.itemName.Equals(newItem.itemName));
            if (index != -1)
            {
                Debug.Log($"Item {newItem.itemName} already exists in the inventory. Index is " + index + ". Adding quantity." + newItem.quantity);
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
                items.Add(newItem);
                UpdateItemList(items);
            }
            return true;
        }

        public void UseItem(ItemEntity itemEntity, int quantity = 1)
        {
            ItemEntity item = items.Find(item => item.itemName.Equals(itemEntity.itemName));
            if (item?.quantity > 0)
            {
                item.quantity -= quantity;
                Debug.Log($"Used 1 {item.itemName}. Remaining: {item.quantity}");

                if (item.quantity <= 0)
                {
                    int index = items.IndexOf(item);
                    items.RemoveAt(index);
                    UpdateItemList(items);
                    Debug.Log($"{item.itemName} is removed from the inventory.");
                }
            }
            UpdateItemList(items);
        }

        public List<ItemEntity> GetItems()
        {
            return items;
        }

        public void SelectItem(int index)
        {
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
                    items = items.Where(item => item != null).OrderBy(item => item.itemName).ToList();
                    break;
                case SortType.ITEM_COST:
                    items = items.Where(item => item != null).OrderByDescending(item => item.itemCost).ToList();
                    break;
            }
            UpdateItemList(items);
            Debug.Log("Inventory sorted.");
        }

        public List<ItemEntity> FilterItemsByType(ItemType itemType)
        {
            return items.Where(item => item != null && item.itemType == itemType).ToList();
        }

        private void UpdateItemList(List<ItemEntity> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                itemSlots[i].SetItem(items[i]);
            }
            for (int i = items.Count; i < inventorySize; i++)
            {
                itemSlots[i].SetItem(null);
            }
        }
    }
}