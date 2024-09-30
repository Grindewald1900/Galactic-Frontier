using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ItemManager : MonoBehaviour
{
    public List<Item> inventory = new List<Item>();
    public int inventorySize = 20;

    public static ItemManager Instance;

    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }

        for (int i = 0; i < Constants.defaultInventorySlots; i++)
        {
            inventory.Add(null);
        }
    }

    public void AddItem(Item newItem)
    {
        Debug.Log($"AddItem:" + newItem.itemName);
        Item existingItem = inventory.Find(item => item != null && item.itemName == newItem.itemName);

        if (existingItem != null && existingItem.quantity < Constants.maxItemCountPerSlot)
        {
            existingItem.quantity = Mathf.Min(existingItem.quantity + newItem.quantity, Constants.maxItemCountPerSlot);
            InventoryManager.Instance.UpdateInventoryItem(newItem, inventory.IndexOf(existingItem));
            Debug.Log($"Added {newItem.itemName}.  quantity: {existingItem.quantity}");
        }
        else
        {
            int emptySlotIndex = inventory.FindIndex(item => item == null);
            if (emptySlotIndex != -1)
            {
                inventory[emptySlotIndex] = newItem;
                InventoryManager.Instance.UpdateInventoryItem(newItem, emptySlotIndex);
                Debug.Log($"Picked up {newItem.itemName}, placed in slot {emptySlotIndex}");
            }
            else
            {
                Debug.Log("Inventory is full!");
            }
        }
    }

    public void UseItem(Item item)
    {
        if (item != null && item.quantity > 0)
        {
            item.quantity--;
            Debug.Log($"Used 1 {item.itemName}. Remaining: {item.quantity}");

            if (item.quantity == 0)
            {
                int index = inventory.IndexOf(item);
                inventory[index] = null;
                Debug.Log($"{item.itemName} is removed from the inventory.");
            }
        }
    }

    public void SortItems(SortType sortType)
    {
        switch (sortType)
        {
            case SortType.ByName:
                inventory = inventory.Where(item => item != null).OrderBy(item => item.itemName).ToList();
                break;
            case SortType.ByValue:
                inventory = inventory.Where(item => item != null).OrderByDescending(item => item.value).ToList();
                break;
        }

        while (inventory.Count < inventorySize)
        {
            inventory.Add(null);
        }
        Debug.Log("Inventory sorted.");
    }


    public List<Item> FilterItemsByType(Item.ItemType itemType)
    {
        return inventory.Where(item => item != null && item.itemType == itemType).ToList();
    }
}

public enum SortType
{
    ByName,
    ByValue
}