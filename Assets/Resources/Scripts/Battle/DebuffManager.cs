using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Cards;
using Assets.Resources.Scripts.Props;
using Assets.Resources.Scripts.Utils;

namespace Assets.Resources.Scripts.Battle
{
    public class DebuffManager : MonoBehaviour
    {
        public Image[] debuffImages;
        public Image background;
        public List<DebuffEntity> currentDebuffs = new List<DebuffEntity>();

        void Start()
        {
            foreach (Image image in debuffImages)
            {
                image.gameObject.SetActive(false);
            }
            background.gameObject.SetActive(false);
        }

        public void AddDebuff(DebuffEntity newDebuff, Card card)
        {
            // 检查是否已存在相同类型的 debuff
            Debug.Log("AddDebuff: " + newDebuff.name);
            DebuffEntity debuffEntity = currentDebuffs.Find(d => d.name == newDebuff.name);
            if (debuffEntity != null)
            {
                debuffEntity.roundsRemaining += newDebuff.roundsRemaining;
            }
            else
            {
                debuffEntity = newDebuff;
                currentDebuffs.Add(debuffEntity);
            }

            // e.g. damageType = Bleeding, damage = 20 + 10 = 30, for each round, reduce card's health by 30
            if (debuffEntity.damageType != Status.DamageType.None)
            {
                debuffEntity.damage += newDebuff.damage;
            }
            // e.g. attributeType = health, attribute = 0.1, change card's health by 10%
            if (debuffEntity.attributeType != Status.AttributeType.None)
            {
                debuffEntity.attribute += newDebuff.attribute;
                card.cardEntity.ChangeBattleAttribute(debuffEntity.attributeType, debuffEntity.attribute);
            }
            UpdateDebuffUI();
        }

        public void UpdateDebuffs()
        {
            // 遍历当前 debuff 列表（倒序处理删除）
            for (int i = currentDebuffs.Count - 1; i >= 0; i--)
            {
                currentDebuffs[i].roundsRemaining--;
                if (currentDebuffs[i].roundsRemaining <= 0)
                {
                    currentDebuffs.RemoveAt(i);
                }
            }
            // 更新UI，使剩余的 Debuff 向上填充空白位置
            UpdateDebuffUI();
        }

        private void UpdateDebuffUI()
        {
            Debug.Log("currentDebuffs.Count: " + currentDebuffs.Count);
            background.gameObject.SetActive(currentDebuffs.Count > 0);
            // 清空所有 Image（隐藏）
            for (int i = 0; i < debuffImages.Length; i++)
            {
                debuffImages[i].sprite = null;
                debuffImages[i].gameObject.SetActive(false);
            }
            // 将当前 debuff 列表的图标按顺序填入 UI Image 数组中
            for (int i = 0; i < currentDebuffs.Count && i < debuffImages.Length; i++)
            {
                debuffImages[i].gameObject.SetActive(true);

                debuffImages[i].sprite = ImageUtil.GetSpriteByName(ImageUtil.statusImagePath, currentDebuffs[i].type.ToString());
                if (debuffImages[i].sprite.name.Contains("Default"))
                {
                    debuffImages[i].sprite = ImageUtil.GetSpriteByName(ImageUtil.statusImagePath, "Question");
                }
            }
        }
    }
}