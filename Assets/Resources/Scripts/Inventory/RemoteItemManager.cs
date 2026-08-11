using UnityEngine;

namespace Assets.Resources.Scripts.Inventory
{
    /// <summary>
    /// Manages inventory stored at a remote location.
    /// </summary>
    public sealed class RemoteItemManager : InventoryItemManagerBase
    {
        public static RemoteItemManager Instance;

        protected override bool IsRemote => true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                enabled = false;
                return;
            }

            Instance = this;
        }
    }
}
