using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using Assets.Resources.Scripts.Entity;

namespace Assets.Resources.Scripts.UI
{
    public class HoverShowDetailPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public static HoverShowDetailPanel Instance;
        public GameObject detailPanel; // 需要显示/隐藏的编辑按钮
        public TextMeshProUGUI cardType;
        public TextMeshProUGUI level;
        public TextMeshProUGUI exp;
        public TextMeshProUGUI power;
        public TextMeshProUGUI score;
        public TextMeshProUGUI tier;
        public TextMeshProUGUI health;
        public TextMeshProUGUI attack;
        public TextMeshProUGUI defense;
        public TextMeshProUGUI accuracy;
        public TextMeshProUGUI dodge;
        public TextMeshProUGUI critical;
        public TextMeshProUGUI criticalDamage;
        public TextMeshProUGUI damageReduction;
        public TextMeshProUGUI energyGenerate;
        public TextMeshProUGUI speed;

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            detailPanel?.SetActive(false);
        }

        public void SetDetailPanel(CardEntity cardEntity)
        {
            cardType.text = "Type: " + cardEntity.cardType.ToString();
            level.text = "Level: " + cardEntity.Level.ToString();
            exp.text = "Exp: " + cardEntity.CurrentExp.ToString() + "/" + cardEntity.expToLevelUp.ToString();
            power.text = "Power: " + cardEntity.power.ToString();
            score.text = "Score: " + cardEntity.score.ToString();
            tier.text = "Tier: " + cardEntity.characterTier.ToString();
            health.text = "Health: " + cardEntity.health.ToString();
            attack.text = "Attack: " + cardEntity.attack.ToString();
            defense.text = "Defense: " + cardEntity.defense.ToString();
            accuracy.text = "Accuracy: " + cardEntity.accuracy.ToString();
            dodge.text = "Dodge: " + cardEntity.dodge.ToString();
            critical.text = "Crit Rate: " + cardEntity.critical.ToString();
            criticalDamage.text = "Crit Damage: " + cardEntity.criticalDamage.ToString();
            damageReduction.text = "Damage reduction: " + cardEntity.dagameReduction.ToString();
            energyGenerate.text = "Energy Generate: " + cardEntity.energyGenerateRate.ToString();
            speed.text = "Speed: " + cardEntity.speed.ToString();
        }

        public void ShowCardDetailPanel()
        {
            detailPanel?.SetActive(true);
        }

        public void HideCardDetailPanel()
        {
            detailPanel?.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ShowCardDetailPanel();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideCardDetailPanel();
        }
    }
}