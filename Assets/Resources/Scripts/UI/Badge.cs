using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Assets.Resources.Scripts.Utils;

namespace Assets.Resources.Scripts.UI
{
    public class Badge : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string badgeName = "";
        public string displayText = "";
        public TextMeshProUGUI textComponent;
        public Image imageComponent;

        void Start()
        {
            if (textComponent != null)
            {
                textComponent.gameObject.SetActive(false);
            }
        }

        public void SetBadge(string badgeName, string displayText)
        {
            this.badgeName = badgeName;
            this.displayText = displayText;
            SetImageSprite();
        }

        private void SetImageSprite()
        {
            Sprite sprite = ImageUtil.GetSpriteByName(ImageUtil.badgeImagePath, badgeName);
            if (LogUtil.CheckNull(sprite, "Badge sprite")) return;
            if (imageComponent != null)
            {
                imageComponent.sprite = sprite;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (textComponent != null)
            {
                textComponent.text = displayText;
                textComponent.gameObject.SetActive(true);
            }
        }

        // 当鼠标指针离开该UI元素区域时调用
        public void OnPointerExit(PointerEventData eventData)
        {
            if (textComponent != null)
            {
                textComponent.gameObject.SetActive(false);
            }
        }
    }
}