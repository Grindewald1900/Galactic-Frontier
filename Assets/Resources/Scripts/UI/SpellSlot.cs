using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Utils;

namespace Assets.Resources.Scripts.UI
{
    public class SpellSlot : MonoBehaviour, IPointerClickHandler
    {
        public Image icon;
        private readonly string defaultIcon = "Spell-Default";
        public SpellEntity spellEntity;
        public delegate void SpellSlotClicked(SpellSlot slot);
        public event SpellSlotClicked OnSpellSlotClicked;

        public void SetSpell(SpellEntity spell)
        {
            spellEntity = spell;
            SetSpellIcon();
        }

        public void SetSpellIcon()
        {
            if (spellEntity?.isActivated == true)
            {
                icon.sprite = ImageUtil.GetSpriteByName(ImageUtil.spellImagePath, spellEntity.spellName);
            }
            else
            {
                icon.sprite = ImageUtil.GetSpriteByName(ImageUtil.spellImagePath, defaultIcon);
            }
        }

        public void SetActive(bool active)
        {
            spellEntity.isActivated = active;
            SetSpellIcon();
            if (TryGetComponent<Button>(out var btn))
            {
                btn.interactable = active;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // 只有激活状态下才响应点击
            if (spellEntity.isActivated && OnSpellSlotClicked != null)
            {
                OnSpellSlotClicked(this);
            }
        }
    }
}