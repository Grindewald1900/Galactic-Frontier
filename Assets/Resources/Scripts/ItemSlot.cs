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
        // TODO: Find the images for the selected icon and item image if not initialized correctly.
        // Image[] images = GetComponentsInChildren<Image>();
        // if (images.Length >= 3)
        // {
        //     selectedIcon = images[1];
        //     itemImage = images[2];
        // }
        // else
        // {
        //     Debug.Log("Not enough Image components found.");
        // }
    }

    public void SetItem(Item item)
    {
        if (item != null)
        {
            itemImage.sprite = item.itemIcon;
            Debug.Log("item.quantity.ToString() " + item.quantity.ToString());
            count.text = item.quantity.ToString(); // Set the count text to the item's stack size
        }
        else
        {
            itemImage.sprite = null;
            count.text = ""; // Set the count text to an empty string if there is no item
        }
    }
}