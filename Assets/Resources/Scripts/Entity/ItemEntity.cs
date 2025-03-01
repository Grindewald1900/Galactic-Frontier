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
    }
}

public enum ItemType
{
    Equipment,
    Food,
    Material,
    Unknown
}