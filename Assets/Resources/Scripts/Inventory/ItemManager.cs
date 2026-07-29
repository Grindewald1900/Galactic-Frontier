using UnityEngine;

namespace Assets.Resources.Scripts.Inventory
{
    /// <summary>
    /// Manages the player's local inventory.
    /// </summary>
    public sealed class ItemManager : InventoryItemManagerBase
    {
        public static ItemManager Instance;

        protected override bool IsRemote => false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }
    }
}
