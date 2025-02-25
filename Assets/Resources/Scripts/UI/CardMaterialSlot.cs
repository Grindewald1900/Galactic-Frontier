using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardMaterialSlot : MonoBehaviour
{
    public Image itemImage;
    public TextMeshProUGUI count;
    public ItemEntity item;

    public void SetItem(ItemEntity item)
    {
        if (item != null)
        {
            itemImage.sprite = ImageUtil.GetSpriteByName(ImageUtil.itemImagePath, item.itemIcon);
            count.text = item.quantity.ToString();
        }
        else
        {
            itemImage.sprite = null;
            count.text = "";
        }
        this.item = item;
    }

    public void SetQuantity(int quantity)
    {
        count.text = quantity.ToString();
    }
}