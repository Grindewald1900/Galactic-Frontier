// using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System.Collections.Generic;


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
        // InitInventory();
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

    void Update()
    {
        if (Input.GetButtonDown("Equipment"))
        {
            ToggleTab(TabType.EQUIPMENT);
        }
        if (Input.GetButtonDown("Inventory"))
        {
            ToggleTab(TabType.INVENTORY);
        }
        if (Input.GetButtonDown("Cancel"))
        {
            CloseMenu();
        }
    }

    void ChangeTabAlpha(GameObject menu, float alpha)
    {
        Image image = menu.gameObject.GetComponent<Image>();
        image.color = ColorUtil.ChangeAlpha(image.color, alpha);
    }

    public void ToggleTab(TabType type)
    {
        if (type == currentActiveType)
        {
            CloseMenu();
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

    public void CloseMenu()
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

    // 动态生成itemSlot，并设置它的子元素
    public void UpdateInventoryItem(Item item, int index)
    {
        if (item.itemIcon != null)
        {
            Debug.Log("UpdateInventoryItem Index:" + index);
            itemSlots[index].itemImage.sprite = item.itemIcon;  // 设置物品图标
        }
        Destroy(item.gameObject);
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