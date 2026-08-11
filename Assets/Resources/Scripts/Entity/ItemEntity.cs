namespace Assets.Resources.Scripts.Entity
{
    [System.Serializable]
    public class ItemEntity
    {
        public ItemType itemType = ItemType.Unknown;
        public string itemName;
        public string itemDescription;
        public string itemIcon;
        public int quantity = 1;
        public int itemCost = 0;
        public bool isRemote = false;
        public readonly int pickUpRange = 3;

        /// <summary>Catalog id (P3). Empty for legacy name-only rows until migrated.</summary>
        public string itemDefId = "";
        /// <summary>Quality tier 1–5.</summary>
        public int quality = 2;
        /// <summary>Unique instance id for non-stacking equipment.</summary>
        public string itemInstanceId = "";
        public int durability;
        public int maxDurability;
        /// <summary>Card id this equipment is equipped to (MVP).</summary>
        public string equippedToCardId = "";

        public ItemEntity(string name, string description, string icon, int cost, ItemType type)
        {
            itemName = name;
            itemDescription = description;
            itemIcon = icon;
            itemCost = cost;
            itemType = type;
        }

        public ItemEntity SetQuantity(int quantity)
        {
            this.quantity = quantity;
            return this;
        }

        public ItemEntity CloneOwnership(bool remote)
        {
            return new ItemEntity(itemName, itemDescription, itemIcon, itemCost, itemType)
            {
                quantity = quantity,
                isRemote = remote,
                itemDefId = itemDefId,
                quality = quality,
                itemInstanceId = itemInstanceId,
                durability = durability,
                maxDurability = maxDurability,
                equippedToCardId = equippedToCardId
            };
        }
    }
}

public enum ItemType
{
    Equipment,
    Food,
    Material,
    Unknown
}
