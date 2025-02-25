using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IPointerClickHandler
{
    public Image selectedIcon;
    public Image bacgroundImage;
    public Image itemImage;
    public Image CountImage;
    public TextMeshProUGUI count;
    public int slotIndex;
    public bool isRemote;

    void Awake()
    {
        SetSelected(false); // Set the selected icon to false by default
        SetCount(0); // Set the count to 0 by default
    }

    public void SetItem(ItemEntity item)
    {
        if (item != null)
        {
            itemImage.sprite = ImageUtil.GetSpriteByName(ImageUtil.itemImagePath, item.itemIcon); // Set the item image to the item's icon
            SetCount(item.quantity); // Set the count to the item's quantity
        }
        else
        {
            itemImage.sprite = null;
            SetCount(0);
        }
    }

    public void SetSelected(bool isSelected)
    {
        selectedIcon.gameObject.SetActive(isSelected); // Show or hide the selected icon based on the isSelected parameter
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (count.text == "") return; // If the count is empty, return
        if (isRemote)
        {
            Debug.Log("Selected Name: " + RemoteItemManager.Instance.GetItems()[slotIndex].itemName);
            Debug.Log("Selected quantity: " + RemoteItemManager.Instance.GetItems()[slotIndex].quantity);
            ItemOperationManager.Instance.SetButtonInteractable(true, true);
            ItemOperationManager.Instance.selectedItem = RemoteItemManager.Instance.GetItems()[slotIndex];
            RemoteItemManager.Instance.SelectItem(slotIndex);
        }
        else
        {
            ItemOperationManager.Instance.SetButtonInteractable(false, true);
            ItemOperationManager.Instance.selectedItem = ItemManager.Instance.GetItems()[slotIndex];
            ItemManager.Instance.SelectItem(slotIndex);
        }
    }

    private void SetCount(int count)
    {
        CountImage.gameObject.SetActive(count != 0); // Hide the count image if the count is 0
        this.count.text = count == 0 ? "" : count.ToString(); // Set the count text to the specified count
    }
}