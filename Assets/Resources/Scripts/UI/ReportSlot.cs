using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Resources.Scripts.Utils;

namespace Assets.Resources.Scripts.UI
{
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
}