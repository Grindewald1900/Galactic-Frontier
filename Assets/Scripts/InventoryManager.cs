/** 
 * This class manages the inventory and equipment menu
 * Focus on UI related stuff rather than item logic
 */

using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public ItemSlot itemSlotPrefab;
    public Transform inventoryGrid;
    public GameObject equipmentMenu;
    public GameObject inventoryMenu;
    public GameObject basePanel;
    public GameObject equipmentTab;
    public GameObject inventoryTab;
    public List<ItemSlot> itemSlots;
    private TabType currentActiveType;
    // public ItemSO[] itemSos;


    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }

    }

    void Start()
    {
        basePanel.SetActive(false);
        currentActiveType = TabType.NONE;
    }

    void ChangeTabAlpha(GameObject menu, float alpha)
    {
        Image image = menu.gameObject.GetComponent<Image>();
        image.color = ColorUtil.ChangeAlpha(image.color, alpha);
    }

    // Trigger tab view with different type (equipment, inventory)
    public void ToggleTab(TabType type)
    {
        if (type == currentActiveType)
        {
            CloseInventoryMenu();
            return;
        }
        // Close menu if its open already 
        Time.timeScale = 0;
        GameObject menu = GetMenu(type);
        GameObject tab = GetTab(type);
        basePanel.SetActive(true);
        menu.SetActive(true);
        currentActiveType = type;
        ChangeTabAlpha(tab, 0.8f);
        foreach (TabType t in TabType.GetValues(typeof(TabType)))
        {
            if (t != type && t != TabType.NONE)
            {
                GetMenu(t).SetActive(false);
                ChangeTabAlpha(GetTab(t), 0.2f);
            }
        }
    }

    private GameObject GetTab(TabType type)
    {
        return type switch
        {
            TabType.EQUIPMENT => equipmentTab,
            TabType.INVENTORY => inventoryTab,
            _ => null,
        };
    }

    private GameObject GetMenu(TabType type)
    {
        return type switch
        {
            TabType.EQUIPMENT => equipmentMenu,
            TabType.INVENTORY => inventoryMenu,
            _ => null,
        };
    }

    public void CloseInventoryMenu()
    {
        Time.timeScale = 1;
        foreach (TabType t in TabType.GetValues(typeof(TabType)))
        {
            if (t != TabType.NONE)
            {
                GetMenu(t).SetActive(false);
            }
            // GetTab(t).SetActive(false);
        }
        currentActiveType = TabType.NONE;
        basePanel.SetActive(false);
    }

    public void UpdateInventory()
    {
        Debug.Log("UpdateInventory: size " + ItemManager.Instance.items.Count + "First item: " + ItemManager.Instance.items[0]);
        for (int i = 0; i < ItemManager.Instance.items.Count; i++)
        {
            itemSlots[i].SetItem(ItemManager.Instance.items[i]);
            Debug.Log("UpdateInventory: " + ItemManager.Instance.items[i].itemName + "Quantity" + ItemManager.Instance.items[i].quantity);
        }
    }

    public void InitInventory()
    {
        for (int i = 0; i < Constants.defaultInventorySlots; i++)
        {
            ItemSlot newItemSlot = Instantiate<ItemSlot>(itemSlotPrefab, inventoryGrid);
            itemSlots.Add(newItemSlot);
        }
    }

    public enum TabType
    {
        EQUIPMENT, INVENTORY, NONE
    }
}