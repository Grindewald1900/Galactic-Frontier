using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Assets.Resources.Scripts.Entity;
using Assets.Resources.Scripts.Utils;
using static Assets.Resources.Scripts.Props.Status;

namespace Assets.Resources.Scripts.UI
{
    public class BattleReportSlot : MonoBehaviour
    {
        public Image itemImage;
        public Image background;
        public Image valueBar;
        public TextMeshProUGUI valueText;

        public void SetItem(BattleReportType type, float progress, float value)
        {
            switch (type)
            {
                case BattleReportType.Damage:
                    itemImage.sprite = ImageUtil.GetSpriteByName(ImageUtil.UIImagePath, "sword");
                    valueBar.color = Color.red;
                    break;
                case BattleReportType.Injury:
                    itemImage.sprite = ImageUtil.GetSpriteByName(ImageUtil.UIImagePath, "blood-drop");
                    valueBar.color = Color.blue;
                    break;
                case BattleReportType.Heal:
                    itemImage.sprite = ImageUtil.GetSpriteByName(ImageUtil.UIImagePath, "healing");
                    valueBar.color = Color.green;
                    break;
                default:
                    itemImage.sprite = null;
                    break;
            }
            valueBar.fillAmount = progress;
            valueText.text = value.ToString();
        }
    }
}