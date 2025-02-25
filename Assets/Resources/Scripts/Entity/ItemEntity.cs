[System.Serializable]
public class ItemEntity
{
    public ItemType itemType = ItemType.Unknown;
    public string itemName;
    public string itemDescription;
    public string itemIcon;
    public int quantity = 1;
    public int cost = 0;
    public readonly int pickUpRange = 3;

    public ItemEntity(string name, string description, string icon, int cost, ItemType type)
    {
        this.itemName = name;
        this.itemDescription = description;
        this.itemIcon = icon;
        this.cost = cost;
        this.itemType = type;
    }

    public enum ItemType
    {
        Equipment,
        Food,
        Material,
        Unknown
    }
}