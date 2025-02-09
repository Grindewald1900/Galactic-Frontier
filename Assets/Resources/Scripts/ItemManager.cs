/**
 * Manages the player's inventory and handles adding and using items.
 * Provides a singleton instance for easy access throughout the game.
 * Focus on item logic e.g. adding, using, removing or filtering items.
 */

using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ItemManager : MonoBehaviour
{
    // List all of items player has
    public List<Item> items = new List<Item>();
    public int inventorySize = 20;
    public static ItemManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public bool AddItem(Item newItem)
    {
        int index = items.FindIndex(item => item.itemName.Equals(newItem.itemName));
        if (index != -1)
        {
            Debug.Log($"Item {newItem.itemName} already exists in the inventory. Index is " + index);
            items[index].quantity += newItem.quantity;
        }
        else
        {
            Debug.Log($"Item {newItem.itemName} does not exist in the inventory.");
            items.Add(newItem);
        }
        InventoryManager.Instance.UpdateInventory();
        return true;
    }

    public void UseItem(Item item)
    {
        if (item != null && item.quantity > 0)
        {
            item.quantity--;
            Debug.Log($"Used 1 {item.itemName}. Remaining: {item.quantity}");

            if (item.quantity == 0)
            {
                int index = items.IndexOf(item);
                items[index] = null;
                Debug.Log($"{item.itemName} is removed from the inventory.");
            }
        }
    }

    public void SortItems(SortType sortType)
    {
        switch (sortType)
        {
            case SortType.ByName:
                items = items.Where(item => item != null).OrderBy(item => item.itemName).ToList();
                break;
            case SortType.ByValue:
                items = items.Where(item => item != null).OrderByDescending(item => item.value).ToList();
                break;
        }

        while (items.Count < inventorySize)
        {
            items.Add(null);
        }
        Debug.Log("Inventory sorted.");
    }

    public List<Item> FilterItemsByType(Item.ItemType itemType)
    {
        return items.Where(item => item != null && item.itemType == itemType).ToList();
    }
}

public enum SortType
{
    ByName,
    ByValue
}