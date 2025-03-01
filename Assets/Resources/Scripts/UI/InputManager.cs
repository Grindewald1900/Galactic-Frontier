using Assets.Resources.Scripts.Inventory;
using UnityEngine;

namespace Assets.Resources.Scripts.UI
{
    public class InputManager : MonoBehaviour
    {
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
            }
        }
    }
}