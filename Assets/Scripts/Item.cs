using UnityEngine;

[System.Serializable]

public class Item : MonoBehaviour
{
    public Transform player;
    public Sprite itemIcon;
    public string itemName;  // 物品名称
    public int quantity;     // 物品数量
    public int value;        // 物品价值
    public int range;
    public ItemType itemType;  // 物品类型
    private bool isCollected;
    void Start()
    {
        isCollected = false;
        quantity = 1;
        range = 5;
        itemType = ItemType.Unknown;
    }

    void Update()
    {
        CheckCollectivity();
    }

    private void CheckCollectivity()
    {
        if (isCollected)
            return;
        if (player == null)
        {
            player = GameObject.Find("Player").transform;
        }
        float distance = Vector2.Distance(player.position, transform.position);
        if (distance < range)
        {
            Debug.Log($"Collecting item: {itemName}");
            ItemManager.Instance.AddItem(this);
            isCollected = true;
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