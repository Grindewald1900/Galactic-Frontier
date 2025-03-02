using UnityEngine;
using System.Collections.Generic;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.UI;

namespace Assets.Resources.Scripts.CharacterPanel
{
    public class SpellController : MonoBehaviour
    {
        public GameObject skillSlotPrefab; // 技能槽预制体
        public Transform gridParent;       // 网格布局容器
        public int totalSlots = 10;        // 总共10个槽
        public int activeSlots = 3;        // 前3个为激活状态
        public SpellController instance;

        // 存储所有技能槽
        public List<SpellSlot> slots = new();

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
        }

        void Start()
        {
            for (int i = 0; i < totalSlots; i++)
            {
                GameObject slotGO = Instantiate(skillSlotPrefab, gridParent);
                SpellSlot slot = slotGO.GetComponent<SpellSlot>();
                slot.SetSpell(new SpellEntity());
                if (slot != null)
                {
                    slot.SetActive(i < activeSlots);
                    // 订阅点击事件
                    if (i < activeSlots)
                    {
                        slot.OnSpellSlotClicked += OnSpellSlotClicked;
                    }
                    slots.Add(slot);
                }
            }
        }

        private void OnSpellSlotClicked(SpellSlot slot)
        {
            Debug.Log("Skill slot clicked: " + slot.name);
            // 在这里调用技能选择面板逻辑，比如：
            // SkillSelectionPanel.Instance.Open(slot);
            // 这里的逻辑视你如何设计技能选择面板而定。
        }

        public void UpdateSpellSlot(SpellSlot slot, SpellEntity spell)
        {
            slot?.SetSpell(spell);
        }
    }
}