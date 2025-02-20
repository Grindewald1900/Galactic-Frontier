using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AchievementController : MonoBehaviour
{
    public GameObject cardPrefab; // 要生成的卡牌预制体
    public int rows = 3;
    public int columns = 5;
    public Vector2 spacing = new Vector2(10f, 10f); // 横向和纵向间隔
    public List<BadgeEntity> badges = new List<BadgeEntity>(); // 所有成就的列表
    public AchievementController instance;

    // 父容器：必须带有 Grid Layout Group 组件，或者你可以手动计算并设置 RectTransform
    public Transform gridContainer;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        //TODO: Fake data
        badges.Add(new BadgeEntity("award", "First Badge"));
        badges.Add(new BadgeEntity("badge", "Second Badge"));
        badges.Add(new BadgeEntity("level-1", "Third Badge"));
        badges.Add(new BadgeEntity("level-5", "Fourth Badge"));
        badges.Add(new BadgeEntity("level-10", "Fifth Badge"));
        badges.Add(new BadgeEntity("level-20", "Sixth Badge"));
        badges.Add(new BadgeEntity("level-50", "Seventh Badge"));
        badges.Add(new BadgeEntity("level-100", "Eighth Badge"));
        badges.Add(new BadgeEntity("level-150", "Ninth Badge"));
        badges.Add(new BadgeEntity("level-200", "Tenth Badge"));
        badges.Add(new BadgeEntity("level-badge", "Ninth Badge"));
        badges.Add(new BadgeEntity("badge", "Tenth Badge"));
    }

    void Start()
    {
        // 生成指定数量的卡牌
        for (int i = 0; i < badges.Count; i++)
        {
            GameObject card = Instantiate(cardPrefab, gridContainer);
            Badge badge = card.GetComponent<Badge>();
            badge.SetBadge(badges[i].badgeName, badges[i].badgeDescription);
        }
    }

    public int GetBadgeAmount()
    {
        return badges.Count;
    }
}