using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SpellSlot : MonoBehaviour, IPointerClickHandler
{
    public Image icon;             // 用于显示技能图标的 Image
    private string defaultIcon = "Spell-Default";     // 默认图标
    public SpellEntity spellEntity;
    public delegate void SpellSlotClicked(SpellSlot slot);
    public event SpellSlotClicked OnSpellSlotClicked;

    void Start()
    {
        // if (icon != null)
        // {
        //     Debug.Log("Set spell icon: default on start");
        //     icon.sprite = ImageUtil.GetSpriteByName(ImageUtil.spellImagePath, defaultIcon);
        // }
    }

    public void SetSpell(SpellEntity spell)
    {
        spellEntity = spell;
        SetSpellIcon();
    }

    public void SetSpellIcon()
    {
        if (spellEntity != null && spellEntity.isActivated == true)
        {
            Debug.Log("Set spell icon: " + spellEntity.spellName);
            icon.sprite = ImageUtil.GetSpriteByName(ImageUtil.spellImagePath, spellEntity.spellName);
        }
        else
        {
            Debug.Log("Set spell icon: default");
            icon.sprite = ImageUtil.GetSpriteByName(ImageUtil.spellImagePath, defaultIcon);
        }
    }

    public void SetActive(bool active)
    {
        spellEntity.isActivated = active;
        SetSpellIcon();
        // 如果挂有 Button 组件，可以设置 interactable 属性
        Button btn = GetComponent<Button>();
        if (btn != null)
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