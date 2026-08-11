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
            // Disabled leftovers (Shop Scroll View) must never become Instance.
            if (!enabled)
                return;

            // Shop leftover also had ItemManager; never destroy the GameObject (wipes the inventory grid).
            if (Instance != null && Instance != this)
            {
                if (!IsPreferredHost(transform) && IsPreferredHost(Instance.transform))
                {
                    enabled = false;
                    return;
                }

                Instance.enabled = false;
            }

            Instance = this;
        }

        private static bool IsPreferredHost(Transform t)
        {
            while (t != null)
            {
                if (t.name.IndexOf("LocalRepo", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
                t = t.parent;
            }

            return false;
        }
    }
}
