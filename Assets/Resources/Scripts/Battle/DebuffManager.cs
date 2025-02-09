using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
public class DebuffManager : MonoBehaviour
{
    public Image[] debuffImages;
    public List<DebuffEntity> currentDebuffs = new List<DebuffEntity>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (Image image in debuffImages)
        {
            image.gameObject.SetActive(false);
        }
    }

    public void AddDebuff(DebuffEntity newDebuff)
    {
        // 检查是否已存在相同类型的 debuff
        DebuffEntity existing = currentDebuffs.Find(d => d.type == newDebuff.type);
        if (existing != null)
        {
            existing.roundsRemaining += newDebuff.roundsRemaining;
        }
        else
        {
            currentDebuffs.Add(newDebuff);
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
        Debug.Log("UpdateDebuffUI");
        Debug.Log("currentDebuffs.Count: " + currentDebuffs.Count);
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
                Debug.Log("Replace Default Sprite with Question Sprite");
                debuffImages[i].sprite = ImageUtil.GetSpriteByName(ImageUtil.statusImagePath, "Question");
            }
        }
    }
}