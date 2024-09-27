using UnityEngine;

[System.Serializable]

public class Item : MonoBehaviour
{
    public Transform player;
    public string itemName;  // 物品名称
    public int quantity;     // 物品数量
    public int value;        // 物品价值
    public int range;
    public ItemType itemType;  // 物品类型
                               // 物品类别

    void Start()
    {
        quantity = 1;
        range = 5;
        itemType = ItemType.Unknown;
    }

    private void CheckCollectivity()
    {
        if (player == null)
        {
            player = GameObject.Find("Player").transform;
        }
        float distance = Vector2.Distance(player.position, transform.position);
        if (distance < range)
        {
            ItemManager.Instance.AddItem(this);
            Debug.Log($"Collecting item: {itemName}");
        }
    }
    public enum ItemType
    {
        Equipment,
        Food,
        Material,
        Unknown
    }
}