using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public GameObject equipmentMenu;
    public GameObject inventoryMenu;
    public GameObject tabView;
    private bool inventoryOpen = false;
    private bool equipmentOpen = false;

    private bool menuActivated = false;
    public ItemSlot[] itemSlots;
    // public ItemSO[] itemSos;

    private void Start()
    {
        equipmentMenu.SetActive(false);
        inventoryMenu.SetActive(false);
    }

    void Update()
    {
        if (Input.GetButtonDown("Equipment"))
        {
            ToggleEquipment();
        }
        if (Input.GetButtonDown("Inventory"))
        {
            ToggleInventory();
        }
        tabView.SetActive(menuActivated);
    }

    public void ToggleEquipment()
    {
        // Close menu if its open already 
        Time.timeScale = menuActivated ? 1 : 0;
        equipmentMenu.SetActive(!equipmentOpen);
        equipmentOpen = !equipmentOpen;
        if (inventoryOpen)
        {
            inventoryMenu.SetActive(false);
            inventoryOpen = false;
        }
        menuActivated = equipmentOpen || inventoryOpen;

    }

    public void ToggleInventory()
    {
        // Close menu if its open already 

        Time.timeScale = menuActivated ? 1 : 0;
        inventoryMenu.SetActive(!inventoryOpen);
        inventoryOpen = !inventoryOpen;
        if (equipmentOpen)
        {
            equipmentMenu.SetActive(false);
            equipmentOpen = false;
        }
        menuActivated = equipmentOpen || inventoryOpen;

    }
}