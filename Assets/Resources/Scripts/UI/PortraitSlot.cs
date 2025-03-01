using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Battle;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Utils;

namespace Assets.Resources.Scripts.UI
{
    public class PortraitSlot : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Image frameImage;
        public Image portraitImage;
        public Image tierImage;
        public Image tierImageFrame;
        public CardEntity cardEntity;
        public bool isSelected = false; // Flag to check if the item is selected 
        public int slotIndex;
        public float xPosition; // X position of the portrait slot

        private Transform originalParent;
        private CanvasGroup canvasGroup;
        private bool isDragging = false;
        private bool isDraggable = true;

        void Awake()
        {
            SetSelected(false); // Set the selected icon to false by default
            tierImageFrame.gameObject.SetActive(false);
            canvasGroup = GetComponent<CanvasGroup>(); // **确保 Prefab 上有 CanvasGroup**
        }

        public void SetPortrait(PortraitEntity portrait, CardEntity entity)
        {
            tierImageFrame.gameObject.SetActive(portrait.IsShowFrame()); // Show or hide the tier image frame based on the tier of the item
            if (portrait != null)
            {
                cardEntity = entity;
                frameImage.sprite = ImageUtil.GetSpriteByName(ImageUtil.cardBkImagePath, portrait.portraitFrame); // Set the item image to the item's icon
                portraitImage.sprite = ImageUtil.GetSpriteByName(ImageUtil.badgeImagePath, portrait.portraitName); // Set the item image to the item's icon
                tierImage.sprite = ImageUtil.GetSpriteByName(ImageUtil.badgeImagePath, portrait.characterTier.ToString()); // Set the item image to the item's icon
            }
        }

        public void SetSelected(bool isSelected)
        {
            this.isSelected = isSelected;
            float scale = isSelected ? 1.1f : 1f; // Set the scale to 1.1 when selected
            gameObject.transform.localScale = new Vector3(scale, scale, 1f);
        }

        public void SetDraggable(bool isDraggable)
        {
            this.isDraggable = isDraggable;
        }

        public bool IsDraggable()
        {
            return isDraggable;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isSelected) return;
            LineupManager.Instance.SelectPortrait(slotIndex);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!isSelected) return;
            if (!isDraggable) return;
            Debug.Log("Start Dragging Card");
            isDragging = true;
            originalParent = transform.parent;
            //canvasGroup.blocksRaycasts = false; // **让拖拽中的卡牌不会阻挡射线**
            transform.SetParent(originalParent.parent); // **脱离 LayoutGroup**
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isSelected) return;
            if (!isDraggable) return;
            // Debug.Log("Draging Card pos: " + eventData.position);
            // Debug.Log("Screen size: " + Screen.width + ", " + Screen.height);
            transform.localPosition = TransPosition(eventData.position); // **拖拽跟随鼠标**
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isSelected) return;
            if (!isDraggable) return;
            Debug.Log("Start Dragging End");
            isDragging = false;
            canvasGroup.blocksRaycasts = true; // **重新启用射线检测**
            DropZoneHandler.Instance.DropCard(GetComponent<PortraitSlot>(), transform.localPosition);
        }

        private Vector3 TransPosition(Vector3 pos)
        {
            float xPos = pos.x - (Screen.width / 2);
            float yPos = pos.y - Screen.height / 2 - 176f; // 176 为 UGUI Canvas 的 y 轴偏移量
            return new Vector3(xPos, yPos, pos.z);
        }
    }
}