using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Props;
using Assets.Resources.Scripts.Utils;

namespace Assets.Resources.Scripts.Battle
{
    public class BuffManager : MonoBehaviour
    {
        public Image[] buffImages;
        public Image background;
        public List<BuffEntity> currentBuffs = new List<BuffEntity>();

        void Start()
        {
            foreach (Image image in buffImages)
            {
                image.gameObject.SetActive(false);
            }
            background.gameObject.SetActive(false);
        }

        public void AddBuff(BuffEntity newBuff)
        {
            // 检查是否已存在相同类型的 buff
            BuffEntity existing = currentBuffs.Find(b => b.name == newBuff.name);
            if (existing != null)
            {
                existing.roundsRemaining += newBuff.roundsRemaining;
            }
            else
            {
                existing = newBuff;
                currentBuffs.Add(existing);
            }
            if (existing.healType != Status.HealType.None)
            {
                existing.heal += newBuff.heal;
            }
            if (existing.attributeType != Status.AttributeType.None)
            {
                existing.attribute += newBuff.attribute;
            }
            UpdateBuffUI();
        }

        public void UpdateBuffs()
        {
            // 遍历当前 buff 列表（倒序处理删除）
            for (int i = currentBuffs.Count - 1; i >= 0; i--)
            {
                currentBuffs[i].roundsRemaining--;
                if (currentBuffs[i].roundsRemaining <= 0)
                {
                    currentBuffs.RemoveAt(i);
                }
            }
            UpdateBuffUI();
        }

        private void UpdateBuffUI()
        {
            Debug.Log("UpdateBuffUI");
            Debug.Log("currentBuffs.Count: " + currentBuffs.Count);
            background.gameObject.SetActive(currentBuffs.Count > 0);
            for (int i = 0; i < buffImages.Length; i++)
            {
                buffImages[i].sprite = null;
                buffImages[i].gameObject.SetActive(false);
            }
            for (int i = 0; i < currentBuffs.Count && i < buffImages.Length; i++)
            {
                buffImages[i].gameObject.SetActive(true);
                buffImages[i].sprite = currentBuffs[i].icon;
                if (buffImages[i].sprite.name.Contains("Default"))
                {
                    Debug.Log("Replace Default Sprite with Question Sprite");
                    buffImages[i].sprite = ImageUtil.GetSpriteByName(ImageUtil.statusImagePath, "Question");
                }
            }
        }
    }
}