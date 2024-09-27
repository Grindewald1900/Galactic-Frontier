// using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine.UI;
using UnityEngine;
using System;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public GameObject equipmentMenu;
    public GameObject inventoryMenu;
    public GameObject basePanel;
    public GameObject equipmentTab;
    public GameObject inventoryTab;
    private TabType currentActiveType;
    public ItemSlot[] itemSlots;
    // public ItemSO[] itemSos;

    private void Start()
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

    public enum TabType
    {
        EQUIPMENT, INVENTORY, NONE
    }
}