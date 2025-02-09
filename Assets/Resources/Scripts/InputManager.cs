using UnityEngine;

public class InputManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Equipment"))
        {
            InventoryManager.Instance.ToggleTab(InventoryManager.TabType.EQUIPMENT);
        }
        if (Input.GetButtonDown("Inventory"))
        {
            InventoryManager.Instance.ToggleTab(InventoryManager.TabType.INVENTORY);
        }
        if (Input.GetButtonDown("Cancel"))
        {
            InventoryManager.Instance.CloseInventoryMenu();
        }
        if (Input.GetButtonDown("Map"))
        {
            MapManager.Instance.ToggleMap();
        }
    }
}
