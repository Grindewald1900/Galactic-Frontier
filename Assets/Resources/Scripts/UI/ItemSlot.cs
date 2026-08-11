using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.Inventory;

namespace Assets.Resources.Scripts.UI
{
    public class ItemSlot : MonoBehaviour, IPointerClickHandler
    {
        public Image selectedIcon;
        public Image bacgroundImage;
        public Image itemImage;
        public Image CountImage;
        public TextMeshProUGUI count;
        public int slotIndex;
        public bool isRemote;
        private ItemEntity boundItem;

        void Awake()
        {
            SetSelected(false); // Set the selected icon to false by default
            SetCount(0); // Set the count to 0 by default
        }

        public void SetItem(ItemEntity item)
        {
            boundItem = item;
            if (item != null)
            {
                var icon = string.IsNullOrEmpty(item.itemIcon) ? "Steel" : item.itemIcon;
                itemImage.sprite = ImageUtil.GetSpriteByName(ImageUtil.itemImagePath, icon);
                var q = item.quality > 0 ? item.quality : 2;
                SetCount(item.quantity);
                if (count != null)
                    count.text = item.quantity <= 0 ? "" : $"Q{q}×{item.quantity}";
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
            if (boundItem == null || boundItem.quantity <= 0) return;
            if (isRemote)
            {
                ItemOperationManager.Instance.SetButtonInteractable(true, true);
                ItemOperationManager.Instance.selectedItem = boundItem;
                RemoteItemManager.Instance.SelectItem(slotIndex);
            }
            else
            {
                ItemOperationManager.Instance.SetButtonInteractable(false, true);
                ItemOperationManager.Instance.selectedItem = boundItem;
                ItemManager.Instance.SelectItem(slotIndex);
            }
        }

        private void SetCount(int count)
        {
            CountImage.gameObject.SetActive(count != 0); // Hide the count image if the count is 0
            this.count.text = count == 0 ? "" : count.ToString(); // Set the count text to the specified count
        }
    }
}