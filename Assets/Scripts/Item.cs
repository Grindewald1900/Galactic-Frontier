using UnityEngine;

[System.Serializable]

public class Item : MonoBehaviour
{
    public Transform player;
    public Sprite itemIcon;
    public ItemType itemType;
    public string itemName;
    public int quantity;
    public int value;
    public int pickUpRange;
    private bool isCollected;
    void Start()
    {
        isCollected = false;
        quantity = 1;
        pickUpRange = 5;
        itemType = ItemType.Unknown;
        itemName ??= gameObject.name; // If itemName is null or empty, set it to the GameObject's name
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
        if (distance < pickUpRange)
        {
            if (ItemManager.Instance.AddItem(this))
            {
                isCollected = true;
                // TODO: Should remove item gameobject from scene
                gameObject.SetActive(false);
                // Destroy(gameObject);
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
}