using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemSlot : MonoBehaviour
{
    public Image selectedIcon;
    public Image itemImage;
    public TextMeshProUGUI count;

    void Start()
    {
        Image[] images = GetComponentsInChildren<Image>();
        if (images.Length >= 3)
        {
            selectedIcon = images[1];
            itemImage = images[2];
        }
        else
        {
            Debug.Log("Not enough Image components found.");
        }
        count.text = ""; // Set the count text to an empty string initially
    }

    public void SetItem(Item item)
    {
        if (item != null)
        {
            itemImage.sprite = item.itemIcon;
            count.text = item.quantity.ToString(); // Set the count text to the item's stack size
        }
        else
        {
            itemImage.sprite = null;
            count.text = "empty"; // Set the count text to an empty string if there is no item
        }
    }
}