using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Utils;
using Assets.Resources.Scripts.Inventory;
using Assets.Scripts.Utils;

namespace Assets.Resources.Scripts.UI
{
    public class SkillItemSlot : MonoBehaviour, IPointerClickHandler
    {
        public Image itemImage;
        public Image backgroundImage;
        public TextMeshProUGUI descriptionText;
        public SkillItemType itemType;
        public int slotIndex;

        void Awake()
        {
            SetSelected(false); // Set the selected icon to false by default
            descriptionText.gameObject.SetActive(false); // Hide the description text by default
        }

        public void SetExpertiseItem(ExpertiseEntity expertise)
        {
            if (expertise != null)
            {
                itemImage.sprite = ImageUtil.GetSpriteByName(ImageUtil.spellImagePath, expertise.attributeType.ToString()); // Set the item image to the item's icon
                backgroundImage.sprite = ImageUtil.GetSpriteByName(ImageUtil.cardBkImagePath, expertise.expertiseTier.ToString() + "_Default"); // Set the background image to the item's icon
            }
            else
            {
                itemImage.sprite = null;
            }
        }

        public void SetSkillItem(SkillEntity skill)
        {
            if (skill == null) return;
            itemType = SkillItemType.SKILL;
            Debug.Log("Setting skill image: " + skill.skillImage + "Tier: " + skill.skillTier);
            itemImage.sprite = ImageUtil.GetSpriteByName(ImageUtil.skillImagePath, skill.skillImage); // Set the item image to the item's icon
            backgroundImage.sprite = ImageUtil.GetSpriteByName(ImageUtil.badgeImagePath, skill.skillTier.ToString()); // Set the background image to the item's icon)
            descriptionText.text = LocalizationUtil.GetLocalizedText(skill.skillDescription); // Set the description text to the item's description
        }

        public void SetItemType(SkillItemType type)
        {
            itemType = type;
        }

        public void SetSelected(bool isSelected)
        {
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (itemType == SkillItemType.EXPERTISE_PREVIEW)
            {

            }
        }
    }

    public enum SkillItemType
    {
        EXPERTISE_PREVIEW,
        SKILL
    }
}