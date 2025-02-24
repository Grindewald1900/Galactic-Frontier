using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PlanetRadarItem : MonoBehaviour
{
    public string planetName; // 星球名称
    public Image roundCorner; // 外层圆角 Image
    public Image planetImage; // 星球 Image
    private Vector3 originalScale; // 原始大小
    private bool isSelected = false; // 选中状态

    void Start()
    {
        originalScale = transform.localScale; // 记录初始大小
        if (roundCorner != null)
        {
            roundCorner.gameObject.SetActive(false); // **默认隐藏 RoundCorner**
        }
    }

    public void InitPlanet(string imageName, string name)
    {
        Sprite sprite = ImageUtil.GetSpriteByName(ImageUtil.planetImagePath, imageName);
        float randomScale = Random.Range(0.6f, 1.4f); // **随机缩放**
        planetImage.sprite = sprite;
        planetImage.transform.localScale = new Vector3(randomScale, randomScale, randomScale);
        planetName = name;
    }

    // **当星球被选中**
    public void Select()
    {
        if (isSelected) return; // **避免重复执行**
        isSelected = true;

        if (roundCorner != null)
        {
            roundCorner.gameObject.SetActive(true); // **显示 RoundCorner**
        }

        // **使用 DOTween 放大**
        transform.DOScale(originalScale * 1.2f, 0.2f).SetEase(Ease.OutBack);
    }

    // **当星球取消选中**
    public void Deselect()
    {
        if (!isSelected) return;
        isSelected = false;

        // **缩小回原始大小**
        transform.DOScale(originalScale, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
        {
            if (roundCorner != null)
            {
                roundCorner.gameObject.SetActive(false); // **动画结束后隐藏 RoundCorner**
            }
        });
    }
}