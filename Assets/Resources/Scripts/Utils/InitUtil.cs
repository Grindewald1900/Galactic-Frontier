using UnityEngine;

public class InitUtil : MonoBehaviour
{
    void Start()
    {
        InventoryManager.Instance.InitInventory();
    }
}