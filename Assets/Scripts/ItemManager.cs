using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ItemManager : MonoBehaviour
{
    public List<Item> inventory = new List<Item>();  // 背包，存储所有物品
    public int inventorySize = 20;  // 背包大小

    public static ItemManager Instance;

    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        // 初始化背包，填充空物品栏
        for (int i = 0; i < inventorySize; i++)
        {
            inventory.Add(null);
        }
    }

    // 拾取物品
    public void AddItem(Item newItem)
    {
        // 查找背包中是否已有此物品
        Item existingItem = inventory.Find(item => item != null && item.itemName == newItem.itemName);

        if (existingItem != null && existingItem.quantity < Constants.maxItemCountPerSlot)
        {
            // 如果已有此物品，增加物品数量，注意数量上限

            existingItem.quantity = Mathf.Min(existingItem.quantity + newItem.quantity, Constants.maxItemCountPerSlot);
            Debug.Log($"Added {newItem.itemName}.  quantity: {existingItem.quantity}");
        }
        else
        {
            // 如果背包中没有此物品，查找第一个空闲位置
            int emptySlotIndex = inventory.FindIndex(item => item == null);

            if (emptySlotIndex != -1)
            {
                inventory[emptySlotIndex] = newItem;
                Debug.Log($"Picked up {newItem.itemName}, placed in slot {emptySlotIndex}");
            }
            else
            {
                Debug.Log("Inventory is full!");
            }
        }
    }

    // 点击物品，使用物品
    public void UseItem(Item item)
    {
        if (item != null && item.quantity > 0)
        {
            item.quantity--;
            Debug.Log($"Used 1 {item.itemName}. Remaining: {item.quantity}");

            // 如果物品数量为0，删除物品
            if (item.quantity == 0)
            {
                int index = inventory.IndexOf(item);
                inventory[index] = null;
                Debug.Log($"{item.itemName} is removed from the inventory.");
            }
        }
    }

    // 物品排序功能
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
        // 填充背包中的空位
        while (inventory.Count < inventorySize)
        {
            inventory.Add(null);
        }
        Debug.Log("Inventory sorted.");
    }

    // 物品过滤功能
    public List<Item> FilterItemsByType(Item.ItemType itemType)
    {
        return inventory.Where(item => item != null && item.itemType == itemType).ToList();
    }
}



// 物品排序类型
public enum SortType
{
    ByName,
    ByValue
}