using Assets.Resources.Scripts.Inventory;
using UnityEngine;

namespace Assets.Resources.Scripts.Utils
{
    public class InitUtil : MonoBehaviour
    {
        void Start()
        {
            InventoryManager.Instance.InitInventory();
        }
    }
}