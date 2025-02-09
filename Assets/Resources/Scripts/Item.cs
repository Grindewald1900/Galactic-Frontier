using UnityEngine;

[System.Serializable]

public class Item : MonoBehaviour
{
    public Transform player;
    public Sprite itemIcon;
    public ItemType itemType = ItemType.Unknown;
    public AudioClip collectSound;
    public string itemName;
    public int quantity = 1;
    public int value;
    public readonly int pickUpRange = 3;
    private bool isCollected = false;
    void Start()
    {
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
                SoundManager.Instance.PlaySound(SoundManager.SoundType.SOUND_EFFECT, collectSound);
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