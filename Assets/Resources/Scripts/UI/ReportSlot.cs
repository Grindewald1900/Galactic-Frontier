using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReportSlot : MonoBehaviour
{
    public Image reportImage;
    public TextMeshProUGUI reportText;

    public void SetReport(string imageName, int count)
    {
        reportImage.sprite = ImageUtil.GetSpriteByName(ImageUtil.badgeImagePath, imageName);
        reportText.text = "x" + count.ToString();
    }
}